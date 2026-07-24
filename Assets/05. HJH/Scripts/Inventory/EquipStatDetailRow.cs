using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 장비 상세정보의 스탯 목록에서 한 줄(행)을 표시하는 프리팹 스크립트.
/// Shop / Inventory 양쪽의 장비 상세정보에서 공용으로 사용.
/// </summary>
public class EquipStatDetailRow : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _labelText;
    [SerializeField] private TextMeshProUGUI _valueText;

    public void Setup(string label, string valueDisplayText)
    {
        if (_labelText != null)
        {
            _labelText.text = label;
        }

        if (_valueText != null)
        {
            _valueText.text = valueDisplayText;
        }
    }
}