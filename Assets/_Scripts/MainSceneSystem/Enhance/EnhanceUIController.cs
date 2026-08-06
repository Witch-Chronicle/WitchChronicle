using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Pool;
using TMPro;
using DG.Tweening;

/// <summary>
/// EnhancePanel 메인 컨트롤러.
/// - EquipList: 보유 장비를 캐러셀(페이지 단위 5개씩)로 나열. ObjectPool로 최대 5슬롯만 재사용.
///   count가 5 이하면 Prev/Next 비활성. 그 이상이면 5개씩 페이지 순환(마지막 페이지는 남는 개수만).
/// - Current/Next: 선택된 장비의 "지금 단계"와 "다음 단계" 스탯을 나란히 비교 표시.
///   Preview 행은 고정 3슬롯(늘어나는 스탯이 없으면 남는 슬롯 숨김).
/// - EnhanceBtn 클릭 시 EnhanceController에 강화 실행을 위임.
/// * 실제 강화 판정/비용 처리는 EnhanceController가 담당. 여기서는 UI 표시/입력만 담당.
/// </summary>
public class EnhanceUIController : MonoBehaviour
{
    [Header("Close Btn")]
    [SerializeField] private Button _closeBtn;
    [SerializeField] private EnhanceNPC _enhanceNPC;

    [Header("Gold Txt")]
    [SerializeField] private TextMeshProUGUI _goldText;

    [Header("Enhance Controller")]
    [SerializeField] private EnhanceController _enhanceController;

    [Header("Base Info")]
    [SerializeField] private TextMeshProUGUI _baseInfoName;
    [SerializeField] private TextMeshProUGUI _baseInfoGrade;
    [SerializeField] private GameObject _equipEmptyTxt;

    [Header("Current")]
    [SerializeField] private Image _currentIcon;
    [SerializeField] private TextMeshProUGUI _currentLvText;
    [SerializeField] private List<EnhanceValueRow> _currentValueRows = new List<EnhanceValueRow>(); // 고정 3슬롯

    [Header("Next")]
    [SerializeField] private Image _nextIcon;
    [SerializeField] private TextMeshProUGUI _nextLvText;
    [SerializeField] private List<EnhanceValueRow> _nextValueRows = new List<EnhanceValueRow>(); // 고정 3슬롯

    [Header("Enhance Execute - SuccessRate")]
    [SerializeField] private TextMeshProUGUI _successRateText;
    [SerializeField] private TextMeshProUGUI _pityProgressText;
    [SerializeField] private Image _pityFilledImage; // PityBG/PityFilled (Filled Image)
    [SerializeField] private GameObject _pityGuaranteedObject;
    [SerializeField] private GameObject _tooltipObject;

    [Header("Enhance Execute - Required")]
    [SerializeField] private EnhanceRequiredRow _requiredRowPrefab;
    [SerializeField] private Transform _requiredRoot;

    [Header("Enhance Execute - Btn")]
    [SerializeField] private Button _enhanceBtn;

    [Header("Enhancement Result")]
    [SerializeField] private EnhancementResultController _enhancementResultController;

    [Header("Equip List (Carousel, 순환 윈도우 5칸)")]
    [SerializeField] private EnhanceEquipSlot _equipSlotPrefab;
    [SerializeField] private Transform _carouselParent; // Carousel (Horizontal Layout Group)
    [SerializeField] private Button _prevBtn;
    [SerializeField] private Button _nextBtn;
    [SerializeField] private int _pageSize = 5;

    [Header("Equip Filter - Panel")]
    [SerializeField] private Button _filterButton;
    [SerializeField] private GameObject _filterPanel;
    [SerializeField] private CanvasGroup _filterPanelCanvasGroup;
    [SerializeField] private float _filterPanelFadeDuration = 0.15f;
    [SerializeField] private Toggle _weaponFilterToggle;
    [SerializeField] private Toggle _robeFilterToggle;
    [SerializeField] private Toggle _cloakFilterToggle;
    [SerializeField] private Toggle _glovesFilterToggle;
    [SerializeField] private Toggle _shoesFilterToggle;
    [SerializeField] private Toggle _necklaceFilterToggle;
    [SerializeField] private Toggle _earringFilterToggle;
    [SerializeField] private Toggle _ringFilterToggle;

