using TMPro;
using UnityEngine;

/// <summary>
/// NPC 하위 World Space Canvas에 부착하는 상호작용 안내 UI입니다.
/// 부모 NPC의 NPCData에서 이름과 Prompt를 가져오고,
/// 플레이어가 표시 범위 안에 들어오면 CanvasGroup을 부드럽게 표시합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class NPCWorldInteractionUI : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("비워두면 부모 오브젝트에서 NPC를 자동으로 찾습니다.")]
    [SerializeField] private NPC _npc;

    [Header("UI References")]
    [Tooltip("NPCData.NpcName을 표시합니다.")]
    [SerializeField] private TMP_Text _roleText;
    [Tooltip("NPCData.Prompt를 표시합니다.")]
    [SerializeField] private TMP_Text _actionText;
    [Tooltip("비워두면 이 오브젝트의 CanvasGroup을 자동으로 사용합니다.")]
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Player")]
    [Tooltip("직접 연결하는 것이 가장 안전합니다. 비워두면 Player Tag로 자동 탐색합니다.")]
    [SerializeField] private Transform _player;
    [SerializeField] private string _playerTag = "Player";
    [Tooltip("플레이어가 나중에 생성되는 경우 재탐색하는 간격입니다.")]
    [SerializeField, Min(0.1f)] private float _playerSearchInterval = 0.5f;

    [Header("Visibility")]
    [Tooltip("이 거리 안에 플레이어가 들어오면 UI를 표시합니다.")]
    [SerializeField, Min(0f)] private float _visibleRange = 3f;
    [Tooltip("경계에서 UI가 깜빡이지 않도록 숨김 거리에 더하는 여유 거리입니다.")]
    [SerializeField, Min(0f)] private float _hideRangePadding = 0.25f;
    [Tooltip("0이면 즉시 표시하며, 값이 있으면 해당 시간 동안 Fade됩니다.")]
    [SerializeField, Min(0f)] private float _fadeDuration = 0.18f;
    [Tooltip("체크하면 Y 높이 차이를 무시하고 평면 거리만 계산합니다.")]
    [SerializeField] private bool _useHorizontalDistance = true;

    [Header("Billboard")]
    [Tooltip("MainCamera를 바라보도록 World Canvas를 회전시킵니다.")]
    [SerializeField] private bool _useBillboard = true;
    [Tooltip("직접 연결하지 않으면 Camera.main을 사용합니다.")]
    [SerializeField] private Camera _mainCamera;
    [Tooltip("체크하면 상하로 기울지 않고 Y축으로만 카메라를 바라봅니다.")]
    [SerializeField] private bool _lockYAxis;

    private bool _isInRange;
    private bool _suppressed;
    private float _nextPlayerSearchTime;

    public bool IsInRange => _isInRange;
    public float VisibleRange => _visibleRange;

    private void Reset()
    {
        _npc = GetComponentInParent<NPC>();
        _canvasGroup = GetComponent<CanvasGroup>();
        AutoAssignTexts();
    }

    private void Awake()
    {
        ResolveReferences();
        RefreshData();
        SetCanvasAlphaImmediate(0f);
    }

    private void OnEnable()
    {
        _isInRange = false;
        SetCanvasAlphaImmediate(0f);
        TryFindPlayer();

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (_player == null && Time.unscaledTime >= _nextPlayerSearchTime)
        {
            TryFindPlayer();
        }

        UpdateRangeState();
        UpdateCanvasAlpha();
    }

    private void LateUpdate()
    {
        UpdateBillboard();
    }

    /// <summary>NPCData 값이 런타임에 바뀌었다면 호출하여 텍스트를 다시 채웁니다.</summary>
    public void RefreshData()
    {
        if (_npc == null)
        {
            _npc = GetComponentInParent<NPC>();
        }

        NPCData data = _npc != null ? _npc.Data : null;

        if (_roleText != null)
        {
            _roleText.text = data != null ? data.NpcName : string.Empty;
        }

        if (_actionText != null)
        {
            _actionText.text = data != null ? data.Prompt : string.Empty;
        }
    }

    /// <summary>플레이어 Transform을 외부에서 명시적으로 지정할 때 사용합니다.</summary>
    public void SetPlayer(Transform player)
    {
        _player = player;
        UpdateRangeState(forceEvaluate: true);
    }

    /// <summary>
    /// 대화·상점 UI가 열렸을 때 World Canvas를 임시로 숨길 수 있습니다.
    /// 닫힌 후 false를 전달하면 거리 조건에 따라 다시 표시됩니다.
    /// </summary>
    public void SetSuppressed(bool suppressed)
    {
        _suppressed = suppressed;
    }

    /// <summary>거리와 관계없이 즉시 숨깁니다.</summary>
    public void HideImmediate()
    {
        _suppressed = true;
        SetCanvasAlphaImmediate(0f);
    }

    private void ResolveReferences()
    {
        if (_npc == null)
        {
            _npc = GetComponentInParent<NPC>();
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        AutoAssignTexts();

        if (_npc == null)
        {
            Debug.LogWarning($"[{name}] 부모에서 NPC 컴포넌트를 찾지 못했습니다.", this);
        }

        if (_canvasGroup == null)
        {
            Debug.LogError($"[{name}] CanvasGroup이 필요합니다.", this);
        }
    }

    private void AutoAssignTexts()
    {
        // Hierarchy 이름이 일치할 때만 편의를 위해 자동 연결합니다.
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];

            if (_roleText == null && text.gameObject.name == "RoleText")
            {
                _roleText = text;
            }
            else if (_actionText == null && text.gameObject.name == "ActionText")
            {
                _actionText = text;
            }
        }
    }

    private void TryFindPlayer()
    {
        _nextPlayerSearchTime = Time.unscaledTime + _playerSearchInterval;

        if (_player != null || string.IsNullOrWhiteSpace(_playerTag))
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(_playerTag);

        if (playerObject != null)
        {
            _player = playerObject.transform;
        }
    }

    private void UpdateRangeState(bool forceEvaluate = false)
    {
        if (_player == null || _npc == null)
        {
            _isInRange = false;
            return;
        }

        Vector3 delta = _player.position - _npc.transform.position;

        if (_useHorizontalDistance)
        {
            delta.y = 0f;
        }

        float sqrDistance = delta.sqrMagnitude;
        float showRange = _visibleRange;
        float hideRange = _visibleRange + _hideRangePadding;

        if (forceEvaluate)
        {
            _isInRange = sqrDistance <= showRange * showRange;
            return;
        }

        if (_isInRange)
        {
            if (sqrDistance > hideRange * hideRange)
            {
                _isInRange = false;
            }
        }
        else if (sqrDistance <= showRange * showRange)
        {
            _isInRange = true;
        }
    }

    private void UpdateCanvasAlpha()
    {
        if (_canvasGroup == null)
        {
            return;
        }

        float targetAlpha = _isInRange && _suppressed == false ? 1f : 0f;

        if (_fadeDuration <= 0f)
        {
            _canvasGroup.alpha = targetAlpha;
        }
        else
        {
            float speed = 1f / _fadeDuration;
            _canvasGroup.alpha = Mathf.MoveTowards(
                _canvasGroup.alpha,
                targetAlpha,
                speed * Time.unscaledDeltaTime);
        }

        // 이 UI는 마우스로 클릭하지 않으므로 항상 입력을 막지 않습니다.
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    private void UpdateBillboard()
    {
        if (_useBillboard == false)
        {
            return;
        }

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;

            if (_mainCamera == null)
            {
                return;
            }
        }

        if (_lockYAxis)
        {
            // 카메라에서 Canvas로 향하는 평면 방향을 사용하면
            // 기본 World Canvas의 앞면이 카메라를 향하는 방향으로 정렬됩니다.
            Vector3 forward = transform.position - _mainCamera.transform.position;
            forward.y = 0f;

            if (forward.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            }
        }
        else
        {
            // 카메라 화면과 평행하게 맞춰 상하 시점 변화에서도 정면을 유지합니다.
            transform.rotation = _mainCamera.transform.rotation;
        }
    }

    private void SetCanvasAlphaImmediate(float alpha)
    {
        if (_canvasGroup == null)
        {
            return;
        }

        _canvasGroup.alpha = alpha;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _visibleRange = Mathf.Max(0f, _visibleRange);
        _hideRangePadding = Mathf.Max(0f, _hideRangePadding);
        _fadeDuration = Mathf.Max(0f, _fadeDuration);
        _playerSearchInterval = Mathf.Max(0.1f, _playerSearchInterval);

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (_npc == null)
        {
            _npc = GetComponentInParent<NPC>();
        }

        AutoAssignTexts();

        if (Application.isPlaying)
        {
            RefreshData();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = _npc != null ? _npc.transform : transform;
        Gizmos.color = new Color(0.88f, 0.81f, 0.60f, 0.8f);
        Gizmos.DrawWireSphere(origin.position, _visibleRange);

        if (_hideRangePadding > 0f)
        {
            Gizmos.color = new Color(0.55f, 0.55f, 0.60f, 0.45f);
            Gizmos.DrawWireSphere(origin.position, _visibleRange + _hideRangePadding);
        }
    }
#endif
}