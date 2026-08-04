using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Pool;
using TMPro;
using DG.Tweening;

/// <summary>
/// ShopNPC가 들고 있는 판매 아이템 목록(List&lt;ItemData&gt;)을 Shop UI에 뿌려주는 역할.
/// - Main 카테고리는 아이콘 버튼(고정 배치). 실제로 판매하는 Main만 활성화, 기본 선택은 그 중 첫 번째.
///   선택 시 아이콘 스프라이트가 평소/선택 이미지로 교체됨.
/// - Sub 카테고리는 고정 슬롯 풀(0번=항상 "전체", 나머지는 그 Main의 서브카테고리로 채워짐).
///   선택 시 각 버튼의 Selected(Image) 알파를 DOTween으로 0/1 트윈.
/// - 아이템 슬롯(ShopItemSlot)은 ObjectPool로 재사용. 클릭 시 Normal/Selected 토글로 단일 선택 표시,
///   카테고리 전환 시 선택 초기화.
/// * 구매 로직은 없음. 진열만 담당.
/// </summary>
public class ShopUIController : MonoBehaviour
{
    [Serializable]
    private class MainCategoryButton
    {
        public MainCategory mainCategory;
        public Button mainButton;
        [Tooltip("이 버튼의 아이콘 Image")]
        public Image iconImage;
        [Tooltip("평소(미선택) 아이콘")]
        public Sprite normalIcon;
        [Tooltip("선택됐을 때 아이콘")]
        public Sprite selectedIcon;
    }

    [Header("Shop NPC")]
    [SerializeField] private ShopNPC _shopNPC;

    [Header("Main Category Btns (항상 고정 배치, 판매 여부에 따라 표시/숨김)")]
    [SerializeField] private List<MainCategoryButton> _mainButtons = new List<MainCategoryButton>();

    [Header("Close Btn")]
    [SerializeField] private Button _closeBtn;

    [Header("Sub Category - 재사용 슬롯 풀 (0번 슬롯은 항상 '전체')")]
    [SerializeField] private List<Button> _subButtonSlots = new List<Button>();
    [SerializeField] private string _allCategoryLabel = "전체";
    [SerializeField] private float _subSelectedFadeDuration = 0.15f;

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

    private ObjectPool<ShopItemSlot> _slotPool;
    private readonly List<ShopItemSlot> _activeSlots = new List<ShopItemSlot>();

    private List<ItemData> _currentSellItems = new List<ItemData>();
    private MainCategory _currentMainCategory;
    private SubCategory? _currentSubCategory; // null이면 "전체"

    private ShopItemSlot _selectedSlot;

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

