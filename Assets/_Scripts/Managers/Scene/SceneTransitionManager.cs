using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환을 담당하는 범용 유틸리티. 어디서든 SceneTransitionManager.Instance.LoadScene(...)으로 호출 가능.
/// - SceneId(enum) 기반 호출을 권장 (오타 방지). 문자열 버전도 남겨둠(필요 시).
/// - 비동기 로딩(LoadSceneAsync) 사용, 중복 호출 방지.
/// - delayBeforeLoad: 페이드/전환 애니메이션이 끝날 때까지 기다렸다가 실제 로드를 시작하고 싶을 때 사용.
/// - onBeforeLoad/onLoaded: 씬 로드 전/후 훅.
/// - IsInBattleScene()/IsInDungeonScene(): 지금 해당 씬이 로드되어 있는지 확인하는 창구.
/// - TransitionController(SystemUI 하위, DontDestroyOnLoad)를 참조해서, LoadScene/LoadSceneAdditive/UnloadScene
///   호출 시 자동으로 화면을 덮고(CoverScreen) 걷는(RevealScreen) 연출을 감싸줌.
///   각 호출부는 더 이상 TransitionController를 직접 호출할 필요 없음.
/// - skipCover: true로 넘기면 CoverScreen 단계를 건너뜀 (Boot 씬처럼 이미 SetCoveredImmediate()로
///   화면을 미리 덮어둔 상태에서 조용히 로드하고, 도착 후 RevealScreen만 자연스럽게 보여주고 싶을 때 사용).
/// * DontDestroyOnLoad로 씬이 바뀌어도 이 매니저 자체는 계속 살아있음.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Transition (SystemUI 하위, 없으면 연출 없이 즉시 전환)")]
    [SerializeField] private TransitionController _transitionController;

    public bool IsLoading { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 지금 Battle 씬이 로드되어 있는지 여부.
    /// </summary>
    public bool IsInBattleScene()
    {
        Scene battleScene = SceneManager.GetSceneByName(SceneId.Battle.ToString());
        return battleScene.IsValid() && battleScene.isLoaded;
    }

    /// <summary>
    /// 지금 Dungeon 씬이 로드되어 있는지 여부.
    /// </summary>
    public bool IsInDungeonScene()
    {
        Scene dungeonScene = SceneManager.GetSceneByName(SceneId.Dungeon.ToString());
        return dungeonScene.IsValid() && dungeonScene.isLoaded;
    }

    /// <summary>
    /// SceneId(enum)로 씬 전환. 오타 걱정 없이 이걸 기본으로 사용하면 됨.
    /// </summary>
    public void LoadScene(SceneId sceneId, float delayBeforeLoad = 0f, Action onBeforeLoad = null, Action onLoaded = null, bool skipCover = false)
    {
        LoadScene(sceneId.ToString(), delayBeforeLoad, onBeforeLoad, onLoaded, skipCover);
    }

    /// <summary>
    /// 문자열로 직접 씬 전환. SceneId에 없는 씬을 임시로 불러야 할 때만 사용.
    /// </summary>
    public void LoadScene(string sceneName, float delayBeforeLoad = 0f, Action onBeforeLoad = null, Action onLoaded = null, bool skipCover = false)
    {
        if (IsLoading)
        {
            Debug.LogWarning($"[SceneTransitionManager] 이미 씬 전환 중입니다. 요청 무시: {sceneName}");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneTransitionManager] sceneName이 비어있습니다.");
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName, delayBeforeLoad, onBeforeLoad, onLoaded, skipCover));
    }

    private IEnumerator LoadSceneRoutine(string sceneName, float delayBeforeLoad, Action onBeforeLoad, Action onLoaded, bool skipCover)
    {
        IsLoading = true;

        if (skipCover == false)
        {
            yield return CoverScreenRoutine();
        }

        onBeforeLoad?.Invoke();

        if (delayBeforeLoad > 0f)
        {
            yield return new WaitForSeconds(delayBeforeLoad);
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        if (operation == null)
        {
            Debug.LogError($"[SceneTransitionManager] 씬을 찾을 수 없습니다: {sceneName} (Build Settings 등록 확인)");
            IsLoading = false;

            yield return RevealScreenRoutine();

            yield break;
        }

        while (operation.isDone == false)
        {
            yield return null;
        }

        onLoaded?.Invoke();

        yield return RevealScreenRoutine();

        IsLoading = false;
    }

    /// <summary>
    /// 기존 씬을 유지한 채 새 씬을 겹쳐 로드 (전투 씬 진입용)
    /// </summary>
    public void LoadSceneAdditive(string sceneName, System.Action onLoaded = null, System.Action onCovered = null)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneTransitionManager] sceneName이 비어있습니다.");
            return;
        }

        StartCoroutine(LoadSceneAdditiveRoutine(sceneName, onLoaded, onCovered));
    }

    private IEnumerator LoadSceneAdditiveRoutine(string sceneName, Action onLoaded, Action onCovered)
    {
        yield return CoverScreenRoutine();

        // 화면이 완전히 가려진 시점 - 씬 로드 시작 전에 미리 정리할 것들(예: 이전 씬 카메라/컨트롤러 비활성화)을 처리
        onCovered?.Invoke();

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        if (operation == null)
        {
            Debug.LogError($"[SceneTransitionManager] 씬을 찾을 수 없습니다: {sceneName}");

            yield return RevealScreenRoutine();

            yield break;
        }

        while (operation.isDone == false)
        {
            yield return null;
        }

        onLoaded?.Invoke();

        yield return RevealScreenRoutine();
    }

    /// <summary>
    /// Additive로 로드했던 씬을 제거 (전투 종료 후 전투 씬만 정리)
    /// </summary>
    public void UnloadScene(string sceneName, Action onUnloaded = null)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneTransitionManager] sceneName이 비어있습니다.");
            return;
        }

        StartCoroutine(UnloadSceneRoutine(sceneName, onUnloaded));
    }

    private IEnumerator UnloadSceneRoutine(string sceneName, Action onUnloaded)
    {
        yield return CoverScreenRoutine();

        AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);

        if (operation == null)
        {
            Debug.LogWarning($"[SceneTransitionManager] 언로드할 씬을 찾지 못했습니다: {sceneName}");

            yield return RevealScreenRoutine();

            yield break;
        }

        while (operation.isDone == false)
        {
            yield return null;
        }

        onUnloaded?.Invoke();

        yield return RevealScreenRoutine();
    }

    /// <summary>
    /// TransitionController가 있으면 화면을 덮을 때까지 대기, 없으면 즉시 통과.
    /// </summary>
    private IEnumerator CoverScreenRoutine()
    {
        if (_transitionController == null)
        {
            yield break;
        }

        bool covered = false;
        _transitionController.CoverScreen(() => covered = true);
        yield return new WaitUntil(() => covered);
    }

    /// <summary>
    /// TransitionController가 있으면 화면이 걷힐 때까지 대기, 없으면 즉시 통과.
    /// </summary>
    private IEnumerator RevealScreenRoutine()
    {
        if (_transitionController == null)
        {
            yield break;
        }

        bool revealed = false;
        _transitionController.RevealScreen(() => revealed = true);
        yield return new WaitUntil(() => revealed);
    }
}