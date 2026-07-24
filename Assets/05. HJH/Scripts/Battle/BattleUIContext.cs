using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// BattleCycleController/BattleManager를 직접 아는 유일한 창구.
/// UI 쪽 컨트롤러들은 이 스크립트(BattleUIContext.Instance)만 참조하면 되고,
/// BattleCycleController/BattleManager를 직접 몰라도 됨.
/// </summary>
public class BattleUIContext : MonoBehaviour
{
    public static BattleUIContext Instance { get; private set; }

    [SerializeField] private BattleCycleController _battleCycleController;
    [SerializeField] private BattleManager _battleManager;

    public BattleUnit CurrentUnit { get; private set; }
    public IReadOnlyList<BattleUnit> PartyUnits { get; private set; } = new List<BattleUnit>();

    public event Action<BattleUnit> OnTurnStarted;
    public event Action<BattleUnit> OnTurnEnded;
    public event Action OnBattleStarted;
    public event Action<BattleTeamType> OnBattleEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        if (_battleCycleController == null) return;

        _battleCycleController.OnBattleStarted += HandleBattleStarted;
        _battleCycleController.OnTurnStarted += HandleTurnStarted;
        _battleCycleController.OnTurnEnded += HandleTurnEnded;
        _battleCycleController.OnBattleEnded += HandleBattleEnded;
    }

    private void OnDisable()
    {
        if (_battleCycleController == null) return;

        _battleCycleController.OnBattleStarted -= HandleBattleStarted;
        _battleCycleController.OnTurnStarted -= HandleTurnStarted;
        _battleCycleController.OnTurnEnded -= HandleTurnEnded;
        _battleCycleController.OnBattleEnded -= HandleBattleEnded;
    }

    private void HandleBattleStarted()
    {
        if (_battleManager != null)
        {
            PartyUnits = _battleManager.ActiveBattleUnits
                .Where(unit => unit != null && unit.TeamType == BattleTeamType.Player)
                .ToList();
        }

        OnBattleStarted?.Invoke();
    }

    private void HandleTurnStarted(BattleUnit unit, int actionCount)
    {
        CurrentUnit = unit;
        OnTurnStarted?.Invoke(unit);
    }

    private void HandleTurnEnded(BattleUnit unit)
    {
        OnTurnEnded?.Invoke(unit);
    }

    private void HandleBattleEnded(BattleTeamType winner)
    {
        Debug.Log($"[BattleUIContext] HandleBattleEnded 호출됨: {winner}");   // 임시

        CurrentUnit = null;
        OnBattleEnded?.Invoke(winner);
    }

    // BattleUIContext에 창구 추가
    public void ForceEndBattle(BattleTeamType winner)
    {
        if (_battleCycleController == null) return;
        _battleCycleController.ForceEndBattle(winner);
    }

    /// <summary>
    /// 공격/스킬 등 행동 요청을 전투 시스템에 제출.
    /// </summary>
    public void SubmitAction(BattleActionRequest request)
    {
        if (_battleCycleController == null || request == null) return;

        _battleCycleController.SubmitAction(request);
    }

    /// <summary>
    /// actor 기준 생존한 상대 목록 조회 (기본 공격용).
    /// </summary>
    public void GetAliveOpponents(BattleUnit actor, List<BattleUnit> targets)
    {
        if (_battleCycleController == null) return;

        _battleCycleController.GetAliveOpponents(actor, targets);
    }

    /// <summary>
    /// 이 스킬이 대상 선택 UI를 열어야 하는지 여부. false면 대상 없이 바로 제출해야 함.
    /// </summary>
    public bool DoesSkillRequireTargetSelection(SkillData skillData)
    {
        if (_battleCycleController == null || skillData == null) return true;

        return _battleCycleController.DoesSkillRequireTargetSelection(skillData);
    }

    /// <summary>
    /// 스킬의 TargetType에 맞는 선택 가능 대상 목록 조회 (아군 힐 스킬이면 아군, 공격 스킬이면 적 등).
    /// </summary>
    public void GetSelectableSkillTargets(BattleUnit actor, SkillData skillData, List<BattleUnit> targets)
    {
        if (_battleCycleController == null) return;

        _battleCycleController.GetSelectableSkillTargets(actor, skillData, targets);
    }

    /// <summary>
    /// 이번 라운드 전체 턴 순서(아군+적 섞여있음)를 복사해서 반환.
    /// </summary>
    public void GetCurrentTurnOrder(List<BattleUnit> result, bool includeDead = true)
    {
        if (_battleCycleController == null) return;

        _battleCycleController.GetCurrentTurnOrder(result, includeDead);
    }

    /// <summary>
    /// 이번 라운드 턴 순서 리스트 상에서 지금 행동 중인 유닛의 인덱스.
    /// </summary>
    public int GetCurrentTurnOrderIndex()
    {
        return _battleCycleController != null ? _battleCycleController.GetCurrentTurnOrderIndex() : -1;
    }

    /// <summary>
    /// BattleUnit에 대응하는 BattleActor 검색 (아웃라인 표시 등에서 사용).
    /// </summary>
    public bool TryGetActor(BattleUnit unit, out BattleActor actor)
    {
        actor = null;

        if (_battleManager == null || unit == null)
        {
            return false;
        }

        return _battleManager.TryGetActor(unit, out actor);
    }
}