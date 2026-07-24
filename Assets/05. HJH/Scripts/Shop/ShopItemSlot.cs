using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 상점에 진열되는 아이템 슬롯 UI.
/// 클릭하면 DetailSection에 아이템 정보를 바인딩
/// </summary>
[RequireComponent(typeof(Button))]
public class ShopItemSlot : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _priceText;
    [SerializeField] private Button _slotButton;

    // 나중에 구매 버튼 로직을 만들 때 이 데이터를 그대로 사용하면 됨
    public ItemData ItemData { get; private set; }

    private Action<ItemData> _onClickCallback;

    private void Awake()
    {
        if (_slotButton == null)
        {
            _slotButton = GetComponent<Button>();
        }

        _slotButton.onClick.AddListener(HandleClick);
    }

    /// <summary>
    /// 아이템 데이터를 받아서 슬롯 UI를 채워준다.
    /// onClick: 슬롯 클릭 시 호출할 콜백 (ShopUIController가 DetailSection 갱신용으로 넘겨줌)
    /// </summary>
    public void Setup(ItemData itemData, Action<ItemData> onClick)
    {
        ItemData = itemData;
        _onClickCallback = onClick;

        if (_iconImage != null)
        {
            _iconImage.sprite = itemData.icon;
        }

        if (_nameText != null)
        {
            _nameText.text = itemData.itemName;
        }

        if (_priceText != null)
        {
            _priceText.text = itemData.buyPrice.ToString();
        }
    }

    private void HandleClick()
    {
        _onClickCallback?.Invoke(ItemData);
    }
}