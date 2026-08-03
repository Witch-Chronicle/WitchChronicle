using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Pool;
using TMPro;

/// <summary>
/// ShopNPC가 들고 있는 판매 아이템 목록(List&lt;ItemData&gt;)을 Shop UI에 뿌려주는 역할.
/// - MainCategory 4종 버튼은 항상 고정 배치하되, 그 상점이 실제로 판매하는 Main만 표시.
/// - Main 클릭 시, 그 상점이 그 Main 안에서 실제로 파는 SubCategory 버튼만 동적 생성(+"전체"),
///   판매하지 않는 서브카테고리는 아예 생성하지 않음.
/// - 아이템 슬롯(ShopItemSlot)은 ObjectPool로 재사용.
/// * 구매 로직은 없음. 진열만 담당.
/// </summary>
public class ShopUIController : MonoBehaviour
{
    [Serializable]
    private class MainCategoryButton
    {
        public MainCategory mainCategory;
        public Button mainButton;
    }

    [Header("Shop NPC")]
    [SerializeField] private ShopNPC _shopNPC;

    [Header("Main Category Btns (항상 고정 배치, 판매 여부에 따라 표시/숨김)")]
    [SerializeField] private List<MainCategoryButton> _mainButtons = new List<MainCategoryButton>();

    [Header("Category Btn 색상")]
    [SerializeField] private Color _normalBackgroundColor = Color.white;
    [SerializeField] private Color _normalTextColor = Color.black;
    [SerializeField] private Color _selectedBackgroundColor = Color.gray;
    [SerializeField] private Color _selectedTextColor = Color.white;

    [Header("Close Btn")]
    [SerializeField] private Button _closeBtn;

    [Header("Sub Category Btns (동적 생성, 실제 판매하는 서브카테고리만)")]
    [SerializeField] private Button _subCategoryButtonPrefab;
    [SerializeField] private Transform _subCategoryParent; // BtnsWrap

    [Header("Item List")]
    [SerializeField] private ShopItemSlot _itemSlotPrefab;
    [SerializeField] private Transform _itemSlotParent; // Content

    [Header("Item Slot Pool")]
    [SerializeField] private int _poolDefaultCapacity = 20;
    [SerializeField] private int _poolMaxSize = 200;

    [Header("Item Detail")]
    [SerializeField] private ShopDetailController _shopDetailController;

    [Header("Gold Txt")]
    [SerializeField] private TextMeshProUGUI _goldText;

    private readonly List<GameObject> _spawnedSubCategoryButtons = new List<GameObject>();

    private ObjectPool<ShopItemSlot> _slotPool;
    private readonly List<ShopItemSlot> _activeSlots = new List<ShopItemSlot>();

    private List<ItemData> _currentSellItems = new List<ItemData>();

    private MainCategory _currentMainCategory;
    private Button _selectedSubCategoryButton;

    private void Awake()
    {
        _slotPool = new ObjectPool<ShopItemSlot>(
            createFunc: CreateSlot,
            actionOnGet: OnGetSlot,
            actionOnRelease: OnReleaseSlot,
            actionOnDestroy: OnDestroySlot,
            collectionCheck: true,
            defaultCapacity: _poolDefaultCapacity,
            maxSize: _poolMaxSize);
    }

    private void OnEnable()
    {
        for (int i = 0; i < _mainButtons.Count; i++)
        {
            MainCategoryButton entry = _mainButtons[i];
            if (entry.mainButton == null) continue;

            MainCategory captured = entry.mainCategory;
            entry.mainButton.onClick.AddListener(() => OnClickMainCategory(captured));
        }

        if (_closeBtn != null)
        {
            _closeBtn.onClick.AddListener(OnClickClose);
        }

        FindShopkeeper();

        _currentSellItems = _shopNPC != null
            ? _shopNPC.SellItems.Where(item => item != null).ToList()
            : new List<ItemData>();

        RefreshMainButtonAvailability();

        MainCategory? firstAvailable = _mainButtons
            .Where(b => _currentSellItems.Any(item => item.mainCategory == b.mainCategory))
            .Select(b => (MainCategory?)b.mainCategory)
            .FirstOrDefault();

        if (firstAvailable.HasValue)
        {
            SelectMainCategory(firstAvailable.Value);
        }
        else
        {
            ClearSubCategoryButtons();
            ReleaseAllSlots();
        }

        if (_shopDetailController != null)
        {
            _shopDetailController.HideDetail();
        }

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnGoldChanged += UpdateGoldText;
            UpdateGoldText(PlayerInventory.Instance.Gold);
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < _mainButtons.Count; i++)
        {
            _mainButtons[i].mainButton?.onClick.RemoveAllListeners();
        }

