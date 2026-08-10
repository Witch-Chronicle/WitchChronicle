using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// PausePanel에 부착. Pause / Setting / ExitConfirm / EscapeConfirm 네 뷰를 전환.
/// - ResumeBtn: PausePanel 전체를 닫음
/// - SettingBtn: Pause 뷰를 끄고 Setting 뷰를 켬 (패널 자체는 계속 열려있음)
/// - ExitBtn: Pause 뷰를 끄고 ExitConfirm(게임 종료 확인) 뷰를 켬
/// - ExitConfirmBtn(ExitConfirm 뷰 내부): 게임 종료
/// - ExitCancelBtn(ExitConfirm 뷰 내부): Pause 뷰로 되돌림
/// - EscapeBtn: 던전 씬에서만 보임. 클릭 시 EscapeConfirm(탈출 확인) 뷰를 켬
/// - EscapeConfirmBtn(EscapeConfirm 뷰 내부): 실제로 패널을 닫고 _returnScene으로 이동
/// - EscapeCancelBtn(EscapeConfirm 뷰 내부): Pause 뷰로 되돌림
/// * Setting, ExitConfirm, EscapeConfirm 뷰가 열린 상태에서 Esc를 누르면 PlayerUIInputReader가
///   ShowPauseView()를 호출해서 Pause 뷰로 되돌림 (패널을 닫지 않음).
/// </summary>
public class PauseController : MonoBehaviour
{
    [Header("Views")]
    [SerializeField] private GameObject _pauseObject;        // "Pause"
    [SerializeField] private GameObject _settingObject;      // "Setting"
    [SerializeField] private GameObject _exitConfirmObject;  // "ExitConfirm" (게임 종료 확인)
    [SerializeField] private GameObject _escapeConfirmObject; // "EscapeConfirm" (던전 탈출 확인)
    [Header("Pause Buttons")]
    [SerializeField] private Button _resumeBtn;
    [SerializeField] private Button _settingBtn;
    [SerializeField] private Button _escapeBtn;
    [SerializeField] private Button _exitBtn;
    [Header("Exit Confirm Buttons (ExitConfirm 뷰 내부)")]
    [SerializeField] private Button _exitConfirmBtn; // 게임 종료
    [SerializeField] private Button _exitCancelBtn;  // Pause 뷰로 복귀
    [Header("Escape Confirm Buttons (EscapeConfirm 뷰 내부)")]
    [SerializeField] private Button _escapeConfirmBtn; // 실제 탈출
    [SerializeField] private Button _escapeCancelBtn;  // Pause 뷰로 복귀
    [Header("Return Scene (EscapeBtn - 던전 씬에서만 활성)")]
    [SerializeField] private SceneId _returnScene = SceneId.Main;
    [Header("Field Lock-On")]
    [Tooltip("EscapeConfirmBtn으로 던전을 나갈 때 록온 상태를 완전히 리셋할 컨트롤러입니다. 필드/던전 씬이 아니면 비워둬도 됩니다.")]
    [SerializeField] private FieldLockOnIndicatorController _fieldLockOnIndicatorController;
    /// <summary>
    /// 지금 Setting 뷰가 열려있는지 여부. PlayerUIInputReader의 Esc 처리 분기에 사용됨.
    /// </summary>
    public bool IsSettingOpen { get; private set; }
    /// <summary>
    /// 지금 ExitConfirm(게임 종료 확인) 뷰가 열려있는지 여부. PlayerUIInputReader의 Esc 처리 분기에 사용됨.
    /// </summary>
    public bool IsExitConfirmOpen { get; private set; }
    /// <summary>
    /// 지금 EscapeConfirm(던전 탈출 확인) 뷰가 열려있는지 여부. PlayerUIInputReader의 Esc 처리 분기에 사용됨.
    /// </summary>
    public bool IsEscapeConfirmOpen { get; private set; }
    private void OnEnable()
    {
        if (_resumeBtn != null) _resumeBtn.onClick.AddListener(HandleResumeClicked);
        if (_settingBtn != null) _settingBtn.onClick.AddListener(HandleSettingClicked);
        if (_escapeBtn != null) _escapeBtn.onClick.AddListener(HandleEscapeClicked);
        if (_exitBtn != null) _exitBtn.onClick.AddListener(HandleExitClicked);
        if (_exitConfirmBtn != null) _exitConfirmBtn.onClick.AddListener(HandleExitConfirmClicked);
        if (_exitCancelBtn != null) _exitCancelBtn.onClick.AddListener(HandleExitCancelClicked);
        if (_escapeConfirmBtn != null) _escapeConfirmBtn.onClick.AddListener(HandleEscapeConfirmClicked);
        if (_escapeCancelBtn != null) _escapeCancelBtn.onClick.AddListener(HandleEscapeCancelClicked);
        // 패널이 새로 열릴 때마다 항상 Pause 뷰부터 시작
        ShowPauseView();
    }
    private void OnDisable()
    {
        if (_resumeBtn != null) _resumeBtn.onClick.RemoveListener(HandleResumeClicked);
        if (_settingBtn != null) _settingBtn.onClick.RemoveListener(HandleSettingClicked);
        if (_escapeBtn != null) _escapeBtn.onClick.RemoveListener(HandleEscapeClicked);
        if (_exitBtn != null) _exitBtn.onClick.RemoveListener(HandleExitClicked);
        if (_exitConfirmBtn != null) _exitConfirmBtn.onClick.RemoveListener(HandleExitConfirmClicked);
        if (_exitCancelBtn != null) _exitCancelBtn.onClick.RemoveListener(HandleExitCancelClicked);
        if (_escapeConfirmBtn != null) _escapeConfirmBtn.onClick.RemoveListener(HandleEscapeConfirmClicked);
        if (_escapeCancelBtn != null) _escapeCancelBtn.onClick.RemoveListener(HandleEscapeCancelClicked);
    }
    private void HandleResumeClicked()
    {
        if (PlayerUIInputReader.Instance != null)
        {
            PlayerUIInputReader.Instance.TogglePausePanel();
        }
    }
    private void HandleSettingClicked()
    {
        ShowSettingView();
    }
    private void HandleExitClicked()
    {
        ShowExitConfirmView();
    }
    private void HandleExitConfirmClicked()
    {
        Debug.Log("[PauseController] 게임 종료");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    private void HandleExitCancelClicked()
    {
        ShowPauseView();
    }
    /// <summary>
    /// EscapeBtn 클릭 시: 바로 나가지 않고 EscapeConfirm 뷰를 먼저 보여준다.
    /// </summary>
    private void HandleEscapeClicked()
    {
        ShowEscapeConfirmView();
    }
    private void HandleEscapeCancelClicked()
    {
        ShowPauseView();
    }
    /// <summary>
    /// EscapeConfirmBtn 클릭 시: 실제로 던전을 나가 _returnScene으로 이동한다.
    /// </summary>
    private void HandleEscapeConfirmClicked()
    {
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("[PauseController] SceneTransitionManager.Instance가 없습니다.");
            return;
        }
        // 던전을 나가 Main으로 돌아가므로, 록온 상태를 완전히 리셋합니다.
        // (SetExternallySuppressed와 달리 타겟 자체를 비워서 되돌아왔을 때 이전 록온이 남지 않게 함)
        if (_fieldLockOnIndicatorController != null)
        {
            _fieldLockOnIndicatorController.ForceHideIndicator();
        }
        if (PlayerUIInputReader.Instance != null)
        {
            PlayerUIInputReader.Instance.TogglePausePanel();
        }
        if (PersistentCharacterManager.Instance != null)
        {
            PersistentCharacterManager.Instance.RestoreActivePartyVitals();
        }
        SceneTransitionManager.Instance.LoadSceneWithLoading(_returnScene, waitForReadySignal: true);
    }
    /// <summary>
    /// Pause 뷰로 전환하고, EscapeBtn 노출 여부를 현재 씬 기준으로 다시 계산.
    /// 패널이 새로 열릴 때, 그리고 Setting/ExitConfirm/EscapeConfirm 뷰에서 Esc로 되돌아올 때 호출됨.
    /// </summary>
    public void ShowPauseView()
    {
        IsSettingOpen = false;
        IsExitConfirmOpen = false;
        IsEscapeConfirmOpen = false;
        if (_pauseObject != null) _pauseObject.SetActive(true);
        if (_settingObject != null) _settingObject.SetActive(false);
        if (_exitConfirmObject != null) _exitConfirmObject.SetActive(false);
        if (_escapeConfirmObject != null) _escapeConfirmObject.SetActive(false);
        bool isDungeonScene = SceneTransitionManager.Instance != null
            && SceneTransitionManager.Instance.IsInDungeonScene();
        if (_escapeBtn != null) _escapeBtn.gameObject.SetActive(isDungeonScene);
    }
    private void ShowSettingView()
    {
        IsSettingOpen = true;
        IsExitConfirmOpen = false;
        IsEscapeConfirmOpen = false;
        if (_pauseObject != null) _pauseObject.SetActive(false);
        if (_settingObject != null) _settingObject.SetActive(true);
        if (_exitConfirmObject != null) _exitConfirmObject.SetActive(false);
        if (_escapeConfirmObject != null) _escapeConfirmObject.SetActive(false);
    }
    private void ShowExitConfirmView()
    {
        IsSettingOpen = false;
        IsExitConfirmOpen = true;
        IsEscapeConfirmOpen = false;
        if (_pauseObject != null) _pauseObject.SetActive(false);
        if (_settingObject != null) _settingObject.SetActive(false);
        if (_exitConfirmObject != null) _exitConfirmObject.SetActive(true);
        if (_escapeConfirmObject != null) _escapeConfirmObject.SetActive(false);
    }
    private void ShowEscapeConfirmView()
    {
        IsSettingOpen = false;
        IsExitConfirmOpen = false;
        IsEscapeConfirmOpen = true;
        if (_pauseObject != null) _pauseObject.SetActive(false);
        if (_settingObject != null) _settingObject.SetActive(false);
        if (_exitConfirmObject != null) _exitConfirmObject.SetActive(false);
        if (_escapeConfirmObject != null) _escapeConfirmObject.SetActive(true);
    }
}