using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// IntergrationPanel/Equip/StatSection 컨트롤러.
/// - Current: CharacterSelectionManager가 가리키는 "지금 선택된 캐릭터"의 장착 스탯 합 (항상 표시)
/// - Change: 인벤토리에서 미장착 장비를 선택했을 때만 활성화되는 "장착 시 예상 변화" 미리보기
/// * 장착 중인 장비를 볼 때는 Change를 비활성화하고 Current만 보여줌 (InventoryDetailController가 호출)
/// * 선택된 캐릭터가 바뀌면 CharacterEquipment 구독을 자동으로 갈아끼움 (BindToSelectedCharacter)
/// * HorizontalLayoutGroup은 Awake에서 딱 한 번만 참고하고 바로 비활성화함.
///   그 이후로는 Current(anchoredPosition.x)와 Change(sizeDelta.x)를 DOTween으로 직접 제어.
///   (레이아웃 그룹이 매 프레임 위치를 강제로 되돌리는 문제를 피하기 위함)
/// </summary>
public class EquipStatSectionController : MonoBehaviour
{
    [Header("Stat Layout")]
    [Tooltip("Stat 오브젝트의 HorizontalLayoutGroup. Awake에서 위치 참고용으로만 쓰고 바로 비활성화함")]
    [SerializeField] private HorizontalLayoutGroup _statLayoutGroup;

    [Header("Current")]
    [Tooltip("Current 오브젝트의 RectTransform")]
    [SerializeField] private RectTransform _currentSection;
    [SerializeField] private EquipStatRow _currentHp;
    [SerializeField] private EquipStatRow _currentMp;
    [SerializeField] private EquipStatRow _currentPower;
    [SerializeField] private EquipStatRow _currentInt;
    [SerializeField] private EquipStatRow _currentDef;
    [SerializeField] private EquipStatRow _currentSpd;
    [SerializeField] private EquipStatRow _currentLuk;

    [Header("Change")]
    [Tooltip("Change 오브젝트의 RectTransform. Pivot X는 0(왼쪽 기준) 권장")]
    [SerializeField] private RectTransform _changeSection;
    [Tooltip("Change 오브젝트에 붙인 CanvasGroup (콘텐츠 페이드용)")]
    [SerializeField] private CanvasGroup _changeCanvasGroup;
    [SerializeField] private EquipStatChangeRow _changeHp;
    [SerializeField] private EquipStatChangeRow _changeMp;
    [SerializeField] private EquipStatChangeRow _changePower;
    [SerializeField] private EquipStatChangeRow _changeInt;
    [SerializeField] private EquipStatChangeRow _changeDef;
    [SerializeField] private EquipStatChangeRow _changeSpd;
    [SerializeField] private EquipStatChangeRow _changeLuk;

    [Header("Change 애니메이션")]
    [SerializeField] private float _changeWidthDuration = 0.25f;
    [SerializeField] private float _changeFadeDuration = 0.15f;

    // Awake 시점에 에디터에 배치된 "Change 켜진 상태" 좌표/너비를 기준으로 계산해둠
    private float _currentPairedX;   // Change가 켜져있을 때 Current의 x
    private float _currentCenteredX; // Change가 꺼져있을 때(Current 혼자) Current의 x
    private float _changeTargetWidth;

    private bool _isChangeVisible;

    // 지금 이 화면이 보여주고 있는 캐릭터의 CharacterEquipment (선택 캐릭터 바뀌면 다시 바인딩됨)
    private CharacterEquipment _boundEquipment;

    private void Awake()
    {
        if (_currentSection != null)
        {
            _currentPairedX = _currentSection.anchoredPosition.x;
        }

        if (_changeSection != null)
        {
            _changeTargetWidth = _changeSection.rect.width;
        }

        // spacing 0 기준: Change가 사라지면 Current는 그만큼의 절반만큼 오른쪽(중앙)으로 이동
        _currentCenteredX = _currentPairedX + _changeTargetWidth / 2f;

        // 이제부터는 레이아웃 그룹 대신 직접 좌표 제어
        if (_statLayoutGroup != null)
        {
            _statLayoutGroup.enabled = false;
        }
    }

