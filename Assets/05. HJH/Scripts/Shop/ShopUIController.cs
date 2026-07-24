using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ShopNPC가 들고 있는 아이템 S.O 목록을 실제 Shop UI에 뿌려주는 역할.
/// - CategorySection(EquipBtn/ConsumeBtn/MaterialBtn) 선택 -> 서브카테고리 버튼 동적 생성
/// - 서브카테고리 선택 -> Contents/Scroll View/Content에 ShopItemSlot 생성
/// - 선택된 대분류 카테고리 버튼은 배경/텍스트 색을 다르게 표시
/// * 구매 로직은 없음. 진열만 담당.
/// </summary>
public class ShopUIController : MonoBehaviour
{
    [Header("Shop NPC")]
    [SerializeField] private ShopNPC _shopNPC;

    [Header("Main Category Btns")]
    [SerializeField] private Button _equipCategoryBtn;
    [SerializeField] private Button _consumeCategoryBtn;
    [SerializeField] private Button _materialCategoryBtn;
    [SerializeField] private Button _seedCategoryBtn;

    [Header("Category Btn 색상")]
    [SerializeField] private Color _normalBackgroundColor = Color.white;
    [SerializeField] private Color _normalTextColor = Color.black;
    [SerializeField] private Color _selectedBackgroundColor = Color.gray;
    [SerializeField] private Color _selectedTextColor = Color.white;

    [Header("Close Btn")]
    [SerializeField] private Button _closeBtn;

    [Header("Sub Category Btns")]
    [SerializeField] private Button _subCategoryButtonPrefab;
    [SerializeField] private Transform _subCategoryParent; // BtnsWrap

    [Header("Item List")]
    [SerializeField] private ShopItemSlot _itemSlotPrefab;
    [SerializeField] private Transform _itemSlotParent; // Content

    [Header("Item Detail")]
    [SerializeField] private ShopDetailController _shopDetailController;

    [Header("Gold Txt")]
    [SerializeField] private TextMeshProUGUI _goldText;

    private readonly List<GameObject> _spawnedSubCategoryButtons = new List<GameObject>();
    private readonly List<GameObject> _spawnedItemSlots = new List<GameObject>();

    private ItemType _currentCategory = ItemType.Equipment;
    private Button _selectedSubCategoryButton;

