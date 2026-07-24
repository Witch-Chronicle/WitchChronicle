using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Characters 오브젝트에 붙어서 아군 파티 상태를 "큐(대기열)" 형태로 표시.
/// - 이번 라운드의 고정된 턴 순서(BattleCycleController._turnOrder, 라운드 내내 안 바뀜)를 기준으로 회전.
///   -> 라운드 중간에 순서가 재조정되는 일이 없음. 다음 라운드가 되어야 갱신됨.
/// - 단, 화면에 배치할 때는 죽은 아군을 항상 맨 위 쪽 자리에 고정하고,
///   살아있는 아군만 맨 아래(선택 자리)부터 위로 채움.
/// - 적 턴 중에는 완전히 무시하고 화면 그대로 유지.
/// </summary>
public class PartyQueueController : MonoBehaviour
{
    [Header("Slots (순서 중요: 맨 위/가장 먼 자리 -> 맨 아래/선택 자리)")]
    [SerializeField] private List<BattleCharacterStatusView> _views = new List<BattleCharacterStatusView>();

    [Header("Animation")]
    [SerializeField] private float _duration = 0.35f;
    [SerializeField] private Ease _ease = Ease.OutQuad;

    private readonly List<Vector2> _slotPositions = new List<Vector2>();
    private readonly List<Vector3> _slotScales = new List<Vector3>();

    private bool _isSlotCacheReady;
    private bool _isSubscribed;

