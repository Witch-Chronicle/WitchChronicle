using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Battle_ItemPrefab에 붙는 뷰 컴포넌트. 아이템 데이터 + 수량 표시, 클릭 시 콜백 전달만 담당.
/// * 클릭했을 때 실제로 뭘 할지(사용 처리 등)는 전혀 모름 - ItemListController가 결정.
/// </summary>
public class BattleItemView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _nameTxt;
    [SerializeField] private TMP_Text _amountTxt;
    [SerializeField] private TMP_Text _descriptionTxt;
    [SerializeField] private Button _button;

    private Action<ItemData> _onClick;
    private ItemData _itemData;

    private void Awake()
    {
        if (_button != null) _button.onClick.AddListener(HandleClick);
    }

    public void Bind(ItemData itemData, int quantity, Action<ItemData> onClick)
    {
        if (itemData == null) return;

        _itemData = itemData;
        _onClick = onClick;

        if (_icon != null) _icon.sprite = itemData.icon;
        if (_nameTxt != null) _nameTxt.text = itemData.itemName;
        if (_amountTxt != null) _amountTxt.text = $"보유 수량 : {quantity}";
        if (_descriptionTxt != null) _descriptionTxt.text = itemData.description;
    }

    private void HandleClick()
    {
        _onClick?.Invoke(_itemData);
    }
}