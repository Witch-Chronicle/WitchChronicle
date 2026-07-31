using System;
using System.Collections;
using UnityEngine;
/// <summary>
/// 전투 유닛의 연출(애니메이션) 재생 창구.
/// 전투 코어는 이 컴포넌트의 public 메서드만 호출하면 된다.
/// 판정(HP 계산 등)은 하지 않고, 전투 이벤트에 반응만 한다.
/// </summary>
public class BattleUnitPresenter : MonoBehaviour, IBattlePresenter
{
    [SerializeField] private Animator _animator;
    [SerializeField] private float _animationTimeout = 3f;
    [Range(0f, 1f)]
    [SerializeField] private float _attackImpactNormalizedTime = 0.45f;

    private Coroutine _animationRoutine;

    // BattleAnimator의 상태 이름과 일치해야 한다.
    private const string IdleState = "Idle";
    private const string SkillState = "Skill";                 // 공격형 캐스팅
    private const string SkillSupportState = "SkillSupport";   // 지원형(힐/버프) 캐스팅
    private const string HitState = "Hit";
    private const string ParryState = "Parry";
    private const string DeathState = "Death";
    private const string VictoryState = "Victory";
    private static readonly string[] AttackStates = { "Attack_01", "Attack_02", "Attack_03" };

    private const float CrossFade = 0.1f;

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// 애니메이터 초기화: Idle 상태로 즉시 되돌린다.
    /// 전투 시작 시 호출 (이전 전투의 승리/사망 잔여 상태 제거).
    /// </summary>
    public void ResetToIdle()
    {
        StopAnimationRoutine();

        if (_animator == null || _animator.runtimeAnimatorController == null)
        {
            return;
        }

        _animator.Play(IdleState, 0, 0f);
    }

    /// <summary>
    /// 지정 상태 재생
    /// </summary>
    /// <param name="stateName">재생 상태 이름</param>
    /// <param name="onComplete">재생 완료 콜백</param>
    /// <param name="onImpact">타격 시점 콜백</param>
    /// <param name="impactNormalizedTime">타격 진행률</param>
    private void PlayState(
        string stateName,
        Action onComplete = null,
        Action onImpact = null,
        float impactNormalizedTime = 0f)
    {
        if (_animator == null || _animator.runtimeAnimatorController == null)
        {
            onImpact?.Invoke();
            onComplete?.Invoke();
            return;
        }

        int stateHash = Animator.StringToHash(stateName);

        if (_animator.HasState(0, stateHash) == false)
        {
            onImpact?.Invoke();
            onComplete?.Invoke();
            return;
        }

        StopAnimationRoutine();

        _animator.CrossFadeInFixedTime(stateName, CrossFade, 0);

        if (onImpact != null || onComplete != null)
        {
            _animationRoutine = StartCoroutine(
                WaitForStateComplete(
                    stateHash,
                    onImpact,
                    impactNormalizedTime,
                    onComplete));
        }
    }

    /// <summary>일반 공격. index를 생략하면 3종 중 랜덤.</summary>
    public void PlayAttack(
        int index = -1,
        Action onImpact = null,
        Action onComplete = null)
    {
        if (index < 0 || index >= AttackStates.Length)
            index = UnityEngine.Random.Range(0, AttackStates.Length);

        PlayState(
            AttackStates[index],
            onComplete,
            onImpact,
            _attackImpactNormalizedTime);
    }

    /// <summary>공격형 스킬 캐스팅.</summary>
    public void PlaySkill(Action onComplete = null) => PlayState(SkillState, onComplete);

    /// <summary>지원형(힐/버프) 스킬 캐스팅. 전용 상태가 없으면 일반 Skill로 폴백.</summary>
    public void PlaySkillSupport(Action onComplete = null)
    {
        if (_animator != null && _animator.HasState(0, Animator.StringToHash(SkillSupportState)))
        {
            PlayState(SkillSupportState, onComplete);
        }
        else
        {
            PlayState(SkillState, onComplete);
        }
    }

    /// <summary>피격 연출. Base 레이어에서 전신 재생.</summary>
    public void PlayHit(Action onComplete = null) => PlayState(HitState, onComplete);

    public void PlayParry(Action onComplete = null) => PlayState(ParryState, onComplete);

    /// <summary>사망 연출. 애니메이션 종료 후 쓰러진 포즈를 유지한다.</summary>
    public void PlayDeath(Action onComplete = null) => PlayState(DeathState, onComplete);

    /// <summary>
    /// 지정 상태의 타격 및 재생 완료 대기
    /// </summary>
    /// <param name="stateHash">대기 상태 해시</param>
    /// <param name="onImpact">타격 시점 콜백</param>
    /// <param name="impactNormalizedTime">타격 진행률</param>
    /// <param name="onComplete">재생 완료 콜백</param>
    private IEnumerator WaitForStateComplete(
        int stateHash,
        Action onImpact,
        float impactNormalizedTime,
        Action onComplete)
    {
        float elapsedTime = 0f;
        bool enteredState = false;
        bool isImpactInvoked = false;

        while (elapsedTime < _animationTimeout)
        {
            if (_animator == null ||
                _animator.isActiveAndEnabled == false)
            {
                break;
            }

            AnimatorStateInfo stateInfo =
                _animator.GetCurrentAnimatorStateInfo(0);

            bool isTargetState =
                stateInfo.shortNameHash == stateHash ||
                stateInfo.fullPathHash == stateHash;

            if (isTargetState)
            {
                enteredState = true;

                if (isImpactInvoked == false &&
                    stateInfo.normalizedTime >= impactNormalizedTime)
                {
                    isImpactInvoked = true;
                    onImpact?.Invoke();
                }

                if (stateInfo.normalizedTime >= 1f &&
                    _animator.IsInTransition(0) == false)
                {
                    break;
                }
            }
            else if (enteredState &&
                     _animator.IsInTransition(0) == false)
            {
                break;
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

    public void PlayVictory() => PlayState(VictoryState);
}
