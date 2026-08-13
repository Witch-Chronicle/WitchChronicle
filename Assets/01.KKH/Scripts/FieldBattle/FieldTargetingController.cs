using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// 필드 적 록온 대상 탐색
/// </summary>
public class FieldTargetingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputActionAsset _inputAsset;
    [Tooltip("화면 방향 판정에 사용할 실제 출력 카메라")]
    [SerializeField] private Transform _cameraTransform;

    [Header("Target Detection")]
    [Tooltip("록온 가능한 적 레이어")]
    [SerializeField] private LayerMask _targetMask;

    [Tooltip("록온 시야를 막는 벽 및 지형 레이어")]
    [SerializeField] private LayerMask _obstacleMask;

    [Tooltip("록온 탐색 거리")]
    [SerializeField] private float _lockRange = 10f;

    [Tooltip("카메라 정면 기준 최대 록온 각도")]
    [Range(0f, 180f)]
    [SerializeField] private float _lockAngle = 45f;

    [Tooltip("현재 대상이 해제되는 거리 배율")]
    [SerializeField] private float _releaseRangeMultiplier = 1.2f;

    [Tooltip("한 번에 탐색할 최대 Collider 수")]
    [SerializeField] private int _maxDetectionCount = 32;

    [Header("Target Score")]
    [Tooltip("거리 점수 반영 비율")]
    [SerializeField] private float _distanceScoreWeight = 0.35f;

    private Collider[] _detectionResults;

    private readonly HashSet<FieldCombatTarget> _checkedTargets = new HashSet<FieldCombatTarget>();
    private FieldCombatTarget _currentTarget;
    private InputAction _lockOnAction;

    public FieldCombatTarget CurrentTarget => _currentTarget;
    public bool HasTarget => _currentTarget != null;

    public event Action<FieldCombatTarget> OnTargetChanged;

    /// <summary>
    /// 탐색 배열 및 입력 참조 초기화
    /// </summary>
    private void Awake()
    {
        _detectionResults =
            new Collider[Mathf.Max(1, _maxDetectionCount)];

        ResolveCameraTransform();

        if (_inputAsset != null)
        {
            _lockOnAction =
                _inputAsset.FindAction(
                    "Player/LockOn",
                    throwIfNotFound: true);
        }
    }

    /// <summary>
    /// 록온 입력 등록
    /// </summary>
    private void OnEnable()
    {
        if (_lockOnAction == null)
        {
            return;
        }

        _lockOnAction.performed +=
            HandleLockOnPerformed;

        _lockOnAction.Enable();
    }

    /// <summary>
    /// 록온 입력 해제 및 대상 초기화
    /// </summary>
    private void OnDisable()
    {
        ClearTarget();

        if (_lockOnAction == null)
        {
            return;
        }

        _lockOnAction.performed -=
            HandleLockOnPerformed;

        _lockOnAction.Disable();
    }

    /// <summary>
    /// 현재 록온 대상 유효성 검사
    /// </summary>
    private void Update()
    {
        ValidateCurrentTarget();
    }

    /// <summary>
    /// 공격 가능한 록온 대상 탐색
    /// </summary>
    /// <param name="target">선택된 대상</param>
    /// <returns>대상 탐색 성공 여부</returns>
    public bool TryAcquireTarget(
        out FieldCombatTarget target)
    {
        ResolveCameraTransform();

        if (IsCurrentTargetValid())
        {
            target = _currentTarget;
            return true;
        }

        _currentTarget = null;
        _checkedTargets.Clear();

        int detectedCount =
            Physics.OverlapSphereNonAlloc(
                transform.position,
                _lockRange,
                _detectionResults,
                _targetMask,
                QueryTriggerInteraction.Collide);

        FieldCombatTarget bestTarget = null;
        float bestScore = float.MaxValue;

        Vector3 viewForward =
            GetHorizontalViewForward();

        for (int i = 0; i < detectedCount; i++)
        {
            Collider detectedCollider =
                _detectionResults[i];

            if (detectedCollider == null)
            {
                continue;
            }

            FieldCombatTarget candidate =
                detectedCollider.GetComponentInParent<FieldCombatTarget>();

            if (candidate == null ||
                candidate.IsAvailable == false ||
                _checkedTargets.Add(candidate) == false)
            {
                continue;
            }

            Vector3 targetPosition =
                candidate.GetAimPosition();

            Vector3 toTarget =
                targetPosition -
                transform.position;

            float distance =
                toTarget.magnitude;

            if (distance <= 0.001f ||
                distance > _lockRange)
            {
                continue;
            }

            Vector3 horizontalDirection =
                toTarget;

            horizontalDirection.y = 0f;

            if (horizontalDirection.sqrMagnitude <= 0.001f)
            {
                continue;
            }

            horizontalDirection.Normalize();

            float angle =
                Vector3.Angle(
                    viewForward,
                    horizontalDirection);

            if (angle > _lockAngle)
            {
                continue;
            }

            if (HasLineOfSight(candidate) == false)
            {
                continue;
            }

            float angleScore =
                angle /
                Mathf.Max(
                    0.01f,
                    _lockAngle);

            float distanceScore =
                distance /
                Mathf.Max(
                    0.01f,
                    _lockRange);

            float totalScore =
                angleScore +
                distanceScore *
                _distanceScoreWeight;

            if (totalScore >= bestScore)
            {
                continue;
            }

            bestScore = totalScore;
            bestTarget = candidate;
        }

        _currentTarget = bestTarget;
        target = _currentTarget;

        OnTargetChanged?.Invoke(
            _currentTarget);

        return _currentTarget != null;
    }

    /// <summary>
    /// 록온 대상 직접 설정
    /// </summary>
    /// <param name="target">설정 대상</param>
    public void SetTarget(
        FieldCombatTarget target)
    {
        if (target == null ||
            target.IsAvailable == false)
        {
            ClearTarget();
            return;
        }

        _currentTarget = target;

        OnTargetChanged?.Invoke(
            _currentTarget);
    }

    /// <summary>
    /// 록온 대상 해제
    /// </summary>
    public void ClearTarget()
    {
        if (_currentTarget == null)
        {
            return;
        }

        _currentTarget = null;

        OnTargetChanged?.Invoke(
            null);
    }

    /// <summary>
    /// 현재 록온 대상 유효성 검사
    /// </summary>
    private void ValidateCurrentTarget()
    {
        if (_currentTarget == null)
        {
            return;
        }

        if (IsCurrentTargetValid())
        {
            return;
        }

        ClearTarget();
    }

    /// <summary>
    /// 현재 록온 대상 유효 여부 반환
    /// </summary>
    /// <returns>대상 유효 여부</returns>
    private bool IsCurrentTargetValid()
    {
        if (_currentTarget == null ||
            _currentTarget.IsAvailable == false)
        {
            return false;
        }

        float releaseRange =
            _lockRange *
            Mathf.Max(
                1f,
                _releaseRangeMultiplier);

        Vector3 toTarget =
            _currentTarget.GetAimPosition() -
            transform.position;

        if (toTarget.sqrMagnitude >
            releaseRange * releaseRange)
        {
            return false;
        }

        return HasLineOfSight(
            _currentTarget);
    }

    /// <summary>
    /// 대상 시야 확보 여부 반환
    /// </summary>
    /// <param name="target">검사 대상</param>
    /// <returns>시야 확보 여부</returns>
    private bool HasLineOfSight(
        FieldCombatTarget target)
    {
        if (target == null)
        {
            return false;
        }

        if (_obstacleMask.value == 0)
        {
            return true;
        }

        Vector3 origin =
            _cameraTransform != null
                ? _cameraTransform.position
                : transform.position +
                  Vector3.up * 1.5f;

        Vector3 targetPosition =
            target.GetAimPosition();

        return Physics.Linecast(
            origin,
            targetPosition,
            _obstacleMask,
            QueryTriggerInteraction.Ignore) == false;
    }

    /// <summary>
    /// 카메라 수평 정면 방향 반환
    /// </summary>
    /// <returns>수평 정면 방향</returns>
    private Vector3 GetHorizontalViewForward()
    {
        Vector3 forward =
            _cameraTransform != null
                ? _cameraTransform.forward
                : transform.forward;

        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.001f)
        {
            return transform.forward;
        }

        return forward.normalized;
    }

    /// <summary>
    /// 카메라 참조 보정
    /// </summary>
    private void ResolveCameraTransform()
    {
        if (_cameraTransform != null)
        {
            return;
        }

        Camera mainCamera =
            Camera.main;

        if (mainCamera != null)
        {
            _cameraTransform =
                mainCamera.transform;
        }
    }

    /// <summary>
    /// 록온 대상 탐색 테스트
    /// </summary>
    [ContextMenu("Debug Acquire Target")]
    private void DebugAcquireTarget()
    {
        if (TryAcquireTarget(
                out FieldCombatTarget target))
        {
            Debug.Log(
                $"[FieldTargeting] Target: {target.name}");

            return;
        }

        Debug.Log(
            "[FieldTargeting] Target 없음");
    }

    /// <summary>
    /// 록온 탐색 범위 표시
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color =
            Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            _lockRange);

        Transform viewTransform =
            _cameraTransform != null
                ? _cameraTransform
                : transform;

        Vector3 forward =
            viewTransform.forward;

        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.001f)
        {
            forward =
                transform.forward;
        }

        forward.Normalize();

        Quaternion leftRotation =
            Quaternion.AngleAxis(
                -_lockAngle,
                Vector3.up);

        Quaternion rightRotation =
            Quaternion.AngleAxis(
                _lockAngle,
                Vector3.up);

        Gizmos.color =
            Color.cyan;

        Gizmos.DrawRay(
            transform.position,
            leftRotation *
            forward *
            _lockRange);

        Gizmos.DrawRay(
            transform.position,
            rightRotation *
            forward *
            _lockRange);
    }

    /// <summary>
    /// 록온 입력 처리
    /// </summary>
    /// <param name="context">입력 컨텍스트</param>
    private void HandleLockOnPerformed(
        InputAction.CallbackContext context)
    {
        ToggleLockOn();
    }

    /// <summary>
    /// 록온 설정 및 해제
    /// </summary>
    public void ToggleLockOn()
    {
        if (_currentTarget != null)
        {
            ClearTarget();

            Debug.Log(
                "[FieldTargeting] 록온 해제");

            return;
        }

        if (TryAcquireTarget(
                out FieldCombatTarget target))
        {
            Debug.Log(
                $"[FieldTargeting] 록온: {target.name}");

            return;
        }

        Debug.Log(
            "[FieldTargeting] 록온 가능한 대상 없음");
    }
}