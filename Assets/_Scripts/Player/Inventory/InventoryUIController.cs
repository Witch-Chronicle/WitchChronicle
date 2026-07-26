using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PlayerInventory가 들고 있는 보유 아이템 목록을 IntergrationPanel/Inventory UI에 뿌려주는 역할.
/// - CategorySection(EquipBtn/ConsumeBtn/MaterialBtn/QuestBtn) 선택 -> 해당 타입 아이템만 필터링
/// - InventorySection/Content에 InventoryItemSlot 생성
/// - 선택된 카테고리 버튼은 배경/텍스트 색을 다르게 표시
/// </summary>
public class InventoryUIController : MonoBehaviour
{
    [Header("Category Btns")]
    [SerializeField] private Button _equipCategoryBtn;
    [SerializeField] private Button _consumeCategoryBtn;
    [SerializeField] private Button _materialCategoryBtn;
    [SerializeField] private Button _questCategoryBtn;
    [SerializeField] private Button _seedCategoryBtn;

    [Header("Category Btn 색상")]
    [SerializeField] private Color _normalBackgroundColor = Color.white;
    [SerializeField] private Color _normalTextColor = Color.black;
    [SerializeField] private Color _selectedBackgroundColor = Color.gray;
    [SerializeField] private Color _selectedTextColor = Color.white;

    [Header("Close Btn")]
    [SerializeField] private Button _closeBtn;

    [Header("Item List")]
    [SerializeField] private InventoryItemSlot _itemSlotPrefab;
    [SerializeField] private Transform _itemSlotParent; // Content

    [Header("Gold Txt")]
    [SerializeField] private TextMeshProUGUI _goldText;

    [Header("Item Detail")]
    [SerializeField] private InventoryDetailController _itemDetailController;

    private readonly List<GameObject> _spawnedItemSlots = new List<GameObject>();
    private ItemType _currentCategory = ItemType.Equipment;

    private void OnEnable()
    {
        _equipCategoryBtn.onClick.AddListener(OnClickEquipCategory);
        _consumeCategoryBtn.onClick.AddListener(OnClickConsumeCategory);
        _materialCategoryBtn.onClick.AddListener(OnClickMaterialCategory);
        _questCategoryBtn.onClick.AddListener(OnClickQuestCategory);
        _seedCategoryBtn.onClick.AddListener(OnClickSeedCategory);
        _closeBtn.onClick.AddListener(OnClickClose);

        // 패널이 켜질 때마다 기본 카테고리(Equip)부터 보여줌
        SelectCategory(ItemType.Equipment);

        // 상세 패널은 애니메이션 없이 숨긴 상태로 초기화
        if (_itemDetailController != null)
        {
            _itemDetailController.HideImmediate();
        }

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnGoldChanged += UpdateGoldText;
            UpdateGoldText(PlayerInventory.Instance.Gold);

            PlayerInventory.Instance.OnInventoryChanged += HandleInventoryChanged;
        }