    private void OnEnable()
    {
        _equipCategoryBtn.onClick.AddListener(OnClickEquipCategory);
        _consumeCategoryBtn.onClick.AddListener(OnClickConsumeCategory);
        _materialCategoryBtn.onClick.AddListener(OnClickMaterialCategory);
        _seedCategoryBtn.onClick.AddListener(OnClickSeedCategory);
        _closeBtn.onClick.AddListener(OnClickClose);

        // 상점이 켜질 때마다 기본 카테고리(Equip)부터 보여줌
        SelectCategory(ItemType.Equipment);

        // 아직 아무 아이템도 선택 안 한 상태로 초기화
        if (_shopDetailController != null)
        {
            _shopDetailController.HideDetail();
        }

        // 골드 실시간 갱신 구독 + 현재 값으로 초기 표시
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnGoldChanged += UpdateGoldText;
            UpdateGoldText(PlayerInventory.Instance.Gold);
        }
    }

    private void OnDisable()
    {
        _equipCategoryBtn.onClick.RemoveListener(OnClickEquipCategory);
        _consumeCategoryBtn.onClick.RemoveListener(OnClickConsumeCategory);
        _materialCategoryBtn.onClick.RemoveListener(OnClickMaterialCategory);
        _seedCategoryBtn.onClick.RemoveListener(OnClickSeedCategory);
        _closeBtn.onClick.RemoveListener(OnClickClose);

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnGoldChanged -= UpdateGoldText;
        }
    }

    void Start()
    {
        FindShopkeeper();
    }

    private void OnClickEquipCategory() => SelectCategory(ItemType.Equipment);
    private void OnClickConsumeCategory() => SelectCategory(ItemType.Consumable);
    private void OnClickMaterialCategory() => SelectCategory(ItemType.Material);
    private void OnClickSeedCategory() => SelectCategory(ItemType.SeedItem);

    private void OnClickClose()
    {
        // 닫기 전에 클릭 이벤트 제거
        _closeBtn.onClick.RemoveListener(OnClickClose);

        _shopNPC.ToggleShop();
    }

    /// <summary>
    /// 대분류 선택 -> 서브카테고리 버튼들을 새로 생성하고, 첫 번째(전체)를 기본으로 보여줌
    /// </summary>
    private void SelectCategory(ItemType category)
    {
        _currentCategory = category;
        UpdateCategoryHighlight();

        List<(string label, List<ItemData> items)> subCategories = BuildSubCategories(category);

        ClearSubCategoryButtons();

        Button firstButton = null;

        foreach (var subCategory in subCategories)
        {
            Button createdButton = CreateSubCategoryButton(subCategory.label, subCategory.items);
            if (firstButton == null)
            {
                firstButton = createdButton;
            }
        }

        // 대분류를 새로 고를 때마다 첫 번째("전체") 서브카테고리를 기본 선택 상태로 표시
        SetSubCategorySelected(firstButton);
        ShowItems(subCategories.Count > 0 ? subCategories[0].items : new List<ItemData>());
    }

    /// <summary>
    /// 지금 선택된 대분류 카테고리 버튼만 강조 색상으로, 나머지는 기본 색상으로.
    /// </summary>
    private void UpdateCategoryHighlight()
    {
        SetButtonHighlighted(_equipCategoryBtn, _currentCategory == ItemType.Equipment);
        SetButtonHighlighted(_consumeCategoryBtn, _currentCategory == ItemType.Consumable);
        SetButtonHighlighted(_materialCategoryBtn, _currentCategory == ItemType.Material);
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
    /// 지금 선택된 서브카테고리 버튼만 강조 색상으로, 나머지는 기본 색상으로.
    /// </summary>
    private void SetSubCategorySelected(Button selected)
    {
        _selectedSubCategoryButton = selected;

        foreach (var buttonObj in _spawnedSubCategoryButtons)
        {
            Button button = buttonObj.GetComponent<Button>();
            SetButtonHighlighted(button, button == selected);
        }
    }

    /// <summary>
    /// 카테고리별 서브카테고리 구성 (라벨 + 해당 아이템 목록).
    /// 서브카테고리 이름/구성은 기획에 맞게 자유롭게 수정 가능.
    /// </summary>
    private List<(string label, List<ItemData> items)> BuildSubCategories(ItemType category)
    {
        var result = new List<(string, List<ItemData>)>();

        if (_shopNPC == null)
        {
            Debug.LogWarning("[ShopUIController] shopNPC가 연결되지 않았습니다.");
            return result;
        }

        switch (category)
        {
            case ItemType.Equipment:
                var weapons = _shopNPC.WeaponItems.Cast<ItemData>().ToList();
                var armors = _shopNPC.ArmorItems.Cast<ItemData>().ToList();
                var accessories = _shopNPC.AccessoryItems.Cast<ItemData>().ToList();

                result.Add(("전체", weapons.Concat(armors).Concat(accessories).ToList()));
                result.Add(("무기", weapons));
                result.Add(("방어구", armors));
                result.Add(("장신구", accessories));
                break;

            case ItemType.Consumable:
                var consumables = _shopNPC.ConsumableItems.Cast<ItemData>().ToList();
                result.Add(("전체", consumables));

                foreach (ConsumableType type in Enum.GetValues(typeof(ConsumableType)))
                {
                    var filtered = _shopNPC.ConsumableItems
                        .Where(item => item.consumableType == type)
                        .Cast<ItemData>()
                        .ToList();

                    if (filtered.Count > 0)
                    {
                        result.Add((type.ToString(), filtered));
                    }
                }
                break;

            case ItemType.Material:
                var materials = _shopNPC.MaterialItems.Cast<ItemData>().ToList();
                result.Add(("전체", materials));

                foreach (MaterialType type in Enum.GetValues(typeof(MaterialType)))
                {
                    var filtered = _shopNPC.MaterialItems
                        .Where(item => item.materialType == type)
                        .Cast<ItemData>()
                        .ToList();

                    if (filtered.Count > 0)
                    {
                        result.Add((type.ToString(), filtered));
                    }
                }
                break;

            case ItemType.SeedItem:
                var seeds = _shopNPC.SeedItems.Cast<ItemData>().ToList();
                result.Add(("전체", seeds));
                break;
        }

        return result;
    }

    private Button CreateSubCategoryButton(string label, List<ItemData> items)
    {
        if (_subCategoryButtonPrefab == null || _subCategoryParent == null)
        {
            return null;
        }

        Button newButton = Instantiate(_subCategoryButtonPrefab, _subCategoryParent);

        TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = label;
        }

        newButton.onClick.AddListener(() =>
        {
            SetSubCategorySelected(newButton);
            ShowItems(items);
        });

        _spawnedSubCategoryButtons.Add(newButton.gameObject);

        return newButton;
    }

    private void ClearSubCategoryButtons()
    {
        foreach (var buttonObj in _spawnedSubCategoryButtons)
        {
            Destroy(buttonObj);
        }
        _spawnedSubCategoryButtons.Clear();
        _selectedSubCategoryButton = null;
    }

    /// <summary>
    /// 선택된 아이템 목록을 Content에 슬롯으로 뿌려준다. itemId 오름차순으로 정렬해서 표시.
    /// </summary>
    private void ShowItems(List<ItemData> items)
    {
        ClearItemSlots();

        if (_itemSlotPrefab == null || _itemSlotParent == null)
        {
            return;
        }

        var sortedItems = items.OrderBy(item => item.itemId);

        foreach (var itemData in sortedItems)
        {
            ShopItemSlot slot = Instantiate(_itemSlotPrefab, _itemSlotParent);
            slot.Setup(itemData, HandleItemSlotClicked);
            _spawnedItemSlots.Add(slot.gameObject);
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

    private void HandleItemSlotClicked(ItemData itemData)
    {
        if (_shopDetailController != null)
        {
            _shopDetailController.ShowItemDetail(itemData);
        }
    }

    private void UpdateGoldText(int gold)
    {
        if (_goldText != null)
        {
            _goldText.text = gold.ToString();
        }
    }

    // 추가, 던전에서 생성된 ShopNPC 를 런타임 중에 가져오기 위해서
    private void FindShopkeeper()
    {
        if(_shopNPC == null)
        {
            _shopNPC = FindAnyObjectByType<ShopNPC>();
        }
    }
}