using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerUIInputReader : MonoBehaviour
{
    public static PlayerUIInputReader Instance { get; private set; }

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

        // 배틀 씬에서는 이 스크립트가 아무 키 입력도 받지 않음.
        // 배틀 씬의 모든 키 입력(스킬/아이템 리스트, 커맨드 UI 등)은 BattleUIInputReader가 전담.
        if (IsInBattleScene())
        {
            return;
        }

        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            ToggleIntegrationPanel();
        }

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            ToggleStatPanel();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscape();
        }

        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleQuestList();
        }
    }

    /// <summary>
    /// Esc 입력 처리.
    /// - 열려있는 패널(Integration/Stat/Pause)이 있으면 그것부터 닫음.
    /// - 아무 패널도 안 열려있고 던전 씬이면 PausePanel을 염.
    /// </summary>
    private void HandleEscape()
    {
        if (_integrationPanelAnimator != null && _integrationPanelAnimator.IsOpen)
        {
            CloseIntegrationPanel();
            return;
        }

        if (_statPanelAnimator != null && _statPanelAnimator.IsOpen)
        {
            CloseStatPanel();
            return;
        }

        if (_pausePanelAnimator != null && _pausePanelAnimator.IsOpen)
        {
            ClosePausePanel();
            return;
        }

        if (IsInDungeonScene())
        {
            OpenPausePanel();
        }
    }

    /// <summary>
    /// 지금 이 세 패널 중 하나라도 열려있는지 여부. 새 패널을 열기 전 중복 방지 체크용.
    /// </summary>
    private bool IsAnyPanelOpen()
    {
        bool integrationOpen = _integrationPanelAnimator != null && _integrationPanelAnimator.IsOpen;
        bool statOpen = _statPanelAnimator != null && _statPanelAnimator.IsOpen;
        bool pauseOpen = _pausePanelAnimator != null && _pausePanelAnimator.IsOpen;

        return integrationOpen || statOpen || pauseOpen;
    }

    /// <summary>
    /// QuestListUI 슬라이드 토글 (Tab 키)
    /// </summary>
    public void ToggleQuestList()
    {
        if (QuestListUI.Instance == null)
        {
            Debug.LogWarning("[PlayerUIInputReader] QuestListUI.Instance가 없습니다.");
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
            Debug.LogWarning("[PlayerUIInputReader] integrationPanelAnimator가 연결되지 않았습니다.");
            return;
        }

        if (_integrationPanelAnimator.IsOpen)
        {
            CloseIntegrationPanel();
            return;
        }

        // 다른 패널이 열려있으면 새로 열지 않음
        if (IsAnyPanelOpen())
        {
            return;
        }

        OpenIntegrationPanel();
    }

    public void ToggleStatPanel()
    {
        if (_statPanelAnimator == null)
        {
            Debug.LogWarning("[PlayerUIInputReader] statPanelAnimator가 연결되지 않았습니다.");
            return;
        }

        if (_statPanelAnimator.IsOpen)
        {
            CloseStatPanel();
            return;
        }

        // 다른 패널이 열려있으면 새로 열지 않음
        if (IsAnyPanelOpen())
        {
            return;
        }

        OpenStatPanel();
    }

    /// <summary>
    /// 던전 씬 전용 일시정지 패널 토글.
    /// </summary>
    public void TogglePausePanel()
    {
        if (_pausePanelAnimator == null)
        {
            Debug.LogWarning("[PlayerUIInputReader] pausePanelAnimator가 연결되지 않았습니다.");
            return;
        }

        if (_pausePanelAnimator.IsOpen)
        {
            ClosePausePanel();
            return;
        }

        // 다른 패널이 열려있으면 새로 열지 않음
        if (IsAnyPanelOpen())
        {
            return;
        }

        OpenPausePanel();
    }

    private void OpenIntegrationPanel()
    {
        _integrationPanelAnimator.Open();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseIntegrationPanel()
    {
        _integrationPanelAnimator.Close();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OpenStatPanel()
    {
        _statPanelAnimator.Open();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseStatPanel()
    {
        _statPanelAnimator.Close();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OpenPausePanel()
    {
        _pausePanelAnimator.Open();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
    }

    private void ClosePausePanel()
    {
        _pausePanelAnimator.Close();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Time.timeScale = 1f;
    }

    private void LoadScene(SceneId sceneId)
    {
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("[PlayerUIInputReader] SceneTransitionManager.Instance가 null입니다.");
            return;
        }

        SceneTransitionManager.Instance.LoadScene(sceneId);
    }
}