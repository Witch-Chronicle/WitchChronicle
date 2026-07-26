using UnityEngine;
using TMPro;

/// <summary>
/// EnhancePanel/Enhance/Execute/EnhanceData/Required 쪽 Prefab_EnhanceRequiredRow에 붙는 스크립트.
/// 항목 이름 + "현재 보유량 / 필요량"을 표시. 보유량 충분하면 초록, 부족하면 빨강.
/// </summary>
public class EnhanceRequiredRow : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private TextMeshProUGUI _itemTxt;
    [SerializeField] private TextMeshProUGUI _currentValue;
    [SerializeField] private TextMeshProUGUI _requiredValue;

    private static readonly Color _enoughColor = Color.green;
    private static readonly Color _notEnoughColor = Color.red;

    public void Setup(string itemLabel, int currentAmount, int requiredAmount)
    {
        if (_itemTxt != null)
        {
            _itemTxt.text = itemLabel;
        }

        if (_currentValue != null)
        {
            _currentValue.text = currentAmount.ToString();
            _currentValue.color = currentAmount >= requiredAmount ? _enoughColor : _notEnoughColor;
        }

        if (_requiredValue != null)
        {
            _requiredValue.text = requiredAmount.ToString();
        }
    }
}