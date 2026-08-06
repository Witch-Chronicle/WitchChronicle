using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// AlertManager의 Queue를 읽어 Alert Popup을 화면에 표시합니다.
///
/// - 새 Alert는 항상 가장 위에 생성
/// - 기존 Popup은 한 칸 아래로 이동
/// - 최대 3개까지 표시
/// - 최대 개수 초과 시 가장 오래된 Popup부터 Fade Out
/// - Popup별 LifeTime 종료 시 독립적으로 Fade Out
/// - Popup 오브젝트 풀링 사용
/// </summary>
public class AlertUIController : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private AlertPopupUI _popupPrefab;
    [SerializeField] private RectTransform _popupRoot;

    [Header("Display")]
    [Min(1)]
    [SerializeField] private int _maxVisibleCount = 3;

    [Tooltip("가장 위에 표시되는 첫 번째 Popup 위치")]
    [SerializeField] private Vector2 _firstPopupPosition = Vector2.zero;

    [Tooltip("Popup 사이의 세로 간격")]
    [Min(0f)]
    [SerializeField] private float _spacing = 12f;

    [Tooltip("Popup 한 개의 높이. 프리팹 높이와 동일하게 설정")]
    [Min(1f)]
    [SerializeField] private float _popupHeight = 120f;

    [Header("Enter Animation")]
    [Min(0f)]
    [SerializeField] private float _enterDuration = 0.25f;

    [Tooltip("새 Popup이 목표 위치보다 얼마나 위에서 등장할지")]
    [SerializeField] private float _enterOffset = 40f;

    [SerializeField] private Ease _enterEase = Ease.OutCubic;

    [Header("Move Animation")]
    [Min(0f)]
    [SerializeField] private float _moveDuration = 0.22f;

    [SerializeField] private Ease _moveEase = Ease.OutCubic;

    [Header("Exit Animation")]
    [Min(0f)]
    [SerializeField] private float _fadeOutDuration = 0.2f;

    [Tooltip("Fade Out 시 아래로 이동할 거리. 0이면 Fade만 적용")]
    [SerializeField] private float _exitMoveDistance = 15f;

    [SerializeField] private Ease _exitEase = Ease.InCubic;

    [Header("Pool")]
    [Min(0)]
    [SerializeField] private int _initialPoolSize = 4;

    private readonly List<AlertPopupUI> _activePopups = new();
    private readonly Queue<AlertPopupUI> _inactivePool = new();

    private AlertManager _boundManager;
    private bool _isInitialized;

    private void Awake()
    {
        InitializePool();
    }

    private void OnEnable()
    {
        TryBindManager();
        ConsumePendingAlerts();
    }

    private void Start()
    {
        /*
         * AlertManager의 Awake 실행 순서가 늦은 경우를 대비해
         * Start에서도 한 번 더 연결을 시도합니다.
         */
        TryBindManager();
        ConsumePendingAlerts();
    }

    private void Update()
    {
        /*
         * AlertManager가 씬 로드 순서상 늦게 생성되는 경우 대응.
         * 한 번 연결된 뒤에는 아무 작업도 하지 않습니다.
         */
        if (_boundManager == null)
        {
            TryBindManager();
        }
    }

    private void InitializePool()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        if (_popupPrefab == null)
        {
            Debug.LogError(
                "[AlertUIController] AlertPopup 프리팹이 연결되지 않았습니다.",
                this
            );

            return;
        }

        if (_popupRoot == null)
        {
            Debug.LogError(
                "[AlertUIController] PopupRoot가 연결되지 않았습니다.",
                this
            );

            return;
        }

        for (int i = 0; i < _initialPoolSize; i++)
        {
            AlertPopupUI popup = CreatePopup();
            ReleasePopup(popup);
        }
    }

    private void TryBindManager()
    {
        if (_boundManager == AlertManager.Instance)
        {
            return;
        }

        UnbindManager();

        if (AlertManager.Instance == null)
        {
            return;
        }

        _boundManager = AlertManager.Instance;
        _boundManager.OnAlertEnqueued += HandleAlertEnqueued;

        ConsumePendingAlerts();
    }

    private void UnbindManager()
    {
        if (_boundManager == null)
        {
            return;
        }

        _boundManager.OnAlertEnqueued -= HandleAlertEnqueued;
        _boundManager = null;
    }

    private void HandleAlertEnqueued()
    {
        ConsumePendingAlerts();
    }

    /// <summary>
    /// AlertManager Queue에 쌓인 요청을 모두 읽어서 표시합니다.
    /// </summary>
    private void ConsumePendingAlerts()
    {
        if (_boundManager == null)
        {
            return;
        }

        while (_boundManager.TryDequeue(out AlertRequest request))
        {
            ShowAlert(request);
        }
    }

    /// <summary>
    /// 새로운 Popup을 가장 위에 표시합니다.
    /// </summary>
    private void ShowAlert(AlertRequest request)
    {
        RemoveOldestIfFull();

        AlertPopupUI popup = GetPopup();

        if (popup == null)
        {
            return;
        }

        /*
         * 새 Popup을 가장 최근 Alert 위치인 Index 0에 추가합니다.
         */
        _activePopups.Insert(0, popup);

        /*
         * 새 Popup을 제외한 기존 Popup들을 아래 슬롯으로 이동시킵니다.
         */
        RepositionActivePopups(
            skipPopup: popup,
            useAnimation: true
        );

        Vector2 targetPosition = GetSlotPosition(0);

        popup.Initialize(
            request,
            targetPosition,
            targetPosition,
            _enterDuration,
            _enterOffset,
            _enterEase,
            HandlePopupLifeTimeExpired
        );
    }

    /// <summary>
    /// 이미 최대 개수가 표시 중이면 가장 오래된 Popup을 제거합니다.
    /// </summary>
    private void RemoveOldestIfFull()
    {
        if (_activePopups.Count < _maxVisibleCount)
        {
            return;
        }

        int oldestIndex = _activePopups.Count - 1;
        AlertPopupUI oldestPopup = _activePopups[oldestIndex];

        _activePopups.RemoveAt(oldestIndex);

        if (oldestPopup == null)
        {
            return;
        }

        oldestPopup.Dismiss(
            _fadeOutDuration,
            _exitMoveDistance,
            _exitEase,
            HandlePopupDismissCompleted
        );
    }

    /// <summary>
    /// Popup의 LifeTime이 끝났을 때 호출됩니다.
    /// </summary>
    private void HandlePopupLifeTimeExpired(
        AlertPopupUI popup)
    {
        if (popup == null)
        {
            return;
        }

        bool removed = _activePopups.Remove(popup);

        popup.Dismiss(
            _fadeOutDuration,
            _exitMoveDistance,
            _exitEase,
            HandlePopupDismissCompleted
        );

        if (removed)
        {
            RepositionActivePopups(
                skipPopup: null,
                useAnimation: true
            );
        }
    }

    /// <summary>
    /// Fade Out이 완전히 끝난 Popup을 풀로 반환합니다.
    /// </summary>
    private void HandlePopupDismissCompleted(
        AlertPopupUI popup)
    {
        ReleasePopup(popup);
    }

    /// <summary>
    /// 활성 Popup들을 현재 Index에 맞는 위치로 재정렬합니다.
    /// </summary>
    private void RepositionActivePopups(
        AlertPopupUI skipPopup,
        bool useAnimation)
    {
        for (int i = 0; i < _activePopups.Count; i++)
        {
            AlertPopupUI popup = _activePopups[i];

            if (popup == null || popup == skipPopup)
            {
                continue;
            }

            Vector2 targetPosition = GetSlotPosition(i);

            if (useAnimation)
            {
                popup.MoveTo(
                    targetPosition,
                    _moveDuration,
                    _moveEase
                );
            }
            else
            {
                popup.RectTransform.anchoredPosition =
                    targetPosition;
            }
        }
    }

    /// <summary>
    /// Index에 해당하는 Alert Popup의 위치를 계산합니다.
    /// </summary>
    private Vector2 GetSlotPosition(int index)
    {
        float positionY =
            _firstPopupPosition.y -
            index * (_popupHeight + _spacing);

        return new Vector2(
            _firstPopupPosition.x,
            positionY
        );
    }

    private AlertPopupUI GetPopup()
    {
        AlertPopupUI popup;

        if (_inactivePool.Count > 0)
        {
            popup = _inactivePool.Dequeue();
        }
        else
        {
            popup = CreatePopup();
        }

        if (popup == null)
        {
            return null;
        }

        popup.transform.SetParent(_popupRoot, false);
        popup.transform.SetAsLastSibling();
        popup.gameObject.SetActive(true);

        return popup;
    }

    private AlertPopupUI CreatePopup()
    {
        if (_popupPrefab == null || _popupRoot == null)
        {
            return null;
        }

        AlertPopupUI popup =
            Instantiate(_popupPrefab, _popupRoot);

        popup.gameObject.SetActive(false);

        return popup;
    }

    private void ReleasePopup(AlertPopupUI popup)
    {
        if (popup == null)
        {
            return;
        }

        popup.ResetPopup();
        popup.transform.SetParent(_popupRoot, false);

        _inactivePool.Enqueue(popup);
    }

    /// <summary>
    /// 현재 표시 중인 모든 Popup과 대기 중인 요청을 즉시 제거합니다.
    /// 씬 전환이나 강제 UI 정리 시 사용할 수 있습니다.
    /// </summary>
    public void ClearAll()
    {
        if (_boundManager != null)
        {
            _boundManager.ClearPendingQueue();
        }

        for (int i = _activePopups.Count - 1; i >= 0; i--)
        {
            AlertPopupUI popup = _activePopups[i];

            if (popup != null)
            {
                ReleasePopup(popup);
            }
        }

        _activePopups.Clear();
    }

    private void OnDisable()
    {
        UnbindManager();
    }

    private void OnDestroy()
    {
        UnbindManager();

        /*
         * 파괴 중에는 풀에 다시 넣지 않고 Tween만 정리되도록
         * Popup GameObject 파괴는 Unity의 계층 파괴에 맡깁니다.
         */
        _activePopups.Clear();
        _inactivePool.Clear();
    }
}