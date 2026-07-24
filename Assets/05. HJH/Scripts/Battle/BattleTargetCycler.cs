using System.Collections.Generic;
using EPOOutline;
using UnityEngine;
using UnityEngine.UI;

public class BattleTargetCycler : MonoBehaviour
{
    public static BattleTargetCycler Instance { get; private set; }

    private enum Mode { Idle, PendingAttack, PendingSkill }

    [Header("Action Bar (Btns)")]
    [SerializeField] private BattleActionBarController _actionBar;

    [Header("Confirm / Cancel Group (공격/스킬 대기 중에만 표시)")]
    [SerializeField] private BattleActionBarController _confirmCancelBar;
    [SerializeField] private Button _confirmBtn;
    [SerializeField] private Button _cancelBtn;

    [Header("Skill List (취소 시 되돌아갈 대상)")]
    [SerializeField] private SkillListController _skillListController;

    [Header("Debug (임시, 확인 끝나면 제거)")]
    [SerializeField] private TMPro.TMP_Text _debugTargetTxt;

    private Mode _mode = Mode.Idle;

    private BattleUnit _idleTarget;

    private readonly List<BattleUnit> _cycleCandidates = new List<BattleUnit>();
    private int _cycleIndex;
    private BattleUnit _snapshotIdleTarget;
    private SkillData _pendingSkill;
    private bool _isSubscribed;
    private readonly HashSet<BattleUnit> _outlinedUnits = new HashSet<BattleUnit>();
    private readonly List<BattleUnit> _hpSubscribedUnits = new List<BattleUnit>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_confirmBtn != null) _confirmBtn.onClick.AddListener(Confirm);
        if (_cancelBtn != null) _cancelBtn.onClick.AddListener(Cancel);

        if (_confirmCancelBar != null) _confirmCancelBar.Hide();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void TrySubscribe()
    {
        if (_isSubscribed) return;

        if (BattleUIContext.Instance == null)
        {
            return;
        }

        BattleUIContext.Instance.OnBattleStarted += HandleBattleStarted;
        BattleUIContext.Instance.OnTurnStarted += HandleTurnStarted;

        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (_isSubscribed == false) return;

        if (BattleUIContext.Instance != null)
        {
            BattleUIContext.Instance.OnBattleStarted -= HandleBattleStarted;
            BattleUIContext.Instance.OnTurnStarted -= HandleTurnStarted;
        }

        _isSubscribed = false;

        UnsubscribeAllUnitHp();
    }

    /// <summary>
    /// 전투 시작 시: 모든 유닛의 OnHpChanged를 구독(사망 감지용) + 즉시 기본 타겟 지정.
    /// </summary>
    private void HandleBattleStarted()
    {
        _idleTarget = null;

        SubscribeAllUnitHp();
        RefreshIdleTarget();
    }

    private void SubscribeAllUnitHp()
    {
        UnsubscribeAllUnitHp();

        if (BattleUIContext.Instance == null) return;

        List<BattleUnit> allUnits = new List<BattleUnit>();
        BattleUIContext.Instance.GetCurrentTurnOrder(allUnits, true);


        // 전투 시작 시점엔 아직 턴 순서가 안 잡혀있을 수 있어 GetAliveOpponents로 대체 확보
        if (allUnits.Count == 0 && BattleUIContext.Instance.CurrentUnit != null)
        {
            BattleUIContext.Instance.GetAliveOpponents(BattleUIContext.Instance.CurrentUnit, allUnits);
        }

        foreach (var unit in allUnits)
        {
            if (unit == null) continue;

            unit.OnHpChanged += HandleAnyUnitHpChanged;
            _hpSubscribedUnits.Add(unit);
        }
    }

    private void UnsubscribeAllUnitHp()
    {
        foreach (var unit in _hpSubscribedUnits)
        {
            if (unit != null)
            {
                unit.OnHpChanged -= HandleAnyUnitHpChanged;
            }
        }

        _hpSubscribedUnits.Clear();
    }

    /// <summary>
    /// 아무 유닛의 HP가 바뀔 때마다 호출. 지금 타겟이 죽었거나, 마지막 적이 남거나 하는 경우를 즉시 재검증.
    /// Pending 상태(공격/스킬 확정 대기 중)에는 후보 목록이 이미 확정되어 있으니 건드리지 않음.
    /// </summary>
    private void HandleAnyUnitHpChanged()
    {
        if (_mode != Mode.Idle) return;

        RefreshIdleTarget();
    }

    /// <summary>
    /// 생존 적 기준으로 Idle 타겟을 재계산. 기존 타겟이 살아있으면 유지, 아니면 첫 생존 적으로 교체.
    /// </summary>
    private void RefreshIdleTarget()
    {
        if (BattleUIContext.Instance == null) return;

        BattleUnit actor = BattleUIContext.Instance.CurrentUnit;

        List<BattleUnit> opponents = new List<BattleUnit>();

        if (actor != null)
        {
            BattleUIContext.Instance.GetAliveOpponents(actor, opponents);
        }
        else
        {
            // CurrentUnit이 아직 없는 경우(전투 시작 직후)를 대비해 아군 아무나 기준으로도 조회 시도
            List<BattleUnit> party = new List<BattleUnit>(BattleUIContext.Instance.PartyUnits);
            if (party.Count > 0)
            {
                BattleUIContext.Instance.GetAliveOpponents(party[0], opponents);
            }
        }


        BattleUnit previousTarget = _idleTarget;

        if (opponents.Count == 0)
        {
            if (previousTarget != null) SetOutline(previousTarget, false);
            _idleTarget = null;
            return;
        }

        if (_idleTarget == null || _idleTarget.IsAlive == false || opponents.Contains(_idleTarget) == false)
        {
            if (previousTarget != null) SetOutline(previousTarget, false);

            _idleTarget = opponents[0];
            SetOutline(_idleTarget, true);

        }
    }

    /// <summary>
    /// 아군 턴이 시작될 때마다 Pending 상태 정리 + 타겟 재검증.
    /// </summary>
    private void HandleTurnStarted(BattleUnit unit)
    {
        if (unit == null || unit.TeamType != BattleTeamType.Player) return;
        if (BattleUIContext.Instance == null) return;

        _mode = Mode.Idle;

        if (_confirmCancelBar != null) _confirmCancelBar.Hide();

        RefreshIdleTarget();
    }

    // ===================== Q/E, Enter, ESC (UITestInputReader에서 호출) =====================

    public void CyclePrevious() => CycleTarget(-1);

    public void CycleNext() => CycleTarget(1);

    private void CycleTarget(int direction)
    {
        if (_mode == Mode.Idle)
        {
            CycleIdleTarget(direction);
            return;
        }

        if (_cycleCandidates.Count <= 1) return;

        SetOutline(_cycleCandidates[_cycleIndex], false);

        _cycleIndex = (_cycleIndex + direction + _cycleCandidates.Count) % _cycleCandidates.Count;

        SetOutline(_cycleCandidates[_cycleIndex], true);
    }

    private void CycleIdleTarget(int direction)
    {
        if (BattleUIContext.Instance == null || BattleUIContext.Instance.CurrentUnit == null) return;

        List<BattleUnit> opponents = new List<BattleUnit>();
        BattleUIContext.Instance.GetAliveOpponents(BattleUIContext.Instance.CurrentUnit, opponents);

        if (opponents.Count <= 1) return;

        int currentIndex = opponents.IndexOf(_idleTarget);
        if (currentIndex < 0) currentIndex = 0;

        SetOutline(_idleTarget, false);

        int newIndex = (currentIndex + direction + opponents.Count) % opponents.Count;
        _idleTarget = opponents[newIndex];

        SetOutline(_idleTarget, true);
    }

    public void Confirm()
    {
        if (_mode == Mode.PendingAttack)
        {
            ConfirmAttack();
        }
        else if (_mode == Mode.PendingSkill)
        {
            ConfirmSkill();
        }
    }

    public void Cancel()
    {
        if (_mode == Mode.Idle) return;

        bool wasSkill = _mode == Mode.PendingSkill;

        RestoreIdleSnapshot();

        _mode = Mode.Idle;

        if (_confirmCancelBar != null) _confirmCancelBar.Hide();

        _pendingSkill = null;

        if (wasSkill && _skillListController != null)
        {
            _skillListController.Reopen();
            return;
        }

        if (_actionBar != null)
        {
            _actionBar.Show();
        }
    }

    // ===================== 공격 / 스킬 Confirm Cancl 진입 =====================

    public void EnterAttackMode()
    {
        if (BattleUIContext.Instance == null || BattleUIContext.Instance.CurrentUnit == null) return;

        BattleUnit actor = BattleUIContext.Instance.CurrentUnit;

        _snapshotIdleTarget = _idleTarget;

        _cycleCandidates.Clear();
        BattleUIContext.Instance.GetAliveOpponents(actor, _cycleCandidates);

        if (_cycleCandidates.Count == 0) return;

        _cycleIndex = _idleTarget != null ? Mathf.Max(0, _cycleCandidates.IndexOf(_idleTarget)) : 0;

        ClearAllOutlines();
        SetOutline(_cycleCandidates[_cycleIndex], true);

        _mode = Mode.PendingAttack;

        if (_actionBar != null) _actionBar.Hide();
        if (_confirmCancelBar != null) _confirmCancelBar.Show();
    }

    public void EnterSkillMode(SkillData skillData)
    {
        if (skillData == null || BattleUIContext.Instance == null || BattleUIContext.Instance.CurrentUnit == null) return;

        BattleUnit actor = BattleUIContext.Instance.CurrentUnit;

        _pendingSkill = skillData;
        _snapshotIdleTarget = _idleTarget;

        List<BattleUnit> candidates = new List<BattleUnit>();
        BattleUIContext.Instance.GetSelectableSkillTargets(actor, skillData, candidates);

        if (candidates.Count == 0) return;

        ClearAllOutlines();
        _cycleCandidates.Clear();

        bool isSingleTarget = skillData.TargetType == TargetType.SingleEnemy || skillData.TargetType == TargetType.SingleAlly;

        if (isSingleTarget)
        {
            _cycleCandidates.AddRange(candidates);

            BattleUnit preferred = (skillData.TargetType == TargetType.SingleEnemy && _idleTarget != null && candidates.Contains(_idleTarget))
                ? _idleTarget
                : candidates[0];

            _cycleIndex = Mathf.Max(0, _cycleCandidates.IndexOf(preferred));

            SetOutline(_cycleCandidates[_cycleIndex], true);
        }
        else
        {
            foreach (var unit in candidates)
            {
                SetOutline(unit, true);
            }
        }

        _mode = Mode.PendingSkill;

        if (_confirmCancelBar != null) _confirmCancelBar.Show();
    }

    // ===================== 확정 처리 =====================

    private void ConfirmAttack()
    {
        if (_cycleCandidates.Count == 0) return;
        if (BattleUIContext.Instance == null || BattleUIContext.Instance.CurrentUnit == null) return;

        BattleUnit actor = BattleUIContext.Instance.CurrentUnit;
        BattleUnit target = _cycleCandidates[_cycleIndex];

        BattleActionRequest request = BattleActionRequest.CreateAttack(actor, target);
        BattleUIContext.Instance.SubmitAction(request);

        _idleTarget = target;

        FinishPending();
    }

    private void ConfirmSkill()
    {
        if (BattleUIContext.Instance == null || BattleUIContext.Instance.CurrentUnit == null || _pendingSkill == null) return;

        BattleUnit actor = BattleUIContext.Instance.CurrentUnit;

        bool isSingleTarget = _pendingSkill.TargetType == TargetType.SingleEnemy || _pendingSkill.TargetType == TargetType.SingleAlly;

        BattleUnit target = isSingleTarget && _cycleCandidates.Count > 0
            ? _cycleCandidates[_cycleIndex]
            : null;

        BattleActionRequest request = BattleActionRequest.CreateSkill(actor, _pendingSkill, target);
        BattleUIContext.Instance.SubmitAction(request);

        if (isSingleTarget && _pendingSkill.TargetType == TargetType.SingleEnemy && target != null)
        {
            _idleTarget = target;
        }

        _pendingSkill = null;

        FinishPending();
    }

    private void FinishPending()
    {
        _mode = Mode.Idle;

        if (_confirmCancelBar != null) _confirmCancelBar.Hide();

        ClearAllOutlines();

        RefreshIdleTarget();

        if (_idleTarget != null)
        {
            SetOutline(_idleTarget, true);
        }

        if (_actionBar != null)
        {
            _actionBar.Show();
        }
    }

    private void RestoreIdleSnapshot()
    {
        ClearAllOutlines();

        _idleTarget = _snapshotIdleTarget;

        if (_idleTarget != null && _idleTarget.IsAlive)
        {
            SetOutline(_idleTarget, true);
        }

        _cycleCandidates.Clear();
    }

    // ===================== 아웃라인 =====================

    private void SetOutline(BattleUnit unit, bool enabled)
    {
        if (unit == null || BattleUIContext.Instance == null) return;

        if (BattleUIContext.Instance.TryGetActor(unit, out BattleActor actor) == false || actor == null) return;

        Outlinable outlinable = actor.GetComponent<Outlinable>();

        if (outlinable != null)
        {
            outlinable.enabled = enabled;
        }

        if (enabled)
        {
            _outlinedUnits.Add(unit);
        }
        else
        {
            _outlinedUnits.Remove(unit);
        }

        UpdateDebugTargetText();
    }

    private void ClearAllOutlines()
    {
        if (_outlinedUnits.Count == 0) return;

        List<BattleUnit> toClear = new List<BattleUnit>(_outlinedUnits);

        foreach (var unit in toClear)
        {
            SetOutline(unit, false);
        }
    }

    /// <summary>
    /// 임시 디버그: 현재 아웃라인이 켜진 유닛 이름을 화면에 표시. 확인 끝나면 제거.
    /// </summary>
    private void UpdateDebugTargetText()
    {
        if (_debugTargetTxt == null) return;

        if (_outlinedUnits.Count == 0)
        {
            _debugTargetTxt.text = "Target: 없음";
            return;
        }

        List<string> names = new List<string>();

        foreach (var unit in _outlinedUnits)
        {
            names.Add(unit.UnitName);
        }

        _debugTargetTxt.text = "Target: " + string.Join(", ", names);
    }
}