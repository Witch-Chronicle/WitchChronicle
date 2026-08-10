using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerInteractor : MonoBehaviour
{
    public InputActionAsset InputAsset;
    public float Range = 2f;
    public LayerMask InteractableMask = ~0;
    private InputAction _interactAction;
    private ITFInteractable _current;
    public ITFInteractable Current => _current;
    private void Awake()
    {
        _interactAction = InputAsset.FindAction("Player/Interact", throwIfNotFound: true);
    }
    private void Update()
    {
        ITFInteractable found = FindNearest();
        if (found != _current)
        {
            NPC prevNpc = (_current as Component)?.GetComponent<NPC>();
            if (prevNpc != null)
            {
                prevNpc.ShowInteractPrompt(false);
            }
            NPC nextNpc = (found as Component)?.GetComponent<NPC>();
            if (nextNpc != null)
            {
                nextNpc.ShowInteractPrompt(true);
            }
        }
        _current = found;
        if (_current != null && _interactAction.WasPressedThisFrame())
            _current.Interact(gameObject);
    }
    private ITFInteractable FindNearest()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, Range, InteractableMask);
        ITFInteractable nearest = null;
        float minSqrDist = float.MaxValue;
        foreach (var hit in hits)
        {
            ITFInteractable interactable = ResolveInteractable(hit.gameObject);
            if (interactable == null) continue;
            float sqrDist = (hit.transform.position - transform.position).sqrMagnitude;
            if (sqrDist < minSqrDist)
            {
                minSqrDist = sqrDist;
                nearest = interactable;
            }
        }
        return nearest;
    }
    /// <summary>
    /// 같은 오브젝트에 ITFInteractable이 여러 개 붙어있을 수 있다
    /// (예: NPC + PortalNPC, NPC + TeleportPortal).
    /// 컴포넌트 순서에 의존하지 않도록, NPC(대화)보다
    /// 별도의 액션 스크립트(Portal, Teleport 등)를 항상 우선한다.
    /// </summary>
    private static ITFInteractable ResolveInteractable(GameObject go)
    {
        ITFInteractable[] candidates = go.GetComponents<ITFInteractable>();
        if (candidates.Length == 0)
        {
            return null;
        }
        if (candidates.Length == 1)
        {
            return candidates[0];
        }
        foreach (ITFInteractable candidate in candidates)
        {
            if (candidate is NPC)
            {
                continue;
            }
            return candidate;
        }
        return candidates[0];
    }
}