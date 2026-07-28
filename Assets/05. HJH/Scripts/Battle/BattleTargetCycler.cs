using System.Collections.Generic;
using EPOOutline;
using UnityEngine;

public class BattleTargetCycler : MonoBehaviour
{
    /// <summary>
    /// 씬에 하나만 존재. "지금 턴인 캐릭터"의 타겟팅 상태만 다루는 세션 전역 컨트롤러라
    /// 캐릭터마다 둘 필요 없음. AtkController/SkillListController(캐릭터 프리팹 소속)가
    /// 이 씬 오브젝트를 인스펙터로 직접 참조할 수 없으므로 이 Instance를 통해 접근.
    /// </summary>
    public static BattleTargetCycler Instance { get; private set; }

    private enum Mode { Idle, PendingAttack, PendingSkill }

    /// <summary>
    /// 지금 공격/스킬 대상을 조준 중인지(Confirm/Cancel 대기 상태) 여부.
    /// BattleUIInputReader가 Enter/Esc를 커맨드 UI/리스트로 보낼지, 타겟 조준 확정/취소로 보낼지
    /// 판단할 때 사용.
    /// </summary>
    public bool IsTargeting => _mode != Mode.Idle;

    [Header("Camera (씬 오브젝트라 인스펙터 연결 대신 런타임 자동 탐색)")]
    [SerializeField] private BattleCameraDirector _cameraDirector;

