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
            NPC prevNpc = GetNpcSafely(_current);
            if (prevNpc != null)
            {
                prevNpc.ShowInteractPrompt(false);
            }
            NPC nextNpc = GetNpcSafely(found);
            if (nextNpc != null)
            {
                nextNpc.ShowInteractPrompt(true);
            }
        }
        _current = found;
        if (_current != null && _interactAction.WasPressedThisFrame())
            _current.Interact(gameObject);
    }
    /// <summary>
    /// ITFInteractable이 파괴된 오브젝트를 가리키고 있어도 안전하게 NPC를 조회한다.
    /// (예: EventGameObject처럼 상호작용 후 스스로 Destroy되는 대상)
    /// </summary>
    private static NPC GetNpcSafely(ITFInteractable target)
    {
        Component component = target as Component;
        if (component == null) // Unity 오버로드: 파괴된 오브젝트도 여기서 true
        {
            return null;
        }
        return component.GetComponent<NPC>();
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