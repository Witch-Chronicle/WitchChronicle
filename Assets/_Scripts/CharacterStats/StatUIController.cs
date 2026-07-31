using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StatPanel 전체를 관리하는 컨트롤러. StatPanel 자체에 붙어있음(CanvasGroup + UIPanelAnimator도 StatPanel에 있음).
/// - Base: 레벨/경험치/스탯 포인트 분배 화면
/// - Detail: 전투 스탯(CombatStat) 상세보기 화면. Base와 별도 UIPanelAnimator로 독립적으로 열고닫힘.
///   단, StatPanel 자체가 어떤 경로로든(C키/CloseBtn) 닫히면 OnDisable()에서 Detail도 무조건 같이 닫힘.
/// - StatType별 툴팁(LabelTxt 호버 시 설명 표시)도 여기서 초기화
/// * StatController는 건드리지 않고 CharacterEquipment 레지스트리를 경유해서 캐릭터별 StatController를 찾음.
/// * StatPanel 열기/닫기 자체는 PlayerUIInputReader.Instance.ToggleStatPanel()이 담당.
/// </summary>
public class StatUIController : MonoBehaviour
{
    /// <summary>
    /// Detail 패널에 표시할 전투 스탯 종류. CharacterStats의 Combat 프로퍼티들과 1:1 매핑.
    /// </summary>
    public enum CombatStatType
    {
        MaxHp,
        MaxMp,
        AttackPower,
        MagicPower,
        Defense,
        MagicDefense,
        Speed,
        Luck
    }

    [Serializable]
    public class StatAllocationRow
    {
        [Tooltip("이 행이 어떤 StatType에 해당하는지 (HP_AllocatePoint -> MaxHP 등)")]
        public StatType statType;
        public TMP_Text labelTxt;   // 툴팁 트리거를 붙일 대상
        public TMP_Text valueTxt;
        public Button plusBtn;
    }

    [Serializable]
    public class CombatStatRow
    {
        [Tooltip("이 행이 어떤 전투 스탯인지 (HP_CombatStat -> MaxHp 등)")]
        public CombatStatType statType;
        public TMP_Text valueTxt;
    }

    [Serializable]
    public class StatTooltipEntry
    {
        public StatType statType;

        [TextArea(3, 6)]
        public string tooltipText;
    }

    [Header("Panel - Base")]
    [SerializeField] private Button _closeBtn;

    [Header("Base Info")]
    [SerializeField] private TMP_Text _characterNameTxt;
    [SerializeField] private TMP_Text _levelTxt;
    [SerializeField] private TMP_Text _expTxt;
    [SerializeField] private Slider _expSlider;

    [Header("Stat Info")]
    [SerializeField] private TMP_Text _availableStatTxt;

    [Header("Stat Rows (HP/MP/ATK/INT/DEF/SPD/LUK 순서대로 7개 등록)")]
    [SerializeField] private List<StatAllocationRow> _statRows = new List<StatAllocationRow>();

    [Header("Stat Tooltips (타입별 설명 텍스트, 수치까지 직접 기재)")]
    [SerializeField] private List<StatTooltipEntry> _statTooltips = new List<StatTooltipEntry>();

    [Header("Stat Etc")]
    [SerializeField] private Button _detailButton;
    [SerializeField] private Button _resetBtn;

    [Header("Panel - Detail")]
    [SerializeField] private UIPanelAnimator _detailPanelAnimator;
    [SerializeField] private Button _detailCloseBtn;
    [SerializeField] private Button _detailBackgroundBtn;

    [Header("Combat Stat Rows (HP/MP/ATK/MAGIC/DEF/MDEF/SPD/LUK 순서대로 8개 등록)")]
    [SerializeField] private List<CombatStatRow> _combatStatRows = new List<CombatStatRow>();

    private StatController _currentStatController;

