using UnityEngine;
using UnityEngine.InputSystem;

/// 주변 IInteractable 감지 + 상호작용 키 처리.
/// InputActionAsset에 "Player/Interact"(Button) 액션
public class PlayerInteractor : MonoBehaviour
{
    public InputActionAsset InputAsset;
    public float Range = 2f;
    public LayerMask InteractableMask = ~0;

    private InputAction _interactAction;
    private ITFInteractable _current;

    /// 현재 상호작용 가능한 대상. UI(③) 프롬프트 표시에 사용.
    public ITFInteractable Current => _current;

    private void Awake()
    {
        _interactAction = InputAsset.FindAction("Player/Interact", throwIfNotFound: true);
    }

    private void Update()
    {
        _current = FindNearest();
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
            if (!hit.TryGetComponent<ITFInteractable>(out var interactable)) continue;
            float sqrDist = (hit.transform.position - transform.position).sqrMagnitude;
            if (sqrDist < minSqrDist)
            {
                minSqrDist = sqrDist;
                nearest = interactable;
            }
        }
        return nearest;
    }
}
