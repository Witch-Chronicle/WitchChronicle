using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Characters 오브젝트에 붙어서 아군 파티 상태 표시를 담당.
/// - 지금 턴인 아군 -> SelectedCharacter/Selected_BattleCharacter
/// - 나머지 아군 -> CharacterList의 슬롯들 (미리 배치된 인스턴스 재사용, 파티 수만큼만 활성화)
/// - 적 턴일 때는 SelectedCharacter 비우고, 아군 전체를 CharacterList에 표시
/// </summary>
public class PartyStatusController : MonoBehaviour
{
    [Header("Selected (지금 턴인 아군)")]
    [SerializeField] private BattleCharacterStatusView _selectedView;

    [Header("Character List (턴 아닌 아군들, 미리 배치된 슬롯 재사용)")]
    [SerializeField] private List<BattleCharacterStatusView> _listSlots = new List<BattleCharacterStatusView>();

    private void OnEnable()
    {
        if (BattleUIContext.Instance != null)
        {
            BattleUIContext.Instance.OnTurnStarted += HandleTurnStarted;
        }
    }

    private void OnDisable()
    {
        if (BattleUIContext.Instance != null)
        {
            BattleUIContext.Instance.OnTurnStarted -= HandleTurnStarted;
        }
    }

    private void HandleTurnStarted(BattleUnit unit)
    {
        if (BattleUIContext.Instance == null) return;

        IReadOnlyList<BattleUnit> party = BattleUIContext.Instance.PartyUnits;
        bool isPlayerTurn = unit != null && unit.TeamType == BattleTeamType.Player;

        if (isPlayerTurn)
        {
            if (_selectedView != null)
            {
                _selectedView.gameObject.SetActive(true);
                _selectedView.Bind(unit);
            }
        }
        else if (_selectedView != null)
        {
            _selectedView.Clear();
            _selectedView.gameObject.SetActive(false);
        }

        List<BattleUnit> remaining = party
            .Where(member => member != null && (isPlayerTurn == false || member != unit))
            .ToList();

        for (int i = 0; i < _listSlots.Count; i++)
        {
            BattleCharacterStatusView slot = _listSlots[i];
            if (slot == null) continue;

            if (i < remaining.Count)
            {
                slot.gameObject.SetActive(true);
                slot.Bind(remaining[i]);
            }
            else
            {
                slot.Clear();
                slot.gameObject.SetActive(false);
            }
        }
    }
}