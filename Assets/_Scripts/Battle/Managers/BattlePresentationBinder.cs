using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 이벤트를 연출(애니메이션)로 변환하는 바인더.
/// 전투 시작 시 유닛별 HP 변화를 구독해서 피격/사망 연출을 재생하고,
/// 전투 종료 시 승리 팀의 승리 연출을 재생한다.
/// 판정 로직은 없으며 전투 코어의 이벤트에 반응만 한다.
/// </summary>
public class BattlePresentationBinder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleManager _battleManager;
    [SerializeField] private BattleCycleController _battleCycleController;
    [SerializeField] private SkillVfxPlayer _skillVfxPlayer;

    /// <summary>
    /// 유닛별 연출 바인딩 정보
    /// </summary>
    private class UnitBinding
    {
        public BattleUnit Unit;
        public IBattlePresenter Presenter;
        public DeathDissolve Dissolve;
        public CharacterAudio Audio;
        public BattleTeamType TeamType;
        public int LastHp;
        public System.Action HpHandler;
        public bool IsReactionPlaying;
    }

    private readonly List<UnitBinding> _bindings = new List<UnitBinding>();

    private void Awake()
    {
        if (_battleManager == null)
        {
            _battleManager = FindFirstObjectByType<BattleManager>();
        }

        if (_battleCycleController == null)
        {
            _battleCycleController = FindFirstObjectByType<BattleCycleController>();
        }

        if (_skillVfxPlayer == null)
        {
            _skillVfxPlayer = GetComponent<SkillVfxPlayer>();
        }
    }

    private void OnEnable()
    {
        if (_battleCycleController == null)
        {
            return;
        }

        _battleCycleController.OnBattleStarted += HandleBattleStarted;
        _battleCycleController.OnTurnStarted += HandleTurnStarted;
        _battleCycleController.OnBattleEnded += HandleBattleEnded;
        _battleCycleController.OnStatusApplied += HandleStatusApplied;
        _battleCycleController.OnStatusRemoved += HandleStatusRemoved;
    }

    private void OnDisable()
    {
        if (_battleCycleController == null)
        {
            return;
        }

        _battleCycleController.OnBattleStarted -= HandleBattleStarted;
        _battleCycleController.OnTurnStarted -= HandleTurnStarted;
        _battleCycleController.OnBattleEnded -= HandleBattleEnded;
        _battleCycleController.OnStatusApplied -= HandleStatusApplied;
        _battleCycleController.OnStatusRemoved -= HandleStatusRemoved;

        ClearBindings();
    }

    /// <summary>
    /// 전투 시작: 스폰된 유닛들의 HP 변화 구독
    /// </summary>
    private void HandleBattleStarted()
    {
        BindUnits();
    }

    /// <summary>
    /// 턴 시작: 바인딩이 비어있으면 재시도 (스폰 타이밍 보정)
    /// </summary>
    private void HandleTurnStarted(BattleUnit unit, int turnIndex)
    {
        if (_bindings.Count == 0)
        {
            BindUnits();
        }
    }

    /// <summary>
    /// 행동 실행 이벤트 처리
    /// </summary>
    /// <param name="actionRequest">실행 행동 요청</param>
    public void HandleActionExecuting(BattleActionRequest actionRequest)
    {
        PlayAction(
            actionRequest,
            null,
            null);
    }

    /// <summary>
    /// 행동 연출 재생
    /// </summary>
    /// <param name="actionRequest">실행 행동 요청</param>
    /// <param name="onImpact">스킬 명중 콜백</param>
    /// <param name="onComplete">전체 연출 완료 콜백</param>
    public void PlayAction(
        BattleActionRequest actionRequest,
        System.Action onImpact,
        System.Action onComplete)
    {
        if (actionRequest == null)
        {
            onImpact?.Invoke();
            onComplete?.Invoke();
            return;
        }

        UnitBinding binding = FindBinding(actionRequest.Actor);

        if (binding == null || binding.Presenter == null)
        {
            onImpact?.Invoke();
            onComplete?.Invoke();
            return;
        }

        bool hasVfx =
            actionRequest.HasSkill &&
            _skillVfxPlayer != null;

        int remainingCount =
            hasVfx
                ? 2
                : 1;

        bool isCompleted = false;

        void HandlePartCompleted()
        {
            remainingCount--;

            if (remainingCount > 0 || isCompleted)
            {
                return;
            }

            isCompleted = true;
            onComplete?.Invoke();
        }

        PlayActorPresentation(
            binding,
            actionRequest,
            onImpact,
            HandlePartCompleted);

        if (hasVfx)
        {
            Transform casterTransform =
                GetActorTransform(actionRequest.Actor);

            IReadOnlyList<Transform> targets =
                GatherTargetTransforms(actionRequest);

            _skillVfxPlayer.Play(
                actionRequest.SkillData,
                casterTransform,
                targets,
                onImpact,
                HandlePartCompleted);
        }
        else if (actionRequest.CommandType != CommandType.Attack)
        {
            onImpact?.Invoke();
        }
    }

    /// <summary>
    /// 행동자 애니메이션 재생
    /// </summary>
    /// <param name="binding">행동자 바인딩</param>
    /// <param name="actionRequest">실행 행동 요청</param>
    /// <param name="onComplete">애니메이션 완료 콜백</param>
    private void PlayActorPresentation(
        UnitBinding binding,
        BattleActionRequest actionRequest,
        System.Action onImpact,
        System.Action onComplete)
    {
        switch (actionRequest.CommandType)
        {
            case CommandType.Attack:
                binding.Presenter.PlayAttack(
                    onImpact: onImpact,
                    onComplete: onComplete);

                binding.Audio?.PlayAttack();
                break;

            case CommandType.Skill:
                if (IsSupportSkill(actionRequest.SkillData))
                {
                    binding.Presenter.PlaySkillSupport(
                        onComplete);
                }
                else
                {
                    binding.Presenter.PlaySkill(
                        onComplete);
                }

                binding.Audio?.PlaySkill();
                break;

            case CommandType.Defense:
                binding.Presenter.PlayParry(
                    onComplete);

                binding.Audio?.PlayParry();
                break;

            default:
                onComplete?.Invoke();
                break;
        }
    }

    private readonly List<Transform> _targetBuffer = new List<Transform>();

    /// <summary>
    /// 스킬의 대상 Transform 목록을 모은다.
    /// 광역(AllEnemies/AllAllies)이면 해당 팀 생존 유닛 전체, 아니면 단일 대상.
    /// </summary>
    private IReadOnlyList<Transform> GatherTargetTransforms(BattleActionRequest actionRequest)
    {
        _targetBuffer.Clear();

        SkillData skill = actionRequest.SkillData;
        bool isAllTargets = skill != null
            && (skill.TargetType == TargetType.AllEnemies || skill.TargetType == TargetType.AllAllies);

        if (isAllTargets
            && _battleManager != null
            && _battleManager.SpawnedActors != null
            && _battleManager.TryGetActor(actionRequest.Actor, out BattleActor casterActor)
            && casterActor != null)
        {
            bool wantEnemies = skill.TargetType == TargetType.AllEnemies;
            BattleTeamType casterTeam = casterActor.TeamType;

            for (int i = 0; i < _battleManager.SpawnedActors.Count; i++)
            {
                BattleActor actor = _battleManager.SpawnedActors[i];

                if (actor == null || actor.HasBattleUnit == false)
                {
                    continue;
                }

                if (actor.BattleUnit != null && actor.BattleUnit.IsAlive == false)
                {
                    continue;
                }

                bool isEnemy = actor.TeamType != casterTeam;

                if (wantEnemies == isEnemy)
                {
                    _targetBuffer.Add(actor.transform);
                }
            }

            return _targetBuffer;
        }

        Transform single = GetActorTransform(actionRequest.Target);
        if (single != null)
        {
            _targetBuffer.Add(single);
        }

        return _targetBuffer;
    }

    /// <summary>대상이 아군/자신이면 지원형 스킬로 본다(힐·버프). 적 대상이면 공격형.</summary>
    private static bool IsSupportSkill(SkillData skill)
    {
        if (skill == null)
        {
            return false;
        }

        TargetType target = skill.TargetType;
        return target == TargetType.SingleAlly
            || target == TargetType.AllAllies
            || target == TargetType.Self;
    }

    /// <summary>BattleUnit에 해당하는 액터의 Transform을 BattleManager에서 조회한다(적/아군 공통).</summary>
    private Transform GetActorTransform(BattleUnit unit)
    {
        if (unit == null || _battleManager == null)
        {
            return null;
        }

        if (_battleManager.TryGetActor(unit, out BattleActor actor) && actor != null)
        {
            return actor.transform;
        }

        return null;
    }

    /// <summary>상태이상 부여 시 대상의 StatusEffectView에 표시.</summary>
    private void HandleStatusApplied(BattleUnit unit, StatusEffectType type)
    {
        StatusEffectView view = GetStatusView(unit);
        if (view != null)
        {
            view.ShowStatus(type);
        }
    }

    /// <summary>상태이상 해제/만료 시 대상의 StatusEffectView에서 제거.</summary>
    private void HandleStatusRemoved(BattleUnit unit, StatusEffectType type)
    {
        StatusEffectView view = GetStatusView(unit);
        if (view != null)
        {
            view.HideStatus(type);
        }
    }

    /// <summary>BattleUnit의 액터에서 StatusEffectView 조회(적/아군 공통, 프레젠터 없어도 됨).</summary>
    private StatusEffectView GetStatusView(BattleUnit unit)
    {
        if (unit == null || _battleManager == null)
        {
            return null;
        }

        if (_battleManager.TryGetActor(unit, out BattleActor actor) && actor != null)
        {
            return actor.GetComponentInChildren<StatusEffectView>();
        }

        return null;
    }

    /// <summary>
    /// 전투 종료: 현재 승리 연출은 사용하지 않는다 (승리 시 유닛은 Idle 유지).
    /// </summary>
    /// <param name="winner">승리 팀</param>
    private void HandleBattleEnded(BattleTeamType winner)
    {
    }

    /// <summary>
    /// 스폰된 액터들의 유닛-프레젠터 바인딩 생성
    /// </summary>
    private void BindUnits()
    {
        ClearBindings();

        if (_battleManager == null || _battleManager.SpawnedActors == null)
        {
            return;
        }

        for (int i = 0; i < _battleManager.SpawnedActors.Count; i++)
        {
            BattleActor actor = _battleManager.SpawnedActors[i];

            if (actor == null || actor.HasBattleUnit == false)
            {
                continue;
            }

            IBattlePresenter presenter = actor.GetComponent<IBattlePresenter>();

            if (presenter == null)
            {
                continue;
            }

            UnitBinding binding = new UnitBinding
            {
                Unit = actor.BattleUnit,
                Presenter = presenter,
                Dissolve = actor.GetComponent<DeathDissolve>(),
                Audio = actor.GetComponent<CharacterAudio>(),
                TeamType = actor.TeamType,
                LastHp = actor.BattleUnit.CurrentHp
            };

            binding.HpHandler = () => HandleHpChanged(binding);
            binding.Unit.OnHpChanged += binding.HpHandler;

            presenter.ResetToIdle();

            _bindings.Add(binding);
        }
    }

    /// <summary>
    /// HP 변화에 따른 피격·사망 연출 재생
    /// </summary>
    /// <param name="binding">대상 바인딩</param>
    private void HandleHpChanged(UnitBinding binding)
    {
        if (binding.Unit == null || binding.Presenter == null)
        {
            return;
        }

        int currentHp = binding.Unit.CurrentHp;

        if (currentHp < binding.LastHp)
        {
            binding.IsReactionPlaying = true;

            void HandleReactionCompleted()
            {
                binding.IsReactionPlaying = false;
            }

            if (binding.Unit.IsAlive == false)
            {
                binding.Presenter.PlayDeath(
                    HandleReactionCompleted);

                binding.Audio?.PlayDeath();
                binding.Dissolve?.Play();
            }
            else
            {
                binding.Presenter.PlayHit(
                    HandleReactionCompleted);

                binding.Audio?.PlayHit();
            }
        }

        binding.LastHp = currentHp;
    }

    /// <summary>
    /// 대상 피격 연출 진행 여부 반환
    /// </summary>
    /// <param name="unit">확인 유닛</param>
    /// <returns>피격 연출 진행 여부</returns>
    public bool IsReactionPlaying(BattleUnit unit)
    {
        UnitBinding binding = FindBinding(unit);

        return binding != null &&
            binding.IsReactionPlaying;
    }

    /// <summary>
    /// 대상 목록의 피격 연출 진행 여부 반환
    /// </summary>
    /// <param name="units">확인 유닛 목록</param>
    /// <returns>피격 연출 진행 여부</returns>
    public bool IsAnyReactionPlaying(
        IReadOnlyList<BattleUnit> units)
    {
        if (units == null)
        {
            return false;
        }

        for (int i = 0; i < units.Count; i++)
        {
            if (IsReactionPlaying(units[i]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 유닛 기준 바인딩 검색
    /// </summary>
    /// <param name="unit">검색 유닛</param>
    /// <returns>바인딩 (없으면 null)</returns>
    private UnitBinding FindBinding(BattleUnit unit)
    {
        if (unit == null)
        {
            return null;
        }

        for (int i = 0; i < _bindings.Count; i++)
        {
            if (_bindings[i].Unit == unit)
            {
                return _bindings[i];
            }
        }

        return null;
    }

    /// <summary>
    /// 바인딩 및 구독 해제
    /// </summary>
    private void ClearBindings()
    {
        for (int i = 0; i < _bindings.Count; i++)
        {
            UnitBinding binding = _bindings[i];

            if (binding.Unit != null && binding.HpHandler != null)
            {
                binding.Unit.OnHpChanged -= binding.HpHandler;
            }

            binding.IsReactionPlaying = false;
        }

        _bindings.Clear();
    }
}