        // 4명 중 누구든 장착 상태가 바뀌면(장착/해제) 목록도 같이 갱신되어야 함 (static 이벤트)
        CharacterEquipment.OnAnyEquipmentChanged += HandleInventoryChanged;
    }

    private void OnDisable()
    {
        _equipCategoryBtn.onClick.RemoveListener(OnClickEquipCategory);
        _consumeCategoryBtn.onClick.RemoveListener(OnClickConsumeCategory);
        _materialCategoryBtn.onClick.RemoveListener(OnClickMaterialCategory);
        _questCategoryBtn.onClick.RemoveListener(OnClickQuestCategory);
        _seedCategoryBtn.onClick.RemoveListener(OnClickSeedCategory);
        _closeBtn.onClick.RemoveListener(OnClickClose);

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnGoldChanged -= UpdateGoldText;
            PlayerInventory.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }

        CharacterEquipment.OnAnyEquipmentChanged -= HandleInventoryChanged;
    }

    private void OnClickEquipCategory() => SelectCategory(ItemType.Equipment);
    private void OnClickConsumeCategory() => SelectCategory(ItemType.Consumable);
    private void OnClickMaterialCategory() => SelectCategory(ItemType.Material);
    private void OnClickQuestCategory() => SelectCategory(ItemType.KeyItem);
    private void OnClickSeedCategory() => SelectCategory(ItemType.SeedItem);

    private void OnClickClose()
    {
        // 닫기 전에 클릭 이벤트 제거
        _closeBtn.onClick.RemoveListener(OnClickClose);

        UITestInputReader.Instance.ToggleIntegrationPanel();
    }

    /// <summary>
    /// 선택된 카테고리에 해당하는 보유 아이템만 걸러서 Content에 뿌려준다.
    /// </summary>
    private void SelectCategory(ItemType category)
    {
        _currentCategory = category;

        // 카테고리 바꿀 때는 상세 패널 닫기
        if (_itemDetailController != null)
        {
            _itemDetailController.Hide();
        }

        UpdateCategoryHighlight();
        RefreshList();
    }

    /// <summary>
    /// 지금 선택된 카테고리 버튼만 강조 색상으로, 나머지는 기본 색상으로.
    /// </summary>
    private void UpdateCategoryHighlight()
    {
        SetButtonHighlighted(_equipCategoryBtn, _currentCategory == ItemType.Equipment);
        SetButtonHighlighted(_consumeCategoryBtn, _currentCategory == ItemType.Consumable);
        SetButtonHighlighted(_materialCategoryBtn, _currentCategory == ItemType.Material);
        SetButtonHighlighted(_questCategoryBtn, _currentCategory == ItemType.KeyItem);
        SetButtonHighlighted(_seedCategoryBtn, _currentCategory == ItemType.SeedItem);
    }

    private void SetButtonHighlighted(Button button, bool isSelected)
    {
        if (button == null) return;

        Image background = button.GetComponent<Image>();
        if (background != null)
        {
            background.color = isSelected ? _selectedBackgroundColor : _normalBackgroundColor;
        }

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.color = isSelected ? _selectedTextColor : _normalTextColor;
        }
    }

    /// <summary>
    /// 인벤토리 내용이 바뀌었을 때(구매/판매 등) 호출. 상세 패널은 건드리지 않고 목록만 갱신.
    /// </summary>
    private void HandleInventoryChanged()
    {
        RefreshList();
    }

    private void RefreshList()
    {
        ClearItemSlots();

        if (PlayerInventory.Instance == null || _itemSlotPrefab == null || _itemSlotParent == null)
        {
            return;
        }

        if (_currentCategory == ItemType.Equipment)
        {
            RefreshEquipmentList();
        }
        else
        {
            RefreshStackableList();
        }
    }

    /// <summary>
    /// 장비는 EquipmentInstances(개체 단위)에서 가져와서 뿌린다. 강화 단계가 달라도 각각 따로 슬롯 생성.
    /// 정렬 없이 인벤토리에 들어온 순서 그대로 표시.
    /// * 4명 중 누구든 장착 중인 장비는 목록에서 제외 (해당 캐릭터의 Equip 화면에 이미 표시되고 있음)
    /// </summary>
    private void RefreshEquipmentList()
    {
        var equipmentList = PlayerInventory.Instance.EquipmentInstances
            .Where(instance => !CharacterEquipment.IsEquippedByAnyone(instance));

        foreach (var instance in equipmentList)
        {
            InventoryItemSlot itemSlot = Instantiate(_itemSlotPrefab, _itemSlotParent);
            itemSlot.Setup(instance, HandleEquipmentSlotClicked);
            _spawnedItemSlots.Add(itemSlot.gameObject);
        }
    }

    /// <summary>
    /// 소비/재료/씨앗/퀘스트는 기존처럼 InventorySlots(수량 기반)에서 가져와서 뿌린다.
    /// 정렬 없이 인벤토리에 들어온 순서 그대로 표시.
    /// </summary>
    private void RefreshStackableList()
    {
        var filtered = PlayerInventory.Instance.InventorySlots
            .Where(slot => slot.ItemData.itemType == _currentCategory);

        foreach (var slot in filtered)
        {
            InventoryItemSlot itemSlot = Instantiate(_itemSlotPrefab, _itemSlotParent);
            itemSlot.Setup(slot.ItemData, slot.Quantity, HandleItemSlotClicked);
            _spawnedItemSlots.Add(itemSlot.gameObject);
        }
    }

    private void ClearItemSlots()
    {
        foreach (var slotObj in _spawnedItemSlots)
        {
            Destroy(slotObj);
        }
        _spawnedItemSlots.Clear();
    }

    private void UpdateGoldText(int gold)
    {
        if (_goldText != null)
        {
            _goldText.text = gold.ToString();
        }
    }

    private void HandleItemSlotClicked(ItemData itemData)
    {
        if (_itemDetailController != null)
        {
            _itemDetailController.Show(itemData);
        }
    }

    private void HandleEquipmentSlotClicked(EquipmentInstance equipmentInstance)
    {
        if (_itemDetailController != null)
        {
            _itemDetailController.Show(equipmentInstance);
        }
    }
}