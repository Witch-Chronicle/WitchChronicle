using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 상점에 진열되는 아이템 슬롯 UI.
/// - 아이콘/이름/가격은 공통으로 항상 표시.
/// - Normal/Selected 오브젝트만 번갈아 활성화되어 선택 상태를 표시 (배경/프레임 등).
/// 클릭하면 DetailSection에 아이템 정보를 바인딩
/// </summary>
[RequireComponent(typeof(Button))]
public class ShopItemSlot : MonoBehaviour
{
    [Header("공통 (항상 표시)")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _priceText;

    [Header("선택 상태 표시 (Normal/Selected 번갈아 활성화)")]
    [SerializeField] private GameObject _normalObject;
    [SerializeField] private GameObject _selectedObject;

    [SerializeField] private Button _slotButton;

    public ItemData ItemData { get; private set; }

    private Action<ItemData> _onClickCallback;

    private void Awake()
    {
        if (_slotButton == null)
        {
            _slotButton = GetComponent<Button>();
        }

        _slotButton.onClick.AddListener(HandleClick);

        SetSelected(false);
    }

    /// <summary>
    /// 아이템 데이터를 받아서 슬롯 UI를 채워준다.
    /// onClick: 슬롯 클릭 시 호출할 콜백 (ShopUIController가 DetailSection 갱신용으로 넘겨줌)
    /// </summary>
    public void Setup(ItemData itemData, Action<ItemData> onClick)
    {
        ItemData = itemData;
        _onClickCallback = onClick;

        if (_iconImage != null) _iconImage.sprite = itemData.icon;
        if (_nameText != null) _nameText.text = itemData.itemName;
        if (_priceText != null) _priceText.text = itemData.buyPrice.ToString();

        SetSelected(false);
    }

    /// <summary>
    /// 선택 상태 토글. true면 Selected만 활성, false면 Normal만 활성.
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (_normalObject != null) _normalObject.SetActive(!isSelected);
        if (_selectedObject != null) _selectedObject.SetActive(isSelected);
    }

    private void HandleClick()
    {
        _onClickCallback?.Invoke(ItemData);
    }
}