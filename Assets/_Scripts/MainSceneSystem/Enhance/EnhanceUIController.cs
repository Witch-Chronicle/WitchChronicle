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

    [Header("Result")]
    [SerializeField] private CanvasGroup _resultCanvasGroup;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private float _resultFadeDuration = 0.25f;
    [SerializeField] private float _resultShowDuration = 1.2f;

    [Header("Equip List (Carousel, 순환 윈도우 5칸)")]
    [SerializeField] private EnhanceEquipSlot _equipSlotPrefab;
    [SerializeField] private Transform _carouselParent; // Carousel (Horizontal Layout Group)
    [SerializeField] private Button _prevBtn;
    [SerializeField] private Button _nextBtn;
    [SerializeField] private int _pageSize = 5;

    private readonly List<GameObject> _spawnedEnhanceRequiredRows = new List<GameObject>();

    private ObjectPool<EnhanceEquipSlot> _slotPool;
    private readonly List<EnhanceEquipSlot> _activeSlots = new List<EnhanceEquipSlot>();
    private List<EquipmentInstance> _ownedEquipments = new List<EquipmentInstance>();
    private int _currentPage;

    private EquipmentInstance _selectedInstance;
    private EnhanceEquipSlot _selectedSlot;

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

        _windowStartIndex = 0;

        RefreshOwnedEquipments();
        RefreshCarouselWindow();
        ClearSelection();
        HideResultImmediate();

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
        _ownedEquipments = PlayerInventory.Instance != null
            ? PlayerInventory.Instance.EquipmentInstances
                .Where(instance => instance != null && instance.baseData != null)
                .OrderBy(instance => instance.baseData.itemId)
                .ToList()
            : new List<EquipmentInstance>();
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

        ItemData itemData = instance.baseData;

        if (_baseInfoName != null) _baseInfoName.text = itemData.itemName;
        if (_baseInfoGrade != null) _baseInfoGrade.text = itemData.itemGrade.ToDisplayString();

        if (_currentIcon != null) _currentIcon.sprite = itemData.icon;
        if (_nextIcon != null) _nextIcon.sprite = itemData.icon;

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

        if (_baseInfoName != null) _baseInfoName.text = string.Empty;
        if (_baseInfoGrade != null) _baseInfoGrade.text = string.Empty;

        if (_currentIcon != null) _currentIcon.sprite = null;
        if (_currentLvText != null) _currentLvText.text = string.Empty;
        if (_nextIcon != null) _nextIcon.sprite = null;
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

        if (_enhanceBtn != null) _enhanceBtn.interactable = canEnhance;
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
        if (_enhanceController == null || _selectedInstance == null) return;

        EnhanceLevelEntry attemptedEntry = _enhanceController.GetNextLevelEntry(_selectedInstance);
        if (attemptedEntry == null) return;

        int attemptedLevel = attemptedEntry.level;

        bool isSuccess = _enhanceController.TryEnhance(_selectedInstance);

        ShowResult(isSuccess, attemptedLevel);

        SelectEquipment(_selectedInstance);
    }

    // ===================== Result 팝업 (DOTween 페이드) =====================

    private void ShowResult(bool isSuccess, int attemptedLevel)
    {
        if (_resultText != null)
        {
            _resultText.text = isSuccess ? $"+{attemptedLevel} 강화 성공" : $"+{attemptedLevel} 강화 실패";
        }

        if (_resultCanvasGroup == null) return;

        _resultCanvasGroup.DOKill();
        _resultCanvasGroup.gameObject.SetActive(true);
        _resultCanvasGroup.alpha = 0f;

        DOTween.Sequence()
            .SetTarget(_resultCanvasGroup)
            .Append(_resultCanvasGroup.DOFade(1f, _resultFadeDuration))
            .AppendInterval(_resultShowDuration)
            .Append(_resultCanvasGroup.DOFade(0f, _resultFadeDuration))
            .OnComplete(() => _resultCanvasGroup.gameObject.SetActive(false));
    }

    private void HideResultImmediate()
    {
        if (_resultCanvasGroup == null) return;

        _resultCanvasGroup.DOKill();
        _resultCanvasGroup.alpha = 0f;
        _resultCanvasGroup.gameObject.SetActive(false);
    }

    private void UpdateGoldText(int gold)
    {
        if (_goldText != null)
        {
            _goldText.text = gold.ToString();
        }
    }
}