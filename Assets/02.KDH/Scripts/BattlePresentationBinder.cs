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
        public BattleUnitPresenter Presenter;
        public DeathDissolve Dissolve;
        public CharacterAudio Audio;
        public BattleTeamType TeamType;
        public int LastHp;
        public System.Action HpHandler;
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
        _battleCycleController.OnActionExecuting += HandleActionExecuting;
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
        _battleCycleController.OnActionExecuting -= HandleActionExecuting;

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
    /// 행동 실행: 행동자 연출 재생.
    /// 기현님이 BattleCycleController에 OnActionExecuting 이벤트를 추가하면 연결된다.
    /// </summary>
    /// <param name="actionRequest">실행 행동 요청</param>
    public void HandleActionExecuting(BattleActionRequest actionRequest)
    {
        if (actionRequest == null)
        {
            return;
        }

        UnitBinding binding = FindBinding(actionRequest.Actor);

        if (binding == null || binding.Presenter == null)
        {
            return;
        }

        switch (actionRequest.CommandType)
        {
            case CommandType.Attack:
                binding.Presenter.PlayAttack();
                binding.Audio?.PlayAttack();
                break;

            case CommandType.Skill:
                if (IsSupportSkill(actionRequest.SkillData))
                {
                    binding.Presenter.PlaySkillSupport();
                }
                else
                {
                    binding.Presenter.PlaySkill();
                }
                binding.Audio?.PlaySkill();
                break;

            case CommandType.Defense:
                binding.Presenter.PlayParry();
                binding.Audio?.PlayParry();
                break;
        }

        // 스킬(또는 스킬 데이터를 가진 공격) VFX 재생: SkillData의 시전/투사체/명중 프리팹.
        // 대상이 적일 수 있어 위치는 프레젠터가 아니라 BattleManager의 액터에서 가져온다(적엔 프레젠터가 없음).
        if (actionRequest.HasSkill && _skillVfxPlayer != null)
        {
            Transform casterTransform = GetActorTransform(actionRequest.Actor);
            Transform targetTransform = GetActorTransform(actionRequest.Target);

            _skillVfxPlayer.Play(actionRequest.SkillData, casterTransform, targetTransform);
        }
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

            BattleUnitPresenter presenter = actor.GetComponent<BattleUnitPresenter>();

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
    /// HP 변화 감지: 감소면 피격, 0이면 사망 연출
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
            if (binding.Unit.IsAlive == false)
            {
                binding.Presenter.PlayDeath();
                binding.Audio?.PlayDeath();
                binding.Dissolve?.Play();
            }
            else
            {
                binding.Presenter.PlayHit();
                binding.Audio?.PlayHit();
            }
        }

        binding.LastHp = currentHp;
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
        }

        _bindings.Clear();
    }
}
