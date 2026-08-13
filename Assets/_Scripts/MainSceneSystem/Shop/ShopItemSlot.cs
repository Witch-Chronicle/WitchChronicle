using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
/// <summary>
/// 상점에 진열되는 아이템 슬롯 UI.
/// - 아이콘/이름/가격은 공통으로 항상 표시.
/// - Normal/Selected 오브젝트만 번갈아 활성화되어 선택 상태를 표시 (배경/프레임 등).
/// 클릭하면 DetailSection에 아이템 정보를 바인딩
///
/// RecycledScrollView(ShopScrollView)가 ShopSlotEntry 단위로 재사용/바인딩합니다.
/// 선택 상태는 셀이 재사용되므로 매번 entry.IsSelected 값을 그대로 반영합니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class ShopItemSlot : MonoBehaviour, IRecycledScrollCell<ShopSlotEntry>
{
    [Header("공통 (항상 표시)")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _priceText;
    [Header("선택 상태 표시 (Normal/Selected 번갈아 활성화)")]
    [SerializeField] private GameObject _normalObject;
    [SerializeField] private GameObject _selectedObject;
    [SerializeField] private Button _slotButton;
    [Header("품절 표시")]
    [SerializeField] private string _soldOutText = "품절";
    public ItemData ItemData { get; private set; }
    public bool IsSoldOut { get; private set; }
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
    public void Bind(ShopSlotEntry entry, int index)
    {
        Setup(entry.ItemData, entry.OnClicked);
        SetSelected(entry.IsSelected);
    }
    public void Setup(ItemData itemData, Action<ItemData> onClick)
    {
        ItemData = itemData;
        _onClickCallback = onClick;
        IsSoldOut = CheckSoldOut(itemData);
        if (_iconImage != null) _iconImage.sprite = itemData.icon;
        if (_nameText != null) _nameText.text = itemData.itemName;
        if (_priceText != null)
        {
            _priceText.text = IsSoldOut ? _soldOutText : itemData.buyPrice.ToString() + "G";
        }
    }
    /// <summary>
    /// RodItemData는 하나라도 보유하고 있으면 품절 처리한다 (중복 보유 방지).
    /// </summary>
    private bool CheckSoldOut(ItemData itemData)
    {
        if (itemData is not RodItemData) return false;
        if (PlayerInventory.Instance == null) return false;
        return PlayerInventory.Instance.GetTotalQuantity(itemData) > 0;
    }
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