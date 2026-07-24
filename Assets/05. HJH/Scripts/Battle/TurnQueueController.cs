using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turn 패널 전담. 이번 라운드 전투 참가자 전원(아군+적)을 턴 순서대로 TurnRoot에 나열.
/// - 아군/적 모두의 턴 시작 이벤트에 반응 (PartyQueueController와 달리 적 턴도 무시하지 않음)
/// - 매 턴마다 전체 카드를 다시 생성해서, 지금 턴인 유닛만 Dimmed 꺼진 상태로 표시
/// </summary>
public class TurnQueueController : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private Transform _turnRootParent;
    [SerializeField] private TurnOrderCardView _cardPrefab;

    private readonly List<TurnOrderCardView> _spawnedCards = new List<TurnOrderCardView>();

    private void Start()
    {
        if (BattleUIContext.Instance == null)
        {
            Debug.LogWarning("[TurnQueueController] BattleUIContext.Instance가 null입니다.");
            return;
        }

        BattleUIContext.Instance.OnTurnStarted += HandleTurnStarted;

        if (BattleUIContext.Instance.CurrentUnit != null)
        {
            HandleTurnStarted(BattleUIContext.Instance.CurrentUnit);
        }
    }

    private void OnDestroy()
    {
        if (BattleUIContext.Instance == null) return;

        BattleUIContext.Instance.OnTurnStarted -= HandleTurnStarted;
    }
    /// <summary>
    /// 아군/적 구분 없이 턴이 시작될 때마다 전체 카드를 다시 그림.
    /// </summary>
    private void HandleTurnStarted(BattleUnit currentUnit)
    {
        Debug.Log($"[TurnQueueController] HandleTurnStarted 호출됨: {currentUnit?.UnitName}");   // 임시

        if (BattleUIContext.Instance == null || _turnRootParent == null || _cardPrefab == null)
        {
            Debug.LogWarning($"[TurnQueueController] 조건 실패 - Instance null: {BattleUIContext.Instance == null}, _turnRootParent null: {_turnRootParent == null}, _cardPrefab null: {_cardPrefab == null}");   // 임시
            return;
        }

        ClearCards();

        List<BattleUnit> order = new List<BattleUnit>();
        BattleUIContext.Instance.GetCurrentTurnOrder(order, true);

        Debug.Log($"[TurnQueueController] order.Count: {order.Count}");   // 임시

        foreach (var member in order)
        {
            if (member == null) continue;

            TurnOrderCardView card = Instantiate(_cardPrefab, _turnRootParent);
            card.Bind(member, member == currentUnit);
            _spawnedCards.Add(card);
        }
    }

    private void ClearCards()
    {
        foreach (var card in _spawnedCards)
        {
            if (card != null) Destroy(card.gameObject);
        }
        _spawnedCards.Clear();
    }
}