    [Header("Equip Filter - Button Visual")]
    [SerializeField] private Image _filterButtonBackground;
    [SerializeField] private Image _filterButtonBorder;
    [SerializeField] private Image _filterButtonIcon;
    [SerializeField] private GameObject _filterActiveMark;
    [SerializeField] private Color _filterNormalBackground = new Color32(36, 34, 40, 90);
    [SerializeField] private Color _filterActiveBackground = new Color32(91, 86, 96, 140);
    [SerializeField] private Color _filterNormalColor = new Color32(162, 160, 159, 190);
    [SerializeField] private Color _filterActiveColor = new Color32(233, 231, 221, 255);

    private readonly List<GameObject> _spawnedEnhanceRequiredRows = new List<GameObject>();

    private ObjectPool<EnhanceEquipSlot> _slotPool;
    private readonly List<EnhanceEquipSlot> _activeSlots = new List<EnhanceEquipSlot>();
    private List<EquipmentInstance> _allOwnedEquipments = new List<EquipmentInstance>();
    private List<EquipmentInstance> _ownedEquipments = new List<EquipmentInstance>();
    private readonly HashSet<EquipSlotType> _selectedFilters = new HashSet<EquipSlotType>();

    private EquipmentInstance _selectedInstance;
    private EnhanceEquipSlot _selectedSlot;
    private bool _isEnhancementResultPlaying;
    private bool _wasFilterActive;

    private void Awake()
    {
        _slotPool = new ObjectPool<EnhanceEquipSlot>(
            createFunc: CreateSlot,
            actionOnGet: OnGetSlot,
            actionOnRelease: OnReleaseSlot,
            actionOnDestroy: OnDestroySlot,
            collectionCheck: true,
            defaultCapacity: _pageSize,
            maxSize: _pageSize);
    }

