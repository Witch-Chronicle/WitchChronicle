using UnityEngine;
using TMPro;

/// <summary>
/// StatSection/Current 쪽 스탯 행 하나 (HpEquipStatRow 등).
/// </summary>
public class EquipStatRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _valueTxt;

    public void SetValue(int value)
    {
        if (_valueTxt != null)
        {
            _valueTxt.text = value.ToString();
        }
    }
}