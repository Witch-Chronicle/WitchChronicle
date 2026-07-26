using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// IntergrationPanel/Inventory 쪽 SlotPrefab에 붙는 스크립트.
/// 아이콘 / 이름 / 보유 수량(또는 강화 단계)을 표시.
/// 클릭하면 ItemDetailPanel에 아이템 정보를 바인딩해줌.
/// - 소비/재료/씨앗/퀘스트: Setup(ItemData, quantity, ...) 사용 -> "x3" 형식으로 수량 표시
/// - 장비: Setup(EquipmentInstance, ...) 사용 -> "+3" 형식으로 강화 단계 표시
/// * ShopItemSlot과는 별개 (상점은 가격 표시, 인벤토리는 수량/강화 표시)
/// </summary>
[RequireComponent(typeof(Button))]
public class InventoryItemSlot : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _amountText;
    [SerializeField] private Button _slotButton;

    public ItemData ItemData { get; private set; }
    public EquipmentInstance EquipmentInstance { get; private set; }

    // 클릭 시 호출할 콜백을 인자 없는 클로저로 감싸서 저장 (ItemData/EquipmentInstance 두 경우 모두 대응)
    private Action _onClickCallback;

    private void Awake()
    {
        if (_slotButton == null)
        {
            _slotButton = GetComponent<Button>();
        }

        _slotButton.onClick.AddListener(HandleClick);
    }

    /// <summary>
    /// 소비/재료/씨앗/퀘스트 아이템용. 수량을 "x3" 형식으로 표시.
    /// </summary>
    public void Setup(ItemData itemData, int quantity, Action<ItemData> onClick)
    {
        ItemData = itemData;
        EquipmentInstance = null;

        SetIconAndName(itemData);

        if (_amountText != null)
        {
            _amountText.text = $"x{quantity}";
        }

        _onClickCallback = () => onClick?.Invoke(itemData);
    }

    /// <summary>
    /// 장비 개체용. 강화 단계가 1 이상이면 이름 뒤에 "+n"을 붙여서 표시.
    /// </summary>
    public void Setup(EquipmentInstance equipmentInstance, Action<EquipmentInstance> onClick)
    {
        EquipmentInstance = equipmentInstance;
        ItemData = equipmentInstance.baseData;

        if (_iconImage != null)
        {
            _iconImage.sprite = equipmentInstance.baseData.icon;
        }

        if (_nameText != null)
        {
            string baseName = equipmentInstance.baseData.itemName;
            _nameText.text = equipmentInstance.enhanceLevel > 0
                ? $"{baseName} +{equipmentInstance.enhanceLevel}"
                : baseName;
        }

        // 장비는 수량 개념이 없으므로 amountText는 비워둠
        if (_amountText != null)
        {
            _amountText.text = string.Empty;
        }

        _onClickCallback = () => onClick?.Invoke(equipmentInstance);
    }

    private void SetIconAndName(ItemData itemData)
    {
        if (_iconImage != null)
        {
            _iconImage.sprite = itemData.icon;
        }

        if (_nameText != null)
        {
            _nameText.text = itemData.itemName;
        }
    }

    private void HandleClick()
    {
        _onClickCallback?.Invoke();
    }
}