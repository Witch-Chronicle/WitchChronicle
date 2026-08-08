using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
/// <summary>
/// PlayerInventory가 들고 있는 보유 아이템 목록을 IntergrationPanel/Inventory UI에 뿌려주는 역할.
/// - MainCategory 4종(Equip/Consume/Life/Material) 버튼은 항상 고정 표시.
///   선택된 버튼은 내부 Selected(Image)의 알파를 0/1로 DOTween 트윈해서 강조.
/// - SubCategory는 FilterBtn 클릭 시 그 안의 SubCategoryRoot(전체+서브 버튼들)가
///   localScale.x 0->1 + CanvasGroup 페이드로 왼쪽으로 펼쳐지며 나타남 (Pivot X=1 고정 기준).
///   그 중 하나를 선택하면 슬라이드가 닫히고 SelectedCatergoryTxt가 다시 나타나며 선택값으로 갱신됨.
/// - Equip Main은 EquipmentInstances(개체 단위)에서, 나머지는 InventorySlots(수량 기반)에서 필터링.
/// - 아이템 슬롯(InventoryItemSlot)은 InventoryScrollView(RecycledScrollView)가 뷰포트 범위만큼만
///   생성/재사용합니다. 데이터가 몇 개든 실제 GameObject 개수는 화면에 보이는 만큼(+버퍼)으로 고정됩니다.
/// * Quest(KeyItem)는 이번 카테고리 체계에서 제외됨 (인벤토리에 표시 안 함).
/// </summary>
public class InventoryUIController : MonoBehaviour
{
    [Serializable]
    private class MainCategorySection
    {
        public MainCategory mainCategory;
        public Button mainButton;
        [Tooltip("이 버튼 안의 Selected(Image). 선택 시 알파 1, 아니면 0으로 트윈")]
        public Image selectedIndicator;
        [Tooltip("이 Main에 속한 서브카테고리 목록 (표시 순서대로)")]
        public List<SubCategory> subCategories;
    }
    [Serializable]
    private class GradeIconEntry
    {
        public ItemGradeType itemGrade;
        public Sprite icon;
    }
    [Header("Item Grade Icons (Common/UnCommon/Rare/Unique/Legendary)")]
    [SerializeField] private List<GradeIconEntry> _gradeIcons = new List<GradeIconEntry>();
    [Header("Main Category Btns (항상 고정 표시)")]
    [SerializeField] private List<MainCategorySection> _mainSections = new List<MainCategorySection>();
    [SerializeField] private float _mainSelectedFadeDuration = 0.15f;
    [Header("Sub Category - Filter 버튼 + 슬라이드")]
    [SerializeField] private Button _filterBtn;
    [Tooltip("전체(SelectedCatergoryTxt)가 아닌, 슬라이드로 펼쳐지는 서브카테고리 목록 루트")]
    [SerializeField] private RectTransform _subCategorySlideRoot;
    [SerializeField] private CanvasGroup _subCategorySlideCanvasGroup;
    [SerializeField] private float _subCategorySlideDuration = 0.2f;
    [Header("Sub Category - 재사용 슬롯 풀 (슬롯 하나는 항상 '전체' 용도로 사용)")]
    [Tooltip("0번 슬롯은 항상 '전체'로 고정 사용. 나머지는 그 Main의 서브카테고리로 채워짐")]
    [SerializeField] private List<Button> _subButtonSlots = new List<Button>();
    [Header("Selected Category Txt (평소 표시, 슬라이드 펼쳐질 때만 숨김)")]
    [SerializeField] private TextMeshProUGUI _selectedCategoryText;
    [SerializeField] private string _allCategoryLabel = "전체";
    [Header("Category Btn 색상")]
    [SerializeField] private Color _normalTextColor = Color.black;
    [SerializeField] private Color _selectedTextColor = Color.white;
    [Header("Close Btn")]
    [SerializeField] private Button _closeBtn;
    [Header("Item List")]
    [Tooltip("뷰포트 범위만큼만 셀을 재사용해서 그려주는 스크롤 뷰입니다. ScrollRect가 붙은 오브젝트를 연결하세요.")]
    [SerializeField] private InventoryScrollView _scrollView;
    [Header("Gold Txt")]
    [SerializeField] private TextMeshProUGUI _goldText;
    [Header("Item Detail")]
    [SerializeField] private InventoryDetailController _itemDetailController;
    private MainCategory _currentMainCategory;
    private SubCategory? _currentSubCategory; // null이면 "전체"
    private bool _isSubCategorySlideOpen;
    private readonly List<InventorySlotEntry> _entryBuffer = new List<InventorySlotEntry>();
    private void Awake()
    {
        if (_mainSections.Count > 0)
        {
            _currentMainCategory = _mainSections[0].mainCategory;
        }
        if (_subCategorySlideRoot != null)
        {
            _subCategorySlideRoot.localScale = new Vector3(0f, 1f, 1f);
        }
        if (_subCategorySlideCanvasGroup != null)
        {
            _subCategorySlideCanvasGroup.alpha = 0f;
            _subCategorySlideCanvasGroup.interactable = false;
            _subCategorySlideCanvasGroup.blocksRaycasts = false;
        }
    }
    private void OnEnable()
    {
        for (int i = 0; i < _mainSections.Count; i++)
        {
            MainCategorySection section = _mainSections[i];
            if (section.mainButton == null) continue;
            MainCategory captured = section.mainCategory;
            section.mainButton.onClick.AddListener(() => OnClickMainCategory(captured));
        }
        if (_filterBtn != null)
        {
            _filterBtn.onClick.AddListener(OnClickFilterBtn);
        }
        if (_closeBtn != null)
        {
            _closeBtn.onClick.AddListener(OnClickClose);
        }
        SelectMainCategory(_mainSections.Count > 0 ? _mainSections[0].mainCategory : _currentMainCategory, animateMainHighlight: false);
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
        CharacterEquipment.OnAnyEquipmentChanged += HandleInventoryChanged;
    }
    private void OnDisable()
    {
        for (int i = 0; i < _mainSections.Count; i++)
        {
            _mainSections[i].mainButton?.onClick.RemoveAllListeners();
        }
        if (_filterBtn != null)
        {
            _filterBtn.onClick.RemoveListener(OnClickFilterBtn);
        }
        for (int i = 0; i < _subButtonSlots.Count; i++)
        {
            _subButtonSlots[i]?.onClick.RemoveAllListeners();
        }
        if (_closeBtn != null)
        {
            _closeBtn.onClick.RemoveListener(OnClickClose);
        }
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnGoldChanged -= UpdateGoldText;
            PlayerInventory.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }
        CharacterEquipment.OnAnyEquipmentChanged -= HandleInventoryChanged;
        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.ResetToDefault();
        }
    }
    // ===================== Main Category =====================
    private void OnClickClose()
    {
        _closeBtn.onClick.RemoveListener(OnClickClose);
        PlayerUIInputReader.Instance.ToggleIntegrationPanel();
    }
    private void OnClickMainCategory(MainCategory category)
    {
        SelectMainCategory(category, animateMainHighlight: true);
    }
    /// <summary>
    /// Main 카테고리 선택. Selected 인디케이터를 갱신하고, 서브카테고리를 그 Main에 맞게 재구성 + "전체"로 리셋.
    /// </summary>
    private void SelectMainCategory(MainCategory category, bool animateMainHighlight)
    {
        _currentMainCategory = category;
        UpdateMainHighlight(animateMainHighlight);
        RebuildSubCategoryButtons(category);
        CloseSubCategorySlideImmediate();
        SelectSubCategory(null);
    }
    private void UpdateMainHighlight(bool animate)
    {
        for (int i = 0; i < _mainSections.Count; i++)
        {
            MainCategorySection section = _mainSections[i];
            if (section.selectedIndicator == null) continue;
            bool isSelected = section.mainCategory == _currentMainCategory;
            float targetAlpha = isSelected ? 1f : 0f;
            section.selectedIndicator.DOKill();
            if (animate)
            {
                section.selectedIndicator.DOFade(targetAlpha, _mainSelectedFadeDuration);
            }
            else
            {
                Color c = section.selectedIndicator.color;
                c.a = targetAlpha;
                section.selectedIndicator.color = c;
            }
        }
    }
    // ===================== Sub Category =====================
    private void OnClickFilterBtn()
    {
        if (_isSubCategorySlideOpen)
        {
            CloseSubCategorySlide();
        }
        else
        {
            OpenSubCategorySlide();
        }
    }
    private void OpenSubCategorySlide()
    {
        _isSubCategorySlideOpen = true;
        if (_selectedCategoryText != null)
        {
            _selectedCategoryText.gameObject.SetActive(false);
        }
        if (_subCategorySlideRoot != null)
        {
            _subCategorySlideRoot.DOKill();
            _subCategorySlideRoot
                .DOScaleX(1f, _subCategorySlideDuration)
                .SetEase(Ease.OutQuad);
        }
        if (_subCategorySlideCanvasGroup != null)
        {
            _subCategorySlideCanvasGroup.DOKill();
            _subCategorySlideCanvasGroup.interactable = true;
            _subCategorySlideCanvasGroup.blocksRaycasts = true;
            _subCategorySlideCanvasGroup
                .DOFade(1f, _subCategorySlideDuration)
                .SetEase(Ease.OutQuad);
        }
    }
    private void CloseSubCategorySlide()
    {
        _isSubCategorySlideOpen = false;
        if (_subCategorySlideCanvasGroup != null)
        {
            _subCategorySlideCanvasGroup.DOKill();
            _subCategorySlideCanvasGroup.interactable = false;
            _subCategorySlideCanvasGroup.blocksRaycasts = false;
            _subCategorySlideCanvasGroup
                .DOFade(0f, _subCategorySlideDuration)
                .SetEase(Ease.InQuad);
        }
        if (_subCategorySlideRoot != null)
        {
            _subCategorySlideRoot.DOKill();
            _subCategorySlideRoot
                .DOScaleX(0f, _subCategorySlideDuration)
                .SetEase(Ease.InQuad);
        }
        if (_selectedCategoryText != null)
        {
            _selectedCategoryText.gameObject.SetActive(true);
        }
    }
    private void CloseSubCategorySlideImmediate()
    {
        _isSubCategorySlideOpen = false;
        if (_subCategorySlideRoot != null)
        {
            _subCategorySlideRoot.DOKill();
            _subCategorySlideRoot.localScale = new Vector3(0f, 1f, 1f);
        }
        if (_subCategorySlideCanvasGroup != null)
        {
            _subCategorySlideCanvasGroup.DOKill();
            _subCategorySlideCanvasGroup.alpha = 0f;
            _subCategorySlideCanvasGroup.interactable = false;
            _subCategorySlideCanvasGroup.blocksRaycasts = false;
        }
        if (_selectedCategoryText != null)
        {
            _selectedCategoryText.gameObject.SetActive(true);
        }
    }
    /// <summary>
    /// 0번 슬롯은 항상 "전체", 그 뒤로 이 Main의 서브카테고리들을 순서대로 채움.
    /// 슬롯 개수보다 (전체+서브카테고리 수)가 적으면 남는 슬롯은 비활성화.
    /// </summary>
    private void RebuildSubCategoryButtons(MainCategory category)
    {
        MainCategorySection section = _mainSections.FirstOrDefault(s => s.mainCategory == category);
        List<SubCategory> subCategories = section?.subCategories ?? new List<SubCategory>();
        for (int i = 0; i < _subButtonSlots.Count; i++)
        {
            Button slotButton = _subButtonSlots[i];
            if (slotButton == null) continue;
            slotButton.onClick.RemoveAllListeners();
            if (i == 0)
            {
                // 0번 슬롯 = 전체
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
        SelectSubCategory(subCategory);
        CloseSubCategorySlide();
    }
    private void SelectSubCategory(SubCategory? subCategory)
    {
        _currentSubCategory = subCategory;
        if (_itemDetailController != null)
        {
            _itemDetailController.Hide();
        }
        UpdateSelectedCategoryText();
        RefreshList();
    }
    private void UpdateSelectedCategoryText()
    {
        if (_selectedCategoryText == null) return;
        _selectedCategoryText.text = _currentSubCategory.HasValue
            ? _currentSubCategory.Value.ToDisplayString()
            : _allCategoryLabel;
    }
    // ===================== 목록 갱신 =====================
    private void HandleInventoryChanged()
    {
        RefreshList();
    }
    private void RefreshList()
    {
        if (_scrollView == null)
        {
            Debug.LogWarning("[InventoryUIController] InventoryScrollView가 연결되지 않았습니다.", this);
            return;
        }
        if (PlayerInventory.Instance == null)
        {
            _scrollView.Clear();
            return;
        }
        _entryBuffer.Clear();
        if (_currentMainCategory == MainCategory.Equip)
        {
            BuildEquipmentEntries();
        }
        else
        {
            BuildStackableEntries();
        }
        _scrollView.SetData(_entryBuffer);
    }
    private void BuildEquipmentEntries()
    {
        var equipmentList = PlayerInventory.Instance.EquipmentInstances
            .Where(instance => instance.baseData != null)
            .Where(instance => instance.baseData.mainCategory == MainCategory.Equip)
            .Where(instance => _currentSubCategory == null || instance.baseData.subCategory == _currentSubCategory.Value)
            .Where(instance => !CharacterEquipment.IsEquippedByAnyone(instance));
        foreach (var instance in equipmentList)
        {
            _entryBuffer.Add(new InventorySlotEntry(
                instance,
                GetGradeIcon(instance.baseData.itemGrade),
                HandleEquipmentSlotClicked));
        }
    }
    private void BuildStackableEntries()
    {
        var filtered = PlayerInventory.Instance.InventorySlots
            .Where(slot => slot.ItemData.mainCategory == _currentMainCategory)
            .Where(slot => _currentSubCategory == null || slot.ItemData.subCategory == _currentSubCategory.Value);
        foreach (var slot in filtered)
        {
            _entryBuffer.Add(new InventorySlotEntry(
                slot.ItemData,
                slot.Quantity,
                GetGradeIcon(slot.ItemData.itemGrade),
                HandleItemSlotClicked));
        }
    }
    private void UpdateGoldText(int gold)
    {
        if (_goldText != null)
        {
            _goldText.text = gold.ToString() + " G";
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
    /// <summary>
    /// 아이템 등급에 해당하는 아이콘 스프라이트 조회. 등록 안 된 등급이면 null.
    /// </summary>
    private Sprite GetGradeIcon(ItemGradeType grade)
    {
        for (int i = 0; i < _gradeIcons.Count; i++)
        {
            if (_gradeIcons[i] != null && _gradeIcons[i].itemGrade == grade)
            {
                return _gradeIcons[i].icon;
            }
        }
        return null;
    }
}