    private void Awake()
    {
        CacheSlotTransforms();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
        RefreshInitialState();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void CacheSlotTransforms()
    {
        if (_isSlotCacheReady) return;

        foreach (var view in _views)
        {
            if (view == null)
            {
                _slotPositions.Add(Vector2.zero);
                _slotScales.Add(Vector3.one);
                continue;
            }

            RectTransform rect = view.transform as RectTransform;
            _slotPositions.Add(rect != null ? rect.anchoredPosition : Vector2.zero);
            _slotScales.Add(rect != null ? rect.localScale : Vector3.one);
        }

        _isSlotCacheReady = true;
    }

    private void HandleBattleStarted()
    {
        if (BattleUIContext.Instance == null) return;

        CacheSlotTransforms();

        IReadOnlyList<BattleUnit> party = BattleUIContext.Instance.PartyUnits;
        int emptyCount = _views.Count - party.Count;

        for (int i = 0; i < _views.Count; i++)
        {
            BattleCharacterStatusView view = _views[i];
            if (view == null) continue;

            int partyIndex = i - emptyCount;

            if (partyIndex < 0 || partyIndex >= party.Count)
            {
                view.Clear();
                view.gameObject.SetActive(false);
                continue;
            }

            view.gameObject.SetActive(true);
            view.Bind(party[partyIndex]);

            RectTransform rect = view.transform as RectTransform;
            if (rect != null)
            {
                rect.DOKill();
                rect.anchoredPosition = _slotPositions[i];
                rect.localScale = _slotScales[i];
            }
        }
    }

    /// <summary>
    /// 아군 턴 시작 시: 이번 라운드 고정 턴 순서(죽은 유닛 포함, 라운드 내내 안 바뀜)에서
    /// 지금 유닛을 맨 앞으로 두고 회전. 이 순서 자체는 라운드 끝날 때까지 흔들리지 않음.
    /// </summary>
    private void HandleTurnStarted(BattleUnit unit)
    {
        if (unit == null || unit.TeamType != BattleTeamType.Player) return;
        if (BattleUIContext.Instance == null) return;

        List<BattleUnit> fullOrder = new List<BattleUnit>();
        BattleUIContext.Instance.GetCurrentTurnOrder(fullOrder, true); // 죽은 유닛 포함 - 라운드 고정 순서 유지

        List<(BattleUnit unit, int roundOrderNumber)> allyEntries = new List<(BattleUnit, int)>();
        for (int i = 0; i < fullOrder.Count; i++)
        {
            BattleUnit member = fullOrder[i];
            if (member != null && member.TeamType == BattleTeamType.Player)
            {
                allyEntries.Add((member, i + 1));
            }
        }

        int startIndex = allyEntries.FindIndex(entry => entry.unit == unit);
        if (startIndex < 0) return;

        List<(BattleUnit unit, int roundOrderNumber)> rotated = new List<(BattleUnit, int)>();
        for (int i = 0; i < allyEntries.Count; i++)
        {
            rotated.Add(allyEntries[(startIndex + i) % allyEntries.Count]);
        }

        AnimateQueueToSlots(rotated);
    }

    /// <summary>
    /// rotated(라운드 고정 순서 기준 회전 결과)를 죽음 여부로 나눠서 배치.
    /// - 죽은 아군: 맨 위 자리부터 순서대로 고정
    /// - 살아있는 아군: 맨 아래(선택 자리)부터 위로, rotated 순서(지금 턴 유닛이 맨 앞) 그대로 반영
    /// </summary>
    private void AnimateQueueToSlots(List<(BattleUnit unit, int roundOrderNumber)> rotated)
    {
        List<(BattleUnit unit, int roundOrderNumber)> deadEntries = new List<(BattleUnit, int)>();
        List<(BattleUnit unit, int roundOrderNumber)> aliveEntries = new List<(BattleUnit, int)>();

        foreach (var entry in rotated)
        {
            if (entry.unit != null && entry.unit.IsAlive == false)
            {
                deadEntries.Add(entry);
            }
            else
            {
                aliveEntries.Add(entry);
            }
        }

        int deadSlotIndex = 0;

        for (int i = 0; i < deadEntries.Count; i++)
        {
            BattleCharacterStatusView view = _views.Find(v => v.BoundUnit == deadEntries[i].unit);
            if (view == null) continue;

            MoveViewToSlot(view, deadSlotIndex);
            view.UpdateOrder(0, isDead: true);
            deadSlotIndex++;
        }

        int slotCount = _views.Count;
        for (int i = 0; i < aliveEntries.Count; i++)
        {
            var entry = aliveEntries[i];
            BattleCharacterStatusView view = _views.Find(v => v.BoundUnit == entry.unit);
            if (view == null) continue;

            int slotIndex = slotCount - 1 - i;
            MoveViewToSlot(view, slotIndex);
            view.UpdateOrder(entry.roundOrderNumber);
        }

        // rotated(이번 라운드 턴 순서)에 아예 없는 캐릭터 -> 죽은 상태면 이어서 위쪽 슬롯에 배치
        foreach (var view in _views)
        {
            if (view.BoundUnit != null && view.BoundUnit.IsAlive == false && rotated.Exists(e => e.unit == view.BoundUnit) == false)
            {
                MoveViewToSlot(view, deadSlotIndex);
                view.UpdateOrder(0, isDead: true);
                deadSlotIndex++;
            }
        }
    }

    private void MoveViewToSlot(BattleCharacterStatusView view, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slotPositions.Count) return;

        RectTransform rect = view.transform as RectTransform;
        if (rect == null) return;

        rect.DOKill();
        rect.DOAnchorPos(_slotPositions[slotIndex], _duration).SetEase(_ease);
        rect.DOScale(_slotScales[slotIndex], _duration).SetEase(_ease);
    }

    private void TrySubscribe()
    {
        if (_isSubscribed)
        {
            return;
        }

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
        if (_isSubscribed == false)
        {
            return;
        }

        if (BattleUIContext.Instance == null)
        {
            _isSubscribed = false;
            return;
        }

        BattleUIContext.Instance.OnBattleStarted -= HandleBattleStarted;
        BattleUIContext.Instance.OnTurnStarted -= HandleTurnStarted;

        _isSubscribed = false;
    }

    private void RefreshInitialState()
    {
        if (BattleUIContext.Instance == null)
        {
            return;
        }

        if (BattleUIContext.Instance.PartyUnits != null &&
            BattleUIContext.Instance.PartyUnits.Count > 0)
        {
            HandleBattleStarted();
        }

        if (BattleUIContext.Instance.CurrentUnit != null)
        {
            HandleTurnStarted(BattleUIContext.Instance.CurrentUnit);
        }
    }
}