    private void Awake()
    {
        if (_closeBtn != null) _closeBtn.onClick.AddListener(ClosePanel);
        if (_resetBtn != null) _resetBtn.onClick.AddListener(HandleResetClicked);
        if (_detailButton != null) _detailButton.onClick.AddListener(OpenDetailPanel);
        if (_detailCloseBtn != null) _detailCloseBtn.onClick.AddListener(CloseDetailPanel);
        if (_detailBackgroundBtn != null) _detailBackgroundBtn.onClick.AddListener(CloseDetailPanel);

        // StatType -> 툴팁 텍스트 조회용 딕셔너리로 변환
        var tooltipLookup = new Dictionary<StatType, string>();
        foreach (var entry in _statTooltips)
        {
            tooltipLookup[entry.statType] = entry.tooltipText;
        }

        foreach (var row in _statRows)
        {
            if (row.plusBtn != null)
            {
                StatType capturedType = row.statType; // 클로저 캡처 방지용 지역 변수
                row.plusBtn.onClick.AddListener(() => HandlePlusClicked(capturedType));
            }

            if (row.labelTxt != null)
            {
                StatTooltipTrigger trigger = row.labelTxt.GetComponent<StatTooltipTrigger>();
                if (trigger == null)
                {
                    trigger = row.labelTxt.gameObject.AddComponent<StatTooltipTrigger>();
                }

                string text = tooltipLookup.TryGetValue(row.statType, out var t) ? t : string.Empty;
                trigger.SetTooltipText(text);
            }
        }

        if (_detailPanelAnimator != null)
        {
            _detailPanelAnimator.SetClosedImmediate();
        }
    }

    private void OnEnable()
    {
        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.OnSelectionChanged += HandleSelectionChanged;
        }

