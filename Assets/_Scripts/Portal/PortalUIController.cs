using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PortalPanel 전담. NormalDungeon 버튼 클릭 시 Dungeon_1 씬으로 이동.
/// CloseBtn 클릭 시 PortalNPC.TogglePortal()을 호출해서 패널을 닫음.
/// </summary>
public class PortalUIController : MonoBehaviour
{
    [Header("NPC")]
    [SerializeField] private PortalNPC _portalNPC;

    [Header("Buttons")]
    [SerializeField] private Button _normalDungeonBtn;
    [SerializeField] private Button _closeBtn;

    // 추가, 던전 다르게 생성 하기 위한 SO, 
    // 나중에 버튼 마다 다른 던전 데이터 로 골라서 던전 이동
    [Header("Dungeon Data")]
    [SerializeField] private DungeonData _targetDungeon;


    private void Awake()
    {
        if (_normalDungeonBtn != null) _normalDungeonBtn.onClick.AddListener(HandleNormalDungeonClicked);
        if (_closeBtn != null) _closeBtn.onClick.AddListener(HandleCloseClicked);
    }

    private void HandleNormalDungeonClicked()
    {
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("[PortalUIController] SceneTransitionManager.Instance가 null입니다.");
            return;
        }

        // 추가, 현재 들어갈 던전 저장
        DungeonSelection.CurrentDungeonData = _targetDungeon;

        ShowMessageManager.Instance.ShowMessage($"{_targetDungeon.DungeonName} 에 입장 합니다.");

        SceneTransitionManager.Instance.LoadScene(SceneId.Dungeon);
    }

    private void HandleCloseClicked()
    {
        if (_portalNPC == null)
        {
            Debug.LogWarning("[PortalUIController] PortalNPC가 null입니다.");
            return;
        }

        _portalNPC.TogglePortal();
    }
}