using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 필드 공격 제어
/// </summary>
public class FieldAttackController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputActionAsset _inputAsset;
    [SerializeField] private FieldTargetingController _targetingController;
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _attackSocket;
    [SerializeField] private FieldLightningVfx _lightningVfxPrefab;
    [SerializeField] private FieldEncounterCameraController _encounterCameraController;
    [SerializeField] private PlayerController _playerController;

    [Header("Hit Vfx")]
    [SerializeField] private GameObject _hitVfxPrefab;
    [Tooltip("피격 이펙트 위치 보정")]
    [SerializeField] private Vector3 _hitVfxPositionOffset;
    [Tooltip("피격 이펙트 회전 보정")]
    [SerializeField] private Vector3 _hitVfxRotationOffset;
    [Tooltip("피격 이펙트 크기")]
    [SerializeField] private float _hitVfxScale = 1f;
    [Tooltip("피격 이펙트 제거 시간")]
    [SerializeField] private float _hitVfxLifetime = 1.5f;

    [Header("Animation")]
    [SerializeField] private string _attackTrigger = "FieldAttack";
    [Tooltip("애니메이션 이벤트 누락 시 최대 대기 시간")]
    [SerializeField] private float _animationEventTimeout = 2.5f;

    [Header("Timing")]
    [Tooltip("피격 이펙트 발생 후 전투 진입까지 유지 시간")]
    [SerializeField] private float _impactHoldDuration = 0.1f;
    [Tooltip("Encounter Camera 컷 이후 전투 진입 연출 시작까지 유지 시간")]
    [SerializeField] private float _encounterViewHoldDuration = 0.08f;

    [Header("Free Attack")]
    [Tooltip("비록온 공격 최대 거리")]
    [SerializeField] private float _freeAttackRange = 8f;
    [Tooltip("비록온 공격이 충돌할 벽 및 지형 레이어")]
    [SerializeField] private LayerMask _freeAttackObstacleMask;
    [Tooltip("비록온 공격 끝점 이펙트")]
    [SerializeField] private GameObject _freeAttackImpactVfxPrefab;
    [Tooltip("비록온 끝점 이펙트 회전 보정")]
    [SerializeField] private Vector3 _freeAttackImpactRotationOffset;
    [Tooltip("비록온 끝점 이펙트 크기")]
    [SerializeField] private float _freeAttackImpactScale = 1f;
    [Tooltip("비록온 끝점 이펙트 제거 시간")]
    [SerializeField] private float _freeAttackImpactLifetime = 1.5f;
    [Tooltip("비록온 공격 명중 이벤트 후 재입력 대기 시간")]
    [SerializeField] private float _freeAttackRecoveryDuration = 0.15f;

    private InputAction _attackAction;
    private Coroutine _attackRoutine;

    private FieldCombatTarget _currentTarget;

    private bool _isAttacking;
    private bool _isImpactNotified;

    private readonly RaycastHit[] _freeAttackHits = new RaycastHit[16];
    private Vector3 _freeAttackDirection;
    private bool _isLockedAttack;

    private bool _playerControllerWasEnabled;
    private bool _isMovementLocked;

    /// <summary>
    /// 공격 참조 및 입력 초기화
    /// </summary>
    private void Awake()
    {
        ResolveReferences();

        if (_inputAsset != null)
        {
            _attackAction =
                _inputAsset.FindAction(
                    "Player/Attack",
                    throwIfNotFound: true);
        }
    }

    /// <summary>
    /// 공격 입력 등록
    /// </summary>
    private void OnEnable()
    {
        if (_attackAction == null)
        {
            return;
        }

        _attackAction.performed +=
            HandleAttackPerformed;

        _attackAction.Enable();
    }

    /// <summary>
    /// 공격 입력 해제
    /// </summary>
    private void OnDisable()
    {
        if (_attackAction != null)
        {
            _attackAction.performed -=
                HandleAttackPerformed;

            _attackAction.Disable();
        }

        if (_attackRoutine != null)
        {
            StopCoroutine(
                _attackRoutine);
        }

        if (BattleEntryTransitionController.Instance == null ||
            BattleEntryTransitionController.Instance.IsPlaying == false)
        {
            SetAttackMovementLocked(
                false);
        }

        ResetAttackState();
    }

    /// <summary>
    /// 록온 공격 방향 유지
    /// </summary>
    private void LateUpdate()
    {
        if (_isAttacking == false ||
            _isLockedAttack == false ||
            _currentTarget == null ||
            _currentTarget.IsAvailable == false)
        {
            return;
        }

        RotateTowardTarget(
            _currentTarget);
    }

    /// <summary>
    /// 공격 입력 처리
    /// </summary>
    /// <param name="context">입력 컨텍스트</param>
    private void HandleAttackPerformed(
        InputAction.CallbackContext context)
    {
        TryStartAttack();
    }

    /// <summary>
    /// 필드 공격 시작 시도
    /// </summary>
    public void TryStartAttack()
    {
        if (_isAttacking ||
            _targetingController == null)
        {
            return;
        }

        FieldCombatTarget target =
            _targetingController.HasTarget
                ? _targetingController.CurrentTarget
                : null;

        bool hasLockedTarget =
            target != null &&
            target.IsAvailable &&
            target.BattleEncounter != null;

        if (hasLockedTarget &&
            target.BattleEncounter
                .PreparePlayerAdvantageBattle() == false)
        {
            return;
        }

        _currentTarget =
            hasLockedTarget
                ? target
                : null;

        _isLockedAttack =
            hasLockedTarget;

        _isAttacking = true;
        _isImpactNotified = false;

        SetAttackMovementLocked(true);

        if (_isLockedAttack)
        {
            RotateTowardTarget(
                _currentTarget);
        }
        else
        {
            CacheFreeAttackDirection();
        }

        _attackRoutine =
            StartCoroutine(
                AttackRoutine());
    }

    /// <summary>
    /// 필드 공격 진행
    /// </summary>
    private IEnumerator AttackRoutine()
    {
        RotateTowardTarget(
            _currentTarget);

        bool isAnimationStarted =
            PlayAttackAnimation();

        if (isAnimationStarted == false)
        {
            Debug.LogWarning(
                "[FieldAttack] 공격 애니메이션 실행 실패");

            NotifyAttackImpact();
        }
        else
        {
            float elapsedTime = 0f;

            while (_isImpactNotified == false &&
                   elapsedTime < _animationEventTimeout)
            {
                elapsedTime +=
                    Time.unscaledDeltaTime;

                yield return null;
            }
        }

        if (_isImpactNotified == false)
        {
            Debug.LogWarning(
                "[FieldAttack] Impact 이벤트 누락 / " +
                "피격 및 전투 진입 강제 실행");

            NotifyAttackImpact();
        }

        // 비록온 공격 종료
        if (_isLockedAttack == false)
        {
            if (_freeAttackRecoveryDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    _freeAttackRecoveryDuration);
            }

            SetAttackMovementLocked(false);

            ResetAttackState();

            yield break;
        }

        // 1. 기존 록온 화면에서 피격 이펙트 0.1초 출력
        if (_impactHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                _impactHoldDuration);
        }

        // 2. Encounter Camera 위치 설정 및 Priority 상승
        if (_encounterCameraController != null &&
            _currentTarget != null)
        {
            _encounterCameraController
                .PlayPlayerAdvantageView(
                    transform,
                    _currentTarget);
        }

        // 3. Cinemachine이 Encounter Camera Cut을 적용하고
        // 실제 화면에 한 프레임 출력할 시간 확보
        yield return new WaitForEndOfFrame();

        // 4. Encounter 구도 0.08초 유지
        if (_encounterViewHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                _encounterViewHoldDuration);
        }

        // 5. 현재 Encounter 구도를 기준으로 Push In 시작
        BattleEncounter encounter =
            _currentTarget != null
                ? _currentTarget.BattleEncounter
                : null;

        if (encounter != null)
        {
            encounter.StartPreparedBattle();
        }

        ResetAttackState();
    }

    /// <summary>
    /// 공격 대상 방향 회전
    /// </summary>
    /// <param name="target">공격 대상</param>
    private void RotateTowardTarget(
        FieldCombatTarget target)
    {
        if (target == null)
        {
            return;
        }

        Vector3 direction =
            target.GetAimPosition() -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        transform.rotation =
            Quaternion.LookRotation(
                direction.normalized);
    }

    /// <summary>
    /// 공격 애니메이션 재생
    /// </summary>
    /// <returns>애니메이션 실행 성공 여부</returns>
    private bool PlayAttackAnimation()
    {
        if (_animator == null)
        {
            Debug.LogWarning(
                "[FieldAttack] Animator 참조 없음");

            return false;
        }

        if (string.IsNullOrEmpty(
                _attackTrigger))
        {
            Debug.LogWarning(
                "[FieldAttack] 공격 Trigger 이름 없음");

            return false;
        }

        if (HasAnimatorParameter(
                _attackTrigger) == false)
        {
            Debug.LogWarning(
                $"[FieldAttack] Animator 파라미터 없음: " +
                $"{_attackTrigger}");

            return false;
        }

        _animator.ResetTrigger(
            _attackTrigger);

        _animator.SetTrigger(
            _attackTrigger);

        return true;
    }

    /// <summary>
    /// 공격 명중 Animation Event 처리
    /// </summary>
    public void NotifyAttackImpact()
    {
        if (_isAttacking == false ||
            _isImpactNotified)
        {
            return;
        }

        _isImpactNotified = true;

        Debug.Log(
            "[FieldAttack] Impact Event");

        if (_isLockedAttack &&
            _currentTarget != null &&
            _currentTarget.IsAvailable)
        {
            PlayLightningVfx(
                _currentTarget);

            PlayHitVfx(
                _currentTarget);

            return;
        }

        PlayFreeAttackVfx();
    }

    /// <summary>
    /// 번개 이펙트 재생
    /// </summary>
    /// <param name="target">공격 대상</param>
    private void PlayLightningVfx(
        FieldCombatTarget target)
    {
        if (_lightningVfxPrefab == null ||
            _attackSocket == null ||
            target == null)
        {
            return;
        }

        FieldLightningVfx lightningVfx =
            Instantiate(
                _lightningVfxPrefab);

        lightningVfx.Play(
            _attackSocket,
            target.HitPoint);
    }

    /// <summary>
    /// 적 피격 이펙트 재생
    /// </summary>
    /// <param name="target">피격 대상</param>
    private void PlayHitVfx(
        FieldCombatTarget target)
    {
        if (_hitVfxPrefab == null ||
            target == null)
        {
            return;
        }

        Vector3 hitPosition =
            target.GetAimPosition() +
            _hitVfxPositionOffset;

        Vector3 attackDirection =
            hitPosition -
            transform.position;

        Quaternion hitRotation =
            Quaternion.identity;

        if (attackDirection.sqrMagnitude >
            0.001f)
        {
            hitRotation =
                Quaternion.LookRotation(
                    attackDirection.normalized);
        }

        hitRotation *=
            Quaternion.Euler(
                _hitVfxRotationOffset);

        GameObject hitVfx =
            Instantiate(
                _hitVfxPrefab,
                hitPosition,
                hitRotation);

        hitVfx.transform.localScale *=
            Mathf.Max(
                0.01f,
                _hitVfxScale);

        if (_hitVfxLifetime > 0f)
        {
            Destroy(
                hitVfx,
                _hitVfxLifetime);
        }
    }

    /// <summary>
    /// Animator 파라미터 존재 여부 반환
    /// </summary>
    /// <param name="parameterName">파라미터 이름</param>
    /// <returns>파라미터 존재 여부</returns>
    private bool HasAnimatorParameter(
        string parameterName)
    {
        if (_animator == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters =
            _animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name ==
                parameterName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 참조 자동 연결
    /// </summary>
    private void ResolveReferences()
    {
        if (_targetingController == null)
        {
            _targetingController =
                GetComponent<FieldTargetingController>();
        }

        if (_animator == null)
        {
            _animator =
                GetComponentInChildren<Animator>();
        }

        if (_encounterCameraController == null)
        {
            _encounterCameraController =
                FindFirstObjectByType<FieldEncounterCameraController>();
        }

        if (_playerController == null)
        {
            _playerController =
                GetComponent<PlayerController>();
        }
    }

    /// <summary>
    /// 필드 공격 상태 초기화
    /// </summary>
    private void ResetAttackState()
    {
        _attackRoutine = null;
        _currentTarget = null;

        _freeAttackDirection =
            Vector3.zero;

        _isAttacking = false;
        _isImpactNotified = false;
        _isLockedAttack = false;
    }

    /// <summary>
    /// 비록온 공격 방향 저장
    /// </summary>
    private void CacheFreeAttackDirection()
    {
        _freeAttackDirection =
            transform.forward;

        _freeAttackDirection.y = 0f;

        if (_freeAttackDirection.sqrMagnitude <= 0.001f)
        {
            _freeAttackDirection =
                Vector3.forward;
        }

        _freeAttackDirection.Normalize();
    }

    /// <summary>
    /// 비록온 공격 끝점 계산
    /// </summary>
    /// <param name="endPosition">공격 끝점</param>
    /// <param name="hitNormal">충돌 표면 방향</param>
    /// <returns>벽 충돌 여부</returns>
    private bool TryResolveFreeAttackEndPoint(
        out Vector3 endPosition,
        out Vector3 hitNormal)
    {
        Vector3 origin =
            _attackSocket != null
                ? _attackSocket.position
                : transform.position +
                  Vector3.up;

        Vector3 direction =
            _freeAttackDirection;

        if (direction.sqrMagnitude <= 0.001f)
        {
            direction =
                transform.forward;
        }

        direction.Normalize();

        float range =
            Mathf.Max(
                0.1f,
                _freeAttackRange);

        int hitCount =
            Physics.RaycastNonAlloc(
                origin,
                direction,
                _freeAttackHits,
                range,
                _freeAttackObstacleMask,
                QueryTriggerInteraction.Ignore);

        float nearestDistance =
            float.MaxValue;

        RaycastHit nearestHit =
            default;

        bool hasHit =
            false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _freeAttackHits[i];

            if (hit.collider == null)
            {
                continue;
            }

            Transform hitTransform = hit.collider.transform;

            // 플레이어 자신의 Collider 제외
            if (hitTransform == transform ||
                hitTransform.IsChildOf(transform))
            {
                continue;
            }

            // 비록온 공격은 적 피격 판정을 하지 않음
            if (hit.collider.GetComponentInParent<
                    FieldCombatTarget>() != null)
            {
                continue;
            }

            if (hit.distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = hit.distance;

            nearestHit = hit;

            hasHit = true;
        }

        if (hasHit)
        {
            endPosition =
                nearestHit.point +
                nearestHit.normal *
                0.02f;

            hitNormal = nearestHit.normal;

            return true;
        }

        endPosition = origin + direction * range;

        hitNormal = -direction;

        return false;
    }

    /// <summary>
    /// 비록온 공격 이펙트 재생
    /// </summary>
    private void PlayFreeAttackVfx()
    {
        if (_attackSocket == null)
        {
            return;
        }

        TryResolveFreeAttackEndPoint(
            out Vector3 endPosition,
            out Vector3 hitNormal);

        if (_lightningVfxPrefab != null)
        {
            FieldLightningVfx lightningVfx =
                Instantiate(
                    _lightningVfxPrefab);

            lightningVfx.Play(
                _attackSocket,
                endPosition);
        }

        PlayFreeAttackImpactVfx(
            endPosition,
            hitNormal);
    }

    /// <summary>
    /// 비록온 끝점 이펙트 재생
    /// </summary>
    /// <param name="position">생성 위치</param>
    /// <param name="surfaceNormal">표면 방향</param>
    private void PlayFreeAttackImpactVfx(
        Vector3 position,
        Vector3 surfaceNormal)
    {
        if (_freeAttackImpactVfxPrefab == null)
        {
            return;
        }

        Quaternion rotation = Quaternion.identity;

        if (surfaceNormal.sqrMagnitude > 0.001f)
        {
            rotation =
                Quaternion.LookRotation(
                    surfaceNormal.normalized,
                    Vector3.up);
        }

        rotation *=
            Quaternion.Euler(
                _freeAttackImpactRotationOffset);

        GameObject impactVfx =
            Instantiate(
                _freeAttackImpactVfxPrefab,
                position,
                rotation);

        impactVfx.transform.localScale *=
            Mathf.Max(
                0.01f,
                _freeAttackImpactScale);

        if (_freeAttackImpactLifetime > 0f)
        {
            Destroy(
                impactVfx,
                _freeAttackImpactLifetime);
        }
    }

    /// <summary>
    /// 필드 공격 이동 잠금 설정
    /// </summary>
    /// <param name="isLocked">잠금 여부</param>
    private void SetAttackMovementLocked(
        bool isLocked)
    {
        if (_playerController == null ||
            _isMovementLocked == isLocked)
        {
            return;
        }

        if (isLocked)
        {
            _playerControllerWasEnabled =
                _playerController.enabled;

            _playerController.SetInputEnabled(
                false);

            _playerController.enabled =
                false;

            _isMovementLocked = true;

            return;
        }

        _playerController.enabled =
            _playerControllerWasEnabled;

        if (_playerController.enabled)
        {
            _playerController.SetInputEnabled(
                true);
        }

        _isMovementLocked = false;
    }
}