        for (int i = 0; i < _subButtonSlots.Count; i++)
        {
            _subButtonSlots[i]?.onClick.RemoveAllListeners();
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
        slot.SetSelected(false);
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
    /// 대분류 선택 -> 아이콘 이미지 갱신 + 서브카테고리 재구성("전체" 기본 선택).
    /// </summary>
    private void SelectMainCategory(MainCategory category)
    {
        _currentMainCategory = category;

        UpdateMainIcons();
        RebuildSubCategoryButtons(category);

        if (_shopDetailController != null)
        {
            _shopDetailController.HideDetail();
        }

        SelectSubCategory(null, animate: false);
    }

    private void UpdateMainIcons()
    {
        for (int i = 0; i < _mainButtons.Count; i++)
        {
            MainCategoryButton entry = _mainButtons[i];
            if (entry.iconImage == null) continue;

            bool isSelected = entry.mainCategory == _currentMainCategory;
            entry.iconImage.sprite = isSelected ? entry.selectedIcon : entry.normalIcon;
        }
    }

    // ===================== Sub Category (고정 슬롯 풀) =====================

    /// <summary>
    /// 0번 슬롯은 항상 "전체", 그 뒤로 이 Main의 서브카테고리들을 순서대로 채움.
    /// </summary>
    private void RebuildSubCategoryButtons(MainCategory category)
    {
        List<ItemData> itemsInMain = _currentSellItems
            .Where(item => item.mainCategory == category)
            .ToList();

        List<SubCategory> subCategories = itemsInMain
            .Select(item => item.subCategory)
            .Distinct()
            .ToList();

        for (int i = 0; i < _subButtonSlots.Count; i++)
        {
            Button slotButton = _subButtonSlots[i];
            if (slotButton == null) continue;

            slotButton.onClick.RemoveAllListeners();

            if (i == 0)
            {
                slotButton.gameObject.SetActive(true);

                TextMeshProUGUI allLabel = slotButton.GetComponentInChildren<TextMeshProUGUI>();
                if (allLabel != null) allLabel.text = _allCategoryLabel;

                slotButton.onClick.AddListener(() => OnClickSubCategorySlot(null));
                continue;
            }

            int subIndex = i - 1;

            if (subIndex >= subCategories.Count)
            {
                slotButton.gameObject.SetActive(false);
                continue;
            }

            SubCategory sub = subCategories[subIndex];

            slotButton.gameObject.SetActive(true);

            TextMeshProUGUI label = slotButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = sub.ToDisplayString();

            slotButton.onClick.AddListener(() => OnClickSubCategorySlot(sub));
        }
    }

    private void OnClickSubCategorySlot(SubCategory? subCategory)
    {
        SelectSubCategory(subCategory, animate: true);
    }

    private void SelectSubCategory(SubCategory? subCategory, bool animate)
    {
        _currentSubCategory = subCategory;

        if (_shopDetailController != null)
        {
            _shopDetailController.HideDetail();
        }

        UpdateSubSelectedHighlight(animate);
        ShowItems();
    }

    /// <summary>
    /// 선택된 서브카테고리 슬롯의 Selected(Image) 알파만 1로, 나머지는 0으로 DOTween 트윈.
    /// </summary>
    private void UpdateSubSelectedHighlight(bool animate)
    {
        List<ItemData> itemsInMain = _currentSellItems
            .Where(item => item.mainCategory == _currentMainCategory)
            .ToList();

        List<SubCategory> subCategories = itemsInMain
            .Select(item => item.subCategory)
            .Distinct()
            .ToList();

        for (int i = 0; i < _subButtonSlots.Count; i++)
        {
            Button slotButton = _subButtonSlots[i];
            if (slotButton == null || slotButton.gameObject.activeSelf == false) continue;

            bool isSelected;

            if (i == 0)
            {
                isSelected = _currentSubCategory == null;
            }
            else
            {
                int subIndex = i - 1;
                isSelected = subIndex < subCategories.Count
                    && _currentSubCategory.HasValue
                    && _currentSubCategory.Value == subCategories[subIndex];
            }

            Image selectedImage = FindSelectedIndicator(slotButton);
            if (selectedImage == null) continue;

            float targetAlpha = isSelected ? 1f : 0f;

            selectedImage.DOKill();

            if (animate)
            {
                selectedImage.DOFade(targetAlpha, _subSelectedFadeDuration);
            }
            else
            {
                Color c = selectedImage.color;
                c.a = targetAlpha;
                selectedImage.color = c;
            }
        }
    }

    private Image FindSelectedIndicator(Button slotButton)
    {
        Transform selectedTransform = slotButton.transform.Find("Selected");
        return selectedTransform != null ? selectedTransform.GetComponent<Image>() : null;
    }

    // ===================== Item List (Pooled) =====================

    /// <summary>
    /// 지금 Main/Sub 조건에 맞는 아이템 목록을 Content에 슬롯으로 뿌려준다. itemId 오름차순 정렬.
    /// 카테고리를 바꿀 때마다 선택 상태도 초기화됨.
    /// </summary>
    private void ShowItems()
    {
        ReleaseAllSlots();
        _selectedSlot = null;

        if (_itemSlotPrefab == null || _itemSlotParent == null) return;

        var filtered = _currentSellItems
            .Where(item => item.mainCategory == _currentMainCategory)
            .Where(item => _currentSubCategory == null || item.subCategory == _currentSubCategory.Value)
            .OrderBy(item => item.itemId);

        foreach (var itemData in filtered)
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
        ShopItemSlot clickedSlot = _activeSlots.FirstOrDefault(slot => slot != null && slot.ItemData == itemData);

        if (_selectedSlot != null && _selectedSlot != clickedSlot)
        {
            _selectedSlot.SetSelected(false);
        }

        if (clickedSlot != null)
        {
            clickedSlot.SetSelected(true);
        }

        _selectedSlot = clickedSlot;

        if (_shopDetailController != null)
        {
            _shopDetailController.ShowItemDetail(itemData);
        }
    }

    private void UpdateGoldText(int gold)
    {
        if (_goldText != null)
        {
            _goldText.text = gold.ToString() + " G";
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