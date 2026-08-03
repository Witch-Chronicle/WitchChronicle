using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PortalPanel 전담 컨트롤러. 여러 던전 버튼들의 입력을 수신하고 씬 전환 및 패널 제어를 담당합니다.
/// </summary>
public class PortalUIController : MonoBehaviour
{
    [Header("NPC")]
    [SerializeField] private PortalNPC _portalNPC;

    [Header("Dungeon Buttons")]
    [SerializeField] private DungeonButton[] _dungeonButtons;

    [Header("Close Button")]
    [SerializeField] private Button _closeBtn;

    private void Awake()
    {
        BindEvents();
    }

    private void OnDestroy()
    {
        UnbindEvents();
    }

    private void BindEvents()
    {
        if (_dungeonButtons != null)
        {
            foreach (var dungeonButton in _dungeonButtons)
            {
                if (dungeonButton != null)
                {
                    dungeonButton.OnDungeonSelected += HandleDungeonSelected;
                }
            }
        }

        if (_closeBtn != null)
        {
            _closeBtn.onClick.AddListener(HandleCloseClicked);
        }
    }

    private void UnbindEvents()
    {
        if (_dungeonButtons != null)
        {
            foreach (var dungeonButton in _dungeonButtons)
            {
                if (dungeonButton != null)
                {
                    dungeonButton.OnDungeonSelected -= HandleDungeonSelected;
                }
            }
        }

        if (_closeBtn != null)
        {
            _closeBtn.onClick.RemoveListener(HandleCloseClicked);
        }
    }

    private void HandleDungeonSelected(DungeonData targetDungeon)
    {
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("[PortalUIController] SceneTransitionManager.Instance가 null입니다.");
            return;
        }

        if (targetDungeon == null)
        {
            Debug.LogWarning("[PortalUIController] 전달받은 던전 데이터가 null입니다.");
            return;
        }

        DungeonSelection.CurrentDungeonData = targetDungeon;

        ShowMessageManager.Instance.ShowMessage($"{targetDungeon.DungeonName} 에 입장합니다.");
        Debug.Log($"[PortalUIController] 던전 선택 완료: {targetDungeon.DungeonName} 씬 전환 시작");

        SceneTransitionManager.Instance.LoadScene(SceneId.Dungeon);
    }

    private void HandleCloseClicked()
    {
        if (_portalNPC == null)
        {
            Debug.LogWarning("[PortalUIController] PortalNPC가 null입니다.");
            return;
        }

        Debug.Log("[PortalUIController] 포탈 패널 닫기 요청");
        _portalNPC.TogglePortal();
    }
}