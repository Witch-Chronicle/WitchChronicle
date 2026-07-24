using UnityEngine;
using TMPro;

/// <summary>
/// StatSection/Change 쪽 스탯 행 하나 (HpEquipStatRow 등).
/// 값(ValueTxt) + 증감 아이콘(IncreaseTxtIcon/DecreaseTxtIcon) + 증감량(ChangedTxt)을 표시.
/// </summary>
public class EquipStatChangeRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _valueTxt;
    [SerializeField] private GameObject _increaseTxtIcon;
    [SerializeField] private GameObject _decreaseTxtIcon;
    [SerializeField] private TextMeshProUGUI _changedTxt;

    private static readonly Color _increaseColor = Color.green;
    private static readonly Color _decreaseColor = Color.red;

    /// <summary>
    /// newValue: 장착했을 때의 예상 총합. change: newValue - 현재값 (양수면 증가, 음수면 감소, 0이면 변동없음)
    /// </summary>
    public void SetValue(int newValue, int change)
    {
        if (_valueTxt != null)
        {
            _valueTxt.text = newValue.ToString();
        }

        if (change > 0)
        {
            if (_increaseTxtIcon != null) _increaseTxtIcon.SetActive(true);
            if (_decreaseTxtIcon != null) _decreaseTxtIcon.SetActive(false);

            if (_changedTxt != null)
            {
                _changedTxt.gameObject.SetActive(true);
                _changedTxt.color = _increaseColor;
                _changedTxt.text = $"{change}";
            }
        }
        else if (change < 0)
        {
            if (_increaseTxtIcon != null) _increaseTxtIcon.SetActive(false);
            if (_decreaseTxtIcon != null) _decreaseTxtIcon.SetActive(true);

            if (_changedTxt != null)
            {
                _changedTxt.gameObject.SetActive(true);
                _changedTxt.color = _decreaseColor;
                _changedTxt.text = change.ToString(); // 이미 음수라 "-" 자동 포함
            }
        }
        else
        {
            if (_increaseTxtIcon != null) _increaseTxtIcon.SetActive(false);
            if (_decreaseTxtIcon != null) _decreaseTxtIcon.SetActive(false);
            if (_changedTxt != null) _changedTxt.gameObject.SetActive(false);
        }
    }
}