using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 적(몬스터) 전투 연출. 몬스터 애니메이터 컨트롤러의 공통 트리거로 애니메이션을 구동한다.
/// (TriggerAttack1/2, TriggerGetHit, TriggerDie — 몬스터 팩 공통)
/// 모델은 런타임에 _visualRoot 하위로 생성되므로 Animator를 지연 조회한다.
/// 판정은 하지 않고 전투 이벤트에 반응만 한다.
/// </summary>
public class EnemyBattlePresenter : MonoBehaviour, IBattlePresenter
{
    [Header("몬스터 컨트롤러 공통 파라미터 이름")]
    [SerializeField] private string _attackTrigger1 = "TriggerAttack1";
    [SerializeField] private string _attackTrigger2 = "TriggerAttack2";
    [SerializeField] private string _getHitTrigger = "TriggerGetHit";
    [SerializeField] private string _dieTrigger = "TriggerDie";

    [Header("사망")]
    [Tooltip("죽는 애니메이션 재생 후 사라지기까지 시간(초)")]
    [SerializeField] private float _deathHideDelay = 1.5f;

    [Tooltip("사라질 때 비활성화할 대상. 비우면 이 오브젝트(액터)")]
    [SerializeField] private GameObject _hideTarget;

    [Range(0f, 1f)]
    [SerializeField] private float _attackImpactNormalizedTime = 0.45f;

    private Animator _animator;
    private MonsterAnimationController _animationController;

    private Coroutine _animationRoutine;

    [SerializeField] private float _animationTimeout = 3f;

    /// <summary>자식 모델의 Animator를 지연 조회(런타임 생성 대응).</summary>
    private Animator ResolvedAnimator
    {
        get
        {
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            return _animator;
        }
    }

    /// <summary>자식 모델의 MonsterAnimationController를 지연 조회.</summary>
    private MonsterAnimationController ResolvedAnimationController
    {
        get
        {
            if (_animationController == null)
            {
                _animationController = GetComponentInChildren<MonsterAnimationController>();
            }

            return _animationController;
        }
    }

    /// <summary>몬스터는 Idle이 기본 상태라 완료 대기만 초기화.</summary>
    public void ResetToIdle()
    {
        StopAnimationRoutine();
        ResolvedAnimationController?.ResetToIdle();
    }

    public void PlayAttack(
        int index = -1,
        Action onImpact = null,
        Action onComplete = null)
    {
        bool second =
            index == 1 ||
            (index < 0 && UnityEngine.Random.value < 0.5f);

        int attackIndex = second ? 2 : 1;

        if (ResolvedAnimationController != null)
        {
            Animator animator = ResolvedAnimator;
            int previousStateHash = animator != null ? animator.GetCurrentAnimatorStateInfo(0).fullPathHash : 0;

            StopAnimationRoutine();
            ResolvedAnimationController.PlayAttack(attackIndex);

            if (onImpact != null || onComplete != null)
            {
                _animationRoutine = StartCoroutine(
                    WaitForTriggeredStateComplete(
                        animator,
                        previousStateHash,
                        onImpact,
                        onComplete));
            }
        }
        else
        {
            PlayTriggeredAnimation(
                second ? _attackTrigger2 : _attackTrigger1,
                onImpact,
                onComplete);
        }
    }

    /// <summary>몬스터엔 전용 스킬 모션이 없어 두 번째 공격 모션으로 대체.</summary>
    public void PlaySkill(Action onComplete = null) => PlayAttack(1, null, onComplete);

    /// <summary>지원 스킬도 시전 모션(두 번째 공격)으로 대체.</summary>
    public void PlaySkillSupport(Action onComplete = null) => PlayAttack(1, null, onComplete);

    /// <summary>몬스터엔 패리 모션이 없어 즉시 완료 처리.</summary>
    public void PlayParry(Action onComplete = null)
    {
        onComplete?.Invoke();
    }

    public void PlayHit(Action onComplete = null)
    {
        if (ResolvedAnimationController != null)
        {
            Animator animator = ResolvedAnimator;
            int previousStateHash = animator != null ? animator.GetCurrentAnimatorStateInfo(0).fullPathHash : 0;

            StopAnimationRoutine();
            ResolvedAnimationController.PlayGetHit();

            if (onComplete != null)
            {
                _animationRoutine = StartCoroutine(
                    WaitForTriggeredStateComplete(
                        animator,
                        previousStateHash,
                        null,
                        onComplete));
            }
        }
        else
        {
            PlayTriggeredAnimation(
                _getHitTrigger,
                null,
                onComplete);
        }
    }

