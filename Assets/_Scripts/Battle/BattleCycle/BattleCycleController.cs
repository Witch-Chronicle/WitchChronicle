using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Battle.Rules;

/// <summary>
/// 전투 턴 사이클 관리
/// </summary>
public class BattleCycleController : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool _autoPlay = true;
    [SerializeField] private float _actionDelay = 0.5f;
    [Tooltip("행동 연출 종료 후 기본 카메라 복귀 전 유지 시간")]
    [SerializeField] private float _actionEndHoldDuration = 0.1f;

    [Header("Impact Timing")]
    [Tooltip("행동 연출(OnActionExecuting) 후 실제 데미지가 적용되기까지 지연(초). 스킬 이펙트가 대상에 닿는 시점에 맞춘다")]
    [SerializeField] private float _impactDelay = 0.3f;

    [Header("Battle End Timing")]
    [Tooltip("승패가 결정된 시점부터 Result 패널(OnBattleEnded)이 뜨기까지 대기하는 시간(초). " +
             "마지막 타격의 쓰러지는 연출/데미지 팝업/HP바 트윈이 다 보일 시간을 확보하기 위함.")]
    [SerializeField] private float _battleEndDelay = 1.5f;

    [Header("Constellation")]
    [SerializeField] private BattleCameraDirector _battleCameraDirector;
    [SerializeField] private ConstellationPathBattleManager _constellationPathBattleManager;
    [SerializeField] private BattlePresentationBinder _battlePresentationBinder;
    [SerializeField] private BattleActionBanner _battleActionBanner;
    [Tooltip("적 행동 배너 표시 후 대상 카메라로 넘어가기 전 유지 시간")]
    [SerializeField] private float _enemyActionBannerHoldDuration = 0.2f;

    private readonly List<BattleUnit> _battleUnits = new List<BattleUnit>();

    private readonly List<BattleUnit> _turnOrder = new List<BattleUnit>();

    private readonly List<BattleUnit> _skillTargets = new List<BattleUnit>();

    private readonly EnemyBattleAI _enemyBattleAI = new EnemyBattleAI();

    private Coroutine _battleRoutine;
    private BattleState _battleState = BattleState.None;
    private BattleTurnContext _currentTurnContext;

    private int _roundCount;
    private int _currentTurnOrderIndex = -1;

    private BattleActionRequest _pendingActionRequest;

    [Header("Status Effect (상태이상 적용/알림용)")]
    [SerializeField] private StatusEffectDatabase _statusEffectDatabase;
    private readonly StatusEffectController _statusEffectController = new StatusEffectController();
    private BattleItemExecutor _itemExecutor;

    public event Action OnBattleStarted;
    public event Action<int> OnRoundStarted;
    public event Action<BattleUnit, int> OnTurnStarted;
    public event Action<BattleUnit> OnTurnEnded;
    public event Action<BattleTeamType> OnBattleEnded;
    public event Action OnTurnOrderChanged;
    public event Action<BattleActionRequest> OnActionExecuting;

    public event Action<BattleActionRequest, ConstellationPathResult> OnConstellationResolved;

    // 연출용: 상태이상 부여/해제 알림 (StatusEffectController에서 중계)
    public event Action<BattleUnit, StatusEffectType> OnStatusApplied;
    public event Action<BattleUnit, StatusEffectType> OnStatusRemoved;

    public BattleState BattleState => _battleState;
    public BattleTurnContext CurrentTurnContext => _currentTurnContext;

    /// <summary>
    /// 전투 카메라와 별자리 매니저 참조 자동 연결
    /// </summary>
    private void Awake()
    {
        if (_battleCameraDirector == null)
        {
            _battleCameraDirector =
                FindFirstObjectByType<BattleCameraDirector>();
        }

        if (_constellationPathBattleManager == null)
        {
            _constellationPathBattleManager =
                FindFirstObjectByType<ConstellationPathBattleManager>();
        }

        if (_battlePresentationBinder == null)
        {
            _battlePresentationBinder =
                FindFirstObjectByType<BattlePresentationBinder>();
        }

        if (_battleActionBanner == null)
        {
            _battleActionBanner =
                FindFirstObjectByType<BattleActionBanner>();
        }

        // 상태이상 부여/해제를 연출 이벤트로 중계
        _statusEffectController.OnApplied += HandleStatusApplied;
        _statusEffectController.OnRemoved += HandleStatusRemoved;

        // 아이템(포션) 실행부 — 배틀의 실제 상태이상 컨트롤러를 공유
        _itemExecutor = new BattleItemExecutor(_statusEffectController);
    }

    private void HandleStatusApplied(BattleUnit unit, StatusEffectType type)
    {
        OnStatusApplied?.Invoke(unit, type);
    }

    private void HandleStatusRemoved(BattleUnit unit, StatusEffectType type)
    {
        OnStatusRemoved?.Invoke(unit, type);
    }

    /// <summary>
    /// 스킬의 상태이상 확률을 판정해 대상에게 부여한다.
    /// 데이터베이스 미설정 시 조용히 무시(기존 전투에 영향 없음).
    /// </summary>
    private void TryApplyStatusEffect(BattleUnit target, SkillData skillData)
    {
        if (target == null || skillData == null)
        {
            return;
        }

        if (skillData.StatusEffectType == StatusEffectType.None || skillData.StatusChance <= 0f)
        {
            return;
        }

        if (_statusEffectDatabase == null)
        {
            return;
        }

        if (UnityEngine.Random.value > skillData.StatusChance)
        {
            return;
        }

        StatusEffectData data = _statusEffectDatabase.GetData(skillData.StatusEffectType);

        if (data == null)
        {
            return;
        }

        _statusEffectController.ApplyStatusEffect(target, data);
    }

    /// <summary>
    /// 전투 시작
    /// </summary>
    /// <param name="battleUnits">전투 참여 유닛 목록</param>
    public void StartBattle(
        IEnumerable<BattleUnit> battleUnits)
    {
        if (battleUnits == null)
        {
            Debug.LogError(
                "전투 유닛 목록이 비어 있습니다.");

            return;
        }

        StopBattle();

        // 이전 전투의 상태이상 잔류 제거 (연전 대비)
        _statusEffectController.ClearAll();

        _battleUnits.Clear();

        _battleUnits.AddRange(
            battleUnits.Where(unit => unit != null));

        if (_battleUnits.Count == 0)
        {
            Debug.LogError(
                "전투에 참여할 유닛이 없습니다.");

            return;
        }

        _roundCount = 0;
        _currentTurnOrderIndex = -1;

        _battleRoutine =
            StartCoroutine(BattleLoop());
    }

    /// <summary>
    /// 전투 중단
    /// </summary>
    public void StopBattle()
    {
        if (_battleRoutine != null)
        {
            StopCoroutine(_battleRoutine);
        }

        _battleRoutine = null;
        if (_constellationPathBattleManager != null)
        {
            _constellationPathBattleManager.StopConstellationPath();
        }

        _battleState = BattleState.None;
        _currentTurnContext = null;
        _pendingActionRequest = null;
        _currentTurnOrderIndex = -1;

        _turnOrder.Clear();
        _skillTargets.Clear();

        OnTurnOrderChanged?.Invoke();
    }

    /// <summary>
    /// 전투 루프
    /// </summary>
    private IEnumerator BattleLoop()
    {
        _battleState = BattleState.Starting;

        OnBattleStarted?.Invoke();

        yield return null;

        BattleTeamType winner = default;

        if (TryGetAdvantageTeam(
                out BattleTeamType advantageTeam))
        {
            yield return RunAdvantagePhase(
                advantageTeam);

            if (TryGetWinner(out winner))
            {
                yield return StartCoroutine(
                    EndBattleAfterDelay(winner));

                yield break;
            }
        }

        while (TryGetWinner(out winner) == false)
        {
            _roundCount++;
            _battleState = BattleState.RoundStart;

            BuildTurnOrder();

            OnRoundStarted?.Invoke(_roundCount);

            Debug.Log(
                $"[Battle] Round {_roundCount} Start");

            for (int i = 0; i < _turnOrder.Count; i++)
            {
                BattleUnit currentUnit =
                    _turnOrder[i];

                if (currentUnit == null ||
                    currentUnit.IsAlive == false)
                {
                    continue;
                }

                _currentTurnOrderIndex = i;

                OnTurnOrderChanged?.Invoke();

                yield return RunUnitTurn(
                    currentUnit);

                if (TryGetWinner(out winner))
                {
                    yield return StartCoroutine(
                        EndBattleAfterDelay(winner));

                    yield break;
                }
            }

            _currentTurnOrderIndex = -1;

            OnTurnOrderChanged?.Invoke();

            yield return null;
        }

        yield return StartCoroutine(
            EndBattleAfterDelay(winner));
    }

    /// <summary>
    /// 선공 진영 선제 행동 진행
    /// </summary>
    /// <param name="advantageTeam">선공 진영</param>
    private IEnumerator RunAdvantagePhase(
        BattleTeamType advantageTeam)
    {
        BuildAdvantageTurnOrder(
            advantageTeam);

        if (_turnOrder.Count == 0)
        {
            yield break;
        }

        Debug.Log(
            $"[Battle] Advantage Phase Start / " +
            $"Team: {advantageTeam}");

        for (int i = 0; i < _turnOrder.Count; i++)
        {
            BattleUnit currentUnit =
                _turnOrder[i];

            if (currentUnit == null ||
                currentUnit.IsAlive == false)
            {
                continue;
            }

            _currentTurnOrderIndex = i;

            OnTurnOrderChanged?.Invoke();

            yield return RunUnitTurn(
                currentUnit,
                false);

            if (TryGetWinner(out _))
            {
                break;
            }
        }

        _currentTurnOrderIndex = -1;

        OnTurnOrderChanged?.Invoke();

        Debug.Log(
            $"[Battle] Advantage Phase End / " +
            $"Team: {advantageTeam}");
    }

    /// <summary>
    /// 선공 진영 반환
    /// </summary>
    /// <param name="advantageTeam">선공 진영</param>
    /// <returns>선공 존재 여부</returns>
    private bool TryGetAdvantageTeam(
        out BattleTeamType advantageTeam)
    {
        advantageTeam = default;

        BattleEncounterContext encounterContext =
            BattleEncounterContext.Instance;

        if (encounterContext == null ||
            encounterContext.HasEncounter == false)
        {
            return false;
        }

        if (encounterContext.IsPlayerAdvantage ==
            encounterContext.IsEnemyAdvantage)
        {
            return false;
        }

        advantageTeam =
            encounterContext.IsPlayerAdvantage
                ? BattleTeamType.Player
                : BattleTeamType.Enemy;

        return true;
    }

    /// <summary>
    /// 선공 진영 턴 순서 생성
    /// </summary>
    /// <param name="advantageTeam">선공 진영</param>
    private void BuildAdvantageTurnOrder(
        BattleTeamType advantageTeam)
    {
        _turnOrder.Clear();

        _turnOrder.AddRange(
            _battleUnits
                .Where(unit =>
                    unit != null &&
                    unit.IsAlive &&
                    unit.TeamType == advantageTeam)
                .OrderByDescending(unit =>
                    unit.Speed));

        _currentTurnOrderIndex = -1;

        Debug.Log(
            "[Battle] Advantage Turn Order: " +
            string.Join(
                " → ",
                _turnOrder.Select(unit =>
                    unit.UnitName)));

        OnTurnOrderChanged?.Invoke();
    }

    /// <summary>
    /// 승패가 확정된 시점부터 _battleEndDelay만큼 대기한 뒤 EndBattle 실행.
    /// 마지막 타격의 연출(데미지 팝업, 피격/사망 애니메이션 등)이 다 보일 시간을 확보하기 위함.
    /// </summary>
    private IEnumerator EndBattleAfterDelay(BattleTeamType winner)
    {
        if (_battleEndDelay > 0f)
        {
            yield return new WaitForSeconds(_battleEndDelay);
        }

        EndBattle(winner);
    }

    /// <summary>
    /// 유닛 턴 진행
    /// </summary>
    /// <param name="unit">턴 유닛</param>
    private IEnumerator RunUnitTurn(
        BattleUnit unit,
        bool processStatusEffects = true)
    {
        _battleState = BattleState.TurnStart;

        int actionCount =
            GetActionCountForTurn(unit);

        _currentTurnContext =
            new BattleTurnContext(
                unit,
                actionCount);

        OnTurnStarted?.Invoke(
            unit,
            actionCount);

        Debug.Log(
            $"[Battle] {unit.UnitName} Turn Start / " +
            $"Actions: {actionCount}");

        // 정상 라운드 턴 시작 상태이상 처리
        if (processStatusEffects)
        {
            _statusEffectController.ProcessTurnStart(unit);
        }

        while (_currentTurnContext.CanAct && unit.IsAlive)
        {
            _battleState =
                BattleState.ExecutingAction;

            // 상태이상 행동 판정: 수면(항상 불가)·마비(확률 불가)
            if (_statusEffectController.CanAct(unit) == false)
            {
                Debug.Log(
                    $"[Battle] {unit.UnitName} 행동불가(상태이상)");

                _currentTurnContext.ConsumeAction();

                if (_actionDelay > 0f)
                {
                    yield return new WaitForSeconds(_actionDelay);
                }

                continue;
            }

            if (_autoPlay ||
                unit.TeamType == BattleTeamType.Enemy)
            {
                BattleActionRequest actionRequest =
                    CreateAutoActionRequest(unit);

                yield return ExecuteActionRequest(
                    actionRequest);

                _currentTurnContext.ConsumeAction();
            }
            else
            {
                yield return WaitForPlayerAction(unit);
            }

            if (TryGetWinner(out _))
            {
                break;
            }

            if (_actionDelay > 0f)
            {
                yield return new WaitForSeconds(
                    _actionDelay);
            }
        }

        _battleState = BattleState.TurnEnd;

        // 정상 라운드 턴 종료 상태이상 처리
        if (processStatusEffects)
        {
            _statusEffectController.ProcessTurnEnd(unit);
        }

        OnTurnEnded?.Invoke(unit);

        Debug.Log(
            $"[Battle] {unit.UnitName} Turn End");

        yield return null;
    }

    /// <summary>
    /// 턴 순서 생성
    /// </summary>
    private void BuildTurnOrder()
    {
        _turnOrder.Clear();

        _turnOrder.AddRange(
            _battleUnits
                .Where(unit =>
                    unit != null &&
                    unit.IsAlive)
                .OrderByDescending(unit =>
                    unit.Speed));

        _currentTurnOrderIndex = -1;

        Debug.Log(
            "[Battle] Turn Order: " +
            string.Join(
                " → ",
                _turnOrder.Select(unit =>
                    unit.UnitName)));

        OnTurnOrderChanged?.Invoke();
    }

    /// <summary>
    /// 턴 행동 횟수 반환
    /// </summary>
    /// <param name="unit">대상 유닛</param>
    /// <returns>행동 가능 횟수</returns>
    private int GetActionCountForTurn(
        BattleUnit unit)
    {
        if (unit == null ||
            unit.IsAlive == false)
        {
            return 0;
        }

        // TODO: 가속 버프, 행동 추가 주문, 다음 턴 2회 행동
        return 1;
    }

    /// <summary>
    /// 첫 번째 생존 상대 검색
    /// </summary>
    /// <param name="attacker">공격 유닛</param>
    /// <returns>첫 번째 생존 상대</returns>
    private BattleUnit FindFirstAliveOpponent(
        BattleUnit attacker)
    {
        if (attacker == null)
        {
            return null;
        }

        return _battleUnits.FirstOrDefault(
            unit =>
                unit != null &&
                unit.IsAlive &&
                unit.TeamType != attacker.TeamType);
    }

    /// <summary>
    /// 첫 번째 생존 상대 반환
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    /// <param name="target">검색 대상</param>
    /// <returns>검색 성공 여부</returns>
    public bool TryGetFirstAliveOpponent(
        BattleUnit actor,
        out BattleUnit target)
    {
        target =
            FindFirstAliveOpponent(actor);

        return target != null;
    }

    /// <summary>침묵 등으로 스킬을 사용할 수 있는지(UI 스킬 버튼 잠금 판단용).</summary>
    public bool CanUseSkill(BattleUnit unit)
    {
        return _statusEffectController.CanUseSkill(unit);
    }

    /// <summary>포션 사용(HP/MP 회복·상태이상 해제). 배틀의 실제 상태 컨트롤러를 공유한다. UI에서 호출.</summary>
    public BattleItemResult UsePotion(BattleUnit user, PotionItemData potion)
    {
        return _itemExecutor.UsePotion(user, potion);
    }

    /// <summary>
    /// 기본 공격 피해 계산
    /// </summary>
    /// <param name="attacker">공격 유닛</param>
    /// <param name="target">방어 유닛</param>
    /// <returns>피해량</returns>
    private int CalculateBasicAttackDamage(
        BattleUnit attacker,
        BattleUnit target)
    {
        if (attacker == null ||
            target == null)
        {
            return 0;
        }

        float rawDamage =
            attacker.AttackPower -
            target.DefensePower * 0.5f;

        return Mathf.Max(
            1,
            Mathf.RoundToInt(rawDamage));
    }

    /// <summary>
    /// 승리 팀 확인
    /// </summary>
    /// <param name="winner">승리 팀</param>
    /// <returns>승리 여부</returns>
    private bool TryGetWinner(
        out BattleTeamType winner)
    {
        bool hasAlivePlayer =
            _battleUnits.Any(unit =>
                unit != null &&
                unit.IsAlive &&
                unit.TeamType ==
                BattleTeamType.Player);

        bool hasAliveEnemy =
            _battleUnits.Any(unit =>
                unit != null &&
                unit.IsAlive &&
                unit.TeamType ==
                BattleTeamType.Enemy);

        if (hasAlivePlayer &&
            hasAliveEnemy)
        {
            winner = default;
            return false;
        }

        winner = hasAlivePlayer
            ? BattleTeamType.Player
            : BattleTeamType.Enemy;

        return true;
    }

    /// <summary>
    /// 전투 종료
    /// </summary>
    /// <param name="winner">승리 팀</param>
    private void EndBattle(
        BattleTeamType winner)
    {
        if (_constellationPathBattleManager != null)
        {
            _constellationPathBattleManager.StopConstellationPath();
        }

        _battleState = BattleState.BattleEnd;
        _battleRoutine = null;
        _currentTurnContext = null;
        _pendingActionRequest = null;
        _currentTurnOrderIndex = -1;

        _skillTargets.Clear();

        OnTurnOrderChanged?.Invoke();

        Debug.Log(
            $"[Battle] Battle End / Winner: {winner}");

        OnBattleEnded?.Invoke(winner);
    }

    /// <summary>
    /// 전투 행동 요청 등록
    /// </summary>
    /// <param name="actionRequest">실행 행동 요청</param>
    public void SubmitAction(
        BattleActionRequest actionRequest)
    {
        if (actionRequest == null)
        {
            return;
        }

        if (_currentTurnContext == null)
        {
            Debug.LogWarning(
                "현재 진행 중인 턴이 없어 " +
                "행동 요청을 받을 수 없습니다.");

            return;
        }

        if (_currentTurnContext.Unit !=
            actionRequest.Actor)
        {
            Debug.LogWarning(
                "현재 턴 유닛과 " +
                "행동 요청 유닛이 다릅니다.");

            return;
        }

        _pendingActionRequest =
            actionRequest;
    }

    /// <summary>
    /// 자동 행동 요청 생성
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    /// <returns>자동 행동 요청</returns>
    private BattleActionRequest CreateAutoActionRequest(
        BattleUnit actor)
    {
        if (actor == null)
        {
            return null;
        }

        if (actor.TeamType ==
            BattleTeamType.Enemy)
        {
            return _enemyBattleAI.CreateActionRequest(
                actor,
                _battleUnits);
        }

        BattleUnit target =
            FindFirstAliveOpponent(actor);

        if (target == null)
        {
            return null;
        }

        return BattleActionRequest.CreateAttack(
            actor,
            target);
    }

    /// <summary>
    /// 플레이어 행동 요청 대기
    /// </summary>
    /// <param name="unit">현재 턴 플레이어 유닛</param>
    private IEnumerator WaitForPlayerAction(
        BattleUnit unit)
    {
        _pendingActionRequest = null;

        Debug.Log(
            $"[Battle] {unit.UnitName} 행동 입력 대기");

        while (_pendingActionRequest == null)
        {
            yield return null;
        }

        yield return ExecuteActionRequest(
            _pendingActionRequest);

        _currentTurnContext.ConsumeAction();
        _pendingActionRequest = null;
    }

    /// <summary>
    /// 판정과 행동 연출 순차 실행
    /// </summary>
    /// <param name="actionRequest">실행 행동 요청</param>
    /// <param name="executeAction">행동 판정 함수</param>
    private IEnumerator ExecuteSimplePresentedAction(
        BattleActionRequest actionRequest,
        Action executeAction)
    {
        bool isPresentationCompleted = false;

        PlayActionPresentation(
            actionRequest,
            null,
            () => isPresentationCompleted = true);

        executeAction?.Invoke();

        yield return WaitForActionPresentation(
            () => isPresentationCompleted);
    }

    /// <summary>
    /// 행동 종료 카메라 복귀
    /// </summary>
    private void RestoreDefaultBattleCamera()
    {
        if (_battleCameraDirector == null ||
            _battleCameraDirector.isActiveAndEnabled == false)
        {
            return;
        }

        _battleCameraDirector.PlayDefaultBattleView();
    }

    /// <summary>
    /// 전투 행동 요청 실행
    /// </summary>
    /// <param name="actionRequest">실행 행동 요청</param>
    private IEnumerator ExecuteActionRequest(
        BattleActionRequest actionRequest)
    {
        if (actionRequest == null)
        {
            yield break;
        }

        switch (actionRequest.CommandType)
        {
            case CommandType.Attack:
                yield return ExecutePresentedAttack(
                    actionRequest);
                break;

            case CommandType.Skill:
                yield return ExecuteSkill(
                    actionRequest);

                break;

            case CommandType.Defense:
                yield return ExecuteSimplePresentedAction(
                    actionRequest,
                    () => ExecuteDefense(actionRequest));
                break;

            case CommandType.Item:
                yield return ExecuteSimplePresentedAction(
                    actionRequest,
                    () => ExecuteUsingItem(actionRequest));
                break;

            case CommandType.Escape:
                yield return ExecuteSimplePresentedAction(
                    actionRequest,
                    () => ExecuteEscape(actionRequest));
                break;

            default:
                Debug.LogWarning(
                    $"처리되지 않은 커맨드 타입: " +
                    $"{actionRequest.CommandType}");

                break;
        }

        if (TryGetWinner(out _) == false)
        {
            if (_actionEndHoldDuration > 0f)
            {
                yield return new WaitForSeconds(
                    _actionEndHoldDuration);
            }

            if (actionRequest.Actor != null &&
                actionRequest.Actor.TeamType == BattleTeamType.Player)
            {
                RestoreDefaultBattleCamera();
            }
        }

        OnTurnOrderChanged?.Invoke();

        yield return null;
    }

    /// <summary>
    /// 기본 공격을 연출 → 타격 시점 → 데미지 → 연출 완료 순으로 실행
    /// </summary>
    /// <param name="actionRequest">공격 행동 요청</param>
    private IEnumerator ExecutePresentedAttack(
        BattleActionRequest actionRequest)
    {
        yield return PlayEnemyActionCamera(actionRequest);

        bool isImpactReached = false;
        bool isPresentationCompleted = false;

        PlayActionPresentation(
            actionRequest,
            () => isImpactReached = true,
            () => isPresentationCompleted = true);

        yield return WaitForActionImpact(
            () => isImpactReached);

        // 혼란: 일정 확률로 공격이 빗나가 데미지가 들어가지 않음(연출은 그대로 재생)
        if (_statusEffectController.RollConfusionMiss(
                actionRequest.Actor))
        {
            Debug.Log(
                $"[Battle] {actionRequest.Actor?.UnitName} 혼란: 공격이 빗나감(MISS)");

            actionRequest.Target?.NotifyMiss();

            yield return WaitForActionPresentation(
                () => isPresentationCompleted);

            yield break;
        }

        ExecuteAttack(actionRequest);

        yield return WaitForActionPresentation(
            () => isPresentationCompleted);

        yield return WaitForTargetReaction(
            actionRequest.Target);
    }

    /// <summary>
    /// 적 공격 사전 카메라 연출
    /// </summary>
    /// <param name="actionRequest">실행 행동 요청</param>
    /// <param name="targets">해결된 대상 목록</param>
    /// <param name="showTargetView">대상 피격 구도 표시 여부</param>
    private IEnumerator PlayEnemyActionCamera(
        BattleActionRequest actionRequest,
        IReadOnlyList<BattleUnit> targets = null,
        bool showTargetView = true)
    {
        if (actionRequest == null ||
            actionRequest.Actor == null ||
            actionRequest.Actor.TeamType != BattleTeamType.Enemy ||
            _battleCameraDirector == null ||
            _battleCameraDirector.isActiveAndEnabled == false)
        {
            yield break;
        }

        bool isDamageAction =
            actionRequest.CommandType == CommandType.Attack ||
            (actionRequest.CommandType == CommandType.Skill &&
             actionRequest.SkillData != null &&
             actionRequest.SkillData.SkillType == SkillEffectType.Damage);

        if (isDamageAction == false)
        {
            yield break;
        }

        _battleActionBanner?.Show(
            actionRequest);

        bool isActorViewCompleted = false;

        _battleCameraDirector.PlaySingleTargetOverviewCut(
            actionRequest.Actor,
            () => isActorViewCompleted = true);

        while (isActorViewCompleted == false)
        {
            if (_battleState == BattleState.BattleEnd)
            {
                _battleActionBanner?.HideImmediate();
                yield break;
            }

            yield return null;
        }

        if (_enemyActionBannerHoldDuration > 0f)
        {
            yield return new WaitForSeconds(
                _enemyActionBannerHoldDuration);
        }

        _battleActionBanner?.Hide();

        if (showTargetView == false)
        {
            yield break;
        }

        BattleUnit cameraTarget =
            actionRequest.Target;

        if (cameraTarget == null &&
            targets != null)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null)
                {
                    continue;
                }

                cameraTarget = targets[i];
                break;
            }
        }

        if (cameraTarget == null)
        {
            yield break;
        }

        bool isTargetViewCompleted = false;

        bool isGroupTarget =
            actionRequest.SkillData != null &&
            (actionRequest.SkillData.TargetType == TargetType.AllEnemies ||
             actionRequest.SkillData.TargetType == TargetType.AllAllies);

        if (isGroupTarget)
        {
            _battleCameraDirector.PlayGroupTargetOverviewCut(
                actionRequest.Actor,
                cameraTarget.TeamType,
                () => isTargetViewCompleted = true);
        }
        else if (cameraTarget.TeamType == BattleTeamType.Player)
        {
            _battleCameraDirector.PlayPlayerBackViewCut(
                cameraTarget,
                () => isTargetViewCompleted = true);
        }
        else
        {
            _battleCameraDirector.PlaySingleTargetOverviewCut(
                cameraTarget,
                () => isTargetViewCompleted = true);
        }

        while (isTargetViewCompleted == false)
        {
            if (_battleState == BattleState.BattleEnd)
            {
                yield break;
            }

            yield return null;
        }
    }

    /// <summary>
    /// 행동 연출 재생
    /// </summary>
    /// <param name="actionRequest">실행 행동 요청</param>
    /// <param name="onImpact">명중 콜백</param>
    /// <param name="onComplete">연출 완료 콜백</param>
    private void PlayActionPresentation(
        BattleActionRequest actionRequest,
        Action onImpact = null,
        Action onComplete = null)
    {
        OnActionExecuting?.Invoke(
            actionRequest);

        if (_battlePresentationBinder != null &&
            _battlePresentationBinder.isActiveAndEnabled)
        {
            _battlePresentationBinder.PlayAction(
                actionRequest,
                onImpact,
                onComplete);

            return;
        }

        onImpact?.Invoke();
        onComplete?.Invoke();
    }

    /// <summary>
    /// 행동 연출 완료 대기
    /// </summary>
    /// <param name="isCompleted">연출 완료 확인 함수</param>
    private IEnumerator WaitForActionPresentation(
        Func<bool> isCompleted)
    {
        if (isCompleted == null)
        {
            yield break;
        }

        while (isCompleted() == false)
        {
            if (_battleState == BattleState.BattleEnd)
            {
                yield break;
            }

            yield return null;
        }
    }

    /// <summary>
    /// 단일 대상 피격 연출 완료 대기
    /// </summary>
    /// <param name="target">대상 유닛</param>
    private IEnumerator WaitForTargetReaction(
        BattleUnit target)
    {
        if (_battlePresentationBinder == null ||
            target == null)
        {
            yield break;
        }

        while (_battlePresentationBinder
            .IsReactionPlaying(target))
        {
            if (_battleState == BattleState.BattleEnd)
            {
                yield break;
            }

            yield return null;
        }
    }

    /// <summary>
    /// 다중 대상 피격 연출 완료 대기
    /// </summary>
    /// <param name="targets">대상 유닛 목록</param>
    private IEnumerator WaitForTargetReactions(
        IReadOnlyList<BattleUnit> targets)
    {
        if (_battlePresentationBinder == null ||
            targets == null)
        {
            yield break;
        }

        while (_battlePresentationBinder
            .IsAnyReactionPlaying(targets))
        {
            if (_battleState == BattleState.BattleEnd)
            {
                yield break;
            }

            yield return null;
        }
    }

    /// <summary>
    /// 행동 명중 시점 대기
    /// </summary>
    /// <param name="isImpactReached">명중 여부 확인 함수</param>
    private IEnumerator WaitForActionImpact(
        Func<bool> isImpactReached)
    {
        if (isImpactReached == null)
        {
            yield break;
        }

        while (isImpactReached() == false)
        {
            if (_battleState == BattleState.BattleEnd)
            {
                yield break;
            }

            yield return null;
        }
    }

    /// <summary>
    /// 연출 후 데미지 적용까지의 임팩트 딜레이 대기.
    /// 스킬 SO에 개별 ImpactDelay(>0)가 있으면 그 값, 없으면 전역 _impactDelay 사용.
    /// </summary>
    private IEnumerator WaitImpact(SkillData skill = null)
    {
        float delay = (skill != null && skill.ImpactDelay > 0f)
            ? skill.ImpactDelay
            : _impactDelay;

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }
    }

    /// <summary>
    /// 기본 공격 실행
    /// </summary>
    /// <param name="actionRequest">공격 행동 요청</param>
    private void ExecuteAttack(
        BattleActionRequest actionRequest)
    {
        BattleUnit attacker =
            actionRequest.Actor;

        BattleUnit target =
            actionRequest.Target;

        if (attacker == null ||
            target == null)
        {
            return;
        }

        if (attacker.IsAlive == false ||
            target.IsAlive == false)
        {
            return;
        }

        int damage =
            CalculateBasicAttackDamage(
                attacker,
                target);

        target.TakeDamage(damage);

        // 피격 시 자동 해제 상태이상 처리(수면 등), 사망 시 상태이상 전체 해제(이펙트 잔류 방지)
        _statusEffectController.OnUnitHit(target);

        if (target.IsAlive == false)
        {
            _statusEffectController.RemoveAllStatusEffects(target);
        }

        Debug.Log(
            $"[Battle] {attacker.UnitName} attacks " +
            $"{target.UnitName} / " +
            $"Damage: {damage} / " +
            $"Target HP: {target.CurrentHp}");
    }

    /// <summary>
    /// 스킬 실행
    /// 일반 스킬과 별자리 공격 분기
    /// </summary>
    /// <param name="actionRequest">스킬 행동 요청</param>
    private IEnumerator ExecuteSkill(
        BattleActionRequest actionRequest)
    {
        BattleUnit actor =
            actionRequest.Actor;

        SkillData skillData =
            actionRequest.SkillData;

        if (actor == null ||
            skillData == null ||
            actor.IsAlive == false)
        {
            yield break;
        }

        ResolveSkillTargets(
            actionRequest,
            _skillTargets);

        if (_skillTargets.Count == 0)
        {
            Debug.LogWarning(
                $"[Battle] {skillData.SkillName} 대상 없음");

            yield break;
        }

        List<BattleUnit> resolvedTargets =
            new List<BattleUnit>(_skillTargets);

        _skillTargets.Clear();

        // 침묵: 스킬 사용 불가 → 공격 스킬은 기본공격 대체, 지원 스킬은 불발 (MP 소모 전에 판정)
        if (_statusEffectController.CanUseSkill(actor) == false)
        {
            if (skillData.SkillType == SkillEffectType.Damage &&
                resolvedTargets.Count > 0 &&
                resolvedTargets[0] != null)
            {
                Debug.Log(
                    $"[Battle] {actor.UnitName} 침묵: 스킬 불가 → 기본공격 대체");

                BattleActionRequest attackRequest =
                    BattleActionRequest.CreateAttack(
                        actor,
                        resolvedTargets[0]);

                yield return ExecutePresentedAttack(attackRequest);
            }
            else
            {
                Debug.Log(
                    $"[Battle] {actor.UnitName} 침묵: 지원 스킬 불발");
            }

            yield break;
        }

        if (actor.UseMp(
                skillData.MpCost) == false)
        {
            Debug.LogWarning(
                $"[Battle] {actor.UnitName} MP 부족");

            yield break;
        }

        Debug.Log(
            $"[Battle] {actor.UnitName} uses " +
            $"{skillData.SkillName}");

        bool isEnemyConstellationPathAttack =
            actor.TeamType ==
            BattleTeamType.Enemy &&
            skillData.SkillType ==
            SkillEffectType.Damage &&
            skillData.IsConstellationPathAttack;

        if (isEnemyConstellationPathAttack)
        {
            yield return ExecuteConstellationPathSkill(
                actionRequest,
                resolvedTargets);

            yield break;
        }

        yield return ExecuteSkillWithoutConstellationPath(
            actionRequest,
            resolvedTargets);
    }

    /// <summary>
    /// 경로형 별자리 패리 스킬 실행
    /// 카메라 연출, 별자리 입력, 최종 결과 처리
    /// </summary>
    /// <param name="actionRequest">스킬 행동 요청</param>
    /// <param name="targets">스킬 대상 목록</param>
    private IEnumerator ExecuteConstellationPathSkill(
        BattleActionRequest actionRequest,
        IReadOnlyList<BattleUnit> targets)
    {
        BattleUnit actor =
            actionRequest.Actor;

        SkillData skillData =
            actionRequest.SkillData;

        if (actor == null ||
            skillData == null)
        {
            yield break;
        }

        if (_constellationPathBattleManager == null ||
            !_constellationPathBattleManager
                .isActiveAndEnabled)
        {
            Debug.LogWarning(
                "[Battle] 신규 별자리 매니저 참조 없음. " +
                "일반 스킬로 실행",
                this);

            yield return
                ExecuteSkillWithoutConstellationPath(
                    actionRequest,
                    targets);

            yield break;
        }

        ConstellationPathSequenceData sequenceData =
            skillData.ConstellationPathSequenceData;

        if (sequenceData == null)
        {
            Debug.LogWarning(
                $"[Battle] {skillData.SkillName}에 " +
                "경로형 별자리 데이터가 없음",
                skillData);

            yield return
                ExecuteSkillWithoutConstellationPath(
                    actionRequest,
                    targets);

            yield break;
        }

        if (!sequenceData.TryValidate(
                out string errorMessage))
        {
            Debug.LogWarning(
                $"[Battle] 경로형 별자리 데이터 오류: " +
                $"{errorMessage}",
                sequenceData);

            yield return
                ExecuteSkillWithoutConstellationPath(
                    actionRequest,
                    targets);

            yield break;
        }

        yield return PlayEnemyActionCamera(
            actionRequest,
            targets,
            false);

        if (_battleState ==
            BattleState.BattleEnd)
        {
            yield break;
        }

        bool isIntroCompleted = true;

        if (_battleCameraDirector != null &&
            _battleCameraDirector.isActiveAndEnabled)
        {
            isIntroCompleted = false;

            BattleUnit cameraTarget =
                targets != null &&
                targets.Count > 0
                    ? targets[0]
                    : null;

            _battleCameraDirector
                .PlayConstellationAttackIntro(
                    actor,
                    cameraTarget,
                    () => isIntroCompleted = true);
        }

        while (!isIntroCompleted)
        {
            if (_battleState ==
                BattleState.BattleEnd)
            {
                yield break;
            }

            yield return null;
        }

        // 연출(애니메이션·VFX·사운드)은 별자리 종료 후 패리 실패 시에 재생한다.
        // 미니게임 중에는 카메라가 별자리 화면이라 여기서 재생하면 보이지 않는다.
        bool isStarted =
            _constellationPathBattleManager
                .StartConstellationPath(
                    sequenceData);

        if (!isStarted)
        {
            Debug.LogWarning(
                "[Battle] 경로형 별자리 시작 실패. " +
                "기존 스킬 효과 적용",
                this);

            OnActionExecuting?.Invoke(
                actionRequest);

            yield return WaitImpact(
                skillData);

            ApplySkillEffects(
                actor,
                targets,
                skillData,
                actionRequest.DamageMultiplier);

            yield break;
        }

        while (_constellationPathBattleManager.IsRunning)
        {
            if (_battleState ==
                BattleState.BattleEnd)
            {
                _constellationPathBattleManager
                    .StopConstellationPath();

                yield break;
            }

            yield return null;
        }

        if (!_constellationPathBattleManager
                .TryGetLastResult(
                    out ConstellationPathResult result))
        {
            Debug.LogWarning(
                "[Battle] 경로형 별자리 결과 수신 실패. " +
                "스킬 효과 적용",
                this);

            // 실패 처리와 동일하게 연출을 재생한 뒤 데미지 적용
            OnActionExecuting?.Invoke(
                actionRequest);

            yield return WaitImpact(
                skillData);

            ApplySkillEffects(
                actor,
                targets,
                skillData,
                actionRequest.DamageMultiplier);

            yield break;
        }

        OnConstellationResolved?.Invoke(
            actionRequest,
            result);

        Debug.Log(
            $"[Battle] 경로형 별자리 결과" +
            $"\nSkill: {skillData.SkillName}" +
            $"\nSuccess: {result.IsSuccess}" +
            $"\nNodes: {result.CompletedNodeCount}" +
            $"/{result.TotalNodeCount}" +
            $"\nElapsed: {result.ElapsedInputTime:F2}" +
            $"\nRemaining: " +
            $"{result.RemainingTimeAtCompletion:F2}",
            this);

        if (result.IsSuccess)
        {
            Debug.Log(
                $"[Battle] {skillData.SkillName} " +
                "패리 성공 / 스킬 효과 무효화",
                this);

            yield break;
        }

        Debug.Log(
            $"[Battle] {skillData.SkillName} " +
            "패리 실패 / 스킬 효과 적용",
            this);

        // 패리 실패 = 맞는 연출. 미니게임 종료 후이므로 이펙트·사운드를 다시 재생한다.
        OnActionExecuting?.Invoke(
            actionRequest);

        yield return WaitImpact(
            skillData);

        ApplySkillEffects(
            actor,
            targets,
            skillData,
            actionRequest.DamageMultiplier);
    }

    /// <summary>
    /// 방어 실행
    /// </summary>
    /// <param name="actionRequest">방어 행동 요청</param>
    private void ExecuteDefense(
        BattleActionRequest actionRequest)
    {
        if (actionRequest.Actor == null)
        {
            return;
        }

        Debug.Log(
            $"[Battle] {actionRequest.Actor.UnitName} defends");
    }

    /// <summary>
    /// 아이템 사용 실행
    /// </summary>
    /// <param name="actionRequest">아이템 사용 행동 요청</param>
    private void ExecuteUsingItem(
        BattleActionRequest actionRequest)
    {
        if (actionRequest.Actor == null)
        {
            return;
        }

        Debug.Log(
            $"[Battle] {actionRequest.Actor.UnitName} " +
            "tries to use Item");
    }

    /// <summary>
    /// 도망 실행
    /// </summary>
    /// <param name="actionRequest">도망 행동 요청</param>
    private void ExecuteEscape(
        BattleActionRequest actionRequest)
    {
        if (actionRequest.Actor == null)
        {
            return;
        }

        Debug.Log(
            $"[Battle] {actionRequest.Actor.UnitName} " +
            "tries to escape");
    }

    /// <summary>
    /// 생존 상대 목록 생성
    /// </summary>
    /// <param name="actor">기준 유닛</param>
    /// <param name="targets">생존 상대 목록</param>
    public void GetAliveOpponents(
        BattleUnit actor,
        List<BattleUnit> targets)
    {
        if (targets == null)
        {
            return;
        }

        targets.Clear();

        if (actor == null)
        {
            return;
        }

        for (int i = 0;
             i < _battleUnits.Count;
             i++)
        {
            BattleUnit unit =
                _battleUnits[i];

            if (unit == null ||
                unit.IsAlive == false ||
                unit.TeamType == actor.TeamType)
            {
                continue;
            }

            targets.Add(unit);
        }
    }

    /// <summary>
    /// 생존 아군 목록 생성
    /// </summary>
    /// <param name="actor">기준 유닛</param>
    /// <param name="targets">생존 아군 목록</param>
    /// <param name="includeSelf">자기 자신 포함 여부</param>
    public void GetAliveAllies(
        BattleUnit actor,
        List<BattleUnit> targets,
        bool includeSelf = true)
    {
        if (targets == null)
        {
            return;
        }

        targets.Clear();

        if (actor == null)
        {
            return;
        }

        for (int i = 0;
             i < _battleUnits.Count;
             i++)
        {
            BattleUnit unit =
                _battleUnits[i];

            if (unit == null ||
                unit.IsAlive == false ||
                unit.TeamType != actor.TeamType)
            {
                continue;
            }

            if (includeSelf == false &&
                unit == actor)
            {
                continue;
            }

            targets.Add(unit);
        }
    }

    /// <summary>
    /// 스킬 대상 선택 필요 여부
    /// </summary>
    /// <param name="skillData">스킬 데이터</param>
    /// <returns>대상 선택 필요 여부</returns>
    public bool DoesSkillRequireTargetSelection(
        SkillData skillData)
    {
        if (skillData == null)
        {
            return false;
        }

        return
            skillData.TargetType ==
            TargetType.SingleEnemy ||
            skillData.TargetType ==
            TargetType.SingleAlly;
    }

    /// <summary>
    /// 스킬 선택 대상 목록 생성
    /// </summary>
    /// <param name="actor">사용 유닛</param>
    /// <param name="skillData">스킬 데이터</param>
    /// <param name="targets">선택 대상 목록</param>
    public void GetSelectableSkillTargets(
        BattleUnit actor,
        SkillData skillData,
        List<BattleUnit> targets)
    {
        if (targets == null)
        {
            return;
        }

        targets.Clear();

        if (actor == null ||
            skillData == null)
        {
            return;
        }

        switch (skillData.TargetType)
        {
            case TargetType.SingleEnemy:
                GetAliveOpponents(
                    actor,
                    targets);
                break;

            case TargetType.SingleAlly:
                GetAliveAllies(
                    actor,
                    targets,
                    true);
                break;

            case TargetType.Self:
                targets.Add(actor);
                break;

            case TargetType.AllEnemies:
                GetAliveOpponents(
                    actor,
                    targets);
                break;

            case TargetType.AllAllies:
                GetAliveAllies(
                    actor,
                    targets,
                    true);
                break;
        }
    }

    /// <summary>
    /// 스킬 실제 대상 목록 생성
    /// </summary>
    /// <param name="actionRequest">스킬 행동 요청</param>
    /// <param name="targets">적용 대상 목록</param>
    private void ResolveSkillTargets(
        BattleActionRequest actionRequest,
        List<BattleUnit> targets)
    {
        targets.Clear();

        if (actionRequest == null ||
            actionRequest.SkillData == null)
        {
            return;
        }

        BattleUnit actor =
            actionRequest.Actor;

        SkillData skillData =
            actionRequest.SkillData;

        if (actor == null)
        {
            return;
        }

        switch (skillData.TargetType)
        {
            case TargetType.SingleEnemy:
            case TargetType.SingleAlly:
                AddSingleTarget(
                    actionRequest.Target,
                    targets);
                break;

            case TargetType.Self:
                AddSingleTarget(
                    actor,
                    targets);
                break;

            case TargetType.AllEnemies:
                GetAliveOpponents(
                    actor,
                    targets);
                break;

            case TargetType.AllAllies:
                GetAliveAllies(
                    actor,
                    targets,
                    true);
                break;
        }
    }

    /// <summary>
    /// 단일 대상 추가
    /// </summary>
    /// <param name="target">추가 대상</param>
    /// <param name="targets">대상 목록</param>
    private void AddSingleTarget(
        BattleUnit target,
        List<BattleUnit> targets)
    {
        if (target == null ||
            target.IsAlive == false)
        {
            return;
        }

        targets.Add(target);
    }

    /// <summary>
    /// 대상 목록에 스킬 효과 일괄 적용
    /// </summary>
    /// <param name="actor">스킬 사용 유닛</param>
    /// <param name="targets">스킬 대상 목록</param>
    /// <param name="skillData">스킬 데이터</param>
    /// <param name="damageMultiplier">데미지 배율 (Damage 타입 스킬에만 적용됨)</param>
    private void ApplySkillEffects(
        BattleUnit actor,
        IReadOnlyList<BattleUnit> targets,
        SkillData skillData,
        float damageMultiplier = 1f)
    {
        if (actor == null ||
            targets == null ||
            skillData == null)
        {
            return;
        }

        for (int i = 0;
             i < targets.Count;
             i++)
        {
            BattleUnit target =
                targets[i];

            if (target == null ||
                target.IsAlive == false)
            {
                continue;
            }

            ApplySkillEffect(
                actor,
                target,
                skillData,
                damageMultiplier);
        }
    }

    /// <summary>
    /// 스킬 효과 적용
    /// </summary>
    /// <param name="actor">사용 유닛</param>
    /// <param name="target">대상 유닛</param>
    /// <param name="skillData">스킬 데이터</param>
    /// <param name="damageMultiplier">데미지 배율 (Damage 타입에만 적용, Heal은 영향 없음)</param>
    private void ApplySkillEffect(
        BattleUnit actor,
        BattleUnit target,
        SkillData skillData,
        float damageMultiplier)
    {
        if (actor == null ||
            target == null ||
            skillData == null)
        {
            return;
        }

        switch (skillData.SkillType)
        {
            case SkillEffectType.Damage:
                ApplyDamageSkill(
                    actor,
                    target,
                    skillData,
                    damageMultiplier);
                break;

            case SkillEffectType.Heal:
                ApplyHealSkill(
                    actor,
                    target,
                    skillData);
                break;

            case SkillEffectType.HealMp:
                ApplyHealMpSkill(
                    target,
                    skillData);
                break;

            default:
                Debug.Log(
                    $"[Battle] 아직 처리되지 않은 " +
                    $"스킬 효과: {skillData.SkillType}");
                break;
        }

        // 스킬의 상태이상 확률 판정 및 부여 (데미지/힐과 별개로 적용)
        TryApplyStatusEffect(target, skillData);
    }

    /// <summary>
    /// 데미지 스킬 적용
    /// </summary>
    /// <param name="actor">사용 유닛</param>
    /// <param name="target">대상 유닛</param>
    /// <param name="skillData">스킬 데이터</param>
    /// <param name="damageMultiplier">데미지 배율 (마법진 그리기 판정 결과 등)</param>
    private void ApplyDamageSkill(
        BattleUnit actor,
        BattleUnit target,
        SkillData skillData,
        float damageMultiplier)
    {
        int damage =
            CalculateSkillDamage(
                actor,
                target,
                skillData,
                damageMultiplier);

        target.TakeDamage(damage);

        // 피격 시 자동 해제 상태이상 처리(수면 등), 사망 시 상태이상 전체 해제(이펙트 잔류 방지)
        _statusEffectController.OnUnitHit(target);

        if (target.IsAlive == false)
        {
            _statusEffectController.RemoveAllStatusEffects(target);
        }

        Debug.Log(
            $"[Battle] {skillData.SkillName} hit " +
            $"{target.UnitName} / " +
            $"Damage: {damage} (x{damageMultiplier:0.00}) / " +
            $"Target HP: {target.CurrentHp}");
    }

    /// <summary>
    /// 회복 스킬 적용
    /// </summary>
    /// <param name="actor">사용 유닛</param>
    /// <param name="target">대상 유닛</param>
    /// <param name="skillData">스킬 데이터</param>
    private void ApplyHealSkill(
        BattleUnit actor,
        BattleUnit target,
        SkillData skillData)
    {
        int healAmount =
            Mathf.Max(
                1,
                Mathf.RoundToInt(
                    actor.MagicPower +
                    skillData.Power));

        target.Heal(healAmount);

        Debug.Log(
            $"[Battle] {skillData.SkillName} heal " +
            $"{target.UnitName} / " +
            $"Heal: {healAmount} / " +
            $"Target HP: {target.CurrentHp}");
    }

    /// <summary>
    /// MP 회복 스킬 적용. 스킬 Power만큼 대상 MP를 회복한다.
    /// </summary>
    /// <param name="target">대상 유닛</param>
    /// <param name="skillData">스킬 데이터</param>
    private void ApplyHealMpSkill(
        BattleUnit target,
        SkillData skillData)
    {
        int mpAmount =
            Mathf.Max(
                1,
                Mathf.RoundToInt(skillData.Power));

        target.RestoreMp(mpAmount);

        Debug.Log(
            $"[Battle] {skillData.SkillName} MP heal " +
            $"{target.UnitName} / " +
            $"MP: +{mpAmount} / " +
            $"Target MP: {target.CurrentMp}");
    }

    /// <summary>
    /// 스킬 데미지 계산
    /// </summary>
    /// <param name="actor">사용 유닛</param>
    /// <param name="target">대상 유닛</param>
    /// <param name="skillData">스킬 데이터</param>
    /// <param name="damageMultiplier">데미지 배율 (마법진 그리기 판정 결과 등, 기본 1f)</param>
    /// <returns>계산 피해량</returns>
    private int CalculateSkillDamage(
        BattleUnit actor,
        BattleUnit target,
        SkillData skillData,
        float damageMultiplier = 1f)
    {
        float rawDamage;

        if (skillData.DamageType ==
            DamageType.Fixed)
        {
            rawDamage = skillData.Power;
        }
        else
        {
            float attackValue =
                skillData.DamageType ==
                DamageType.Magical
                    ? actor.MagicPower
                    : actor.AttackPower;

            float defenseValue =
                skillData.DamageType ==
                DamageType.Magical
                    ? target.MagicDefensePower
                    : target.DefensePower;

            rawDamage =
                attackValue +
                skillData.Power -
                defenseValue * 0.5f;
        }

        rawDamage *= damageMultiplier;

        return Mathf.Max(
            1,
            Mathf.RoundToInt(rawDamage));
    }

    /// <summary>
    /// 현재 턴 순서 복사
    /// </summary>
    /// <param name="result">복사 대상 목록</param>
    /// <param name="includeDead">사망 유닛 포함 여부</param>
    public void GetCurrentTurnOrder(
        List<BattleUnit> result,
        bool includeDead = true)
    {
        if (result == null)
        {
            return;
        }

        result.Clear();

        for (int i = 0;
             i < _turnOrder.Count;
             i++)
        {
            BattleUnit unit =
                _turnOrder[i];

            if (unit == null)
            {
                continue;
            }

            if (includeDead == false &&
                unit.IsAlive == false)
            {
                continue;
            }

            result.Add(unit);
        }
    }

    /// <summary>
    /// 현재 턴 순서 인덱스 반환
    /// </summary>
    /// <returns>현재 턴 순서 인덱스</returns>
    public int GetCurrentTurnOrderIndex()
    {
        return _currentTurnOrderIndex;
    }

    /// <summary>
    /// 현재 행동 유닛 반환
    /// </summary>
    /// <returns>현재 행동 유닛</returns>
    public BattleUnit GetCurrentTurnUnit()
    {
        if (_currentTurnOrderIndex < 0 ||
            _currentTurnOrderIndex >=
            _turnOrder.Count)
        {
            return null;
        }

        return _turnOrder[
            _currentTurnOrderIndex];
    }

    /// <summary>
    /// 전투 강제 종료
    /// </summary>
    /// <param name="winner">승리 처리할 팀</param>
    public void ForceEndBattle(
        BattleTeamType winner)
    {
        StopBattle();

        _battleState =
            BattleState.BattleEnd;

        OnBattleEnded?.Invoke(winner);
    }

    /// <summary>
    /// 별자리 패리 없이 일반 스킬 실행
    /// </summary>
    /// <param name="actionRequest">스킬 행동 요청</param>
    /// <param name="targets">스킬 대상 목록</param>
    private IEnumerator ExecuteSkillWithoutConstellationPath(
        BattleActionRequest actionRequest,
        IReadOnlyList<BattleUnit> targets)
    {
        if (actionRequest == null ||
            actionRequest.Actor == null ||
            actionRequest.SkillData == null)
        {
            yield break;
        }

        yield return PlayEnemyActionCamera(
            actionRequest,
            targets);

        bool isImpactReached = false;
        bool isPresentationCompleted = false;

        PlayActionPresentation(
            actionRequest,
            () => isImpactReached = true,
            () => isPresentationCompleted = true);

        yield return WaitForActionImpact(
            () => isImpactReached);

        // 혼란: 스킬도 일정 확률로 빗나감 (한 번 굴려 통째 실패, 시전 연출·MP는 이미 소모됨)
        if (_statusEffectController.RollConfusionMiss(
                actionRequest.Actor))
        {
            Debug.Log(
                $"[Battle] {actionRequest.Actor.UnitName} 혼란: 스킬이 빗나감(MISS)");

            // 각 대상에 데미지 0 통지(연출: Miss 표시용)
            for (int i = 0; targets != null && i < targets.Count; i++)
            {
                targets[i]?.NotifyMiss();
            }

            yield return WaitForActionPresentation(
                () => isPresentationCompleted);

            yield break;
        }

        ApplySkillEffects(
            actionRequest.Actor,
            targets,
            actionRequest.SkillData,
            actionRequest.DamageMultiplier);

        yield return WaitForActionPresentation(
            () => isPresentationCompleted);

        yield return WaitForTargetReactions(
            targets);
    }
}