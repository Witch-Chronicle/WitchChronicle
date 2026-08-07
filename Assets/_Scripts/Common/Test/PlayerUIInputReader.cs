using UnityEngine;
using UnityEngine.InputSystem;
using WitchChronicle.IdleFarming;

/// <summary>
/// 필드 및 던전 씬에서 플레이어 공통 UI 입력을 관리합니다.
///
/// - I: 통합 패널
/// - C: 스탯 패널
/// - K: 스킬 장착 패널
/// - ESC: 열린 패널 닫기 / Pause 패널 열기
/// - Tab: 퀘스트 리스트 토글
///
/// 패널을 열 때 전역 UIBackgroundBlurManager에 Blur 표시를 요청하고,
/// 패널 닫기 애니메이션이 완료된 뒤 Blur 요청을 해제합니다.
/// </summary>
public sealed class PlayerUIInputReader : MonoBehaviour
{
    public static PlayerUIInputReader Instance { get; private set; }

    [Header("Integration Panel")]
    [SerializeField]
    private UIPanelAnimator _integrationPanelAnimator;

    [Header("Stat Panel")]
    [SerializeField] private UIPanelAnimator _statPanelAnimator;
    [SerializeField] private StatUIController _statUIController;

    [Header("Skill Equip Panel")]
    [SerializeField]
    private UIPanelAnimator _skillEquipPanelAnimator;

    [Header("Skill Gacha Result Overlay")]
    [SerializeField]
    private SkillGachaResultOverlayController _skillGachaResultOverlayController;

    [Header("Pause Panel")]
    [SerializeField]
    private UIPanelAnimator _pausePanelAnimator;
    [SerializeField]
    private PauseController _pauseController;

    private bool _dialoguePanelHiddenByPause;

    [Header("Player Input")]
    [Tooltip("패널이 열릴 때 캐릭터 이동 및 상호작용을 제어하기 위한 Input Action Asset")]
    [SerializeField]
    private InputActionAsset _inputAsset;

    private InputActionMap _playerMap;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        InitializeInputMap();
        InitializePanels();
        SubscribePanelEvents();
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        // 타이틀 씬의 입력은 타이틀 전용 컨트롤러가 담당합니다.
        if (IsInTitleScene())
        {
            return;
        }

        // 전투 씬의 UI 입력은 BattleUIInputReader가 담당합니다.
        if (IsInBattleScene())
        {
            return;
        }

        // 로딩 씬에서는 어떤 UI 입력도 처리하지 않습니다.
        if (IsInLoadingScene())
        {
            return;
        }

