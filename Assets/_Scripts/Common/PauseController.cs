using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// PausePanel에 부착. Pause/Setting/Confirm 세 뷰를 전환.
/// - ResumeBtn: PausePanel 전체를 닫음
/// - SettingBtn: Pause 뷰를 끄고 Setting 뷰를 켬 (패널 자체는 계속 열려있음)
/// - ExitBtn: Pause 뷰를 끄고 Confirm(종료 확인) 뷰를 켬
/// - ConfirmBtn(Confirm 뷰 내부): 게임 종료
/// - CancelBtn(Confirm 뷰 내부): Pause 뷰로 되돌림
/// - EscapeBtn: 던전 씬에서만 보임. 클릭 시 패널을 닫고 _returnScene으로 이동
/// * Setting 또는 Confirm 뷰가 열린 상태에서 Esc를 누르면 PlayerUIInputReader가
///   ShowPauseView()를 호출해서 Pause 뷰로 되돌림 (패널을 닫지 않음).
/// </summary>
public class PauseController : MonoBehaviour
{
    [Header("Views")]
    [SerializeField] private GameObject _pauseObject;   // "Pause"
    [SerializeField] private GameObject _settingObject; // "Setting"
    [SerializeField] private GameObject _confirmObject; // "Confirm" (종료 확인)
    [Header("Pause Buttons")]
    [SerializeField] private Button _resumeBtn;
    [SerializeField] private Button _settingBtn;
    [SerializeField] private Button _escapeBtn;
    [SerializeField] private Button _exitBtn;
    [Header("Confirm Buttons (Confirm 뷰 내부)")]
    [SerializeField] private Button _confirmBtn; // 게임 종료
    [SerializeField] private Button _cancelBtn;  // Pause 뷰로 복귀
    [Header("Return Scene (EscapeBtn - 던전 씬에서만 활성)")]
    [SerializeField] private SceneId _returnScene = SceneId.Main;
    [Header("Field Lock-On")]
    [Tooltip("EscapeBtn으로 던전을 나갈 때 록온 상태를 완전히 리셋할 컨트롤러입니다. 필드/던전 씬이 아니면 비워둬도 됩니다.")]
    [SerializeField] private FieldLockOnIndicatorController _fieldLockOnIndicatorController;
    /// <summary>
    /// 지금 Setting 뷰가 열려있는지 여부. PlayerUIInputReader의 Esc 처리 분기에 사용됨.
    /// </summary>
    public bool IsSettingOpen { get; private set; }
    /// <summary>
    /// 지금 Confirm(종료 확인) 뷰가 열려있는지 여부. PlayerUIInputReader의 Esc 처리 분기에 사용됨.
    /// </summary>
    public bool IsConfirmOpen { get; private set; }
    private void OnEnable()
    {
        if (_resumeBtn != null) _resumeBtn.onClick.AddListener(HandleResumeClicked);
        if (_settingBtn != null) _settingBtn.onClick.AddListener(HandleSettingClicked);
        if (_escapeBtn != null) _escapeBtn.onClick.AddListener(HandleEscapeClicked);
        if (_exitBtn != null) _exitBtn.onClick.AddListener(HandleExitClicked);
        if (_confirmBtn != null) _confirmBtn.onClick.AddListener(HandleConfirmClicked);
        if (_cancelBtn != null) _cancelBtn.onClick.AddListener(HandleCancelClicked);
        // 패널이 새로 열릴 때마다 항상 Pause 뷰부터 시작
        ShowPauseView();
    }
    private void OnDisable()
    {
        if (_resumeBtn != null) _resumeBtn.onClick.RemoveListener(HandleResumeClicked);
        if (_settingBtn != null) _settingBtn.onClick.RemoveListener(HandleSettingClicked);
        if (_escapeBtn != null) _escapeBtn.onClick.RemoveListener(HandleEscapeClicked);
        if (_exitBtn != null) _exitBtn.onClick.RemoveListener(HandleExitClicked);
        if (_confirmBtn != null) _confirmBtn.onClick.RemoveListener(HandleConfirmClicked);
        if (_cancelBtn != null) _cancelBtn.onClick.RemoveListener(HandleCancelClicked);
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
        ShowConfirmView();
    }
    private void HandleConfirmClicked()
    {
        Debug.Log("[PauseController] 게임 종료");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    private void HandleCancelClicked()
    {
        ShowPauseView();
    }
    private void HandleEscapeClicked()
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
        SceneTransitionManager.Instance.LoadSceneWithLoading(_returnScene, waitForReadySignal: true);
    }
    /// <summary>
    /// Pause 뷰로 전환하고, EscapeBtn 노출 여부를 현재 씬 기준으로 다시 계산.
    /// 패널이 새로 열릴 때, 그리고 Setting/Confirm 뷰에서 Esc로 되돌아올 때 호출됨.
    /// </summary>
    public void ShowPauseView()
    {
        IsSettingOpen = false;
        IsConfirmOpen = false;
        if (_pauseObject != null) _pauseObject.SetActive(true);
        if (_settingObject != null) _settingObject.SetActive(false);
        if (_confirmObject != null) _confirmObject.SetActive(false);
        bool isDungeonScene = SceneTransitionManager.Instance != null
            && SceneTransitionManager.Instance.IsInDungeonScene();
        if (_escapeBtn != null) _escapeBtn.gameObject.SetActive(isDungeonScene);
    }
    private void ShowSettingView()
    {
        IsSettingOpen = true;
        IsConfirmOpen = false;
        if (_pauseObject != null) _pauseObject.SetActive(false);
        if (_settingObject != null) _settingObject.SetActive(true);
        if (_confirmObject != null) _confirmObject.SetActive(false);
    }
    private void ShowConfirmView()
    {
        IsSettingOpen = false;
        IsConfirmOpen = true;
        if (_pauseObject != null) _pauseObject.SetActive(false);
        if (_settingObject != null) _settingObject.SetActive(false);
        if (_confirmObject != null) _confirmObject.SetActive(true);
    }
}