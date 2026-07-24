using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 이 오브젝트(와 자식들)를 씬이 바뀌어도 파괴되지 않게 유지시키는 범용 컴포넌트.
/// - 같은 Key를 가진 오브젝트가 씬에 여러 개 생기면(예: DontDestroyOnLoad로 살아남은 이전 씬의 것 +
///   새로 로드된 씬에 원래 배치되어 있던 것), 먼저 등록된 것만 남기고 나머지는 자동으로 파괴됨.
/// - SystemManagers, PlayerUI처럼 "씬 전환에도 유지되어야 하는 루트 오브젝트"에 붙여서 사용.
///   부모에 이 컴포넌트 하나만 붙이면 자식 전체가 통째로 유지됨.
/// - PlayerUI는 Battle_1 씬에서는 하위 오브젝트 전부 비활성화(숨김).
///   숨기기 직전에, 열려있던 UIPanelAnimator 패널들은 전부 강제로 닫아서
///   나중에 PlayerUI가 다시 켜졌을 때 이전에 열려있던 패널이 그대로 남아있는 문제를 방지.
/// * 이 컴포넌트는 씬 전환에도 파괴되지 않으므로, Awake()는 최초 1회만 실행됨 ->
///   씬이 바뀔 때마다 다시 체크하려면 SceneManager.sceneLoaded/sceneUnloaded 이벤트를 구독해야 함.
/// * Additive 로딩(전투 씬을 겹쳐 로드/언로드하는 방식) 대응: 씬이 "로드"될 때뿐 아니라
///   "언로드"될 때도(예: 전투 씬 Unload 후 던전으로 복귀) 상태를 다시 체크해야 하므로
///   sceneUnloaded 이벤트도 함께 구독함.
/// * Battle_1 여부 판단은 SceneTransitionManager.IsInBattleScene()에 위임 (판단 로직을 한 곳으로 통일).
/// </summary>
public class PersistentRoot : MonoBehaviour
{
    [SerializeField] private PersistentRootKey _key;

    private static readonly Dictionary<PersistentRootKey, PersistentRoot> _registry =
        new Dictionary<PersistentRootKey, PersistentRoot>();

    private void Awake()
    {
        if (_registry.TryGetValue(_key, out var existing) && existing != null)
        {
            // 이미 이 키로 살아남은 오브젝트가 있음 -> 지금 새로 로드된(중복된) 이쪽을 파괴
            Destroy(gameObject);
            return;
        }

        _registry[_key] = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += HandleSceneLoaded;
        SceneManager.sceneUnloaded += HandleSceneUnloaded;

        // 최초 생성 시점의 씬 기준으로도 한 번 체크
        UpdateVisibility();
    }

    private void OnDestroy()
    {
        // 내가 등록된 당사자일 때만 레지스트리에서 제거 (중복이라 파괴된 쪽은 애초에 등록 안 됐으니 상관없음)
        if (_registry.TryGetValue(_key, out var registered) && registered == this)
        {
            _registry.Remove(_key);
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneUnloaded -= HandleSceneUnloaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateVisibility();
    }

    /// <summary>
    /// Additive로 로드했던 씬(예: 전투 씬)이 Unload될 때도 상태를 다시 체크.
    /// 이게 없으면 전투 씬을 Unload해서 던전으로 복귀해도 PlayerUI가 계속 숨겨진 채로 남음.
    /// </summary>
    private void HandleSceneUnloaded(Scene scene)
    {
        UpdateVisibility();
    }

    /// <summary>
    /// PlayerUI는 Battle_1 씬이 로드되어 있는 동안은 하위 오브젝트 전부 비활성화(숨김), 그 외에는 활성화.
    /// * 다른 Key/씬 조합도 필요해지면 여기에 조건만 추가하면 됨.
    /// </summary>
    private void UpdateVisibility()
    {
        if (_key != PersistentRootKey.PlayerUI) return;

        bool shouldHide = SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsInBattleScene();

        if (shouldHide)
        {
            CloseAllOpenPanels();
        }

        gameObject.SetActive(shouldHide == false);
    }

    /// <summary>
    /// 이 오브젝트 하위에 있는 UIPanelAnimator 중 열려있는 것들을 전부 애니메이션 없이 즉시 닫음.
    /// PlayerUI를 비활성화하기 직전에 호출해서, 다시 켜졌을 때 이전 상태가 남아있지 않도록 함.
    /// </summary>
    private void CloseAllOpenPanels()
    {
        UIPanelAnimator[] animators = GetComponentsInChildren<UIPanelAnimator>(true);

        foreach (var animator in animators)
        {
            if (animator != null && animator.IsOpen)
            {
                animator.SetClosedImmediate();
            }
        }
    }
}