        BindToSelectedCharacter();
    }

    private void OnDisable()
    {
        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.OnSelectionChanged -= HandleSelectionChanged;
        }

        UnbindCurrent();

        // StatPanel(이 스크립트가 붙은 오브젝트) 자체가 비활성화될 때 호출됨.
        // C키/CloseBtn 등 어떤 경로로 닫히든 항상 여기로 들어오므로, Detail도 여기서 같이 정리.
        if (_detailPanelAnimator != null && _detailPanelAnimator.IsOpen)
        {
            _detailPanelAnimator.SetClosedImmediate();
        }

        // 패널이 닫히면 캐릭터 선택 상태를 기본값으로 초기화 (다른 UI에 이전 선택이 남지 않도록)
        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.ResetToDefault();
        }
    }

    private void ClosePanel()
    {
        // 닫기 전에 클릭 이벤트 제거 (InventoryUIController와 동일 패턴)
        // if (_closeBtn != null) _closeBtn.onClick.RemoveListener(ClosePanel);

        PlayerUIInputReader.Instance.ToggleStatPanel();
    }

    private void OpenDetailPanel()
    {
        if (_detailPanelAnimator != null)
        {
            _detailPanelAnimator.Open();
        }

        RefreshDetailUI();
    }

    private void CloseDetailPanel()
    {
        if (_detailPanelAnimator != null)
        {
            _detailPanelAnimator.Close();
        }
    }

    private void HandleSelectionChanged(CharacterType type)
    {
        BindToSelectedCharacter();
    }

    /// <summary>
    /// CharacterSelectionManager가 현재 가리키는 캐릭터로 다시 바인딩하고 전체 UI 갱신.
    /// </summary>
    private void BindToSelectedCharacter()
    {
        UnbindCurrent();

        if (CharacterSelectionManager.Instance == null) return;

        CharacterType selected = CharacterSelectionManager.Instance.GetSelected();
        string characterId = selected.ToString();

        if (PersistentCharacterManager.Instance == null)
        {
            Debug.LogWarning("[StatUIController] PersistentCharacterManager.Instance가 없습니다.");
            return;
        }

        if (PersistentCharacterManager.Instance.TryGetCharacter(characterId, out PersistentCharacterUnit unit) == false)
        {
            Debug.LogWarning($"[StatUIController] PersistentCharacterUnit를 찾을 수 없음: {selected}");
            return;
        }

        _currentStatController = unit.StatController;

        if (_currentStatController == null)
        {
            Debug.LogWarning($"[StatUIController] StatController를 찾을 수 없음: {selected}");
            return;
        }

        _currentStatController.OnStatsChanged += RefreshUI;
        _currentStatController.OnStatsChanged += RefreshDetailUI;

        if (_characterNameTxt != null)
        {
            _characterNameTxt.text = unit.CharacterName;
        }

        RefreshUI();
        RefreshDetailUI();
    }

    private void UnbindCurrent()
    {
        if (_currentStatController != null)
        {
            _currentStatController.OnStatsChanged -= RefreshUI;
            _currentStatController.OnStatsChanged -= RefreshDetailUI;
            _currentStatController = null;
        }
    }

    /// <summary>
    /// 현재 바인딩된 캐릭터 기준으로 레벨/경험치/스탯값/보유 포인트 UI를 전부 갱신.
    /// StatController.OnStatsChanged가 발생할 때마다 자동으로도 호출됨.
    /// </summary>
    private void RefreshUI()
    {
        if (_currentStatController == null) return;

        int level = _currentStatController.Level;
        int exp = _currentStatController.Exp;
        int expToNext = _currentStatController.ExpToNextLevel;
        int availablePoints = _currentStatController.AvailablePoints;

        if (_levelTxt != null) _levelTxt.text = $"Lv. {level}";
        if (_expTxt != null) _expTxt.text = $"{exp}/{expToNext}";
        if (_expSlider != null) _expSlider.value = expToNext > 0 ? (float)exp / expToNext : 0f;
        if (_availableStatTxt != null) _availableStatTxt.text = $"보유 스탯 포인트 : {availablePoints}";

        CharacterStats characterStats = _currentStatController.Stats;

        foreach (var row in _statRows)
        {
            if (row.valueTxt != null && characterStats != null)
            {
                row.valueTxt.text = GetAllocatedPointText(characterStats, row.statType).ToString();
            }

            if (row.plusBtn != null)
            {
                row.plusBtn.interactable = availablePoints > 0;
            }
        }
    }

    /// <summary>
    /// StatType에 맞는 CharacterStats.Allocated~ 프로퍼티를 찾아서 반환.
    /// (해당 스탯에 직접 투자한 포인트 개수)
    /// </summary>
    private int GetAllocatedPointText(CharacterStats stats, StatType type)
    {
        switch (type)
        {
            case StatType.MaxHP: return stats.AllocatedHp;
            case StatType.MaxMP: return stats.AllocatedMp;
            case StatType.SpellPower: return stats.AllocatedSpellPower;
            case StatType.Intelligence: return stats.AllocatedIntelligence;
            case StatType.Defense: return stats.AllocatedDefense;
            case StatType.Speed: return stats.AllocatedSpeed;
            case StatType.Luck: return stats.AllocatedLuck;
            default: return 0;
        }
    }

    /// <summary>
    /// Detail 패널의 전투 스탯 8종 표시 갱신. CombatStat들은 float라서 정수로 반올림해서 표시.
    /// </summary>
    private void RefreshDetailUI()
    {
        if (_currentStatController == null) return;

        CharacterStats stats = _currentStatController.Stats;
        if (stats == null) return;

        foreach (var row in _combatStatRows)
        {
            if (row.valueTxt == null) continue;

            row.valueTxt.text = GetCombatStatText(stats, row.statType);
        }
    }

    private string GetCombatStatText(CharacterStats stats, CombatStatType type)
    {
        switch (type)
        {
            case CombatStatType.MaxHp: return stats.CombatMaxHp.ToString();
            case CombatStatType.MaxMp: return stats.CombatMaxMp.ToString();
            case CombatStatType.AttackPower: return Mathf.RoundToInt(stats.CombatAttackPower).ToString();
            case CombatStatType.MagicPower: return Mathf.RoundToInt(stats.CombatMagicPower).ToString();
            case CombatStatType.Defense: return Mathf.RoundToInt(stats.CombatDefensePower).ToString();
            case CombatStatType.MagicDefense: return Mathf.RoundToInt(stats.CombatMagicDefensePower).ToString();
            case CombatStatType.Speed: return Mathf.RoundToInt(stats.CombatSpeed).ToString();
            case CombatStatType.Luck: return Mathf.RoundToInt(stats.CombatLuck).ToString();
            default: return "-";
        }
    }

    private void HandlePlusClicked(StatType type)
    {
        if (_currentStatController == null) return;

        _currentStatController.AllocatePoint(type);
        // RefreshUI는 StatController.OnStatsChanged 이벤트로 자동 호출됨
    }

    private void HandleResetClicked()
    {
        if (_currentStatController == null) return;

        _currentStatController.TryResetAllocations();
        RefreshUI();
    }
}