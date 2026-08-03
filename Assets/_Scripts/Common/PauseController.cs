using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PausePanel에 부착. Pause/Setting 두 뷰를 전환.
/// - ResumeBtn: PausePanel 전체를 닫음
/// - SettingBtn: Pause 뷰를 끄고 Setting 뷰를 켬 (패널 자체는 계속 열려있음)
/// - EscapeBtn: 던전 씬에서만 보임. 클릭 시 패널을 닫고 _returnScene으로 이동
/// - ExitBtn: 게임 종료용 버튼 필드만 (기능 미연결)
/// * Setting 뷰가 열린 상태에서 Esc를 누르면 PlayerUIInputReader가 ShowPauseView()를 호출해서
///   Pause 뷰로 되돌림 (패널을 닫지 않음).
/// </summary>
public class PauseController : MonoBehaviour
{
    [Header("Views")]
    [SerializeField] private GameObject _pauseObject;   // "Pause"
    [SerializeField] private GameObject _settingObject; // "Setting"

    [Header("Pause Buttons")]
    [SerializeField] private Button _resumeBtn;
    [SerializeField] private Button _settingBtn;
    [SerializeField] private Button _escapeBtn;
    [SerializeField] private Button _exitBtn; // 기능 미연결, 필드만

    [Header("Return Scene (EscapeBtn - 던전 씬에서만 활성)")]
    [SerializeField] private SceneId _returnScene = SceneId.Main;

    /// <summary>
    /// 지금 Setting 뷰가 열려있는지 여부. PlayerUIInputReader의 Esc 처리 분기에 사용됨.
    /// </summary>
    public bool IsSettingOpen { get; private set; }

    private void OnEnable()
    {
        if (_resumeBtn != null) _resumeBtn.onClick.AddListener(HandleResumeClicked);
        if (_settingBtn != null) _settingBtn.onClick.AddListener(HandleSettingClicked);
        if (_escapeBtn != null) _escapeBtn.onClick.AddListener(HandleEscapeClicked);

        // 패널이 새로 열릴 때마다 항상 Pause 뷰부터 시작
        ShowPauseView();
    }

    private void OnDisable()
    {
        if (_resumeBtn != null) _resumeBtn.onClick.RemoveListener(HandleResumeClicked);
        if (_settingBtn != null) _settingBtn.onClick.RemoveListener(HandleSettingClicked);
        if (_escapeBtn != null) _escapeBtn.onClick.RemoveListener(HandleEscapeClicked);
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

    private void HandleEscapeClicked()
    {
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("[PauseController] SceneTransitionManager.Instance가 없습니다.");
            return;
        }

        if (PlayerUIInputReader.Instance != null)
        {
            PlayerUIInputReader.Instance.TogglePausePanel();
        }

        SceneTransitionManager.Instance.LoadScene(_returnScene);
    }

    /// <summary>
    /// Pause 뷰로 전환하고, EscapeBtn 노출 여부를 현재 씬 기준으로 다시 계산.
    /// 패널이 새로 열릴 때, 그리고 Setting 뷰에서 Esc로 되돌아올 때 호출됨.
    /// </summary>
    public void ShowPauseView()
    {
        IsSettingOpen = false;

        if (_pauseObject != null) _pauseObject.SetActive(true);
        if (_settingObject != null) _settingObject.SetActive(false);

        bool isDungeonScene = SceneTransitionManager.Instance != null
            && SceneTransitionManager.Instance.IsInDungeonScene();

        if (_escapeBtn != null) _escapeBtn.gameObject.SetActive(isDungeonScene);
    }

    private void ShowSettingView()
    {
        IsSettingOpen = true;

        if (_pauseObject != null) _pauseObject.SetActive(false);
        if (_settingObject != null) _settingObject.SetActive(true);
    }
}