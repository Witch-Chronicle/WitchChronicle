using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 스킬 장착 창 (테스트용).
/// 위: 파티원 탭 / 왼쪽: 장착 슬롯 / 오른쪽: 보유 스킬 목록.
/// 슬롯을 고른 뒤 스킬을 클릭하면 그 자리에 장착된다.
/// </summary>
public class SkillEquipUIController : MonoBehaviour
{
    [Header("창")]
    [SerializeField] private GameObject _root;
    [SerializeField] private Button _closeButton;

    [Header("파티원 탭 (위)")]
    [Tooltip("파티원 수만큼 미리 배치. 남는 건 자동으로 숨겨진다")]
    [SerializeField] private Button[] _memberTabs;
    [SerializeField] private TMP_Text[] _memberTabLabels;

    [Header("장착 슬롯 (왼쪽)")]
    [SerializeField] private SkillEquipSlot[] _equipSlots;
    [SerializeField] private Button _unequipButton;

    [Header("보유 스킬 목록 (오른쪽)")]
    [SerializeField] private SkillListEntry[] _skillEntries;
    [SerializeField] private GameObject _emptyNotice;

    [Header("안내")]
    [SerializeField] private TMP_Text _guideText;

    private readonly List<PersistentCharacterUnit> _party = new List<PersistentCharacterUnit>();
    private readonly List<SkillData> _learned = new List<SkillData>();

    private int _memberIndex = 0;
    private int _selectedSlot = -1;

    /// <summary>CursorLocker의 열림 카운트를 중복으로 올리지 않기 위한 상태.</summary>
    private bool _isOpen;

    private PersistentCharacterUnit CurrentMember =>
        _memberIndex >= 0 && _memberIndex < _party.Count ? _party[_memberIndex] : null;

