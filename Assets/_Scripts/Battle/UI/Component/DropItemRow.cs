using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Result 패널의 획득 아이템 한 줄(Prefab_DropItemRow) 표시 담당.
/// </summary>
public class DropItemRow : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _nameTxt;
    [SerializeField] private TMP_Text _amountTxt;

    /// <summary>
    /// 드롭 결과 데이터를 UI에 반영.
    /// </summary>
    public void SetData(DropResult drop)
    {
        if (drop == null || drop.item == null)
        {
            return;
        }

        if (_icon != null && drop.item.icon != null)
        {
            _icon.sprite = drop.item.icon;
        }

        if (_nameTxt != null)
        {
            _nameTxt.text = drop.item.itemName;
        }

        if (_amountTxt != null)
        {
            _amountTxt.text = $"x {drop.amount}";
        }
    }
}