using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// Title 씬 메인 컨트롤러.
/// - StartBtn: TitleTransition을 페이드인(0->1)한 뒤 Main 씬으로 이동
/// - SettingBtn: SettingPanel 활성화
/// - ExitBtn: 게임 종료
/// - SettingPanel이 열려있을 때 Esc 또는 BG 클릭으로 닫힘
/// </summary>
public class TitleController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _startBtn;
    [SerializeField] private Button _settingBtn;
    [SerializeField] private Button _exitBtn;

    [Header("Setting Panel")]
    [SerializeField] private GameObject _settingPanel;
    [SerializeField] private CanvasGroup _settingPanelCanvasGroup;
    [SerializeField] private float _settingFadeDuration = 0.2f;
    [Tooltip("SettingPanel 안의 배경 클릭 시 닫기용 (BG에 Button 컴포넌트 필요)")]
    [SerializeField] private Button _settingBgBtn;

    [Header("Transition")]
    [SerializeField] private GameObject _titleTransitionObject;
    [SerializeField] private CanvasGroup _titleTransitionCanvasGroup;
    [SerializeField] private float _transitionFadeDuration = 0.5f;
    [Tooltip("페이드가 완전히 끝난 뒤, 씬 전환 전까지 대기하는 시간")]
    [SerializeField] private float _transitionHoldDuration = 1f;

    [Header("Scene")]
    [SerializeField] private SceneId _nextScene = SceneId.Main;

    private bool _isTransitioning;

    private void Awake()
    {
        if (_startBtn != null) _startBtn.onClick.AddListener(OnClickStart);
        if (_settingBtn != null) _settingBtn.onClick.AddListener(OnClickSetting);
        if (_exitBtn != null) _exitBtn.onClick.AddListener(OnClickExit);
        if (_settingBgBtn != null) _settingBgBtn.onClick.AddListener(CloseSettingPanel);

        HideSettingPanelImmediate();

        if (_titleTransitionObject != null) _titleTransitionObject.SetActive(false);
        if (_titleTransitionCanvasGroup != null) _titleTransitionCanvasGroup.alpha = 0f;
    }

    private void Update()
    {
        if (_settingPanel == null || _settingPanel.activeSelf == false) return;
        if (_settingPanelCanvasGroup != null && _settingPanelCanvasGroup.interactable == false) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseSettingPanel();
        }
    }

    private void OnClickStart()
    {
        if (_isTransitioning) return;

        PlayTransitionAndLoadScene();
    }

    private void OnClickSetting()
    {
        ShowSettingPanel();
    }

    private void ShowSettingPanel()
    {
        if (_settingPanel != null) _settingPanel.SetActive(true);

        if (_settingPanelCanvasGroup == null) return;

        _settingPanelCanvasGroup.DOKill();
        _settingPanelCanvasGroup.interactable = false;
        _settingPanelCanvasGroup.blocksRaycasts = false;

        _settingPanelCanvasGroup
            .DOFade(1f, _settingFadeDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                _settingPanelCanvasGroup.interactable = true;
                _settingPanelCanvasGroup.blocksRaycasts = true;
            });
    }

    private void CloseSettingPanel()
    {
        if (_settingPanelCanvasGroup == null)
        {
            if (_settingPanel != null) _settingPanel.SetActive(false);
            return;
        }

        _settingPanelCanvasGroup.DOKill();
        _settingPanelCanvasGroup.interactable = false;
        _settingPanelCanvasGroup.blocksRaycasts = false;

        _settingPanelCanvasGroup
            .DOFade(0f, _settingFadeDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                if (_settingPanel != null) _settingPanel.SetActive(false);
            });
    }

    private void HideSettingPanelImmediate()
    {
        if (_settingPanelCanvasGroup != null)
        {
            _settingPanelCanvasGroup.DOKill();
            _settingPanelCanvasGroup.alpha = 0f;
            _settingPanelCanvasGroup.interactable = false;
            _settingPanelCanvasGroup.blocksRaycasts = false;
        }

        if (_settingPanel != null) _settingPanel.SetActive(false);
    }

    private void OnClickExit()
    {
        Debug.Log("[TitleController] 게임 종료");

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    /// <summary>
    /// TitleTransition의 CanvasGroup을 0->1로 페이드인, 완료되면 Main 씬으로 이동.
    /// </summary>
    private void PlayTransitionAndLoadScene()
    {
        _isTransitioning = true;

        if (_startBtn != null) _startBtn.interactable = false;

        if (_titleTransitionObject != null) _titleTransitionObject.SetActive(true);

        if (_titleTransitionCanvasGroup == null)
        {
            LoadNextScene();
            return;
        }

        _titleTransitionCanvasGroup.DOKill();
        _titleTransitionCanvasGroup.alpha = 0f;

        DOTween.Sequence()
            .Append(_titleTransitionCanvasGroup.DOFade(1f, _transitionFadeDuration).SetEase(Ease.Linear))
            .AppendInterval(_transitionHoldDuration)
            .OnComplete(LoadNextScene);
    }

    private void LoadNextScene()
    {
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("[TitleController] SceneTransitionManager.Instance가 없습니다.");
            return;
        }

        // TitleTransition으로 이미 화면을 덮은 상태라, 전역 TransitionController의 Cover 단계는 건너뜀
        SceneTransitionManager.Instance.LoadScene(_nextScene, skipCover: true);
    }
}