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

    private Animator _animator;

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

    /// <summary>몬스터는 Idle이 기본 상태라 별도 처리 없음.</summary>
    public void ResetToIdle()
    {
    }

    public void PlayAttack(int index = -1)
    {
        bool second = index == 1 || (index < 0 && Random.value < 0.5f);
        SetTriggerSafe(second ? _attackTrigger2 : _attackTrigger1);
    }

    /// <summary>몬스터엔 전용 스킬 모션이 없어 두 번째 공격 모션으로 대체.</summary>
    public void PlaySkill() => PlayAttack(1);

    /// <summary>지원 스킬도 시전 모션(두 번째 공격)으로 대체.</summary>
    public void PlaySkillSupport() => PlayAttack(1);

    /// <summary>몬스터엔 패리 모션이 없어 무시.</summary>
    public void PlayParry()
    {
    }

    public void PlayHit() => SetTriggerSafe(_getHitTrigger);

    public void PlayDeath()
    {
        SetTriggerSafe(_dieTrigger);
        StartCoroutine(HideAfterDeath());
    }

    private IEnumerator HideAfterDeath()
    {
        if (_deathHideDelay > 0f)
        {
            yield return new WaitForSeconds(_deathHideDelay);
        }

        GameObject target = _hideTarget != null ? _hideTarget : gameObject;
        target.SetActive(false);
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
