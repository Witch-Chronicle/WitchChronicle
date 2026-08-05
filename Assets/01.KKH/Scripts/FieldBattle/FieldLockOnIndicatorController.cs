using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 필드 록온 표시 UI 제어
/// </summary>
public class FieldLockOnIndicatorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RectTransform _indicator;
    [SerializeField] private Image _indicatorImage;
    [SerializeField] private FieldTargetingController _targetingController;

    [Header("Position")]
    [Tooltip("록온 표시 위치 화면 보정")]
    [SerializeField] private Vector2 _screenOffset;

    [Tooltip("화면 가장자리 여백")]
    [SerializeField] private float _screenPadding = 36f;

    [Header("Animation")]
    [Tooltip("록온 표시 회전 속도")]
    [SerializeField] private float _rotationSpeed = 45f;

    [Tooltip("록온 표시 최소 크기")]
    [SerializeField] private float _minimumScale = 0.9f;

    [Tooltip("록온 표시 최대 크기")]
    [SerializeField] private float _maximumScale = 1.05f;

    [Tooltip("록온 표시 크기 변화 속도")]
    [SerializeField] private float _pulseSpeed = 3f;

    private Camera _mainCamera;
    private FieldCombatTarget _currentTarget;

    private bool _isSubscribed;
    private bool _isVisible;

    /// <summary>
    /// UI 참조 초기화
    /// </summary>
    private void Awake()
    {
        if (_canvas == null)
        {
            _canvas = GetComponentInParent<Canvas>();
        }

        if (_indicatorImage == null &&
            _indicator != null)
        {
            _indicatorImage =
                _indicator.GetComponent<Image>();
        }

        HideIndicator();
    }

    /// <summary>
    /// 동적 록온 컨트롤러 연결 상태 갱신
    /// </summary>
    private void Update()
    {
        if (_targetingController == null)
        {
            _isSubscribed = false;
            ResolveTargetingController();
        }

        if (_isSubscribed == false)
        {
            SubscribeTargetingEvent();
        }
    }

    /// <summary>
    /// 록온 표시 위치 및 애니메이션 갱신
    /// </summary>
    private void LateUpdate()
    {
        if (_isVisible == false ||
            _currentTarget == null ||
            _currentTarget.IsAvailable == false)
        {
            return;
        }

        UpdateIndicatorPosition();
        UpdateIndicatorAnimation();
    }

    /// <summary>
    /// 이벤트 해제
    /// </summary>
    private void OnDisable()
    {
        UnsubscribeTargetingEvent();
        HideIndicator();
    }

    /// <summary>
    /// 록온 컨트롤러 검색
    /// </summary>
    private void ResolveTargetingController()
    {
        if (_targetingController != null)
        {
            return;
        }

        _targetingController = FindFirstObjectByType<FieldTargetingController>();

        if (_targetingController != null)
        {
            Debug.Log(
                $"[FieldLockOnUI] Targeting Controller 연결: " +
                $"{_targetingController.name}");
        }
    }

    /// <summary>
    /// 록온 변경 이벤트 등록
    /// </summary>
    private void SubscribeTargetingEvent()
    {
        if (_targetingController == null ||
            _isSubscribed)
        {
            return;
        }

        _targetingController.OnTargetChanged +=
            HandleTargetChanged;

        _isSubscribed = true;

        if (_targetingController.CurrentTarget != null)
        {
            HandleTargetChanged(
                _targetingController.CurrentTarget);
        }
    }

    /// <summary>
    /// 록온 변경 이벤트 해제
    /// </summary>
    private void UnsubscribeTargetingEvent()
    {
        if (_targetingController == null ||
            _isSubscribed == false)
        {
            return;
        }

        _targetingController.OnTargetChanged -=
            HandleTargetChanged;

        _isSubscribed = false;
    }

    /// <summary>
    /// 록온 대상 변경 처리
    /// </summary>
    /// <param name="target">변경 대상</param>
    private void HandleTargetChanged(
        FieldCombatTarget target)
    {
        Debug.Log(
            $"[FieldLockOnUI] Target 변경: " +
            $"{(target != null ? target.name : "None")}");

        _currentTarget = target;

        if (_currentTarget == null)
        {
            HideIndicator();
            return;
        }

        ShowIndicator();
        UpdateIndicatorPosition();
    }

    /// <summary>
    /// 록온 표시 활성화
    /// </summary>
    private void ShowIndicator()
    {
        if (_indicator == null)
        {
            return;
        }

        _isVisible = true;
        _indicator.gameObject.SetActive(true);
        _indicator.localScale = Vector3.one;
        _indicator.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// 록온 표시 비활성화
    /// </summary>
    private void HideIndicator()
    {
        _isVisible = false;
        _currentTarget = null;

        if (_indicator == null)
        {
            return;
        }

        _indicator.gameObject.SetActive(false);
    }

    /// <summary>
    /// 록온 표시 화면 위치 갱신
    /// </summary>
    private void UpdateIndicatorPosition()
    {
        if (_indicator == null ||
            _currentTarget == null)
        {
            return;
        }

        ResolveMainCamera();

        if (_mainCamera == null)
        {
            return;
        }

        Vector3 worldPosition =
            _currentTarget.GetAimPosition();

        Vector3 screenPosition =
            _mainCamera.WorldToScreenPoint(
                worldPosition);

        if (screenPosition.z <= 0f)
        {
            SetIndicatorVisible(false);
            return;
        }

        screenPosition.x =
            Mathf.Clamp(
                screenPosition.x,
                _screenPadding,
                Screen.width -
                _screenPadding);

        screenPosition.y =
            Mathf.Clamp(
                screenPosition.y,
                _screenPadding,
                Screen.height -
                _screenPadding);

        screenPosition +=
            (Vector3)_screenOffset;

        SetIndicatorVisible(true);

        if (_canvas != null &&
            _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            Camera canvasCamera =
                _canvas.worldCamera != null
                    ? _canvas.worldCamera
                    : _mainCamera;

            RectTransform canvasRect =
                _canvas.transform as RectTransform;

            if (canvasRect == null)
            {
                return;
            }

            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPosition,
                    canvasCamera,
                    out Vector2 localPosition);

            _indicator.anchoredPosition =
                localPosition;

            return;
        }

        _indicator.position =
            screenPosition;
    }

    /// <summary>
    /// 록온 표시 애니메이션 갱신
    /// </summary>
    private void UpdateIndicatorAnimation()
    {
        if (_indicator == null)
        {
            return;
        }

        _indicator.Rotate(
            0f,
            0f,
            -_rotationSpeed *
            Time.deltaTime);

        float pulseRatio =
            Mathf.Sin(
                Time.time *
                _pulseSpeed) *
            0.5f +
            0.5f;

        float scale =
            Mathf.Lerp(
                _minimumScale,
                _maximumScale,
                pulseRatio);

        _indicator.localScale =
            Vector3.one *
            scale;
    }

    /// <summary>
    /// 록온 표시 이미지 활성화 설정
    /// </summary>
    /// <param name="isVisible">표시 여부</param>
    private void SetIndicatorVisible(
        bool isVisible)
    {
        if (_indicatorImage != null)
        {
            _indicatorImage.enabled =
                isVisible;

            return;
        }

        if (_indicator != null)
        {
            _indicator.gameObject.SetActive(
                isVisible);
        }
    }

    /// <summary>
    /// 메인 카메라 참조 보정
    /// </summary>
    private void ResolveMainCamera()
    {
        if (_mainCamera != null &&
            _mainCamera.isActiveAndEnabled)
        {
            return;
        }

        _mainCamera =
            Camera.main;
    }
}