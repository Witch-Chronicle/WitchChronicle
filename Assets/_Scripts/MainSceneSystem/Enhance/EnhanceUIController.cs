using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// EnhancePanel 메인 컨트롤러.
/// - EquipList: 보유 장비 전체를 나열 (CategorySection 필터링은 추후 추가 예정, 현재 비활성화)
/// - 슬롯 클릭 시 Enhance/Info + Enhance/Preview + Enhance/Execute에 정보 표시
/// - EnhanceBtn 클릭 시 EnhanceController에 강화 실행을 위임
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

    [Header("Equip List")]
    [SerializeField] private EnhanceEquipSlot _equipSlotPrefab;
    [SerializeField] private Transform _equipListParent; // Content

    [Header("Enhance Info")]
    [SerializeField] private Image _infoIcon;
    [SerializeField] private TextMeshProUGUI _infoName;
    [SerializeField] private TextMeshProUGUI _infoGrade;
    [SerializeField] private TextMeshProUGUI _currentLvText;
    [SerializeField] private TextMeshProUGUI _nextLvText;

    [Header("Enhance Preview")]
    [SerializeField] private EnhancePreviewRow _previewRowPrefab;
    [SerializeField] private Transform _previewRoot; // Preview

    [Header("Enhance Execute - SuccessRate")]
    [SerializeField] private TextMeshProUGUI _successRateText; // RateTxt
    [Tooltip("천장까지의 진행률(%) 표시용. 없으면 그냥 비워둬도 됨")]
    [SerializeField] private TextMeshProUGUI _pityProgressText;
    [Tooltip("천장까지의 진행률을 0~1로 표시하는 슬라이더. 없으면 비워둬도 됨")]
    [SerializeField] private Slider _pitySlider;
    [Tooltip("진행률이 100%일 때(확정 성공)만 활성화할 안내 오브젝트")]
    [SerializeField] private GameObject _pityGuaranteedObject;
    [Tooltip("진행률이 100%가 아닐 때(=확정 성공이 아닐 때) 활성화할 안내/툴팁 오브젝트")]
    [SerializeField] private GameObject _tooltipObject;

    [Header("Enhance Execute - Required")]
    [SerializeField] private EnhanceRequiredRow _requiredRowPrefab;
    [SerializeField] private Transform _requiredRoot; // RequiredRoot

    [Header("Enhance Execute - Btn")]
    [SerializeField] private Button _enhanceBtn;

    [Header("Result")]
    [Tooltip("Result 오브젝트에 CanvasGroup 붙여서 연결 (페이드 처리용)")]
    [SerializeField] private CanvasGroup _resultCanvasGroup;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private float _resultFadeDuration = 0.25f;
    [SerializeField] private float _resultShowDuration = 1.2f;

    private readonly List<GameObject> _spawnedEquipSlots = new List<GameObject>();
    private readonly List<GameObject> _spawnedPreviewRows = new List<GameObject>();
    private readonly List<GameObject> _spawnedEnhanceRequiredRows = new List<GameObject>();

    private EquipmentInstance _selectedInstance;
    private EnhanceEquipSlot _selectedSlot;

    private void OnEnable()
    {
        if (_closeBtn != null) _closeBtn.onClick.AddListener(OnClickClose);
        if (_enhanceBtn != null) _enhanceBtn.onClick.AddListener(OnClickEnhance);

        RefreshEquipList();
        ClearSelection();
        HideResultImmediate();

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnGoldChanged += UpdateGoldText;
            UpdateGoldText(PlayerInventory.Instance.Gold);

            PlayerInventory.Instance.OnInventoryChanged += RefreshEquipList;
        }
    }

    private void OnDisable()
    {
        if (_closeBtn != null) _closeBtn.onClick.RemoveListener(OnClickClose);
        if (_enhanceBtn != null) _enhanceBtn.onClick.RemoveListener(OnClickEnhance);

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnGoldChanged -= UpdateGoldText;
            PlayerInventory.Instance.OnInventoryChanged -= RefreshEquipList;
        }
    }

    private void OnClickClose()
    {
        // 닫기 전에 클릭 이벤트 제거
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

    // ===================== Equip List =====================

    private void RefreshEquipList()
    {
        ClearEquipSlots();

        if (PlayerInventory.Instance == null || _equipSlotPrefab == null || _equipListParent == null)
        {
            return;
        }

        var equipmentList = PlayerInventory.Instance.EquipmentInstances
            .OrderBy(instance => instance.baseData.itemId);

        foreach (var instance in equipmentList)
        {
            EnhanceEquipSlot slot = Instantiate(_equipSlotPrefab, _equipListParent);
            slot.Setup(instance, HandleEquipSlotClicked);
            _spawnedEquipSlots.Add(slot.gameObject);
        }

        // 목록이 갱신된 뒤에도 선택 중이던 장비가 있으면 그 정보를 최신 상태로 다시 표시
        if (_selectedInstance != null)
        {
            SelectEquipment(_selectedInstance);
        }
    }

    private void ClearEquipSlots()
    {
        foreach (var slotObj in _spawnedEquipSlots)
        {
            Destroy(slotObj);
        }
        _spawnedEquipSlots.Clear();
        _selectedSlot = null;
    }

    private void HandleEquipSlotClicked(EquipmentInstance equipmentInstance)
    {
        SelectEquipment(equipmentInstance);
    }

    // ===================== 선택 / 미리보기 / Execute =====================

    private void SelectEquipment(EquipmentInstance instance)
    {
        _selectedInstance = instance;

        UpdateSelectedSlotHighlight(instance);

        ItemData itemData = instance.baseData;

        if (_infoIcon != null) _infoIcon.sprite = itemData.icon;
        if (_infoName != null) _infoName.text = itemData.itemName;
        if (_infoGrade != null) _infoGrade.text = itemData.itemGrade.ToDisplayString();

        int currentLevel = instance.enhanceLevel;
        EnhanceLevelEntry nextEntry = _enhanceController != null
            ? _enhanceController.GetNextLevelEntry(instance)
            : null;

        bool isMaxLevel = nextEntry == null;

        if (_currentLvText != null) _currentLvText.text = $"+{currentLevel}";
        if (_nextLvText != null) _nextLvText.text = isMaxLevel ? "MAX" : $"+{nextEntry.level}";

        // 등급별 테이블로 변경 - 이 장비의 등급에 맞는 테이블을 조회
        EnhanceTableData table = _enhanceController != null
            ? _enhanceController.GetTable(itemData.itemGrade)
            : null;

        EquipStatCalculator.StatSet currentStats = instance.cachedStats;
        EquipStatCalculator.StatSet nextStats = isMaxLevel
            ? currentStats
            : EquipStatCalculator.GetCurrentStats(itemData as EquipItemData, nextEntry.level, table);

        BuildPreviewRows(currentStats, nextStats);
        UpdateExecuteSection(nextEntry, isMaxLevel);
    }

    private void UpdateSelectedSlotHighlight(EquipmentInstance instance)
    {
        if (_selectedSlot != null)
        {
            _selectedSlot.SetSelected(false);
        }

        _selectedSlot = _spawnedEquipSlots
            .Select(obj => obj.GetComponent<EnhanceEquipSlot>())
            .FirstOrDefault(slot => slot != null && slot.EquipmentInstance == instance);

        if (_selectedSlot != null)
        {
            _selectedSlot.SetSelected(true);
        }
    }

    /// <summary>
    /// 아무것도 선택 안 한 초기 상태로 정보/미리보기/Execute 비우기
    /// </summary>
    private void ClearSelection()
    {
        _selectedInstance = null;
        _selectedSlot = null;

        if (_infoIcon != null) _infoIcon.sprite = null;
        if (_infoName != null) _infoName.text = string.Empty;
        if (_infoGrade != null) _infoGrade.text = string.Empty;
        if (_currentLvText != null) _currentLvText.text = string.Empty;
        if (_nextLvText != null) _nextLvText.text = string.Empty;
        if (_successRateText != null) _successRateText.text = string.Empty;
        if (_pityProgressText != null) _pityProgressText.text = string.Empty;
        if (_pitySlider != null) _pitySlider.value = 0f;
        SetPityGuaranteed(false);
        if (_enhanceBtn != null) _enhanceBtn.interactable = false;

        ClearPreviewRows();
        ClearEnhanceRequiredRows();
    }

    /// <summary>
    /// 0이 아닌 스탯만 골라서 EnhancePreviewRow를 동적으로 생성.
    /// </summary>
    private void BuildPreviewRows(EquipStatCalculator.StatSet currentStats, EquipStatCalculator.StatSet nextStats)
    {
        ClearPreviewRows();

        if (_previewRowPrefab == null || _previewRoot == null) return;

        AddPreviewRowIfNonZero("체력", currentStats.hp, nextStats.hp);
        AddPreviewRowIfNonZero("마나", currentStats.mp, nextStats.mp);
        AddPreviewRowIfNonZero("공격력", currentStats.spellPower, nextStats.spellPower);
        AddPreviewRowIfNonZero("지능", currentStats.intelligence, nextStats.intelligence);
        AddPreviewRowIfNonZero("방어력", currentStats.defense, nextStats.defense);
        AddPreviewRowIfNonZero("속도", currentStats.speed, nextStats.speed);
        AddPreviewRowIfNonZero("행운", currentStats.luck, nextStats.luck);
    }

    private void AddPreviewRowIfNonZero(string label, int currentValue, int nextValue)
    {
        // 원래 0이었던 스탯(강화해도 계속 0)은 미리보기에 표시할 필요 없음
        if (currentValue == 0) return;

        EnhancePreviewRow row = Instantiate(_previewRowPrefab, _previewRoot);
        row.Setup(label, currentValue, nextValue);
        _spawnedPreviewRows.Add(row.gameObject);
    }

    private void ClearPreviewRows()
    {
        foreach (var rowObj in _spawnedPreviewRows)
        {
            Destroy(rowObj);
        }
        _spawnedPreviewRows.Clear();
    }

    /// <summary>
    /// 성공확률 + 필요 골드/재료(보유량 대비) 표시, 강화 버튼 활성화 여부 결정.
    /// </summary>
    private void UpdateExecuteSection(EnhanceLevelEntry nextEntry, bool isMaxLevel)
    {
        ClearEnhanceRequiredRows();

        if (isMaxLevel || nextEntry == null)
        {
            if (_successRateText != null) _successRateText.text = "MAX";
            if (_pityProgressText != null) _pityProgressText.text = string.Empty;
            if (_pitySlider != null) _pitySlider.value = 0f;
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

            if (_pitySlider != null)
            {
                // 슬라이더는 0~1 기준. 퍼센트(0~100)를 0~1로 정규화.
                _pitySlider.value = pityProgress / 100f;
            }

            SetPityGuaranteed(isPityMax);
        }
        else
        {
            if (_pityProgressText != null) _pityProgressText.text = string.Empty;
            if (_pitySlider != null) _pitySlider.value = 0f;
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

    /// <summary>
    /// 확정 성공(100%) 여부에 따라 _pityGuaranteedObject / _tooltipObject를 서로 반대로 토글.
    /// isGuaranteed가 true면 확정성공 안내만 보이고, false면 툴팁만 보임.
    /// </summary>
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

        // 강화 성공하면 enhanceLevel이 바뀌어버리니, 시도한 단계는 미리 캡처해둔다
        EnhanceLevelEntry attemptedEntry = _enhanceController.GetNextLevelEntry(_selectedInstance);
        if (attemptedEntry == null) return; // 이미 최대 단계 (버튼이 비활성화되어 있어야 하지만 방어적으로 체크)

        int attemptedLevel = attemptedEntry.level;

        bool isSuccess = _enhanceController.TryEnhance(_selectedInstance);

        ShowResult(isSuccess, attemptedLevel);

        // 성공/실패 상관없이 최신 상태로 다시 그려줌 (골드/재료 소모, 강화 성공 시 단계 반영)
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

    /// <summary>
    /// 애니메이션 없이 즉시 숨김 (패널이 새로 열릴 때 초기화용)
    /// </summary>
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