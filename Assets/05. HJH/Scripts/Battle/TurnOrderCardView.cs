using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Prefab_TurnOrder에 붙는 뷰. 전투 참가자 전원(아군+적)의 턴 순서 카드 하나를 표시.
/// - Prefab_TurnOrder 자신의 Image 색상으로 팀 구분(아군 파랑/적 빨강)
/// - Dimmed: 본인 턴이 아니면 켜짐(어두워짐), 본인 턴이면 꺼짐
/// - Death: 죽은 유닛이면 켜짐. 죽었으면 Dimmed도 항상 같이 켜짐(본인 턴이 될 수 없으므로).
/// - NameTxt: 죽었거나 본인 턴이 아니면(Dimmed 상태) 알파를 110/255로 낮춰서 흐리게 표시.
/// - Icon: 아직 캐릭터 아이콘 데이터가 없어서 항상 비활성화 유지
/// </summary>
public class TurnOrderCardView : MonoBehaviour
{
    [SerializeField] private Image _cardImage;
    [SerializeField] private GameObject _icon;
    [SerializeField] private GameObject _dimmed;
    [SerializeField] private GameObject _death;
    [SerializeField] private TMP_Text _nameTxt;

    [Header("Team Colors")]
    [SerializeField] private Color _playerColor = Color.blue;
    [SerializeField] private Color _enemyColor = Color.red;

    [Header("Dimmed NameTxt Alpha")]
    [SerializeField] private float _dimmedAlpha = 110f / 255f;
    [SerializeField] private float _normalAlpha = 1f;

    public void Bind(BattleUnit unit, bool isCurrentTurn)
    {
        if (unit == null) return;

        bool isDead = unit.IsAlive == false;
        bool isDimmed = isDead || isCurrentTurn == false;

        if (_nameTxt != null)
        {
            _nameTxt.text = unit.UnitName;
            _nameTxt.alpha = isDimmed ? _dimmedAlpha : _normalAlpha;
        }

        if (_cardImage != null)
        {
            _cardImage.color = unit.TeamType == BattleTeamType.Player ? _playerColor : _enemyColor;
        }

        if (_icon != null)
        {
            _icon.SetActive(false); // 아직 캐릭터 아이콘 데이터 없음
        }

        if (_death != null)
        {
            _death.SetActive(isDead);
        }

        if (_dimmed != null)
        {
            _dimmed.SetActive(isDimmed);
        }
    }
}