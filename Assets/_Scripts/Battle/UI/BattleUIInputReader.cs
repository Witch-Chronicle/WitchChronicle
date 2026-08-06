using UnityEngine;
using UnityEngine.InputSystem;

public class BattleUIInputReader : MonoBehaviour
{
    public static BattleUIInputReader Instance
    {
        get;
        private set;
    }

    private BattleCommandUIController _currentCommandUI;
    private SkillListController _currentSkillList;
    private ItemListController _currentItemList;

    /*
 * SkillList 또는 ItemList가 열린 프레임에
 * 동일한 Enter 입력이 SubmitSelected까지 전달되는 것을 막는다.
 */
    private int _listOpenedFrame = -1;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        Keyboard keyboard =
            Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        /*
         * Esc 입력은 가장 먼저 처리한다.
         *
         * 열려 있는 리스트가 있다면 CommandUI/타겟 조준에는
         * 입력을 넘기지 않고 리스트 취소만 실행한다.
         * 리스트가 없고 타겟 조준 중(Pending)이라면 조준 취소를 우선 처리한다.
         */
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            /*
             * 마법진 그리기가 진행 중인 동안에는 Esc를 완전히 무시한다.
             * (BattleTargetCycler._mode가 여전히 PendingSkill이라
             *  그대로 HandleCancel()로 넘기면 조준 취소가 실행돼버림)
             */
            if (SkillDrawController.Instance != null &&
                SkillDrawController.Instance.IsDrawing)
            {
                return;
            }
            HandleCancel();
            return;
        }

        if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            HandleMoveUp();
            return;
        }

        if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            HandleMoveDown();
            return;
        }

        if (keyboard.enterKey.wasPressedThisFrame)
        {
            HandleSubmit();
        }

        /*
         * Q/E는 타겟 순환 전용. BattleTargetCycler가 내부적으로
         * Idle(기본 타겟 순환)/Pending(조준 후보 순환) 상태를 알아서 분기하므로
         * 리스트가 열려있든 아니든 항상 그대로 전달해도 안전하다.
         */
        if (keyboard.qKey.wasPressedThisFrame)
        {
            BattleTargetCycler.Instance?.CyclePrevious();
        }

        if (keyboard.eKey.wasPressedThisFrame)
        {
            BattleTargetCycler.Instance?.CycleNext();
        }
    }

    private void HandleMoveUp()
    {
        if (_currentSkillList != null &&
            _currentSkillList.IsInputActive)
        {
            _currentSkillList.MoveSelectionUp();
            return;
        }

        if (_currentItemList != null &&
            _currentItemList.IsInputActive)
        {
            _currentItemList.MoveSelectionUp();
            return;
        }

        _currentCommandUI?.MoveUp();
    }

    private void HandleMoveDown()
    {
        if (_currentSkillList != null &&
            _currentSkillList.IsInputActive)
        {
            _currentSkillList.MoveSelectionDown();
            return;
        }

        if (_currentItemList != null &&
            _currentItemList.IsInputActive)
        {
            _currentItemList.MoveSelectionDown();
            return;
        }

        _currentCommandUI?.MoveDown();
    }

    private void HandleSubmit()
    {
        if (_currentSkillList != null &&
            _currentSkillList.IsInputActive)
        {
            /*
             * 리스트를 연 Enter 입력이 같은 프레임에
             * 스킬 선택 확정까지 이어지는 것을 방지한다.
             */
            if (Time.frameCount == _listOpenedFrame)
            {
                return;
            }

            _currentSkillList.SubmitSelected();
            return;
        }

        if (_currentItemList != null &&
            _currentItemList.IsInputActive)
        {
            /*
             * 리스트를 연 Enter 입력이 같은 프레임에
             * 아이템 사용까지 이어지는 것을 방지한다.
             */
            if (Time.frameCount == _listOpenedFrame)
            {
                return;
            }

            _currentItemList.SubmitSelected();
            return;
        }

        /*
         * 리스트가 열려있지 않고, 지금 공격/스킬 대상을 조준 중(Pending)이라면
         * CommandUI보다 타겟 조준 확정을 우선 처리한다.
         */
        if (BattleTargetCycler.Instance != null &&
            BattleTargetCycler.Instance.IsTargeting)
        {
            BattleTargetCycler.Instance.Confirm();
            return;
        }

        _currentCommandUI?.SubmitCurrent();
    }

    private void HandleCancel()
    {
        if (_currentSkillList != null &&
            _currentSkillList.IsInputActive)
        {
            _currentSkillList.Cancel();
            return;
        }

        if (_currentItemList != null &&
            _currentItemList.IsInputActive)
        {
            _currentItemList.Cancel();
            return;
        }

        /*
         * 리스트가 열려있지 않고, 지금 공격/스킬 대상을 조준 중(Pending)이라면
         * 조준 취소를 처리한다. (Idle 상태면 BattleTargetCycler.Cancel()이 내부적으로 무시함)
         */
        if (BattleTargetCycler.Instance != null &&
            BattleTargetCycler.Instance.IsTargeting)
        {
            BattleTargetCycler.Instance.Cancel();
        }
    }

    public void SetCommandUI(
        BattleCommandUIController commandUI)
    {
        if (_currentCommandUI == commandUI)
        {
            /*
             * 리스트가 열려 있는 동안에는
             * CommandUI 입력을 다시 활성화하면 안 된다.
             */
            if (!HasActiveList())
            {
                _currentCommandUI?
                    .ActivateInput();
            }

            return;
        }

        _currentCommandUI?
            .DeactivateInput();

        _currentCommandUI =
            commandUI;

        if (!HasActiveList())
        {
            _currentCommandUI?
                .ActivateInput();
        }
    }

    public void ClearCommandUI(
        BattleCommandUIController commandUI = null)
    {
        if (commandUI != null &&
            _currentCommandUI != commandUI)
        {
            return;
        }

        _currentCommandUI?
            .DeactivateInput();

        _currentCommandUI = null;
    }

    public void SuspendCommandUI()
    {
        _currentCommandUI?
            .DeactivateInput();
    }

    public void ResumeCommandUI()
    {
        /*
         * SkillList 또는 ItemList가 등록되어 있으면
         * CommandUI 입력을 복구하지 않는다.
         */
        if (HasActiveList())
        {
            return;
        }

        _currentCommandUI?
            .ActivateInput();
    }

    public void SetSkillList(
    SkillListController skillList)
    {
        if (skillList == null)
        {
            return;
        }

        _currentSkillList = skillList;
        _currentItemList = null;

        /*
         * 현재 Enter 입력으로 리스트가 열렸을 수 있으므로
         * 이 프레임의 리스트 Submit을 차단한다.
         */
        _listOpenedFrame = Time.frameCount;

        _currentCommandUI?.DeactivateInput();
    }

    public void ClearSkillList(
    SkillListController skillList = null)
    {
        if (skillList != null &&
            _currentSkillList != skillList)
        {
            return;
        }

        _currentSkillList = null;
        _listOpenedFrame = -1;
    }

    public void SetItemList(
    ItemListController itemList)
    {
        if (itemList == null)
        {
            return;
        }

        _currentItemList = itemList;
        _currentSkillList = null;

        /*
         * 현재 Enter 입력으로 리스트가 열렸을 수 있으므로
         * 이 프레임의 리스트 Submit을 차단한다.
         */
        _listOpenedFrame = Time.frameCount;

        _currentCommandUI?.DeactivateInput();
    }

    public void ClearItemList(
    ItemListController itemList = null)
    {
        if (itemList != null &&
            _currentItemList != itemList)
        {
            return;
        }

        _currentItemList = null;
        _listOpenedFrame = -1;
    }

    private bool HasActiveList()
    {
        bool hasSkillList =
            _currentSkillList != null &&
            _currentSkillList.IsInputActive;

        bool hasItemList =
            _currentItemList != null &&
            _currentItemList.IsInputActive;

        return hasSkillList ||
               hasItemList;
    }
}