    private void Awake()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Close);
        }

        if (_unequipButton != null)
        {
            _unequipButton.onClick.AddListener(OnClickUnequip);
        }

        BindMemberTabs();

        if (_root != null)
        {
            _root.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(Close);
        }

        if (_unequipButton != null)
        {
            _unequipButton.onClick.RemoveListener(OnClickUnequip);
        }
    }

    /// <summary>창 열기.</summary>
    public void Open()
    {
        // 루트가 꺼져 있으면 자식만 켜봐야 안 보인다. 자기 자신부터 켠다.
        // (이때 Awake가 처음 돌면서 _root를 꺼버리므로 _root는 반드시 그 다음에 켠다)
        if (gameObject.activeSelf == false)
        {
            gameObject.SetActive(true);
        }

        if (_root != null)
        {
            _root.SetActive(true);
        }

        _memberIndex = 0;
        _selectedSlot = -1;

        // 필드에서는 커서가 잠겨 있어 클릭이 안 되므로 UI 모드로 전환
        if (_isOpen == false)
        {
            _isOpen = true;

            if (CursorLocker.Instance != null)
            {
                CursorLocker.Instance.EnterUIMode();
            }
        }

        Refresh();
    }

    /// <summary>창 닫기.</summary>
    public void Close()
    {
        if (_root != null)
        {
            _root.SetActive(false);
        }

        if (_isOpen)
        {
            _isOpen = false;

            if (CursorLocker.Instance != null)
            {
                CursorLocker.Instance.ExitUIMode();
            }
        }
    }

    /// <summary>전체 갱신.</summary>
    public void Refresh()
    {
        SkillEquipService.GetPartyMembers(_party);

        // 창을 여는 시점에 새 파티원이 합류했을 수도 있으므로 시작 스킬을 다시 흡수한다
        if (SkillInventory.Instance != null)
        {
            SkillInventory.Instance.SyncPartyEquippedSkills();
        }

        if (_memberIndex >= _party.Count)
        {
            _memberIndex = 0;
        }

        RefreshMemberTabs();
        RefreshEquipSlots();
        RefreshSkillList();
        RefreshGuide();
    }

    /// <summary>파티원 탭 버튼에 클릭 이벤트 연결(1회).</summary>
    private void BindMemberTabs()
    {
        if (_memberTabs == null)
        {
            return;
        }

        for (int i = 0; i < _memberTabs.Length; i++)
        {
            int index = i;   // 클로저 캡처 주의

            if (_memberTabs[i] != null)
            {
                _memberTabs[i].onClick.AddListener(() => OnSelectMember(index));
            }
        }
    }

    /// <summary>탭 라벨·표시 갱신.</summary>
    private void RefreshMemberTabs()
    {
        if (_memberTabs == null)
        {
            return;
        }

        for (int i = 0; i < _memberTabs.Length; i++)
        {
            bool hasMember = i < _party.Count;

            if (_memberTabs[i] != null)
            {
                _memberTabs[i].gameObject.SetActive(hasMember);
                _memberTabs[i].interactable = hasMember && i != _memberIndex;
            }

            if (_memberTabLabels != null && i < _memberTabLabels.Length && _memberTabLabels[i] != null)
            {
                _memberTabLabels[i].text = hasMember ? _party[i].CharacterName : string.Empty;
            }
        }
    }

    /// <summary>장착 슬롯 갱신.</summary>
    private void RefreshEquipSlots()
    {
        if (_equipSlots == null)
        {
            return;
        }

        PersistentCharacterUnit member = CurrentMember;
        int slotCount = SkillEquipService.GetSlotCount(member);

        for (int i = 0; i < _equipSlots.Length; i++)
        {
            SkillEquipSlot slot = _equipSlots[i];

            if (slot == null)
            {
                continue;
            }

            if (i < slotCount)
            {
                slot.Bind(i, SkillEquipService.GetEquippedAt(member, i), OnSelectSlot);
                slot.SetSelected(i == _selectedSlot);
            }
            else
            {
                slot.Clear();
            }
        }

        if (_unequipButton != null)
        {
            bool canUnequip = _selectedSlot >= 0
                && SkillEquipService.GetEquippedAt(member, _selectedSlot) != null;

            _unequipButton.interactable = canUnequip;
        }
    }

    /// <summary>보유 스킬 목록 갱신.</summary>
    private void RefreshSkillList()
    {
        _learned.Clear();

        if (SkillInventory.Instance != null)
        {
            SkillInventory.Instance.GetLearnedSkills(_learned);
        }

        PersistentCharacterUnit member = CurrentMember;

        if (_skillEntries != null)
        {
            for (int i = 0; i < _skillEntries.Length; i++)
            {
                SkillListEntry entry = _skillEntries[i];

                if (entry == null)
                {
                    continue;
                }

                if (i < _learned.Count)
                {
                    SkillData skill = _learned[i];
                    entry.Bind(skill, SkillEquipService.IsEquipped(member, skill), OnSelectSkill);
                }
                else
                {
                    entry.Clear();
                }
            }
        }

        if (_emptyNotice != null)
        {
            _emptyNotice.SetActive(_learned.Count == 0);
        }
    }

    /// <summary>안내 문구 갱신.</summary>
    private void RefreshGuide()
    {
        if (_guideText == null)
        {
            return;
        }

        if (_selectedSlot < 0)
        {
            _guideText.text = "장착할 슬롯을 먼저 선택하세요.";
        }
        else
        {
            _guideText.text = $"{_selectedSlot + 1}번 슬롯 선택됨 — 장착할 스킬을 고르세요.";
        }
    }

    /// <summary>파티원 탭 클릭.</summary>
    private void OnSelectMember(int index)
    {
        if (index < 0 || index >= _party.Count)
        {
            return;
        }

        _memberIndex = index;
        _selectedSlot = -1;

        Refresh();
    }

    /// <summary>장착 슬롯 클릭.</summary>
    private void OnSelectSlot(int slotIndex)
    {
        _selectedSlot = slotIndex;

        RefreshEquipSlots();
        RefreshGuide();
    }

    /// <summary>보유 스킬 클릭 → 선택된 슬롯에 장착.</summary>
    private void OnSelectSkill(SkillData skill)
    {
        if (_selectedSlot < 0)
        {
            if (_guideText != null)
            {
                _guideText.text = "먼저 왼쪽에서 슬롯을 선택하세요.";
            }

            return;
        }

        SkillEquipService.EquipAt(CurrentMember, _selectedSlot, skill);

        RefreshEquipSlots();
        RefreshSkillList();
        RefreshGuide();
    }

    /// <summary>선택 슬롯 해제.</summary>
    private void OnClickUnequip()
    {
        if (_selectedSlot < 0)
        {
            return;
        }

        SkillEquipService.UnequipAt(CurrentMember, _selectedSlot);

        RefreshEquipSlots();
        RefreshSkillList();
    }
}