        // 생활 콘텐츠 패널(밭/낚시/연금술 등)이 열려있으면
        // Esc는 그 패널들을 닫는 데만 사용하고, 나머지 단축키는 전부 무시합니다.
        if (LifeUIManager.Instance != null && LifeUIManager.Instance.IsAnyLifePanelOpen())
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                bool plotOpen = PlotManager.Instance != null && PlotManager.Instance.IsAnyPanelOpen;
                Debug.Log($"[Debug] PlotManager.IsAnyPanelOpen: {plotOpen}");
                LifeUIManager.Instance.CloseAllLifePanels();

            }

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

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            ToggleSkillEquipPanel();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscape();
        }

        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleQuestList();
        }

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            ToggleMainHUD();
        }
    }

    #region Initialization

    private void InitializeInputMap()
    {
        if (_inputAsset == null)
        {
            return;
        }

        _playerMap = _inputAsset.FindActionMap(
            "Player",
            throwIfNotFound: false
        );

        if (_playerMap == null)
        {
            Debug.LogWarning(
                "[PlayerUIInputReader] Player 액션맵을 찾을 수 없습니다.",
                this
            );
        }
    }

    private void InitializePanels()
    {
        /*
         * SetClosedImmediate()는 OnClosed 이벤트를 호출하므로,
         * 반드시 이벤트 구독 전에 실행해야 합니다.
         */
        _integrationPanelAnimator?.SetClosedImmediate();
        _statPanelAnimator?.SetClosedImmediate();
        _skillEquipPanelAnimator?.SetClosedImmediate();
        _pausePanelAnimator?.SetClosedImmediate();
    }

    private void SubscribePanelEvents()
    {
        if (_integrationPanelAnimator != null)
        {
            _integrationPanelAnimator.OnClosed +=
                HandleIntegrationPanelClosed;
        }

        if (_statPanelAnimator != null)
        {
            _statPanelAnimator.OnClosed +=
                HandleStatPanelClosed;
        }

        if (_skillEquipPanelAnimator != null)
        {
            _skillEquipPanelAnimator.OnClosed +=
                HandleSkillEquipPanelClosed;
        }

        if (_pausePanelAnimator != null)
        {
            _pausePanelAnimator.OnClosed +=
                HandlePausePanelClosed;
        }
    }

    private void UnsubscribePanelEvents()
    {
        if (_integrationPanelAnimator != null)
        {
            _integrationPanelAnimator.OnClosed -=
                HandleIntegrationPanelClosed;
        }

        if (_statPanelAnimator != null)
        {
            _statPanelAnimator.OnClosed -=
                HandleStatPanelClosed;
        }

        if (_skillEquipPanelAnimator != null)
        {
            _skillEquipPanelAnimator.OnClosed -=
                HandleSkillEquipPanelClosed;
        }

        if (_pausePanelAnimator != null)
        {
            _pausePanelAnimator.OnClosed -=
                HandlePausePanelClosed;
        }
    }

    #endregion

    #region Input Handling

    /// <summary>
    /// ESC 입력 처리.
    ///
    /// 우선순위:
    /// 1. ShopNPC/EnhanceNPC/PortalNPC 등 자체 UIPanelAnimator로 열리는 외부 UI 닫기
    /// 2. IntegrationPanel 닫기
    /// 3. SkillEquipPanel 닫기
    /// 4. StatPanel 닫기 (Detail이 열려있으면 Detail만 먼저 닫음)
    /// 5. PausePanel의 설정 화면에서 Pause 화면으로 복귀
    /// 6. PausePanel 닫기
    /// 7. 열린 패널이 없으면 PausePanel 열기
    /// </summary>
    private void HandleEscape()
    {
        // 강화 결과 연출 중이면 Esc 입력 자체를 완전히 무시.
        // 연출이 끝나(canClose=true) 텍스트까지 다 표시된 상태면 Result Overlay만 닫음.
        if (EnhancementResultController.Instance != null &&
            EnhancementResultController.Instance.IsOpen)
        {
            if (EnhancementResultController.Instance.IsResultPresented)
            {
                EnhancementResultController.Instance.Close();
            }

            return;
        }

        // 가챠 결과 연출 중에도 동일하게 처리.
        // 연출 중(IsResultPresented == false)이면 Esc 완전 무시,
        // 결과가 다 표시된 뒤라면 Overlay만 닫는다.
        if (_skillGachaResultOverlayController != null &&
            _skillGachaResultOverlayController.IsOpen)
        {
            if (_skillGachaResultOverlayController.IsResultPresented)
            {
                _skillGachaResultOverlayController.Close();
            }
            return;
        }

        if (ShopNPC.Instance != null && ShopNPC.Instance.IsOpen)
        {
            ShopNPC.Instance.ToggleShop();
            return;
        }

        if (PortalNPC.Instance != null && PortalNPC.Instance.IsOpen)
        {
            PortalNPC.Instance.TogglePortal();
            return;
        }

        if (EnhanceNPC.Instance != null && EnhanceNPC.Instance.IsOpen)
        {
            EnhanceNPC.Instance.ToggleEnhanceUI();
            return;
        }

        if (_integrationPanelAnimator != null && _integrationPanelAnimator.IsOpen)
        {
            CloseIntegrationPanel();
            return;
        }

        if (_skillEquipPanelAnimator != null && _skillEquipPanelAnimator.IsOpen)
        {
            CloseSkillEquipPanel();
            return;
        }

        if (_statPanelAnimator != null && _statPanelAnimator.IsOpen)
        {
            if (_statUIController != null && _statUIController.IsDetailOpen)
            {
                _statUIController.CloseDetailPanel();
                return;
            }

            CloseStatPanel();
            return;
        }

        if (_pausePanelAnimator != null && _pausePanelAnimator.IsOpen)
        {
            if (_pauseController != null &&
                (_pauseController.IsSettingOpen || _pauseController.IsConfirmOpen))
            {
                _pauseController.ShowPauseView();
                return;
            }

            ClosePausePanel();
            return;
        }

        OpenPausePanel();
    }

    /// <summary>
    /// Integration, Stat, SkillEquip, Pause 패널 중 하나라도 열려 있는지 확인합니다.
    /// </summary>
    private bool IsAnyPanelOpen()
    {
        bool integrationOpen =
            _integrationPanelAnimator != null &&
            _integrationPanelAnimator.IsOpen;

        bool statOpen =
            _statPanelAnimator != null &&
            _statPanelAnimator.IsOpen;

        bool skillEquipOpen =
            _skillEquipPanelAnimator != null &&
            _skillEquipPanelAnimator.IsOpen;

        bool pauseOpen =
            _pausePanelAnimator != null &&
            _pausePanelAnimator.IsOpen;

        return integrationOpen || statOpen || skillEquipOpen || pauseOpen;
    }

    #endregion

    #region Integration Panel

    public void ToggleIntegrationPanel()
    {
        if (_integrationPanelAnimator == null)
        {
            Debug.LogWarning(
                "[PlayerUIInputReader] IntegrationPanelAnimator가 연결되지 않았습니다.",
                this
            );
            return;
        }

        if (_integrationPanelAnimator.IsOpen)
        {
            CloseIntegrationPanel();
            return;
        }

        if (IsAnyPanelOpen())
        {
            return;
        }

        OpenIntegrationPanel();
    }

    private void OpenIntegrationPanel()
    {
        if (_integrationPanelAnimator == null)
        {
            return;
        }

        /*
         * 패널을 화면에 표시하기 전에 현재 월드 화면을 캡처해야
         * 패널 자체가 Blur 이미지에 포함되지 않습니다.
         */
        ShowBackgroundBlur();
        _integrationPanelAnimator.Open();
        CursorLocker.Instance?.EnterUIMode();
    }

    private void CloseIntegrationPanel()
    {
        if (_integrationPanelAnimator == null)
        {
            return;
        }

        _integrationPanelAnimator.Close();
        CursorLocker.Instance?.ExitUIMode();

        /*
         * Blur는 여기서 바로 숨기지 않습니다.
         * Close 애니메이션이 끝난 뒤
         * HandleIntegrationPanelClosed()에서 해제합니다.
         */
    }

    private void HandleIntegrationPanelClosed()
    {
        HideBackgroundBlur();
    }

    #endregion

    #region Stat Panel

    public void ToggleStatPanel()
    {
        if (_statPanelAnimator == null)
        {
            Debug.LogWarning(
                "[PlayerUIInputReader] StatPanelAnimator가 연결되지 않았습니다.",
                this
            );
            return;
        }

        if (_statPanelAnimator.IsOpen)
        {
            CloseStatPanel();
            return;
        }

        if (IsAnyPanelOpen())
        {
            return;
        }

        OpenStatPanel();
    }

    private void OpenStatPanel()
    {
        if (_statPanelAnimator == null)
        {
            return;
        }

        ShowBackgroundBlur();
        _statPanelAnimator.Open();
        CursorLocker.Instance?.EnterUIMode();
    }

    private void CloseStatPanel()
    {
        if (_statPanelAnimator == null)
        {
            return;
        }

        _statPanelAnimator.Close();
        CursorLocker.Instance?.ExitUIMode();
    }

    private void HandleStatPanelClosed()
    {
        HideBackgroundBlur();
    }

    #endregion

    #region Skill Equip Panel

    public void ToggleSkillEquipPanel()
    {
        if (_skillEquipPanelAnimator == null)
        {
            Debug.LogWarning(
                "[PlayerUIInputReader] SkillEquipPanelAnimator가 연결되지 않았습니다.",
                this
            );
            return;
        }

        if (_skillEquipPanelAnimator.IsOpen)
        {
            CloseSkillEquipPanel();
            return;
        }

        if (IsAnyPanelOpen())
        {
            return;
        }

        OpenSkillEquipPanel();
    }

    private void OpenSkillEquipPanel()
    {
        if (_skillEquipPanelAnimator == null)
        {
            return;
        }

        ShowBackgroundBlur();
        _skillEquipPanelAnimator.Open();
        CursorLocker.Instance?.EnterUIMode();
    }

    private void CloseSkillEquipPanel()
    {
        if (_skillEquipPanelAnimator == null)
        {
            return;
        }

        _skillEquipPanelAnimator.Close();
        CursorLocker.Instance?.ExitUIMode();
    }

    private void HandleSkillEquipPanelClosed()
    {
        HideBackgroundBlur();
    }

    #endregion

    #region Pause Panel

    /// <summary>
    /// Pause 패널을 토글합니다.
    /// </summary>
    public void TogglePausePanel()
    {
        if (_pausePanelAnimator == null)
        {
            Debug.LogWarning(
                "[PlayerUIInputReader] PausePanelAnimator가 연결되지 않았습니다.",
                this
            );
            return;
        }

        if (_pausePanelAnimator.IsOpen)
        {
            ClosePausePanel();
            return;
        }

        if (IsAnyPanelOpen())
        {
            return;
        }

        OpenPausePanel();
    }

    private void OpenPausePanel()
    {
        if (_pausePanelAnimator == null)
        {
            Debug.LogWarning(
                "[PlayerUIInputReader] PausePanelAnimator가 연결되지 않았습니다.",
                this
            );
            return;
        }

        // 대화창이 떠 있는 상태에서 Pause를 열면, 대화창 패널만 잠시 비활성화한다.
        // (CursorLocker 등 다른 상태는 건드리지 않음)
        if (DialogueUI.Instance != null && DialogueUI.Instance.IsPanelActive)
        {
            DialogueUI.Instance.HidePanelOnly();
            _dialoguePanelHiddenByPause = true;
        }

        /*
         * Time.timeScale을 0으로 변경하기 전에 화면을 캡처합니다.
         * 현재 방식은 Camera.Render()를 직접 호출하므로 정지 상태에서도
         * 동작할 수 있지만, 캡처 순서를 일관되게 유지하는 편이 안전합니다.
         */
        ShowBackgroundBlur();
        _pausePanelAnimator.Open();
        CursorLocker.Instance?.EnterUIMode();
        Time.timeScale = 0f;
    }

    private void ClosePausePanel()
    {
        if (_pausePanelAnimator == null)
        {
            return;
        }

        _pausePanelAnimator.Close();
        CursorLocker.Instance?.ExitUIMode();

        /*
         * 패널의 Close Tween은 SetUpdate(true)를 사용하므로
         * Time.timeScale을 먼저 복구해도 정상 동작합니다.
         */
        Time.timeScale = 1f;

        // Pause를 열면서 숨겼던 대화창 패널을 복원한다.
        if (_dialoguePanelHiddenByPause)
        {
            _dialoguePanelHiddenByPause = false;
            DialogueUI.Instance?.ShowPanelOnly();
        }
    }

    private void HandlePausePanelClosed()
    {
        HideBackgroundBlur();
    }

    #endregion

    #region Quest List

    /// <summary>
    /// QuestListUI 슬라이드 토글입니다.
    /// </summary>
    public void ToggleQuestList()
    {
        if (QuestListUI.Instance == null)
        {
            Debug.LogWarning(
                "[PlayerUIInputReader] QuestListUI.Instance가 없습니다.",
                this
            );
            return;
        }

        QuestListUI.Instance.ToggleSlide();
    }

    #endregion

    #region Background Blur

    /// <summary>
    /// 전역 Blur Manager에 배경 Blur 표시를 요청합니다.
    /// Manager는 호출 시점의 Camera.main을 찾아 화면을 캡처합니다.
    /// </summary>
    private void ShowBackgroundBlur()
    {
        if (UIBackgroundBlurManager.Instance == null)
        {
            Debug.LogWarning(
                "[PlayerUIInputReader] UIBackgroundBlurManager.Instance가 없습니다.",
                this
            );
            return;
        }

        UIBackgroundBlurManager.Instance.Show();
    }

    /// <summary>
    /// 이 패널이 사용하던 Blur 요청을 해제합니다.
    /// 요청 횟수가 0이 되었을 때 실제 Blur가 사라집니다.
    /// </summary>
    private void HideBackgroundBlur()
    {
        UIBackgroundBlurManager.Instance?.Hide();
    }

    #endregion

    #region Scene Check

    private bool IsInBattleScene()
    {
        return
            SceneTransitionManager.Instance != null &&
            SceneTransitionManager.Instance.IsInBattleScene();
    }

    private bool IsInDungeonScene()
    {
        return
            SceneTransitionManager.Instance != null &&
            SceneTransitionManager.Instance.IsInDungeonScene();
    }

    private bool IsInTitleScene()
    {
        return
            SceneTransitionManager.Instance != null &&
            SceneTransitionManager.Instance.IsInTitleScene();
    }

    /// <summary>
    /// 현재 활성 씬이 로딩 씬인지 확인합니다.
    /// </summary>
    private bool IsInLoadingScene()
    {
        return
            SceneTransitionManager.Instance != null &&
            SceneTransitionManager.Instance.IsInLoadingScene();
    }

    #endregion

    #region Scene Loading

    private void LoadScene(SceneId sceneId)
    {
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning(
                "[PlayerUIInputReader] SceneTransitionManager.Instance가 null입니다.",
                this
            );
            return;
        }

        /*
         * UI가 열린 상태로 씬이 전환되는 경우
         * OnClosed 이벤트가 호출되지 않을 수 있으므로 강제로 정리합니다.
         */
        UIBackgroundBlurManager.Instance?.ForceHide();
        Time.timeScale = 1f;
        SceneTransitionManager.Instance.LoadScene(sceneId);
    }

    #endregion

    private void OnDestroy()
    {
        UnsubscribePanelEvents();

        if (Instance == this)
        {
            Instance = null;
        }

        /*
         * Pause 상태에서 오브젝트가 파괴되거나 씬이 전환되는 경우를 대비합니다.
         */
        Time.timeScale = 1f;
    }

    #region Main HUD

    /// <summary>
    /// MainHUDPanel 슬라이드 인/아웃 토글입니다.
    /// </summary>
    public void ToggleMainHUD()
    {
        if (MainHUDUIController.Instance == null)
        {
            Debug.LogWarning(
                "[PlayerUIInputReader] MainHUDUIController.Instance가 없습니다.",
                this
            );
            return;
        }

        MainHUDUIController.Instance.ToggleSlide();
    }

    #endregion
}