    private void OnEnable()
    {
        if (_closeBtn != null) _closeBtn.onClick.AddListener(OnClickClose);
        if (_enhanceBtn != null) _enhanceBtn.onClick.AddListener(OnClickEnhance);
        if (_prevBtn != null) _prevBtn.onClick.AddListener(OnClickPrev);
        if (_nextBtn != null) _nextBtn.onClick.AddListener(OnClickNext);
        if (_filterButton != null) _filterButton.onClick.AddListener(OnClickFilterButton);

        AddFilterToggleListeners();

        if (_enhancementResultController != null)
        {
            _enhancementResultController.Closed += HandleEnhancementResultClosed;
        }

        ResetFiltersImmediate();
        _windowStartIndex = 0;

        RefreshOwnedEquipments();
        RefreshCarouselWindow();
        ClearSelection();

        _isEnhancementResultPlaying = false;

        if (_ownedEquipments.Count > 0)
        {
            SelectEquipment(_ownedEquipments[0]);
        }

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnGoldChanged += UpdateGoldText;
            UpdateGoldText(PlayerInventory.Instance.Gold);
            PlayerInventory.Instance.OnInventoryChanged += HandleInventoryChanged;
        }
    }

    private void OnDisable()
    {
        if (_closeBtn != null) _closeBtn.onClick.RemoveListener(OnClickClose);
        if (_enhanceBtn != null) _enhanceBtn.onClick.RemoveListener(OnClickEnhance);
        if (_prevBtn != null) _prevBtn.onClick.RemoveListener(OnClickPrev);
        if (_nextBtn != null) _nextBtn.onClick.RemoveListener(OnClickNext);
        if (_filterButton != null) _filterButton.onClick.RemoveListener(OnClickFilterButton);

        RemoveFilterToggleListeners();

        if (_filterButton != null)
        {
            _filterButton.transform.DOKill();
        }

        if (_filterPanelCanvasGroup != null)
        {
            _filterPanelCanvasGroup.DOKill();
        }

        if (_enhancementResultController != null)
        {
            _enhancementResultController.Closed -= HandleEnhancementResultClosed;
        }

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnGoldChanged -= UpdateGoldText;
            PlayerInventory.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    private void OnDestroy()
    {
        _slotPool?.Dispose();
    }

    private void OnClickClose()
    {
        _closeBtn.onClick.RemoveListener(OnClickClose);

        if (_enhanceNPC != null)
        {
            _enhanceNPC.ToggleEnhanceUI();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // ===================== Equip Filter =====================

    private void AddFilterToggleListeners()
    {
        if (_weaponFilterToggle != null) _weaponFilterToggle.onValueChanged.AddListener(OnWeaponFilterChanged);
        if (_robeFilterToggle != null) _robeFilterToggle.onValueChanged.AddListener(OnRobeFilterChanged);
        if (_cloakFilterToggle != null) _cloakFilterToggle.onValueChanged.AddListener(OnCloakFilterChanged);
        if (_glovesFilterToggle != null) _glovesFilterToggle.onValueChanged.AddListener(OnGlovesFilterChanged);
        if (_shoesFilterToggle != null) _shoesFilterToggle.onValueChanged.AddListener(OnShoesFilterChanged);
        if (_necklaceFilterToggle != null) _necklaceFilterToggle.onValueChanged.AddListener(OnNecklaceFilterChanged);
        if (_earringFilterToggle != null) _earringFilterToggle.onValueChanged.AddListener(OnEarringFilterChanged);
        if (_ringFilterToggle != null) _ringFilterToggle.onValueChanged.AddListener(OnRingFilterChanged);
    }

    private void RemoveFilterToggleListeners()
    {
        if (_weaponFilterToggle != null) _weaponFilterToggle.onValueChanged.RemoveListener(OnWeaponFilterChanged);
        if (_robeFilterToggle != null) _robeFilterToggle.onValueChanged.RemoveListener(OnRobeFilterChanged);
        if (_cloakFilterToggle != null) _cloakFilterToggle.onValueChanged.RemoveListener(OnCloakFilterChanged);
        if (_glovesFilterToggle != null) _glovesFilterToggle.onValueChanged.RemoveListener(OnGlovesFilterChanged);
        if (_shoesFilterToggle != null) _shoesFilterToggle.onValueChanged.RemoveListener(OnShoesFilterChanged);
        if (_necklaceFilterToggle != null) _necklaceFilterToggle.onValueChanged.RemoveListener(OnNecklaceFilterChanged);
        if (_earringFilterToggle != null) _earringFilterToggle.onValueChanged.RemoveListener(OnEarringFilterChanged);
        if (_ringFilterToggle != null) _ringFilterToggle.onValueChanged.RemoveListener(OnRingFilterChanged);
    }

    private void OnClickFilterButton()
    {
        if (_filterPanel == null) return;

        bool willOpen = !_filterPanel.activeSelf;

        if (willOpen)
        {
            OpenFilterPanel();
        }
        else
        {
            CloseFilterPanel();
        }
    }

    private void OpenFilterPanel()
    {
        if (_filterPanel == null) return;

        _filterPanel.SetActive(true);
        _filterPanel.transform.SetAsLastSibling();

        if (_filterPanelCanvasGroup == null) return;

        _filterPanelCanvasGroup.DOKill();
        _filterPanelCanvasGroup.interactable = false;
        _filterPanelCanvasGroup.blocksRaycasts = false;

        _filterPanelCanvasGroup
            .DOFade(1f, _filterPanelFadeDuration)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _filterPanelCanvasGroup.interactable = true;
                _filterPanelCanvasGroup.blocksRaycasts = true;
            });
    }

    private void CloseFilterPanel()
    {
        if (_filterPanel == null) return;

        if (_filterPanelCanvasGroup == null)
        {
            _filterPanel.SetActive(false);
            return;
        }

        _filterPanelCanvasGroup.DOKill();
        _filterPanelCanvasGroup.interactable = false;
        _filterPanelCanvasGroup.blocksRaycasts = false;

        _filterPanelCanvasGroup
            .DOFade(0f, _filterPanelFadeDuration)
            .SetUpdate(true)
            .OnComplete(() => _filterPanel.SetActive(false));
    }

    private void OnWeaponFilterChanged(bool isOn) => SetEquipmentFilter(EquipSlotType.Weapon, isOn);
    private void OnRobeFilterChanged(bool isOn) => SetEquipmentFilter(EquipSlotType.Robe, isOn);
    private void OnCloakFilterChanged(bool isOn) => SetEquipmentFilter(EquipSlotType.Cloak, isOn);
    private void OnGlovesFilterChanged(bool isOn) => SetEquipmentFilter(EquipSlotType.Gloves, isOn);
    private void OnShoesFilterChanged(bool isOn) => SetEquipmentFilter(EquipSlotType.Shoes, isOn);
    private void OnNecklaceFilterChanged(bool isOn) => SetEquipmentFilter(EquipSlotType.Necklace, isOn);
    private void OnEarringFilterChanged(bool isOn) => SetEquipmentFilter(EquipSlotType.Earring, isOn);
    private void OnRingFilterChanged(bool isOn) => SetEquipmentFilter(EquipSlotType.Ring, isOn);

    private void SetEquipmentFilter(EquipSlotType slotType, bool isOn)
    {
        if (isOn)
        {
            _selectedFilters.Add(slotType);
        }
        else
        {
            _selectedFilters.Remove(slotType);
        }

        ApplyFiltersAndRefreshSelection();
        UpdateFilterButtonVisual(true);
    }

    /// <summary>
    /// 선택된 필터가 없으면 전체 장비를 표시하고,
    /// 하나 이상이면 해당 EquipSlotType의 장비만 표시한다.
    /// </summary>
    private void ApplyEquipmentFilters()
    {
        if (_selectedFilters.Count == 0)
        {
            _ownedEquipments = new List<EquipmentInstance>(_allOwnedEquipments);
            return;
        }

        _ownedEquipments = _allOwnedEquipments
            .Where(instance =>
            {
                EquipItemData equipData = instance.baseData as EquipItemData;
                return equipData != null && _selectedFilters.Contains(equipData.equipSlotType);
            })
            .ToList();
    }

    private void ApplyFiltersAndRefreshSelection()
    {
        ApplyEquipmentFilters();

        if (_selectedInstance == null || !_ownedEquipments.Contains(_selectedInstance))
        {
            _selectedInstance = _ownedEquipments.Count > 0 ? _ownedEquipments[0] : null;
        }

        _windowStartIndex = _selectedInstance != null
            ? Mathf.Max(0, _ownedEquipments.IndexOf(_selectedInstance))
            : 0;

        RefreshCarouselWindow();

        if (_selectedInstance != null)
        {
            SelectEquipment(_selectedInstance);
        }
        else
        {
            ClearSelection();
        }
    }

    private void ResetFiltersImmediate()
    {
        _selectedFilters.Clear();

        SetToggleWithoutNotify(_weaponFilterToggle, false);
        SetToggleWithoutNotify(_robeFilterToggle, false);
        SetToggleWithoutNotify(_cloakFilterToggle, false);
        SetToggleWithoutNotify(_glovesFilterToggle, false);
        SetToggleWithoutNotify(_shoesFilterToggle, false);
        SetToggleWithoutNotify(_necklaceFilterToggle, false);
        SetToggleWithoutNotify(_earringFilterToggle, false);
        SetToggleWithoutNotify(_ringFilterToggle, false);

        if (_filterPanelCanvasGroup != null)
        {
            _filterPanelCanvasGroup.DOKill();
            _filterPanelCanvasGroup.alpha = 0f;
            _filterPanelCanvasGroup.interactable = false;
            _filterPanelCanvasGroup.blocksRaycasts = false;
        }

        if (_filterPanel != null)
        {
            _filterPanel.SetActive(false);
        }

        _wasFilterActive = false;
        UpdateFilterButtonVisual(false);
    }

    private static void SetToggleWithoutNotify(Toggle toggle, bool value)
    {
        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(value);
        }
    }

    private void UpdateFilterButtonVisual(bool animate)
    {
        bool isFilterActive = _selectedFilters.Count > 0;

        if (_filterButtonBackground != null)
        {
            _filterButtonBackground.color = isFilterActive
                ? _filterActiveBackground
                : _filterNormalBackground;
        }

        if (_filterButtonBorder != null)
        {
            _filterButtonBorder.color = isFilterActive
                ? _filterActiveColor
                : _filterNormalColor;
        }

        if (_filterButtonIcon != null)
        {
            _filterButtonIcon.color = isFilterActive
                ? _filterActiveColor
                : _filterNormalColor;
        }

        if (_filterActiveMark != null)
        {
            _filterActiveMark.SetActive(isFilterActive);
        }

        if (animate && isFilterActive && !_wasFilterActive && _filterButton != null)
        {
            Transform buttonTransform = _filterButton.transform;
            buttonTransform.DOKill();
            buttonTransform.localScale = Vector3.one;
            buttonTransform
                .DOPunchScale(Vector3.one * 0.08f, 0.25f, 4, 0.5f)
                .SetUpdate(true);
        }

        _wasFilterActive = isFilterActive;
    }

    // ===================== Object Pool =====================

    private EnhanceEquipSlot CreateSlot()
    {
        return Instantiate(_equipSlotPrefab, _carouselParent);
    }

    private void OnGetSlot(EnhanceEquipSlot slot)
    {
        slot.gameObject.SetActive(true);
        slot.transform.SetAsLastSibling();
    }

    private void OnReleaseSlot(EnhanceEquipSlot slot)
    {
        slot.gameObject.SetActive(false);
    }

    private void OnDestroySlot(EnhanceEquipSlot slot)
    {
        if (slot != null)
        {
            Destroy(slot.gameObject);
        }
    }

    // ===================== Equip Carousel (페이지 단위) =====================

    private void RefreshOwnedEquipments()
    {
        _allOwnedEquipments = PlayerInventory.Instance != null
            ? PlayerInventory.Instance.EquipmentInstances
                .Where(instance => instance != null && instance.baseData != null)
                .OrderBy(instance => instance.baseData.itemId)
                .ToList()
            : new List<EquipmentInstance>();

        ApplyEquipmentFilters();
    }

    private int _windowStartIndex;

    /// <summary>
    /// 보유 장비가 _pageSize(5) 이하면 있는 만큼만 표시하고 Prev/Next 비활성화.
    /// 초과하면 항상 5칸을 순환 윈도우로 표시 (windowStartIndex부터 5개, 끝에 도달하면 처음으로 순환).
    /// </summary>
    private void RefreshCarouselWindow()
    {
        ReleaseAllSlots();

        int totalCount = _ownedEquipments.Count;
        bool exceedsPageSize = totalCount > _pageSize;

        if (_prevBtn != null) _prevBtn.gameObject.SetActive(exceedsPageSize);
        if (_nextBtn != null) _nextBtn.gameObject.SetActive(exceedsPageSize);

        if (totalCount == 0)
        {
            ForceRebuildCarouselLayout();
            return;
        }

        if (exceedsPageSize == false)
        {
            _windowStartIndex = 0;
        }
        else
        {
            _windowStartIndex = ((_windowStartIndex % totalCount) + totalCount) % totalCount;
        }

        int showCount = Mathf.Min(_pageSize, totalCount);

        for (int i = 0; i < showCount; i++)
        {
            int index = (_windowStartIndex + i) % totalCount;

            EnhanceEquipSlot slot = _slotPool.Get();
            slot.Setup(_ownedEquipments[index], HandleEquipSlotClicked);
            _activeSlots.Add(slot);

            if (_selectedInstance != null && _ownedEquipments[index] == _selectedInstance)
            {
                slot.SetSelected(true);
                _selectedSlot = slot;
            }
        }

        ForceRebuildCarouselLayout();
    }

    /// <summary>
    /// 슬롯이 동적으로 늘어나거나 줄어든 직후, Content Size Fitter/Layout Group이
    /// 한 프레임 늦게 반영되는 것을 방지하기 위해 즉시 강제로 재계산.
    /// Carousel(자기 크기 계산) -> Main(그 크기를 보고 Prev/Carousel/Next 배치) 순서로 처리.
    /// </summary>
    private void ForceRebuildCarouselLayout()
    {
        if (_carouselParent == null) return;

        RectTransform carouselRect = _carouselParent as RectTransform;
        if (carouselRect == null) return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(carouselRect);

        RectTransform mainRect = carouselRect.parent as RectTransform;
        if (mainRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(mainRect);
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
        _selectedSlot = null;
    }

    private void OnClickPrev()
    {
        _windowStartIndex--;
        RefreshCarouselWindow();
    }

    private void OnClickNext()
    {
        _windowStartIndex++;
        RefreshCarouselWindow();
    }



    private void HandleInventoryChanged()
    {
        RefreshOwnedEquipments();
        RefreshCarouselWindow();

        if (_selectedInstance != null && _ownedEquipments.Contains(_selectedInstance) == false)
        {
            if (_ownedEquipments.Count > 0)
            {
                SelectEquipment(_ownedEquipments[0]);
            }
            else
            {
                ClearSelection();
            }

            return;
        }

        if (_selectedInstance != null)
        {
            SelectEquipment(_selectedInstance);
        }
    }

    private void HandleEquipSlotClicked(EquipmentInstance equipmentInstance)
    {
        SelectEquipment(equipmentInstance);
    }

    // ===================== 선택 / Current-Next / Execute =====================

    private void SelectEquipment(EquipmentInstance instance)
    {
        _selectedInstance = instance;
        UpdateSelectedSlotHighlight(instance);

        if (_equipEmptyTxt != null) _equipEmptyTxt.SetActive(false);

        ItemData itemData = instance.baseData;

        if (_baseInfoName != null) _baseInfoName.text = itemData.itemName;
        if (_baseInfoGrade != null) _baseInfoGrade.text = itemData.itemGrade.ToDisplayString();

        if (_currentIcon != null)
        {
            _currentIcon.sprite = itemData.icon;
            _currentIcon.enabled = itemData.icon != null;
        }

        if (_nextIcon != null)
        {
            _nextIcon.sprite = itemData.icon;
            _nextIcon.enabled = itemData.icon != null;
        }

        int currentLevel = instance.enhanceLevel;
        EnhanceLevelEntry nextEntry = _enhanceController != null
            ? _enhanceController.GetNextLevelEntry(instance)
            : null;

        bool isMaxLevel = nextEntry == null;

        if (_currentLvText != null) _currentLvText.text = $"+{currentLevel}";
        if (_nextLvText != null) _nextLvText.text = isMaxLevel ? "MAX" : $"+{nextEntry.level}";

        EnhanceTableData table = _enhanceController != null
            ? _enhanceController.GetTable(itemData.itemGrade)
            : null;

        EquipStatCalculator.StatSet currentStats = instance.cachedStats;
        EquipStatCalculator.StatSet nextStats = isMaxLevel
            ? currentStats
            : EquipStatCalculator.GetCurrentStats(itemData as EquipItemData, nextEntry.level, table);

        BuildCurrentValueRows(currentStats);
        BuildNextValueRows(nextStats);

        UpdateExecuteSection(nextEntry, isMaxLevel);
    }

    private void UpdateSelectedSlotHighlight(EquipmentInstance instance)
    {
        if (_selectedSlot != null)
        {
            _selectedSlot.SetSelected(false);
        }

        _selectedSlot = _activeSlots.FirstOrDefault(slot => slot != null && slot.EquipmentInstance == instance);

        if (_selectedSlot != null)
        {
            _selectedSlot.SetSelected(true);
        }
    }

    /// <summary>
    /// 아무것도 선택 안 한(또는 보유 장비 없음) 초기 상태로 정보/Preview/Execute 비우기.
    /// </summary>
    private void ClearSelection()
    {
        _selectedInstance = null;
        _selectedSlot = null;

        if (_equipEmptyTxt != null) _equipEmptyTxt.SetActive(true);

        if (_baseInfoName != null) _baseInfoName.text = string.Empty;
        if (_baseInfoGrade != null) _baseInfoGrade.text = string.Empty;

        if (_currentIcon != null)
        {
            _currentIcon.sprite = null;
            _currentIcon.enabled = false;
        }

        if (_currentLvText != null) _currentLvText.text = string.Empty;

        if (_nextIcon != null)
        {
            _nextIcon.sprite = null;
            _nextIcon.enabled = false;
        }

        if (_nextLvText != null) _nextLvText.text = string.Empty;

        HideAllCurrentValueRows();
        HideAllNextValueRows();

        if (_successRateText != null) _successRateText.text = string.Empty;
        if (_pityProgressText != null) _pityProgressText.text = string.Empty;
        if (_pityFilledImage != null) _pityFilledImage.fillAmount = 0f;
        SetPityGuaranteed(false);

        if (_enhanceBtn != null) _enhanceBtn.interactable = false;

        ClearEnhanceRequiredRows();
    }

    // ===================== Current/Next Value Rows (고정 3슬롯) =====================

    /// <summary>
    /// 0이 아닌 스탯만 골라 고정 3슬롯에 채우고, 남는 슬롯은 숨김.
    /// </summary>
    private void BuildCurrentValueRows(EquipStatCalculator.StatSet stats)
    {
        List<(string label, int value)> nonZero = CollectNonZeroStats(stats);

        for (int i = 0; i < _currentValueRows.Count; i++)
        {
            EnhanceValueRow row = _currentValueRows[i];
            if (row == null) continue;

            if (i < nonZero.Count)
            {
                row.gameObject.SetActive(true);
                row.Setup(nonZero[i].label, nonZero[i].value);
            }
            else
            {
                row.gameObject.SetActive(false);
            }
        }
    }

    private void BuildNextValueRows(EquipStatCalculator.StatSet stats)
    {
        List<(string label, int value)> nonZero = CollectNonZeroStats(stats);

        for (int i = 0; i < _nextValueRows.Count; i++)
        {
            EnhanceValueRow row = _nextValueRows[i];
            if (row == null) continue;

            if (i < nonZero.Count)
            {
                row.gameObject.SetActive(true);
                row.Setup(nonZero[i].label, nonZero[i].value);
            }
            else
            {
                row.gameObject.SetActive(false);
            }
        }
    }

    private void HideAllCurrentValueRows()
    {
        for (int i = 0; i < _currentValueRows.Count; i++)
        {
            _currentValueRows[i]?.gameObject.SetActive(false);
        }
    }

    private void HideAllNextValueRows()
    {
        for (int i = 0; i < _nextValueRows.Count; i++)
        {
            _nextValueRows[i]?.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// StatSet에서 0이 아닌 스탯만 (라벨, 값) 리스트로 추출. 최대 3개까지만 사용됨(고정 슬롯 제한).
    /// </summary>
    private List<(string label, int value)> CollectNonZeroStats(EquipStatCalculator.StatSet stats)
    {
        List<(string, int)> result = new List<(string, int)>();

        if (stats.hp != 0) result.Add(("체력", stats.hp));
        if (stats.mp != 0) result.Add(("마나", stats.mp));
        if (stats.spellPower != 0) result.Add(("공격력", stats.spellPower));
        if (stats.intelligence != 0) result.Add(("지능", stats.intelligence));
        if (stats.defense != 0) result.Add(("방어력", stats.defense));
        if (stats.speed != 0) result.Add(("속도", stats.speed));
        if (stats.luck != 0) result.Add(("행운", stats.luck));

        return result;
    }

    // ===================== Execute =====================

    private void UpdateExecuteSection(EnhanceLevelEntry nextEntry, bool isMaxLevel)
    {
        ClearEnhanceRequiredRows();

        if (isMaxLevel || nextEntry == null)
        {
            if (_successRateText != null) _successRateText.text = "MAX";
            if (_pityProgressText != null) _pityProgressText.text = string.Empty;
            if (_pityFilledImage != null) _pityFilledImage.fillAmount = 0f;
            SetPityGuaranteed(false);
            if (_enhanceBtn != null) _enhanceBtn.interactable = false;
            return;
        }

        if (_successRateText != null) _successRateText.text = $"{nextEntry.successRate}%";

        if (nextEntry.pityCount > 0 && _enhanceController != null)
        {
            float pityProgress = _enhanceController.GetPityProgress(_selectedInstance, nextEntry);
            bool isPityMax = pityProgress >= 100f;

            if (_pityProgressText != null)
            {
                if (isPityMax)
                {
                    _pityProgressText.text = $"{pityProgress:F1}%";
                }
                else
                {
                    float increasePerAttempt = _enhanceController.GetPityIncreasePerAttempt(nextEntry);
                    _pityProgressText.text = $"{pityProgress:F1}% (+{increasePerAttempt:F1}%)";
                }
            }

            if (_pityFilledImage != null)
            {
                _pityFilledImage.fillAmount = pityProgress / 100f;
            }

            SetPityGuaranteed(isPityMax);
        }
        else
        {
            if (_pityProgressText != null) _pityProgressText.text = string.Empty;
            if (_pityFilledImage != null) _pityFilledImage.fillAmount = 0f;
            SetPityGuaranteed(false);
        }

        int currentGold = PlayerInventory.Instance != null ? PlayerInventory.Instance.Gold : 0;
        AddEnhanceRequiredRow("골드", currentGold, nextEntry.requiredGold);

        if (nextEntry.requiredMaterials != null)
        {
            foreach (var required in nextEntry.requiredMaterials)
            {
                if (required.material == null) continue;

                int owned = PlayerInventory.Instance != null
                    ? PlayerInventory.Instance.GetTotalQuantity(required.material)
                    : 0;

                AddEnhanceRequiredRow(required.material.itemName, owned, required.amount);
            }
        }

        bool canEnhance = _enhanceController != null && _selectedInstance != null
            && _enhanceController.CanEnhance(_selectedInstance, out _);

        if (_enhanceBtn != null)
        {
            _enhanceBtn.interactable = canEnhance && !_isEnhancementResultPlaying;
        }
    }

    private void SetPityGuaranteed(bool isGuaranteed)
    {
        if (_pityGuaranteedObject != null) _pityGuaranteedObject.SetActive(isGuaranteed);
        if (_tooltipObject != null) _tooltipObject.SetActive(!isGuaranteed);
    }

    private void AddEnhanceRequiredRow(string label, int currentAmount, int requiredAmount)
    {
        if (_requiredRowPrefab == null || _requiredRoot == null) return;

        EnhanceRequiredRow row = Instantiate(_requiredRowPrefab, _requiredRoot);
        row.Setup(label, currentAmount, requiredAmount);
        _spawnedEnhanceRequiredRows.Add(row.gameObject);
    }

    private void ClearEnhanceRequiredRows()
    {
        foreach (var rowObj in _spawnedEnhanceRequiredRows)
        {
            Destroy(rowObj);
        }

        _spawnedEnhanceRequiredRows.Clear();
    }

    private void OnClickEnhance()
    {
        if (_isEnhancementResultPlaying) return;
        if (_enhanceController == null || _selectedInstance == null) return;

        EnhanceLevelEntry attemptedEntry = _enhanceController.GetNextLevelEntry(_selectedInstance);
        if (attemptedEntry == null) return;

        if (_enhanceController.CanEnhance(_selectedInstance, out _) == false) return;

        // TryEnhance()가 인스턴스의 강화 단계와 강화 포인트를 변경하기 전에
        // 결과 연출에 필요한 이전 값을 먼저 저장한다.
        int beforeLevel = _selectedInstance.enhanceLevel;
        float pointBefore = attemptedEntry.pityCount > 0
            ? _enhanceController.GetPityProgress(_selectedInstance, attemptedEntry)
            : 0f;

        Sprite equipmentIcon = _selectedInstance.baseData != null
            ? _selectedInstance.baseData.icon
            : null;

        _isEnhancementResultPlaying = true;

        if (_enhanceBtn != null)
        {
            _enhanceBtn.interactable = false;
        }

        bool isSuccess = _enhanceController.TryEnhance(_selectedInstance);

        // TryEnhance() 실행 이후의 실제 값을 사용한다.
        int afterLevel = _selectedInstance.enhanceLevel;
        float pointAfter = isSuccess == false && attemptedEntry.pityCount > 0
            ? _enhanceController.GetPityProgress(_selectedInstance, attemptedEntry)
            : 0f;

        if (_enhancementResultController != null)
        {
            _enhancementResultController.transform.SetAsLastSibling();
            _enhancementResultController.Play(
                equipmentIcon,
                isSuccess,
                beforeLevel,
                afterLevel,
                pointBefore,
                pointAfter);
        }
        else
        {
            Debug.LogWarning("EnhancementResultController가 EnhanceUIController에 연결되지 않았습니다.");
            HandleEnhancementResultClosed();
        }
    }

    /// <summary>
    /// 결과 Overlay가 닫힌 뒤 변경된 장비/재화/필요 재료 정보를 갱신한다.
    /// </summary>
    private void HandleEnhancementResultClosed()
    {
        _isEnhancementResultPlaying = false;

        RefreshOwnedEquipments();
        RefreshCarouselWindow();

        if (_selectedInstance != null && _ownedEquipments.Contains(_selectedInstance))
        {
            SelectEquipment(_selectedInstance);
        }
        else if (_ownedEquipments.Count > 0)
        {
            SelectEquipment(_ownedEquipments[0]);
        }
        else
        {
            ClearSelection();
        }

        if (PlayerInventory.Instance != null)
        {
            UpdateGoldText(PlayerInventory.Instance.Gold);
        }
    }

    private void UpdateGoldText(int gold)
    {
        if (_goldText != null)
        {
            _goldText.text = gold.ToString() + " G";
        }
    }
}