using System.Collections;
using UnityEngine;

/// <summary>
/// 전투 유닛의 연출(애니메이션) 재생 창구.
/// 전투 코어는 이 컴포넌트의 public 메서드만 호출하면 된다.
/// 판정(HP 계산 등)은 하지 않고, 전투 이벤트에 반응만 한다.
/// </summary>
public class BattleUnitPresenter : MonoBehaviour
{
    [SerializeField] private Animator _animator;

    [Header("피격 상반신 레이어 (다리 고정용)")]
    [Tooltip("Hit를 재생할 상반신 마스크 레이어 인덱스. 0이면 사용 안 함(Base에서 전신 재생)")]
    [SerializeField] private int _hitLayer = 1;

    [Tooltip("피격 모션 재생 시간. 이 시간 뒤 레이어 가중치를 0으로 되돌린다")]
    [SerializeField] private float _hitReactionDuration = 0.5f;

    [Tooltip("피격 후 레이어 가중치를 0으로 낮추는 시간")]
    [SerializeField] private float _hitLayerFade = 0.12f;

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

    private Coroutine _hitRoutine;

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        // 상반신 레이어는 평소 0으로 두고 피격 때만 켠다(항상 1이면 상반신을 덮어써 T-포즈/공격 깨짐).
        if (HasHitLayer())
            _animator.SetLayerWeight(_hitLayer, 0f);
    }

    private bool HasHitLayer()
    {
        return _animator != null
            && _animator.runtimeAnimatorController != null
            && _hitLayer > 0
            && _hitLayer < _animator.layerCount;
    }

    /// <summary>
    /// 애니메이터 초기화: Idle 상태로 즉시 되돌린다.
    /// 전투 시작 시 호출 (이전 전투의 승리/사망 잔여 상태 제거).
    /// </summary>
    public void ResetToIdle()
    {
        if (_animator == null || _animator.runtimeAnimatorController == null)
        {
            return;
        }

        _animator.Play(IdleState, 0, 0f);
    }

    /// <summary>
    /// 지정한 상태를 즉시 재생한다.
    /// 트리거 큐를 쓰지 않아 현재 상태와 무관하게 바로 반영된다.
    /// </summary>
    private void PlayState(string stateName)
    {
        if (_animator == null || _animator.runtimeAnimatorController == null)
        {
            return;
        }

        _animator.CrossFadeInFixedTime(stateName, CrossFade, 0);
    }

    /// <summary>일반 공격. index를 생략하면 3종 중 랜덤.</summary>
    public void PlayAttack(int index = -1)
    {
        if (index < 0 || index >= AttackStates.Length)
            index = Random.Range(0, AttackStates.Length);

        PlayState(AttackStates[index]);
    }

    /// <summary>공격형 스킬 캐스팅.</summary>
    public void PlaySkill() => PlayState(SkillState);

    /// <summary>지원형(힐/버프) 스킬 캐스팅. 전용 상태가 없으면 일반 Skill로 폴백.</summary>
    public void PlaySkillSupport()
    {
        if (_animator != null && _animator.HasState(0, Animator.StringToHash(SkillSupportState)))
        {
            PlayState(SkillSupportState);
        }
        else
        {
            PlayState(SkillState);
        }
    }

    /// <summary>
    /// 피격 연출. 상반신 마스크 레이어가 있으면 그 레이어에만 재생해 하반신(다리)을 고정한다.
    /// 없으면 Base 레이어에서 전신 재생.
    /// </summary>
    public void PlayHit()
    {
        if (_animator == null || _animator.runtimeAnimatorController == null)
        {
            return;
        }

        if (HasHitLayer())
        {
            if (_hitRoutine != null)
                StopCoroutine(_hitRoutine);

            _hitRoutine = StartCoroutine(HitReactionRoutine());
            return;
        }

        PlayState(HitState);
    }

    private IEnumerator HitReactionRoutine()
    {
        _animator.SetLayerWeight(_hitLayer, 1f);
        _animator.CrossFadeInFixedTime(HitState, CrossFade, _hitLayer);

        yield return new WaitForSeconds(_hitReactionDuration);

        float start = _animator.GetLayerWeight(_hitLayer);
        float t = 0f;

        while (t < _hitLayerFade)
        {
            t += Time.deltaTime;
            _animator.SetLayerWeight(_hitLayer, Mathf.Lerp(start, 0f, t / _hitLayerFade));
            yield return null;
        }

        _animator.SetLayerWeight(_hitLayer, 0f);
        _hitRoutine = null;
    }

    public void PlayParry() => PlayState(ParryState);

    /// <summary>사망 연출. 애니메이션 종료 후 쓰러진 포즈를 유지한다.</summary>
    public void PlayDeath() => PlayState(DeathState);

    public void PlayVictory() => PlayState(VictoryState);
}
