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

    [Header("Animation")]
    [SerializeField] private string _attackTrigger = "Attack";

    [Header("Timing")]
    [Tooltip("공격 입력 후 번개 발생 시간")]
    [SerializeField] private float _effectDelay = 0.15f;

    [Tooltip("번개 발생 후 전투 진입 시간")]
    [SerializeField] private float _battleStartDelay = 0.12f;

    private InputAction _attackAction;
    private Coroutine _attackRoutine;
    private bool _isAttacking;

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

        _attackRoutine = null;
        _isAttacking = false;
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
            _targetingController == null ||
            _targetingController.HasTarget == false)
        {
            return;
        }

        FieldCombatTarget target =
            _targetingController.CurrentTarget;

        if (target == null ||
            target.IsAvailable == false ||
            target.BattleEncounter == null)
        {
            return;
        }

        if (target.BattleEncounter
            .PreparePlayerAdvantageBattle() == false)
        {
            return;
        }

        _attackRoutine =
            StartCoroutine(
                AttackRoutine(target));
    }

    /// <summary>
    /// 필드 공격 진행
    /// </summary>
    /// <param name="target">공격 대상</param>
    private IEnumerator AttackRoutine(
        FieldCombatTarget target)
    {
        _isAttacking = true;

        RotateTowardTarget(target);
        PlayAttackAnimation();

        if (_effectDelay > 0f)
        {
            yield return new WaitForSeconds(
                _effectDelay);
        }

        PlayLightningVfx(target);

        if (_battleStartDelay > 0f)
        {
            yield return new WaitForSeconds(
                _battleStartDelay);
        }

        BattleEncounter encounter =
            target != null
                ? target.BattleEncounter
                : null;

        if (encounter != null)
        {
            encounter.StartPreparedBattle();
        }

        _attackRoutine = null;
        _isAttacking = false;
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
    private void PlayAttackAnimation()
    {
        if (_animator == null ||
            string.IsNullOrEmpty(_attackTrigger) ||
            HasAnimatorParameter(
                _attackTrigger) == false)
        {
            return;
        }

        _animator.SetTrigger(
            _attackTrigger);
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
    }
}