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
///
/// RecycledScrollView(InventoryScrollView)가 InventorySlotEntry 단위로 재사용/바인딩합니다.
/// Bind(InventorySlotEntry, int)는 IsEquipment 값을 보고 기존 Setup 두 개 중 하나로 그대로 위임합니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class InventoryItemSlot : MonoBehaviour, IRecycledScrollCell<InventorySlotEntry>
{
    [Header("UI 연결")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _gradeImage;
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
    /// RecycledScrollView가 셀을 재사용/재배치할 때마다 호출합니다.
    /// </summary>
    public void Bind(InventorySlotEntry entry, int index)
    {
        if (entry.IsEquipment)
        {
            Setup(entry.EquipmentInstance, entry.OnEquipmentClicked, entry.GradeIcon);
        }
        else
        {
            Setup(entry.ItemData, entry.Quantity, entry.OnItemClicked, entry.GradeIcon);
        }
    }
    /// <summary>
    /// 소비/재료/씨앗/퀘스트 아이템용. 수량을 "x3" 형식으로 표시.
    /// </summary>
    public void Setup(ItemData itemData, int quantity, Action<ItemData> onClick, Sprite gradeIcon)
    {
        ItemData = itemData;
        EquipmentInstance = null;
        SetIconAndName(itemData);
        SetGradeIcon(gradeIcon);
        if (_amountText != null)
        {
            _amountText.text = $"x{quantity}";
        }
        _onClickCallback = () => onClick?.Invoke(itemData);
    }
    /// <summary>
    /// 장비 개체용. 강화 단계가 1 이상이면 이름 뒤에 "+n"을 붙여서 표시.
    /// </summary>
    public void Setup(EquipmentInstance equipmentInstance, Action<EquipmentInstance> onClick, Sprite gradeIcon)
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
        SetGradeIcon(gradeIcon);
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
    /// <summary>
    /// 아이템 등급에 해당하는 이미지를 표시. 없으면 이미지 자체를 비활성화.
    /// </summary>
    private void SetGradeIcon(Sprite gradeIcon)
    {
        if (_gradeImage == null) return;
        _gradeImage.sprite = gradeIcon;
        _gradeImage.enabled = gradeIcon != null;
    }
}