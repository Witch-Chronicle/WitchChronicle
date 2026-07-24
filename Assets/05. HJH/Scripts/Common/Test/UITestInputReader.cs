using UnityEngine;
using UnityEngine.InputSystem;

public class UITestInputReader : MonoBehaviour
{
    public static UITestInputReader Instance { get; private set; }

    [Header("Integration Panel")]
    [SerializeField] private UIPanelAnimator _integrationPanelAnimator;

    [Header("Stat Panel")]
    [SerializeField] private UIPanelAnimator _statPanelAnimator;

    [Header("Pause Panel (Dungeon 전용)")]
    [SerializeField] private UIPanelAnimator _pausePanelAnimator;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_integrationPanelAnimator != null)
        {
            _integrationPanelAnimator.SetClosedImmediate();
        }

        if (_statPanelAnimator != null)
        {
            _statPanelAnimator.SetClosedImmediate();
        }

        if (_pausePanelAnimator != null)
        {
            _pausePanelAnimator.SetClosedImmediate();
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.iKey.wasPressedThisFrame && IsInBattleScene() == false)
        {
            ToggleIntegrationPanel();
        }

        if (Keyboard.current.cKey.wasPressedThisFrame && IsInBattleScene() == false)
        {
            ToggleStatPanel();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame && IsInDungeonScene())
        {
            TogglePausePanel();
        }

        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleQuestList();
        }

        if (IsInBattleScene() && BattleTargetCycler.Instance != null)
        {
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                BattleTargetCycler.Instance.CyclePrevious();
            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                BattleTargetCycler.Instance.CycleNext();
            }

            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                BattleTargetCycler.Instance.Confirm();
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                BattleTargetCycler.Instance.Cancel();
            }
        }
    }

    /// <summary>
    /// QuestListUI 슬라이드 토글 (Tab 키)
    /// </summary>
    public void ToggleQuestList()
    {
        if (QuestListUI.Instance == null)
        {
            Debug.LogWarning("[UITestInputReader] QuestListUI.Instance가 없습니다.");
            return;
        }

        QuestListUI.Instance.ToggleSlide();
    }

    /// <summary>
    /// SceneTransitionManager에 위임. 지금 활성 씬이 Battle_1인지 여부.
    /// </summary>
    private bool IsInBattleScene()
    {
        return SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsInBattleScene();
    }

    /// <summary>
    /// SceneTransitionManager에 위임. 지금 활성 씬이 Dungeon인지 여부.
    /// PausePanel은 던전 씬에서만 열려야 하고, 거점/전투 씬에서는 열리지 않아야 함.
    /// </summary>
    private bool IsInDungeonScene()
    {
        return SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsInDungeonScene();
    }

    public void ToggleIntegrationPanel()
    {
        if (_integrationPanelAnimator == null)
        {
            Debug.LogWarning("[UITestInputReader] integrationPanelAnimator가 연결되지 않았습니다.");
            return;
        }

        if (_integrationPanelAnimator.IsOpen)
        {
            _integrationPanelAnimator.Close();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            _integrationPanelAnimator.Open();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void ToggleStatPanel()
    {
        if (_statPanelAnimator == null)
        {
            Debug.LogWarning("[UITestInputReader] statPanelAnimator가 연결되지 않았습니다.");
            return;
        }

        if (_statPanelAnimator.IsOpen)
        {
            _statPanelAnimator.Close();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            _statPanelAnimator.Open();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// 던전 씬 전용 일시정지 패널 토글.
    /// </summary>
    public void TogglePausePanel()
    {
        if (_pausePanelAnimator == null)
        {
            Debug.LogWarning("[UITestInputReader] pausePanelAnimator가 연결되지 않았습니다.");
            return;
        }

        if (_pausePanelAnimator.IsOpen)
        {
            _pausePanelAnimator.Close();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            _pausePanelAnimator.Open();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void LoadScene(SceneId sceneId)
    {
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("[UITestInputReader] SceneTransitionManager.Instance가 null입니다.");
            return;
        }

        SceneTransitionManager.Instance.LoadScene(sceneId);
    }
}