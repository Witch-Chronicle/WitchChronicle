using UnityEngine;

/// <summary>
/// Boot 씬 전용. SystemManagers(DontDestroyOnLoad 대상)들의 Awake가 전부 끝난 뒤,
/// 최초 진입 씬(거점)으로 자동 이동.
/// 화면을 미리 덮어둔 채로(연출 없이) 조용히 로드하고, Main 도착 후 자연스럽게 걷히는 연출만 보여줌.
/// </summary>
public class BootLoader : MonoBehaviour
{
    [SerializeField] private SceneId _firstScene = SceneId.Main;
    [SerializeField] private TransitionController _transitionController;

    private void Start()
    {
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("[BootLoader] SceneTransitionManager.Instance가 없습니다.");
            return;
        }

        if (_transitionController != null)
        {
            _transitionController.SetCoveredImmediate();
        }

        SceneTransitionManager.Instance.LoadScene(_firstScene, skipCover: true);
    }
}