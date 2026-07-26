using UnityEngine;

/// <summary>
/// 캐릭터 프리팹 하위 WorldSpaceCanvas의 UI 세트 전체를 관리.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class BattleCharacterUISet : MonoBehaviour
{
    [SerializeField] private CanvasGroup _rootCanvasGroup;

    [Header("Command UI")]
    [SerializeField] private BattleCommandUIController _commandUI;

    [Header("Billboard (카메라 향해 회전)")]
    [SerializeField] private bool _billboard = true;

    private BattleActor _ownerActor;

    public BattleUnit OwnerUnit =>
        _ownerActor != null ? _ownerActor.BattleUnit : null;

    public BattleCommandUIController CommandUI => _commandUI;

    public bool IsVisible =>
        _rootCanvasGroup != null &&
        _rootCanvasGroup.alpha > 0f &&
        _rootCanvasGroup.interactable &&
        _rootCanvasGroup.blocksRaycasts;

    private void Awake()
    {
        if (_rootCanvasGroup == null)
        {
            _rootCanvasGroup = GetComponent<CanvasGroup>();
        }

        if (_ownerActor == null)
        {
            _ownerActor = GetComponentInParent<BattleActor>();
        }

        if (_commandUI == null)
        {
            _commandUI = GetComponentInChildren<BattleCommandUIController>(
                true
            );
        }

        SetVisible(false);
    }

    private void OnEnable()
    {
        TryRegister();
    }

    private void Start()
    {
        TryRegister();
    }

    private void OnDisable()
    {
        if (BattleCharacterUIManager.Instance != null)
        {
            BattleCharacterUIManager.Instance.Unregister(this);
        }
    }

    private void TryRegister()
    {
        if (BattleCharacterUIManager.Instance != null)
        {
            BattleCharacterUIManager.Instance.Register(this);
        }
    }

    private void LateUpdate()
    {
        if (!_billboard) return;
        if (Camera.main == null) return;

        transform.rotation = Camera.main.transform.rotation;
    }

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    private void SetVisible(bool isVisible)
    {
        if (_rootCanvasGroup == null)
            return;

        _rootCanvasGroup.alpha = isVisible ? 1f : 0f;
        _rootCanvasGroup.interactable = isVisible;
        _rootCanvasGroup.blocksRaycasts = isVisible;
    }
}