        if (_closeBtn != null)
        {
            _closeBtn.onClick.RemoveListener(OnClickClose);
        }

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnGoldChanged -= UpdateGoldText;
        }
    }

    private void OnDestroy()
    {
        _slotPool?.Dispose();
    }

    // ===================== Object Pool =====================

    private ShopItemSlot CreateSlot()
    {
        return Instantiate(_itemSlotPrefab, _itemSlotParent);
    }

    private void OnGetSlot(ShopItemSlot slot)
    {
        slot.gameObject.SetActive(true);
        slot.transform.SetAsLastSibling();
    }

    private void OnReleaseSlot(ShopItemSlot slot)
    {
        slot.gameObject.SetActive(false);
    }

    private void OnDestroySlot(ShopItemSlot slot)
    {
        if (slot != null)
        {
            Destroy(slot.gameObject);
        }
    }

    private void OnClickClose()
    {
        _closeBtn.onClick.RemoveListener(OnClickClose);
        _shopNPC.ToggleShop();
    }

    // ===================== Main Category =====================

    /// <summary>
    /// 실제로 판매 중인 Main만 버튼 노출, 나머지는 숨김.
    /// </summary>
    private void RefreshMainButtonAvailability()
    {
        for (int i = 0; i < _mainButtons.Count; i++)
        {
            MainCategoryButton entry = _mainButtons[i];
            if (entry.mainButton == null) continue;

            bool hasItems = _currentSellItems.Any(item => item.mainCategory == entry.mainCategory);
            entry.mainButton.gameObject.SetActive(hasItems);
        }
    }

    private void OnClickMainCategory(MainCategory category)
    {
        SelectMainCategory(category);
    }

    /// <summary>
    /// 대분류 선택 -> 그 안에서 실제 판매하는 서브카테고리 버튼들을 새로 생성(+"전체"), 첫 번째를 기본 선택.
    /// </summary>
    private void SelectMainCategory(MainCategory category)
    {
        _currentMainCategory = category;

        UpdateMainHighlight();

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

        SetSubCategorySelected(firstButton);
        ShowItems(subCategories.Count > 0 ? subCategories[0].items : new List<ItemData>());
    }

    private void UpdateMainHighlight()
    {
        for (int i = 0; i < _mainButtons.Count; i++)
        {
            MainCategoryButton entry = _mainButtons[i];
            SetButtonHighlighted(entry.mainButton, entry.mainCategory == _currentMainCategory);
        }
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

    // ===================== Sub Category (동적 생성) =====================

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
    /// 이 Main 카테고리 안에서 실제로 판매하는 SubCategory만 "전체" + 개별 서브로 구성.
    /// 판매하지 않는 서브카테고리는 목록 자체에 안 들어감.
    /// </summary>
    private List<(string label, List<ItemData> items)> BuildSubCategories(MainCategory category)
    {
        var result = new List<(string, List<ItemData>)>();

        List<ItemData> itemsInMain = _currentSellItems
            .Where(item => item.mainCategory == category)
            .ToList();

        if (itemsInMain.Count == 0)
        {
            return result;
        }

        result.Add(("전체", itemsInMain));

        List<SubCategory> subCategories = itemsInMain
            .Select(item => item.subCategory)
            .Distinct()
            .ToList();

        foreach (SubCategory sub in subCategories)
        {
            List<ItemData> filtered = itemsInMain
                .Where(item => item.subCategory == sub)
                .ToList();

            result.Add((sub.ToDisplayString(), filtered));
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

    // ===================== Item List (Pooled) =====================

    /// <summary>
    /// 선택된 아이템 목록을 Content에 슬롯으로 뿌려준다. itemId 오름차순으로 정렬해서 표시.
    /// </summary>
    private void ShowItems(List<ItemData> items)
    {
        ReleaseAllSlots();

        if (_itemSlotPrefab == null || _itemSlotParent == null)
        {
            return;
        }

        var sortedItems = items.OrderBy(item => item.itemId);

        foreach (var itemData in sortedItems)
        {
            ShopItemSlot slot = _slotPool.Get();
            slot.Setup(itemData, HandleItemSlotClicked);
            _activeSlots.Add(slot);
        }
    }

    private void ReleaseAllSlots()
    {
        for (int i = 0; i < _activeSlots.Count; i++)
        {
            if (_activeSlots[i] != null)
            {
                _slotPool.Release(_activeSlots[i]);
            }
        }

        _activeSlots.Clear();
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

    /// <summary>
    /// 던전에서 생성된 ShopNPC를 런타임 중에 가져오기 위해서.
    /// </summary>
    private void FindShopkeeper()
    {
        if (_shopNPC == null)
        {
            _shopNPC = FindAnyObjectByType<ShopNPC>();
        }
    }
}