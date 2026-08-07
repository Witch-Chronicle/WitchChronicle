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
    /// 지금 Title 씬이 로드되어 있는지 여부.
    /// </summary>
    public bool IsInTitleScene()
    {
        Scene titleScene = SceneManager.GetSceneByName(SceneId.Title.ToString());
        return titleScene.IsValid() && titleScene.isLoaded;
    }

    /// <summary>
    /// 현재 활성 씬이 Loading 씬인지 확인합니다.
    /// </summary>
    public bool IsInLoadingScene()
    {
        Scene loadingScene = SceneManager.GetSceneByName(SceneId.Loading.ToString());
        return loadingScene.IsValid() && loadingScene.isLoaded;
    }

    /// <summary>
    /// SceneId(enum)로 씬 전환. 오타 걱정 없이 이걸 기본으로 사용하면 됨.
    /// </summary>
    public void LoadScene(SceneId sceneId, float delayBeforeLoad = 0f, Action onBeforeLoad = null, Action onLoaded = null, bool skipCover = false, bool skipReveal = false)
    {
        LoadScene(sceneId.ToString(), delayBeforeLoad, onBeforeLoad, onLoaded, skipCover, skipReveal);
    }

    /// <summary>
    /// 문자열로 직접 씬 전환. SceneId에 없는 씬을 임시로 불러야 할 때만 사용.
    /// </summary>
    public void LoadScene(string sceneName, float delayBeforeLoad = 0f, Action onBeforeLoad = null, Action onLoaded = null, bool skipCover = false, bool skipReveal = false)
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

        StartCoroutine(LoadSceneRoutine(sceneName, delayBeforeLoad, onBeforeLoad, onLoaded, skipCover, skipReveal));
    }

    private IEnumerator LoadSceneRoutine(string sceneName, float delayBeforeLoad, Action onBeforeLoad, Action onLoaded, bool skipCover, bool skipReveal)
    {
        IsLoading = true;

        if (skipCover == false)
        {
            yield return CoverScreenRoutine();

        }

        UIBackgroundBlurManager.Instance?.Hide();


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

            if (skipReveal == false)
            {
                yield return RevealScreenRoutine();
            }

            yield break;
        }

        while (operation.isDone == false)
        {
            yield return null;
        }

        onLoaded?.Invoke();

        if (skipReveal == false)
        {
            yield return RevealScreenRoutine();
        }

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
        UIBackgroundBlurManager.Instance?.Hide();

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
    /// 화면 전환 연출 없는 Additive 씬 로드
    /// </summary>
    /// <param name="sceneName">로드 씬 이름</param>
    /// <param name="onBeforeLoad">로드 직전 콜백</param>
    /// <param name="onLoaded">로드 완료 콜백</param>
    public void LoadSceneAdditiveWithoutTransition(
        string sceneName,
        Action onBeforeLoad = null,
        Action onLoaded = null)
    {
        if (IsLoading)
        {
            Debug.LogWarning(
                $"[SceneTransitionManager] 이미 씬 전환 중입니다. 요청 무시: {sceneName}");

            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning(
                "[SceneTransitionManager] sceneName이 비어있습니다.");

            return;
        }

        StartCoroutine(
            LoadSceneAdditiveWithoutTransitionRoutine(
                sceneName,
                onBeforeLoad,
                onLoaded));
    }

    /// <summary>
    /// 화면 전환 연출 없는 Additive 씬 로드 진행
    /// </summary>
    /// <param name="sceneName">로드 씬 이름</param>
    /// <param name="onBeforeLoad">로드 직전 콜백</param>
    /// <param name="onLoaded">로드 완료 콜백</param>
    private IEnumerator LoadSceneAdditiveWithoutTransitionRoutine(
        string sceneName,
        Action onBeforeLoad,
        Action onLoaded)
    {
        IsLoading = true;

        onBeforeLoad?.Invoke();

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Additive);

        if (operation == null)
        {
            Debug.LogError(
                $"[SceneTransitionManager] 씬을 찾을 수 없습니다: {sceneName}");

            IsLoading = false;
            yield break;
        }

        while (operation.isDone == false)
        {
            yield return null;
        }

        onLoaded?.Invoke();

        IsLoading = false;
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

        UIBackgroundBlurManager.Instance?.Hide();

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

    private bool _targetSceneReady;

    public void ReportSceneReady()
    {
        _targetSceneReady = true;
    }

    public void LoadSceneWithLoading(
        SceneId targetSceneId,
        Action onBeforeLoad = null,
        Action onLoaded = null,
        bool skipCover = false,
        bool waitForReadySignal = false,
        float readySignalTimeout = 5f)
    {
        if (IsLoading)
        {
            Debug.LogWarning($"[SceneTransitionManager] 이미 씬 전환 중입니다. 요청 무시: {targetSceneId}");
            return;
        }

        StartCoroutine(LoadSceneWithLoadingRoutine(targetSceneId, onBeforeLoad, onLoaded, skipCover, waitForReadySignal, readySignalTimeout));
    }

    private IEnumerator LoadSceneWithLoadingRoutine(
    SceneId targetSceneId,
    Action onBeforeLoad,
    Action onLoaded,
    bool skipCover,
    bool waitForReadySignal,
    float readySignalTimeout)
    {
        IsLoading = true;
        _targetSceneReady = false;

        if (skipCover == false)
        {
            yield return CoverScreenRoutine();
        }

        UIBackgroundBlurManager.Instance?.Hide();
        onBeforeLoad?.Invoke();

        AsyncOperation loadingSceneOp = SceneManager.LoadSceneAsync(SceneId.Loading.ToString());

        if (loadingSceneOp == null)
        {
            Debug.LogError("[SceneTransitionManager] Loading 씬을 찾을 수 없습니다. Build Settings 확인.");
            IsLoading = false;
            yield return RevealScreenRoutine();
            yield break;
        }

        while (loadingSceneOp.isDone == false)
        {
            yield return null;
        }

        LoadingSceneUIController.Instance?.SetProgressImmediate(0f);

        // Loading 씬은 걷히는 애니메이션 없이 즉시 노출
        _transitionController?.SetRevealedImmediate();

        // 1) 에셋 로드 단계 (0~90%). 화면은 계속 열려있는 상태.
        AsyncOperation targetOp = SceneManager.LoadSceneAsync(targetSceneId.ToString());

        if (targetOp == null)
        {
            Debug.LogError($"[SceneTransitionManager] 씬을 찾을 수 없습니다: {targetSceneId}");
            IsLoading = false;
            yield break;
        }

        targetOp.allowSceneActivation = false;

        while (targetOp.progress < 0.9f)
        {
            float progress01 = Mathf.Clamp01(targetOp.progress / 0.9f);
            LoadingSceneUIController.Instance?.SetProgress(progress01 * 0.9f);
            yield return null;
        }

        // 2) 에셋 로드는 끝났으므로 100%로 채운다. 이 시점까지는 아직 화면이 열려있어서
        //    "100%가 찍히는 순간"이 실제로 유저에게 보인다.
        LoadingSceneUIController.Instance?.SetProgress(1f);

        // 표시된 진행률이 부드러운 보간(MoveTowards)으로 실제 1.0에 도달할 때까지 대기.
        // 그래야 화면이 열린 상태에서 100% 숫자가 눈에 보인 뒤에 CoverIn이 시작된다.
        yield return new WaitUntil(() => LoadingSceneUIController.Instance == null || LoadingSceneUIController.Instance.IsDisplayComplete);

        // 100%를 눈으로 확인할 최소한의 시간 확보
        yield return new WaitForSeconds(0.8f);

        // 3) 이제 화면을 덮고, 그 뒤에서 실제 활성화 + 무거운 초기화를 진행한다.
        yield return CoverScreenRoutine();

        targetOp.allowSceneActivation = true;

        while (targetOp.isDone == false)
        {
            yield return null;
        }

        if (waitForReadySignal)
        {
            float elapsed = 0f;

            // 이미 화면이 덮인 상태이므로 진행률 표시는 갱신할 필요 없이 조용히 대기만 한다.
            while (_targetSceneReady == false && elapsed < readySignalTimeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_targetSceneReady == false)
            {
                Debug.LogWarning($"[SceneTransitionManager] {targetSceneId}에서 SceneReadySignal을 받지 못해 타임아웃으로 진행합니다.");
            }
        }
        else
        {
            yield return null;
        }

        onLoaded?.Invoke();

        yield return RevealScreenRoutine();

        IsLoading = false;
    }
}