    private void OnEnable()
    {
        // 패널이 새로 열릴 때는 애니메이션 없이 즉시 "Current만 중앙" 상태로 초기화
        _isChangeVisible = false;

        if (_currentSection != null)
        {
            _currentSection.DOKill();
            _currentSection.anchoredPosition = new Vector2(_currentCenteredX, _currentSection.anchoredPosition.y);
        }

        if (_changeSection != null)
        {
            _changeSection.DOKill();
            _changeSection.sizeDelta = new Vector2(0f, _changeSection.sizeDelta.y);
            _changeSection.gameObject.SetActive(false);
        }

        if (_changeCanvasGroup != null)
        {
            _changeCanvasGroup.DOKill();
            _changeCanvasGroup.alpha = 0f;
        }

        BindToSelectedCharacter();

        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.OnSelectionChanged += HandleCharacterSelectionChanged;
        }
    }

    private void OnDisable()
    {
        if (_boundEquipment != null)
        {
            _boundEquipment.OnEquipmentChanged -= RefreshCurrent;
            _boundEquipment = null;
        }

        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.OnSelectionChanged -= HandleCharacterSelectionChanged;
        }
    }

    /// <summary>
    /// 캐릭터 탭이 바뀌었을 때 호출. 이전 캐릭터 구독 해제하고 새 캐릭터로 다시 구독 + Change 미리보기는 초기화.
    /// </summary>
    private void HandleCharacterSelectionChanged(CharacterType character)
    {
        BindToSelectedCharacter();
        HideChangePreview();
    }

    /// <summary>
    /// CharacterSelectionManager가 가리키는 현재 캐릭터의 CharacterEquipment로 (재)구독.
    /// </summary>
    private void BindToSelectedCharacter()
    {
        if (_boundEquipment != null)
        {
            _boundEquipment.OnEquipmentChanged -= RefreshCurrent;
        }

        _boundEquipment = CharacterSelectionManager.Instance != null
            ? CharacterEquipment.GetByCharacter(CharacterSelectionManager.Instance.GetSelected())
            : null;

        if (_boundEquipment != null)
        {
            _boundEquipment.OnEquipmentChanged += RefreshCurrent;
        }

        RefreshCurrent();
    }

    /// <summary>
    /// 현재 장착 중인 장비들의 스탯 합으로 Current 섹션 갱신.
    /// </summary>
    private void RefreshCurrent()
    {
        if (_boundEquipment == null) return;

        StatBlock total = _boundEquipment.TotalEquipmentStats;

        if (_currentHp != null) _currentHp.SetValue(total.maxHP);
        if (_currentMp != null) _currentMp.SetValue(total.maxMP);
        if (_currentPower != null) _currentPower.SetValue(total.magicPower);
        if (_currentInt != null) _currentInt.SetValue(total.intelligence);
        if (_currentDef != null) _currentDef.SetValue(total.defense);
        if (_currentSpd != null) _currentSpd.SetValue(total.speed);
        if (_currentLuk != null) _currentLuk.SetValue(total.luck);
    }

    /// <summary>
    /// 인벤토리에서 미장착 장비를 선택했을 때 호출. "이걸 장착하면 어떻게 바뀌는지" 미리보기 표시.
    /// 같은 슬롯에 이미 장착된 게 있으면 그건 빼고, 새로 선택한 장비 스탯을 더해서 계산.
    /// </summary>
    public void ShowChangePreview(EquipmentInstance candidate)
    {
        if (_boundEquipment == null || candidate == null || candidate.baseData == null)
        {
            HideChangePreview();
            return;
        }

        StatBlock hypothetical = _boundEquipment.TotalEquipmentStats.Clone();

        EquipmentInstance currentInSlot = _boundEquipment.GetEquipped(candidate.baseData.equipSlotType);
        if (currentInSlot != null)
        {
            ApplyStatSet(hypothetical, currentInSlot.cachedStats, -1);
        }

        ApplyStatSet(hypothetical, candidate.cachedStats, +1);

        StatBlock current = _boundEquipment.TotalEquipmentStats;

        SetChangeRow(_changeHp, hypothetical.maxHP, current.maxHP);
        SetChangeRow(_changeMp, hypothetical.maxMP, current.maxMP);
        SetChangeRow(_changePower, hypothetical.magicPower, current.magicPower);
        SetChangeRow(_changeInt, hypothetical.intelligence, current.intelligence);
        SetChangeRow(_changeDef, hypothetical.defense, current.defense);
        SetChangeRow(_changeSpd, hypothetical.speed, current.speed);
        SetChangeRow(_changeLuk, hypothetical.luck, current.luck);

        // 이미 펼쳐진 상태면 내용만 갱신하고 애니메이션은 다시 재생하지 않음
        if (!_isChangeVisible)
        {
            AnimateChangeIn();
        }
    }

    /// <summary>
    /// Change 섹션 비활성화 (장착 중인 장비를 보고 있거나, 아무것도 선택 안 했을 때)
    /// </summary>
    public void HideChangePreview()
    {
        if (!_isChangeVisible)
        {
            if (_changeSection != null) _changeSection.gameObject.SetActive(false);
            return;
        }

        AnimateChangeOut();
    }

    private void AnimateChangeIn()
    {
        _isChangeVisible = true;

        if (_changeSection != null)
        {
            _changeSection.gameObject.SetActive(true);
            _changeSection.DOKill();
            _changeSection.DOSizeDelta(new Vector2(_changeTargetWidth, _changeSection.sizeDelta.y), _changeWidthDuration)
                .SetEase(Ease.OutQuad);
        }

        if (_currentSection != null)
        {
            _currentSection.DOKill();
            _currentSection.DOAnchorPosX(_currentPairedX, _changeWidthDuration).SetEase(Ease.OutQuad);
        }

        if (_changeCanvasGroup != null)
        {
            _changeCanvasGroup.DOKill();
            _changeCanvasGroup.alpha = 0f;
            _changeCanvasGroup.DOFade(1f, _changeFadeDuration)
                .SetDelay(_changeWidthDuration * 0.5f);
        }
    }

    private void AnimateChangeOut()
    {
        _isChangeVisible = false;

        if (_changeCanvasGroup != null)
        {
            _changeCanvasGroup.DOKill();
            _changeCanvasGroup.DOFade(0f, _changeFadeDuration);
        }

        if (_currentSection != null)
        {
            _currentSection.DOKill();
            _currentSection.DOAnchorPosX(_currentCenteredX, _changeWidthDuration).SetEase(Ease.InQuad);
        }

        if (_changeSection != null)
        {
            _changeSection.DOKill();
            _changeSection.DOSizeDelta(new Vector2(0f, _changeSection.sizeDelta.y), _changeWidthDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => _changeSection.gameObject.SetActive(false));
        }
    }

    private void SetChangeRow(EquipStatChangeRow row, int newValue, int currentValue)
    {
        if (row == null) return;

        row.SetValue(newValue, newValue - currentValue);
    }

    private void ApplyStatSet(StatBlock block, EquipStatCalculator.StatSet stats, int sign)
    {
        block.Add(StatType.MaxHP, stats.hp * sign);
        block.Add(StatType.MaxMP, stats.mp * sign);
        block.Add(StatType.SpellPower, stats.spellPower * sign);
        block.Add(StatType.Intelligence, stats.intelligence * sign);
        block.Add(StatType.Defense, stats.defense * sign);
        block.Add(StatType.Speed, stats.speed * sign);
        block.Add(StatType.Luck, stats.luck * sign);
    }
}