    public void PlayDeath(Action onComplete = null)
    {
        StopAnimationRoutine();

        if (ResolvedAnimationController != null)
        {
            ResolvedAnimationController.PlayDie();
        }
        else
        {
            SetTriggerSafe(_dieTrigger);
        }

        StartCoroutine(HideAfterDeath(onComplete));
    }

    /// <summary>
    /// 사망 연출 후 비활성화
    /// </summary>
    /// <param name="onComplete">사망 연출 완료 콜백</param>
    private IEnumerator HideAfterDeath(Action onComplete)
    {
        if (_deathHideDelay > 0f)
        {
            yield return new WaitForSeconds(_deathHideDelay);
        }

        onComplete?.Invoke();

        GameObject target = _hideTarget != null ? _hideTarget : gameObject;
        target.SetActive(false);
    }

    /// <summary>
    /// 트리거 애니메이션 재생
    /// </summary>
    /// <param name="trigger">애니메이터 트리거</param>
    /// <param name="onImpact">타격 시점 콜백</param>
    /// <param name="onComplete">재생 완료 콜백</param>
    private void PlayTriggeredAnimation(
        string trigger,
        Action onImpact,
        Action onComplete)
    {
        Animator animator = ResolvedAnimator;

        if (animator == null ||
            animator.runtimeAnimatorController == null ||
            string.IsNullOrEmpty(trigger))
        {
            onImpact?.Invoke();
            onComplete?.Invoke();
            return;
        }

        int previousStateHash =
            animator.GetCurrentAnimatorStateInfo(0).fullPathHash;

        StopAnimationRoutine();

        animator.SetTrigger(trigger);

        if (onImpact != null || onComplete != null)
        {
            _animationRoutine = StartCoroutine(
                WaitForTriggeredStateComplete(
                    animator,
                    previousStateHash,
                    onImpact,
                    onComplete));
        }
    }

    /// <summary>
    /// 트리거로 전환된 상태의 타격 및 재생 완료 대기
    /// </summary>
    /// <param name="animator">대상 애니메이터</param>
    /// <param name="previousStateHash">재생 전 상태 해시</param>
    /// <param name="onImpact">타격 시점 콜백</param>
    /// <param name="onComplete">재생 완료 콜백</param>
    private IEnumerator WaitForTriggeredStateComplete(
        Animator animator,
        int previousStateHash,
        Action onImpact,
        Action onComplete)
    {
        float elapsedTime = 0f;
        bool enteredState = false;
        bool isImpactInvoked = false;
        int actionStateHash = 0;

        while (elapsedTime < _animationTimeout)
        {
            if (animator == null ||
                animator.isActiveAndEnabled == false)
            {
                break;
            }

            AnimatorStateInfo currentState =
                animator.GetCurrentAnimatorStateInfo(0);

            if (enteredState == false)
            {
                if (currentState.fullPathHash != previousStateHash)
                {
                    enteredState = true;
                    actionStateHash = currentState.fullPathHash;
                }
                else if (animator.IsInTransition(0))
                {
                    AnimatorStateInfo nextState =
                        animator.GetNextAnimatorStateInfo(0);

                    if (nextState.fullPathHash != 0 &&
                        nextState.fullPathHash != previousStateHash)
                    {
                        enteredState = true;
                        actionStateHash = nextState.fullPathHash;
                    }
                }
            }
            else
            {
                if (currentState.fullPathHash == actionStateHash)
                {
                    if (isImpactInvoked == false &&
                        currentState.normalizedTime >= _attackImpactNormalizedTime)
                    {
                        isImpactInvoked = true;
                        onImpact?.Invoke();
                    }

                    if (currentState.normalizedTime >= 1f &&
                        animator.IsInTransition(0) == false)
                    {
                        break;
                    }
                }
                else if (animator.IsInTransition(0) == false)
                {
                    break;
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (isImpactInvoked == false)
        {
            onImpact?.Invoke();
        }

        _animationRoutine = null;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 애니메이션 완료 대기 중단
    /// </summary>
    private void StopAnimationRoutine()
    {
        if (_animationRoutine == null)
        {
            return;
        }

        StopCoroutine(_animationRoutine);
        _animationRoutine = null;
    }

    private void SetTriggerSafe(string trigger)
    {
        Animator animator = ResolvedAnimator;

        if (animator == null
            || animator.runtimeAnimatorController == null
            || string.IsNullOrEmpty(trigger))
        {
            return;
        }

        animator.SetTrigger(trigger);
    }
}
