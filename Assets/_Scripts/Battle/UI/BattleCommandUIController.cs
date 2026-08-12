using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleCommandUIController : MonoBehaviour
{
    [Serializable]
    private class CommandEntry
    {
        [SerializeField] private Button _button;
        [SerializeField] private BattleCommandHoverButton _hoverButton;

        public Button Button => _button;
        public BattleCommandHoverButton HoverButton => _hoverButton;
    }

    [Header("Commands")]
    [Tooltip("Attack → Skill → Item → Retreat 순서")]
    [SerializeField] private CommandEntry[] _commands;

    [Header("Selection")]
    [SerializeField] private int _defaultIndex = 0;

    // 커맨드 순서: Attack0 / Skill1 / Item2 / Retreat3
    private const int SkillCommandIndex = 1;
    private const int ItemCommandIndex = 2;

    private int _currentIndex = -1;
    private bool _isInputActive;

    public bool IsInputActive => _isInputActive;
    public int CurrentIndex => _currentIndex;

    public void ActivateInput()
    {
        _isInputActive = true;
        RefreshSkillLock();
        RefreshItemLock();
        ResetToDefault();
    }

    /// <summary>
    /// 스킬 커맨드 활성화 여부 갱신.
    /// - 침묵 등의 상태로 스킬 사용 불가 시 비활성화
    /// - 현재 유닛의 장착 스킬이 하나도 없으면 비활성화
    /// </summary>
    private void RefreshSkillLock()
    {
        if (_commands == null ||
            _commands.Length <= SkillCommandIndex)
        {
            return;
        }

        Button skillButton =
            _commands[SkillCommandIndex].Button;

        if (skillButton == null)
        {
            return;
        }

        bool canUseSkill = false;

        if (BattleUIContext.Instance != null)
        {
            BattleUnit unit =
                BattleUIContext.Instance.CurrentUnit;

            if (unit != null)
            {
                bool hasEquippedSkill =
                    HasEquippedSkill(unit);

                bool isSkillAvailable =
                    BattleUIContext.Instance.CanUseSkill(unit);

                canUseSkill =
                    hasEquippedSkill &&
                    isSkillAvailable;
            }
        }

        skillButton.interactable = canUseSkill;
    }

    /// <summary>
    /// 현재 전투 유닛에게 장착된 스킬이 하나라도 있는지 확인.
    /// null 스킬은 장착된 것으로 취급하지 않는다.
    /// </summary>
    private bool HasEquippedSkill(BattleUnit unit)
    {
        if (unit == null ||
            unit.SkillList == null)
        {
            return false;
        }

        foreach (SkillData skill in unit.SkillList)
        {
            if (skill != null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 사용 가능한 포션 아이템이 하나도 없으면 Item 커맨드 버튼을 비활성화(입력 차단)한다.
    /// interactable=false면 MoveSelection/SubmitCurrent가 IsSelectable로 자동 제외한다.
    /// </summary>
    private void RefreshItemLock()
    {
        if (_commands == null || _commands.Length <= ItemCommandIndex)
            return;
        Button itemButton = _commands[ItemCommandIndex].Button;
        if (itemButton == null)
            return;
        itemButton.interactable = HasUsablePotionItem();
    }
    /// <summary>
    /// PlayerInventory에 수량이 1개 이상인 PotionItemData가 존재하는지 확인한다.
    /// </summary>
    private bool HasUsablePotionItem()
    {
        if (PlayerInventory.Instance == null)
            return false;
        foreach (var inventorySlot in PlayerInventory.Instance.InventorySlots)
        {
            if (inventorySlot == null)
                continue;
            if (inventorySlot.ItemData is not PotionItemData)
                continue;
            if (inventorySlot.Quantity > 0)
                return true;
        }
        return false;
    }

    public void DeactivateInput()
    {
        _isInputActive = false;
        ClearSelection();
    }

    public void ResetToDefault()
    {
        if (_commands == null || _commands.Length == 0)
            return;

        SelectIndex(_defaultIndex, true);
    }

    public void MoveUp()
    {
        MoveSelection(-1);
    }

    public void MoveDown()
    {
        MoveSelection(1);
    }

    public void MoveSelection(int direction)
    {
        if (!_isInputActive)
            return;

        if (_commands == null || _commands.Length == 0)
            return;

        int nextIndex = _currentIndex;

        for (int i = 0; i < _commands.Length; i++)
        {
            nextIndex = WrapIndex(nextIndex + direction);

            Button candidate = _commands[nextIndex].Button;

            if (IsSelectable(candidate))
            {
                SelectIndex(nextIndex);
                return;
            }
        }
    }

    public void SelectByHoverButton(
        BattleCommandHoverButton hoverButton
    )
    {
        if (!_isInputActive || hoverButton == null)
            return;

        for (int i = 0; i < _commands.Length; i++)
        {
            if (_commands[i].HoverButton == hoverButton)
            {
                // 비활성(침묵 등) 커맨드는 마우스 hover로도 선택/하이라이트되지 않도록 차단
                if (!IsSelectable(_commands[i].Button))
                    return;

                SelectIndex(i);
                return;
            }
        }
    }

    public void SubmitCurrent()
    {
        if (!_isInputActive)
            return;

        if (_currentIndex < 0 ||
            _currentIndex >= _commands.Length)
        {
            return;
        }

        Button selectedButton =
            _commands[_currentIndex].Button;

        if (!IsSelectable(selectedButton))
            return;

        selectedButton.onClick.Invoke();
    }

    private void SelectIndex(int index, bool force = false)
    {
        if (_commands == null || _commands.Length == 0)
            return;

        index = WrapIndex(index);

        if (!force && _currentIndex == index)
            return;

        if (_currentIndex >= 0 &&
            _currentIndex < _commands.Length)
        {
            _commands[_currentIndex]
                .HoverButton?
                .SetHovered(false);
        }

        _currentIndex = index;

        CommandEntry selectedEntry =
            _commands[_currentIndex];

        selectedEntry.HoverButton?.SetHovered(true);

        if (EventSystem.current != null &&
            selectedEntry.Button != null)
        {
            EventSystem.current.SetSelectedGameObject(
                selectedEntry.Button.gameObject
            );
        }
    }

    private void ClearSelection()
    {
        if (_currentIndex >= 0 &&
            _currentIndex < _commands.Length)
        {
            _commands[_currentIndex]
                .HoverButton?
                .SetHovered(false);
        }

        _currentIndex = -1;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private bool IsSelectable(Button button)
    {
        return button != null &&
               button.gameObject.activeInHierarchy &&
               button.interactable;
    }

    private int WrapIndex(int index)
    {
        if (_commands == null || _commands.Length == 0)
            return 0;

        if (index < 0)
            return _commands.Length - 1;

        if (index >= _commands.Length)
            return 0;

        return index;
    }
}