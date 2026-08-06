using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Pool;
using TMPro;
using DG.Tweening;

/// <summary>
/// 스킬 장착 패널 메인 컨트롤러.
///
/// 선택 규칙:
/// - EquippedSlot은 스킬이 있는 슬롯만 클릭 가능(선택 UI는 해제용 진입점)
/// - OwnedSkillSlot 클릭 시 EquippedSlot 선택은 무조건 해제됨
/// - 장착(Equip) 대상은 항상 "현재 캐릭터의 첫 빈 슬롯" (선택된 슬롯에 넣는 방식 아님)
/// - EquipBtn은 빈 슬롯 유무와 무관하게 조건 맞으면 항상 활성화(빈 슬롯 없으면 클릭해도 무시, TODO: Alert)
/// </summary>
public class SkillEquipUIController : MonoBehaviour
{
    [Header("창")]
    [SerializeField] private Button _closeButton;

    [Header("장착 슬롯 (고정 6개)")]
    [SerializeField] private SkillEquipEquippedSlot[] _equippedSlots;

    [Header("보유 스킬 목록 (풀링)")]
    [SerializeField] private Transform _ownedSkillContent;
    [SerializeField] private SkillEquipOwnedSlot _ownedSkillPrefab;

    [Header("Detail")]
    [SerializeField] private CanvasGroup _detailCanvasGroup;
    [SerializeField] private Image _detailSkillIcon;
    [SerializeField] private TMP_Text _detailSkillNameTxt;
    [SerializeField] private TMP_Text _detailSkillTierTxt;
    [SerializeField] private TMP_Text _detailDescriptionTxt;
    [SerializeField] private TMP_Text _elementValueTxt;
    [SerializeField] private TMP_Text _skillTypeValueTxt;
    [SerializeField] private TMP_Text _targetTypeValueTxt;
    [SerializeField] private TMP_Text _damageTypeValueTxt;
    [SerializeField] private TMP_Text _powerValueTxt;

    [Header("Detail 버튼")]
    [SerializeField] private Button _equipButton;
    [SerializeField] private Button _unequipButton;

    [Header("Main 슬라이드 애니메이션")]
    [SerializeField] private RectTransform _mainRect;
    [SerializeField] private float _mainDefaultX = 0f;
    [SerializeField] private float _mainSelectedX = -264.03f;
    [SerializeField] private float _slideDuration = 0.25f;
    [SerializeField] private Ease _slideEase = Ease.OutQuad;

    private readonly List<PersistentCharacterUnit> _party = new List<PersistentCharacterUnit>();
    private readonly List<SkillData> _learned = new List<SkillData>();
    private readonly List<SkillEquipOwnedSlot> _activeOwnedSlots = new List<SkillEquipOwnedSlot>();

    private ObjectPool<SkillEquipOwnedSlot> _ownedSlotPool;

    /// <summary>현재 선택된 스킬. Equipped/Owned 어느 쪽에서 선택했든 이 값 하나로 통일 관리.</summary>
    private SkillData _selectedSkill;

    /// <summary>선택된 스킬이 EquippedSlot에서 선택된 경우에만 그 슬롯 인덱스(하이라이트/해제용). 아니면 -1.</summary>
    private int _selectedEquippedSlotIndex = -1;

    private bool _hasSelectionVisual;
    private bool _needsImmediateVisual;
    private Tween _mainSlideTween;
    private Tween _detailFadeTween;