    [Header("Battle Canvas (마법진 그리기 도중 잠깐 비활성화)")]
    [Tooltip("메인 배틀 HUD 캔버스 루트. 같은 씬 오브젝트라 직접 연결 가능.")]
    [SerializeField] private GameObject _battleCanvasRoot;

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
    }

    private void OnEnable()
    {
        TrySubscribe();
        EnsureCameraDirector();
    }

    /// <summary>
    /// BattleCameraDirector는 씬 오브젝트라 프리팹 인스펙터로 직접 연결할 수 없어서
    /// Camera.main 계층에서 런타임에 자동으로 찾음. 실패 시 씬 전체(비활성 포함)에서 검색.
    /// </summary>
    private void EnsureCameraDirector()
    {
        if (_cameraDirector != null) return;

        if (Camera.main != null)
        {
            _cameraDirector = Camera.main.GetComponentInParent<BattleCameraDirector>();
        }

        if (_cameraDirector == null)
        {
            _cameraDirector = FindFirstObjectByType<BattleCameraDirector>(FindObjectsInactive.Include);
        }
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
    /// 적 턴 중에는 완전히 무시 (CurrentUnit이 적이면 GetAliveOpponents가 아군을 반환하게 되어,
    /// 적이 아군을 때려서 HP가 바뀔 때마다 아군이 "타겟 후보"인 것처럼 아웃라인이 켜지는 버그 방지).
    /// </summary>
    private void HandleAnyUnitHpChanged()
    {
        if (_mode != Mode.Idle) return;

        BattleUnit currentUnit = BattleUIContext.Instance != null ? BattleUIContext.Instance.CurrentUnit : null;

        if (currentUnit != null && currentUnit.TeamType != BattleTeamType.Player) return;

        RefreshIdleTarget();
    }

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

    private void HandleTurnStarted(BattleUnit unit)
    {
        // BattleStarted 시점엔 아직 턴 순서/CurrentUnit이 없어 구독이 비어있을 수 있으니,
        // 매 턴 시작마다 한 번 더 갱신해서 확실히 전체 유닛의 HP 변화를 구독하도록 보정.
        SubscribeAllUnitHp();

        if (unit == null || unit.TeamType != BattleTeamType.Player) return;
        if (BattleUIContext.Instance == null) return;

        _mode = Mode.Idle;

        RefreshIdleTarget();
    }

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

        // 기본 공격 또는 SingleEnemy 스킬로 대상을 순환 중일 때만 SingleTargetOverview 카메라도 같이 재조준.
        // (AllEnemies/SingleAlly/AllAllies는 GroupTargetOverview라 카메라가 대상 하나에 붙어있지 않음)
        bool isSingleEnemyTargeting = _mode == Mode.PendingAttack
            || (_mode == Mode.PendingSkill && _pendingSkill != null && _pendingSkill.TargetType == TargetType.SingleEnemy);

        if (isSingleEnemyTargeting)
        {
            EnsureCameraDirector();
            _cameraDirector?.RetargetSingleTargetOverview(_cycleCandidates[_cycleIndex]);
        }
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
            BeginSkillDraw();
        }
    }

    /// <summary>
    /// 스킬 Confirm 시 먼저 카메라를 SkillDrawCamera로 전환한 뒤, 완료되면 마법진 그리기 미니게임 실행.
    /// 그리기(또는 시간초과)가 끝나면 판정 배율을 콜백으로 받아 ConfirmSkill로 넘어감.
    /// SkillDrawController가 없거나 이 스킬에 DrawGuideJson이 없으면 즉시 배율 1로 진행됨
    /// (SkillDrawController.Play() 내부에서 처리).
    /// </summary>
    private void BeginSkillDraw()
    {
        if (_pendingSkill == null || BattleUIContext.Instance == null || BattleUIContext.Instance.CurrentUnit == null) return;

        BattleUnit actor = BattleUIContext.Instance.CurrentUnit;

        // 확인/취소 버튼은 그리는 동안 방해되니 미리 숨김 (그리기 끝나면 FinishPending에서 다시 정리)
        if (GlobalConfirmCancelController.Instance != null) GlobalConfirmCancelController.Instance.Hide();

        EnsureCameraDirector();

        if (_cameraDirector == null)
        {
            StartSkillDraw();
            return;
        }

        // TODO: BattleCameraDirector에 PlaySkillDrawView(BattleUnit, Action) 추가 필요
        _cameraDirector.PlaySkillDrawView(actor, StartSkillDraw);
    }

    private void StartSkillDraw()
    {
        if (_pendingSkill == null) return;

        if (SkillDrawController.Instance == null)
        {
            ConfirmSkill(1f);
            return;
        }

        SetOutlinedTargetVisualsSuppressed(true);

        if (_battleCanvasRoot != null) _battleCanvasRoot.SetActive(false);

        SkillDrawController.Instance.Play(_pendingSkill, damageMultiplier =>
        {
            if (_battleCanvasRoot != null) _battleCanvasRoot.SetActive(true);

            SetOutlinedTargetVisualsSuppressed(false);
            ConfirmSkill(damageMultiplier);
        });
    }

    /// <summary>
    /// SkillDrawCanvas가 떠 있는 동안, 지금 아웃라인된 대상들의 Outline/EnemyTargetOverlay(WorldCanvas)를
    /// 잠깐 숨김(suppressed=true) / 복원(suppressed=false). _outlinedUnits 추적 상태 자체는 건드리지 않아서
    /// 드로잉이 끝나면 원래 아웃라인 상태 그대로 복원됨.
    /// </summary>
    private void SetOutlinedTargetVisualsSuppressed(bool suppressed)
    {
        if (BattleUIContext.Instance == null) return;

        foreach (var unit in _outlinedUnits)
        {
            if (BattleUIContext.Instance.TryGetActor(unit, out BattleActor actor) == false || actor == null) continue;

            Outlinable outlinable = actor.GetComponent<Outlinable>();

            if (outlinable != null)
            {
                outlinable.enabled = suppressed == false;
            }

            EnemyTargetOverlay overlay = actor.GetComponentInChildren<EnemyTargetOverlay>(true);

            if (overlay != null)
            {
                if (suppressed)
                {
                    overlay.Hide();
                }
                else
                {
                    overlay.Show();
                }
            }

            ElementAffinityIndicatorView indicator = actor.GetComponentInChildren<ElementAffinityIndicatorView>(true);

            if (indicator != null)
            {
                if (suppressed)
                {
                    indicator.Hide();
                }
                else
                {
                    // 그리는 동안 모드/스킬이 바뀌지 않으므로, 숨기기 전 상태를 그대로 재계산해서 복원.
                    UpdateElementIndicator(unit, actor, true);
                }
            }
        }
    }

    public void Cancel()
    {
        if (_mode == Mode.Idle) return;

        bool wasSkill = _mode == Mode.PendingSkill;

        // RestoreIdleSnapshot()이 내부에서 SetOutline()을 호출하는데, 그 시점에 _mode가 아직
        // PendingSkill/PendingAttack이면 UpdateElementIndicator가 "아직 스킬 조준 중"으로 착각해서
        // 약점/저항 표시기가 잘못 뜰 수 있음 -> 먼저 Idle로 전환한 뒤 복원.
        _mode = Mode.Idle;

        RestoreIdleSnapshot();

        if (GlobalConfirmCancelController.Instance != null) GlobalConfirmCancelController.Instance.Hide();

        _pendingSkill = null;

        // 스킬 취소는 BackView를 거치지 않고 바로 SkillList.Reopen()(내부에서 SkillLowAngle로 전환)으로 넘김.
        // TargetOverview -> BackView -> SkillLowAngle로 카메라가 두 번 튀는 걸 방지.
        if (wasSkill)
        {
            FinishCancel(true);
            return;
        }

        BattleUnit actor = BattleUIContext.Instance != null ? BattleUIContext.Instance.CurrentUnit : null;

        EnsureCameraDirector();

        if (_cameraDirector != null && actor != null)
        {
            _cameraDirector.PlayPlayerBackView(actor, () => FinishCancel(false));
            return;
        }

        FinishCancel(false);
    }

    private void FinishCancel(bool wasSkill)
    {
        if (BattleCharacterUIManager.Instance != null)
        {
            BattleCharacterUIManager.Instance.ShowCurrentUI();
        }

        if (wasSkill)
        {
            SkillListController skillListController = GetCurrentActorSkillListController();

            if (skillListController != null)
            {
                skillListController.Reopen();
            }
        }
    }

    /// <summary>
    /// 지금 턴인 캐릭터의 SkillListController를 동적으로 찾음.
    /// SkillListController는 캐릭터 프리팹의 WorldSpaceCanvas 하위에 각자 존재하고,
    /// BattleTargetCycler는 씬 싱글톤이라 특정 캐릭터 걸 인스펙터로 고정 연결할 수 없음.
    /// </summary>
    private SkillListController GetCurrentActorSkillListController()
    {
        if (BattleUIContext.Instance == null || BattleUIContext.Instance.CurrentUnit == null) return null;

        if (BattleUIContext.Instance.TryGetActor(BattleUIContext.Instance.CurrentUnit, out BattleActor actor) == false || actor == null)
        {
            return null;
        }

        return actor.GetComponentInChildren<SkillListController>(true);
    }

    /// <summary>
    /// AtkController에서 호출. 기본 공격은 항상 단일 적 타겟이므로 SingleTargetOverview로 전환.
    /// 카메라가 실제로 비출 타겟(예상 후보)을 미리 계산해서 넘김 - _idleTarget이 유효하면 그대로,
    /// 아니면 생존 적 중 첫 번째.
    /// </summary>
    public void EnterAttackMode()
    {
        if (BattleUIContext.Instance == null || BattleUIContext.Instance.CurrentUnit == null) return;

        BattleUnit actor = BattleUIContext.Instance.CurrentUnit;

        EnsureCameraDirector();

        if (_cameraDirector == null)
        {
            ProceedEnterAttackMode(actor);
            return;
        }

        List<BattleUnit> opponents = new List<BattleUnit>();
        BattleUIContext.Instance.GetAliveOpponents(actor, opponents);

        BattleUnit previewTarget = (_idleTarget != null && opponents.Contains(_idleTarget))
            ? _idleTarget
            : (opponents.Count > 0 ? opponents[0] : null);

        if (previewTarget == null)
        {
            ProceedEnterAttackMode(actor);
            return;
        }

        _cameraDirector.PlaySingleTargetOverview(previewTarget, () => ProceedEnterAttackMode(actor));
    }

    private void ProceedEnterAttackMode(BattleUnit actor)
    {
        // 카메라 전환 대기 중 턴이 바뀌었거나 유닛이 죽는 등의 상황 대비
        if (BattleUIContext.Instance == null || BattleUIContext.Instance.CurrentUnit != actor) return;

        _snapshotIdleTarget = _idleTarget;

        _cycleCandidates.Clear();
        BattleUIContext.Instance.GetAliveOpponents(actor, _cycleCandidates);

        if (_cycleCandidates.Count == 0) return;

        _cycleIndex = _idleTarget != null ? Mathf.Max(0, _cycleCandidates.IndexOf(_idleTarget)) : 0;

        ClearAllOutlines();
        SetOutline(_cycleCandidates[_cycleIndex], true);

        _mode = Mode.PendingAttack;

        if (BattleCharacterUIManager.Instance != null) BattleCharacterUIManager.Instance.HideCurrentUI();
        if (GlobalConfirmCancelController.Instance != null) GlobalConfirmCancelController.Instance.Show(Confirm, Cancel);
    }

    /// <summary>
    /// SkillListController에서 스킬 선택 시 호출. 스킬의 TargetType에 따라 카메라 구도를 분기:
    /// - SingleEnemy: 예상 타겟을 SingleTargetOverview로 정면 근접 샷
    /// - AllEnemies: GroupTargetOverview(Enemy)로 적 전체 정면 샷
    /// - SingleAlly / AllAllies / Self: GroupTargetOverview(Player)로 아군 전체 뒷모습 샷
    /// 완료되면 실제 스킬 대상 선택 로직 실행.
    /// </summary>
    public void EnterSkillMode(SkillData skillData)
    {
        if (skillData == null || BattleUIContext.Instance == null || BattleUIContext.Instance.CurrentUnit == null) return;

        BattleUnit actor = BattleUIContext.Instance.CurrentUnit;

        EnsureCameraDirector();

        if (_cameraDirector == null)
        {
            ProceedEnterSkillMode(actor, skillData);
            return;
        }

        switch (skillData.TargetType)
        {
            case TargetType.SingleEnemy:
                {
                    List<BattleUnit> candidates = new List<BattleUnit>();
                    BattleUIContext.Instance.GetSelectableSkillTargets(actor, skillData, candidates);

                    BattleUnit previewTarget = (_idleTarget != null && candidates.Contains(_idleTarget))
                        ? _idleTarget
                        : (candidates.Count > 0 ? candidates[0] : null);

                    if (previewTarget == null)
                    {
                        ProceedEnterSkillMode(actor, skillData);
                        return;
                    }

                    _cameraDirector.PlaySingleTargetOverview(previewTarget, () => ProceedEnterSkillMode(actor, skillData));
                    break;
                }

            case TargetType.AllEnemies:
                _cameraDirector.PlayGroupTargetOverview(actor, BattleTeamType.Enemy, () => ProceedEnterSkillMode(actor, skillData));
                break;

            case TargetType.SingleAlly:
            case TargetType.AllAllies:
                _cameraDirector.PlayGroupTargetOverview(actor, BattleTeamType.Player, () => ProceedEnterSkillMode(actor, skillData));
                break;

            case TargetType.Self:
                // 즉발 스킬: 카메라 전환 없이 지금 보고 있는 BackView 그대로 유지.
                // Confirm 누르면 FinishPending()이 알아서 PlayPlayerBackView로 정리함.
                ProceedEnterSkillMode(actor, skillData);
                break;

            default:
                ProceedEnterSkillMode(actor, skillData);
                break;
        }
    }

    private void ProceedEnterSkillMode(BattleUnit actor, SkillData skillData)
    {
        if (BattleUIContext.Instance == null || BattleUIContext.Instance.CurrentUnit != actor) return;

        _pendingSkill = skillData;
        _snapshotIdleTarget = _idleTarget;

        List<BattleUnit> candidates = new List<BattleUnit>();
        BattleUIContext.Instance.GetSelectableSkillTargets(actor, skillData, candidates);

        if (candidates.Count == 0) return;

        ClearAllOutlines();
        _cycleCandidates.Clear();

        bool isSingleTarget = skillData.TargetType == TargetType.SingleEnemy || skillData.TargetType == TargetType.SingleAlly;

        // SetOutline() 호출들(약점/저항 표시기 판단에 _mode를 참조함)보다 먼저 PendingSkill로 전환해둬야
        // 처음 진입 시부터 표시기가 정상적으로 뜸.
        _mode = Mode.PendingSkill;

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

        if (BattleCharacterUIManager.Instance != null) BattleCharacterUIManager.Instance.HideCurrentUI();
        if (GlobalConfirmCancelController.Instance != null) GlobalConfirmCancelController.Instance.Show(Confirm, Cancel);
    }

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

    /// <summary>
    /// 마법진 그리기 판정이 끝난 뒤 호출됨. damageMultiplier를 BattleActionRequest에 실어서
    /// BattleCycleController.CalculateSkillDamage에서 실제로 적용되도록 함.
    /// </summary>
    private void ConfirmSkill(float damageMultiplier)
    {
        if (BattleUIContext.Instance == null || BattleUIContext.Instance.CurrentUnit == null || _pendingSkill == null) return;

        Debug.Log($"[BattleTargetCycler] {_pendingSkill.SkillName} 그리기 판정 배율: x{damageMultiplier:0.00}");

        BattleUnit actor = BattleUIContext.Instance.CurrentUnit;

        bool isSingleTarget = _pendingSkill.TargetType == TargetType.SingleEnemy || _pendingSkill.TargetType == TargetType.SingleAlly;

        BattleUnit target = isSingleTarget && _cycleCandidates.Count > 0
            ? _cycleCandidates[_cycleIndex]
            : null;

        BattleActionRequest request = BattleActionRequest.CreateSkill(actor, _pendingSkill, target, damageMultiplier);
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

        if (GlobalConfirmCancelController.Instance != null) GlobalConfirmCancelController.Instance.Hide();

        ClearAllOutlines();

        RefreshIdleTarget();

        if (_idleTarget != null)
        {
            SetOutline(_idleTarget, true);
        }

        BattleUnit actor = BattleUIContext.Instance != null ? BattleUIContext.Instance.CurrentUnit : null;

        EnsureCameraDirector();

        // Confirm 이후엔 턴이 넘어가므로 WorldCanvas는 다시 보여주지 않음 (alpha 0 유지).
        // 카메라만 원래 시점(PlayerBackView)으로 되돌림.
        if (_cameraDirector != null && actor != null)
        {
            _cameraDirector.PlayPlayerBackView(actor, () => { });
            return;
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

    private void SetOutline(BattleUnit unit, bool enabled)
    {
        if (unit == null || BattleUIContext.Instance == null) return;

        if (BattleUIContext.Instance.TryGetActor(unit, out BattleActor actor) == false || actor == null) return;

        Outlinable outlinable = actor.GetComponent<Outlinable>();

        if (outlinable != null)
        {
            outlinable.enabled = enabled;
        }

        EnemyTargetOverlay overlay = actor.GetComponentInChildren<EnemyTargetOverlay>(true);

        if (overlay != null)
        {
            if (enabled)
            {
                overlay.Show();
            }
            else
            {
                overlay.Hide();
            }
        }

        UpdateElementIndicator(unit, actor, enabled);

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

    /// <summary>
    /// 스킬 대상 지정 중(PendingSkill)이고 대상이 적일 때만, 그 스킬 속성이 이 적의 약점/저항인지
    /// ElementAffinityIndicatorView로 표시. 기본 공격/Idle 상태거나 대상이 아군이면 표시 안 함(숨김).
    /// </summary>
    private void UpdateElementIndicator(BattleUnit unit, BattleActor actor, bool enabled)
    {
        ElementAffinityIndicatorView indicator = actor.GetComponentInChildren<ElementAffinityIndicatorView>(true);

        if (indicator == null) return;

        if (enabled == false)
        {
            indicator.Hide();
            return;
        }

        if (_mode != Mode.PendingSkill || _pendingSkill == null || unit.TeamType != BattleTeamType.Enemy)
        {
            indicator.Hide();
            return;
        }

        EnemyBattleData enemyData = actor.EnemyBattleData;

        if (enemyData == null)
        {
            indicator.Hide();
            return;
        }

        ElementType skillElement = _pendingSkill.ElementType;

        if (ContainsElement(enemyData.WeakElements, skillElement))
        {
            indicator.ShowWeak();
        }
        else if (ContainsElement(enemyData.ResistElements, skillElement))
        {
            indicator.ShowResist();
        }
        else
        {
            indicator.Hide();
        }
    }

    private static bool ContainsElement(System.Collections.Generic.IReadOnlyList<ElementType> elements, ElementType element)
    {
        if (elements == null) return false;

        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] == element) return true;
        }

        return false;
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