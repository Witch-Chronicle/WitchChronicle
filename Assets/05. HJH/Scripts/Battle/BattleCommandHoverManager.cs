using UnityEngine;

public class BattleCommandHoverManager : MonoBehaviour
{
    public static BattleCommandHoverManager Instance { get; private set; }

    public static bool HasInstance => Instance != null;

    private BattleCommandHoverButton currentHoveredButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetHoveredButton(BattleCommandHoverButton newButton)
    {
        if (newButton == null)
            return;

        // 이미 현재 버튼이 활성화된 상태라면 변경하지 않는다.
        if (currentHoveredButton == newButton)
            return;

        // 기존 Hover 해제
        if (currentHoveredButton != null)
        {
            currentHoveredButton.SetHovered(false);
        }

        // 새로운 Hover 활성화
        currentHoveredButton = newButton;
        currentHoveredButton.SetHovered(true);
    }

    public void ClearIfCurrent(BattleCommandHoverButton button)
    {
        if (currentHoveredButton != button)
            return;

        currentHoveredButton.SetHovered(false);
        currentHoveredButton = null;
    }

    public void ClearHoveredButton()
    {
        if (currentHoveredButton != null)
        {
            currentHoveredButton.SetHovered(false);
            currentHoveredButton = null;
        }
    }
}