    private void Awake()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(HandleCloseClicked);
        }

        if (_equipButton != null) _equipButton.onClick.AddListener(OnClickEquip);
        if (_unequipButton != null) _unequipButton.onClick.AddListener(OnClickUnequip);

        _ownedSlotPool = new ObjectPool<SkillEquipOwnedSlot>(
            createFunc: () => Instantiate(_ownedSkillPrefab, _ownedSkillContent),
            actionOnGet: slot => slot.gameObject.SetActive(true),
            actionOnRelease: slot => slot.ResetSlot(),
            actionOnDestroy: slot => Destroy(slot.gameObject),
            collectionCheck: false);
    }

    private void OnDestroy()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(HandleCloseClicked);
        }

        if (_equipButton != null) _equipButton.onClick.RemoveListener(OnClickEquip);
        if (_unequipButton != null) _unequipButton.onClick.RemoveListener(OnClickUnequip);

        _mainSlideTween?.Kill();
        _detailFadeTween?.Kill();
    }

    private void OnEnable()
    {
        _selectedEquippedSlotIndex = -1;
        _selectedSkill = null;
        _needsImmediateVisual = true;

        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.OnSelectionChanged += HandleCharacterChanged;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.OnSelectionChanged -= HandleCharacterChanged;
        }

        _mainSlideTween?.Kill();
        _detailFadeTween?.Kill();

        for (int i = 0; i < _activeOwnedSlots.Count; i++)
        {
            _ownedSlotPool.Release(_activeOwnedSlots[i]);
        }
        _activeOwnedSlots.Clear();
    }

    private void HandleCloseClicked()
    {
        PlayerUIInputReader.Instance?.ToggleSkillEquipPanel();
    }

    private void HandleCharacterChanged(CharacterType _)
    {
        _selectedEquippedSlotIndex = -1;
        _selectedSkill = null;

        Refresh();
    }

    public void Refresh()
    {
        SkillEquipService.GetPartyMembers(_party);

        if (SkillInventory.Instance != null)
        {
            SkillInventory.Instance.SyncPartyEquippedSkills();
        }

        RefreshEquippedSlots();
        RefreshOwnedList();
        RefreshDetail();
    }

    private PersistentCharacterUnit GetCurrentCharacter()
    {
        if (CharacterSelectionManager.Instance == null) return null;

        CharacterType selected = CharacterSelectionManager.Instance.GetSelected();

        for (int i = 0; i < _party.Count; i++)
        {
            PersistentCharacterUnit member = _party[i];

            if (member != null && member.CharacterEquipment != null
                && member.CharacterEquipment.Character == selected)
            {
                return member;
            }
        }

        return null;
    }

    // ───────────────────────── 장착 슬롯 ─────────────────────────

    private void RefreshEquippedSlots()
    {
        if (_equippedSlots == null) return;

        PersistentCharacterUnit current = GetCurrentCharacter();
        int slotCount = SkillEquipService.GetSlotCount(current);

        for (int i = 0; i < _equippedSlots.Length; i++)
        {
            SkillEquipEquippedSlot slot = _equippedSlots[i];
            if (slot == null) continue;

            if (i < slotCount)
            {
                SkillData equipped = SkillEquipService.GetEquippedAt(current, i);
                slot.Bind(i, equipped, i == _selectedEquippedSlotIndex, OnClickEquippedSlot);
            }
            else
            {
                slot.Clear();
            }
        }
    }

    /// <summary>빈 슬롯은 애초에 컴포넌트에서 콜백을 호출하지 않으므로, 여기 들어오는 slotIndex는 항상 스킬이 있는 슬롯.</summary>
    private void OnClickEquippedSlot(int slotIndex)
    {
        if (_selectedEquippedSlotIndex == slotIndex)
        {
            // 다시 클릭 → 선택 해제
            _selectedEquippedSlotIndex = -1;
            _selectedSkill = null;
        }
        else
        {
            PersistentCharacterUnit current = GetCurrentCharacter();

            _selectedEquippedSlotIndex = slotIndex;
            _selectedSkill = SkillEquipService.GetEquippedAt(current, slotIndex);
        }

        RefreshEquippedSlots();
        RefreshOwnedList();
        RefreshDetail();
    }

    // ───────────────────────── 보유 스킬 목록 ─────────────────────────

    private void RefreshOwnedList()
    {
        for (int i = 0; i < _activeOwnedSlots.Count; i++)
        {
            _ownedSlotPool.Release(_activeOwnedSlots[i]);
        }
        _activeOwnedSlots.Clear();

        if (SkillInventory.Instance == null) return;

        SkillInventory.Instance.GetLearnedSkills(_learned);

        for (int i = 0; i < _learned.Count; i++)
        {
            SkillData skill = _learned[i];

            SkillEquipService.TryFindEquippedOwner(_party, skill, out PersistentCharacterUnit owner, out _);
            string ownerName = owner != null ? owner.CharacterName : null;

            SkillEquipOwnedSlot slot = _ownedSlotPool.Get();
            slot.transform.SetAsLastSibling();
            slot.Bind(skill, skill == _selectedSkill, ownerName, OnClickOwnedSkill);

            _activeOwnedSlots.Add(slot);
        }
    }

    private void OnClickOwnedSkill(SkillData skill)
    {
        // Owned 클릭 시 Equipped 쪽 선택은 무조건 해제
        _selectedEquippedSlotIndex = -1;

        _selectedSkill = (_selectedSkill == skill) ? null : skill;

        RefreshEquippedSlots();
        RefreshOwnedList();
        RefreshDetail();
    }

    // ───────────────────────── Detail ─────────────────────────

    private void RefreshDetail()
    {
        PersistentCharacterUnit current = GetCurrentCharacter();
        SkillData displaySkill = _selectedSkill;

        bool hasSkill = displaySkill != null;

        ApplySelectionVisual(hasSkill);

        if (hasSkill == false)
        {
            SetButtonActive(_equipButton, false);
            SetButtonActive(_unequipButton, false);
            return;
        }

        if (_detailSkillIcon != null)
        {
            _detailSkillIcon.sprite = displaySkill.SkillIcon;
            _detailSkillIcon.enabled = displaySkill.SkillIcon != null;
        }

        if (_detailSkillNameTxt != null) _detailSkillNameTxt.text = displaySkill.SkillName;
        if (_detailSkillTierTxt != null) _detailSkillTierTxt.text = SkillTextFormatter.GetTierText(displaySkill.Tier);
        if (_detailDescriptionTxt != null) _detailDescriptionTxt.text = displaySkill.Description;

        if (_elementValueTxt != null) _elementValueTxt.text = SkillTextFormatter.GetElementTypeText(displaySkill.ElementType);
        if (_skillTypeValueTxt != null) _skillTypeValueTxt.text = SkillTextFormatter.GetSkillTypeText(displaySkill.SkillType);
        if (_targetTypeValueTxt != null) _targetTypeValueTxt.text = SkillTextFormatter.GetTargetTypeText(displaySkill.TargetType);
        if (_damageTypeValueTxt != null) _damageTypeValueTxt.text = SkillTextFormatter.GetDamageTypeText(displaySkill.DamageType);
        if (_powerValueTxt != null) _powerValueTxt.text = displaySkill.Power.ToString();

        RefreshDetailButtons(current, displaySkill);
    }

    private void ApplySelectionVisual(bool hasSkill)
    {
        bool changed = hasSkill != _hasSelectionVisual;

        if (changed == false && _needsImmediateVisual == false)
        {
            return;
        }

        bool animate = _needsImmediateVisual == false;

        _hasSelectionVisual = hasSkill;
        _needsImmediateVisual = false;

        float targetX = hasSkill ? _mainSelectedX : _mainDefaultX;
        float targetAlpha = hasSkill ? 1f : 0f;

        _mainSlideTween?.Kill();
        _detailFadeTween?.Kill();

        if (_detailCanvasGroup != null)
        {
            _detailCanvasGroup.interactable = hasSkill;
            _detailCanvasGroup.blocksRaycasts = hasSkill;
        }

        if (animate)
        {
            if (_mainRect != null)
            {
                _mainSlideTween = _mainRect
                    .DOAnchorPosX(targetX, _slideDuration)
                    .SetEase(_slideEase);
            }

            if (_detailCanvasGroup != null)
            {
                _detailFadeTween = _detailCanvasGroup
                    .DOFade(targetAlpha, _slideDuration)
                    .SetEase(_slideEase);
            }
        }
        else
        {
            if (_mainRect != null)
            {
                Vector2 pos = _mainRect.anchoredPosition;
                pos.x = targetX;
                _mainRect.anchoredPosition = pos;
            }

            if (_detailCanvasGroup != null)
            {
                _detailCanvasGroup.alpha = targetAlpha;
            }
        }
    }

    /// <summary>
    /// 현재 선택 캐릭터가 이 스킬의 소유자면 해제 버튼, 아니면(아무도 없거나 다른 캐릭터) 항상 장착 버튼.
    /// 빈 슬롯이 있는지 여부는 버튼 활성화에 영향을 주지 않는다(클릭 시 내부에서 처리).
    /// </summary>
    private void RefreshDetailButtons(PersistentCharacterUnit current, SkillData displaySkill)
    {
        bool hasOwner = SkillEquipService.TryFindEquippedOwner(
            _party, displaySkill, out PersistentCharacterUnit owner, out _);

        if (hasOwner && owner == current)
        {
            SetButtonActive(_equipButton, false);
            SetButtonActive(_unequipButton, true);
        }
        else
        {
            SetButtonActive(_equipButton, current != null);
            SetButtonActive(_unequipButton, false);
        }
    }

    private void SetButtonActive(Button button, bool active)
    {
        if (button != null)
        {
            button.gameObject.SetActive(active);
        }
    }

    // ───────────────────────── 버튼 클릭 ─────────────────────────

    private void OnClickEquip()
    {
        PersistentCharacterUnit current = GetCurrentCharacter();
        SkillData skill = _selectedSkill;

        if (current == null || skill == null) return;

        int targetSlot = SkillEquipService.GetFirstEmptySlot(current);

        if (targetSlot < 0)
        {
            // TODO: 슬롯이 가득 찼습니다 AlertPopup
            return;
        }

        SkillEquipService.EquipWithTransfer(_party, current, targetSlot, skill);

        Refresh();
    }

    private void OnClickUnequip()
    {
        PersistentCharacterUnit current = GetCurrentCharacter();
        SkillData skill = _selectedSkill;

        if (current == null || skill == null) return;

        SkillEquipService.TryFindEquippedOwner(_party, skill, out _, out int ownerSlot);

        if (ownerSlot < 0) return;

        SkillEquipService.UnequipAt(current, ownerSlot);

        Refresh();
    }
}