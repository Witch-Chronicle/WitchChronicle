using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 필드 록온 전용 카메라 제어
/// </summary>
public class FieldLockOnCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineCamera _lockOnCamera;
    [SerializeField] private CinemachineCamera _freeLookCamera;
    [SerializeField] private CinemachineInputAxisController _freeLookInputController;
    [SerializeField] private FieldTargetingController _targetingController;

    [Header("Priority")]
    [SerializeField] private int _activePriority = 20;
    [SerializeField] private int _inactivePriority;

    [Header("Camera Position")]
    [Tooltip("플레이어 뒤 카메라 거리")]
    [SerializeField] private float _backDistance = 4.5f;

    [Tooltip("플레이어 기준 카메라 높이")]
    [SerializeField] private float _cameraHeight = 0.7f;

    [Tooltip("플레이어 기준 시선 높이")]
    [SerializeField] private float _playerFocusHeight = 1.2f;

    [Tooltip("숄더뷰 좌우 위치. 양수는 오른쪽")]
    [SerializeField] private float _shoulderOffset = 0.65f;

    [Header("Camera Aim")]
    [Tooltip("플레이어와 적 사이 시선 비율")]
    [Range(0f, 1f)]
    [SerializeField] private float _targetFocusWeight = 0.65f;

    [Tooltip("최종 시선 높이 보정")]
    [SerializeField] private float _lookHeightOffset = 0.15f;

    [Header("Camera Movement")]
    [SerializeField] private float _positionSmoothTime = 0.08f;
    [SerializeField] private float _rotationSharpness = 14f;

    private FieldCombatTarget _currentTarget;
    private Vector3 _positionVelocity;

    private bool _isLockedOn;
    private bool _isSubscribed;
    private bool _freeLookInputWasEnabled;

    /// <summary>
    /// 카메라 참조 초기화
    /// </summary>
    private void Awake()
    {
        if (_lockOnCamera == null)
        {
            _lockOnCamera =
                GetComponent<CinemachineCamera>();
        }

        SetLockOnCameraActive(false);
    }

    /// <summary>
    /// 동적 플레이어 록온 컨트롤러 검색
    /// </summary>
    private void Update()
    {
        if (_isSubscribed)
        {
            return;
        }

        ResolveTargetingController();
        SubscribeTargetingEvent();
    }

    /// <summary>
    /// 록온 카메라 위치 갱신
    /// </summary>
    private void LateUpdate()
    {
        if (_isLockedOn == false ||
            _currentTarget == null ||
            _currentTarget.IsAvailable == false)
        {
            return;
        }

        UpdateLockOnCameraPose(false);
    }

    /// <summary>
    /// 이벤트 해제 및 카메라 복구
    /// </summary>
    private void OnDisable()
    {
        UnsubscribeTargetingEvent();
        RestoreFreeLookCamera();
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

        _targetingController =
            FindFirstObjectByType<FieldTargetingController>();
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
        if (target == null)
        {
            RestoreFreeLookCamera();
            return;
        }

        ApplyLockOnCamera(target);
    }

    /// <summary>
    /// 록온 카메라 적용
    /// </summary>
    /// <param name="target">록온 대상</param>
    private void ApplyLockOnCamera(
        FieldCombatTarget target)
    {
        if (_lockOnCamera == null ||
            _targetingController == null ||
            target == null)
        {
            return;
        }

        _currentTarget = target;
        _isLockedOn = true;
        _positionVelocity = Vector3.zero;

        Transform followTarget =
            _freeLookCamera != null &&
            _freeLookCamera.Follow != null
                ? _freeLookCamera.Follow
                : _targetingController.transform;

        _lockOnCamera.Follow = followTarget;

        _lockOnCamera.LookAt = target.HitPoint;

        if (_freeLookInputController != null)
        {
            _freeLookInputWasEnabled =
                _freeLookInputController.enabled;

            _freeLookInputController.enabled = false;
        }

        UpdateLockOnCameraPose(true);
        SetLockOnCameraActive(true);
    }

    /// <summary>
    /// 록온 카메라 위치 및 회전 갱신
    /// </summary>
    /// <param name="immediate">즉시 적용 여부</param>
    private void UpdateLockOnCameraPose(
        bool immediate)
    {
        if (_lockOnCamera == null ||
            _targetingController == null ||
            _currentTarget == null)
        {
            return;
        }

        Transform playerTransform =
            _targetingController.transform;

        Vector3 playerPosition =
            playerTransform.position;

        Vector3 targetPosition =
            _currentTarget.GetAimPosition();

        Vector3 playerToTarget =
            targetPosition -
            playerPosition;

        playerToTarget.y = 0f;

        if (playerToTarget.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Vector3 forward =
            playerToTarget.normalized;

        Vector3 right =
            Vector3.Cross(
                Vector3.up,
                forward).normalized;

        Vector3 playerFocusPosition =
            playerPosition +
            Vector3.up *
            _playerFocusHeight;

        Vector3 desiredCameraPosition =
            playerFocusPosition -
            forward *
            _backDistance +
            right *
            _shoulderOffset +
            Vector3.up *
            _cameraHeight;

        Vector3 lookPosition =
            Vector3.Lerp(
                playerFocusPosition,
                targetPosition,
                _targetFocusWeight) +
            Vector3.up *
            _lookHeightOffset;

        Transform cameraTransform =
            _lockOnCamera.transform;

        if (immediate)
        {
            cameraTransform.position =
                desiredCameraPosition;
        }
        else
        {
            cameraTransform.position =
                Vector3.SmoothDamp(
                    cameraTransform.position,
                    desiredCameraPosition,
                    ref _positionVelocity,
                    _positionSmoothTime);
        }

        Vector3 lookDirection =
            lookPosition -
            cameraTransform.position;

        if (lookDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion desiredRotation =
            Quaternion.LookRotation(
                lookDirection.normalized,
                Vector3.up);

        if (immediate)
        {
            cameraTransform.rotation =
                desiredRotation;

            return;
        }

        float rotationRatio =
            1f -
            Mathf.Exp(
                -_rotationSharpness *
                Time.deltaTime);

        cameraTransform.rotation =
            Quaternion.Slerp(
                cameraTransform.rotation,
                desiredRotation,
                rotationRatio);
    }

    /// <summary>
    /// 록온 카메라 활성화 설정
    /// </summary>
    /// <param name="isActive">활성 여부</param>
    private void SetLockOnCameraActive(
        bool isActive)
    {
        if (_lockOnCamera == null)
        {
            return;
        }

        _lockOnCamera.Priority =
            isActive
                ? _activePriority
                : _inactivePriority;
    }

    /// <summary>
    /// 자유 카메라 복구
    /// </summary>
    private void RestoreFreeLookCamera()
    {
        _currentTarget = null;
        _positionVelocity = Vector3.zero;

        if (_isLockedOn == false)
        {
            SetLockOnCameraActive(false);
            return;
        }

        _isLockedOn = false;

        SetLockOnCameraActive(false);

        if (_lockOnCamera != null)
        {
            _lockOnCamera.Follow = null;
            _lockOnCamera.LookAt = null;
        }

        if (_freeLookInputController != null)
        {
            _freeLookInputController.enabled =
                _freeLookInputWasEnabled;
        }
    }
}