using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터의 애니메이션 상태를 표준화된 파라미터 이름으로 통합 관리하는 컨트롤러 클래스.
/// </summary>
[RequireComponent(typeof(Animator))]
public class MonsterAnimationController : MonoBehaviour
{
    private Animator _animator;

    // 성능 최적화를 위한 Animator Hash 캐싱
    private readonly int _hashBoolIsMoving = Animator.StringToHash("IsMoving");
    private readonly int _hashTriggerAttack1 = Animator.StringToHash("TriggerAttack1");
    private readonly int _hashTriggerAttack2 = Animator.StringToHash("TriggerAttack2");
    private int _hashTriggerAttack3;
    private int _hashTriggerAttack4;
    private int _hashTriggerAttack5;

    [SerializeField] private readonly List<int> _availableAttackHashes = new List<int>();
    
    private readonly int _hashTriggerGetHit = Animator.StringToHash("TriggerGetHit");
    private readonly int _hashBoolIsDizzy = Animator.StringToHash("IsDizzy");
    private readonly int _hashTriggerDie = Animator.StringToHash("TriggerDie");

    // 방어 관련 파라미터 (있는 객체도 있고 없는 객체도 존재)
    [Header("방어형 몬스터, 방어 애니메이션")]
    [SerializeField] private int _hashBoolIsDefending;
    private int _hashTriggerDefenseGetHit;

    // 도발 관련 파라미터 (있는 객체도 있고 없는 객체도 존재)
    [Header("도발, 공포, 등등 상태이상 용 애니메이션")]
    [SerializeField] private int _hashTriggerTaunt;

    private void Awake()
    {
        _animator = GetComponent<Animator>();

        if (_animator == null)
        {
            Debug.LogError($"[MonsterAnimationController] {name} 에 Animator 컴포넌트가 존재하지 않습니다.");
            return;
        }

        // 삼항 연산자를 이용한 공격 파라미터 존재 여부 확인 및 동적 할당
        _hashTriggerAttack3 = HasParameter("TriggerAttack3") ? Animator.StringToHash("TriggerAttack3") : 0;
        _hashTriggerAttack4 = HasParameter("TriggerAttack4") ? Animator.StringToHash("TriggerAttack4") : 0;
        _hashTriggerAttack5 = HasParameter("TriggerAttack5") ? Animator.StringToHash("TriggerAttack5") : 0;

        // 방어 관련 파라미터 동적 할당 (없으면 0 처리)
        _hashBoolIsDefending = HasParameter("IsDefending") ? Animator.StringToHash("IsDefending") : 0;
        _hashTriggerDefenseGetHit = HasParameter("TriggerDefenseGetHit") ? Animator.StringToHash("TriggerDefenseGetHit") : 0;

        // 도발 관련 파라미터 동적 할당 (없으면 0 처리)
        _hashTriggerTaunt = HasParameter("TriggerTaunt") ? Animator.StringToHash("TriggerTaunt") : 0;

        // 기본 공격 1, 2는 공통으로 추가
        _availableAttackHashes.Add(_hashTriggerAttack1);
        _availableAttackHashes.Add(_hashTriggerAttack2);

        // 추가 공격 파라미터가 존재하는 경우에만 리스트에 추가
        if (_hashTriggerAttack3 != 0)
        {
            _availableAttackHashes.Add(_hashTriggerAttack3);
        }
        if (_hashTriggerAttack4 != 0)
        {
            _availableAttackHashes.Add(_hashTriggerAttack4);
        }
        if (_hashTriggerAttack5 != 0)
        {
            _availableAttackHashes.Add(_hashTriggerAttack5);
        }
    }

    /// <summary>
    /// 애니메이터에 특정 이름의 파라미터가 존재하는지 확인한다.
    /// </summary>
    /// <param name="paramName">파라미터 이름</param>
    /// <returns>존재 여부</returns>
    private bool HasParameter(string paramName)
    {
        if (_animator == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter param in _animator.parameters)
        {
            if (param.name == paramName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 이동 여부(IsMoving) 상태를 설정한다.
    /// </summary>
    /// <param name="isMoving">이동 중 여부</param>
    public void SetIsMoving(bool isMoving)
    {
        if (_animator != null)
        {
            _animator.SetBool(_hashBoolIsMoving, isMoving);
        }
    }

    /// <summary>
    /// 몬스터가 보유한 공격 애니메이션 중 하나를 무작위로 선택하여 재생한다.
    /// </summary>
    public void PlayRandomAttack()
    {
        if (_animator == null || _availableAttackHashes.Count == 0)
        {
            return;
        }

        int randomIndex = Random.Range(0, _availableAttackHashes.Count);
        int selectedAttackHash = _availableAttackHashes[randomIndex];

        _animator.SetTrigger(selectedAttackHash);
        Debug.Log($"[MonsterAnimationController] {name}: 무작위 공격 애니메이션 재생 (타입 인덱스: {randomIndex})");
    }

    /// <summary>
    /// 지정한 번호의 공격 애니메이션을 직접 선택하여 재생한다.
    /// </summary>
    /// <param name="attackIndex">공격 번호 (1 ~ 5)</param>
    public void PlayAttack(int attackIndex)
    {
        if (_animator == null)
        {
            return;
        }

        int targetHash = 0;

        if (attackIndex == 1)
        {
            targetHash = _hashTriggerAttack1;
        }
        else if (attackIndex == 2)
        {
            targetHash = _hashTriggerAttack2;
        }
        else if (attackIndex == 3)
        {
            targetHash = _hashTriggerAttack3;
        }
        else if (attackIndex == 4)
        {
            targetHash = _hashTriggerAttack4;
        }
        else if (attackIndex == 5)
        {
            targetHash = _hashTriggerAttack5;
        }

        if (targetHash != 0)
        {
            _animator.SetTrigger(targetHash);
            Debug.Log($"[MonsterAnimationController] {name}: {attackIndex}번 공격 애니메이션 재생");
        }
        else
        {
            Debug.LogWarning($"[MonsterAnimationController] {name}: 존재하지 않거나 유효하지 않은 공격 인덱스입니다 ({attackIndex}).");
        }
    }

    /// <summary>
    /// 피격(GetHit) 애니메이션을 트리거한다.
    /// </summary>
    public void PlayGetHit()
    {
        if (_animator != null)
        {
            _animator.SetTrigger(_hashTriggerGetHit);
            Debug.Log($"[MonsterAnimationController] {name}: 피격 애니메이션 재생");
        }
    }

    /// <summary>
    /// 방어 상태(IsDefending)를 설정한다. (방어 파라미터가 없는 몬스터는 무시됨)
    /// </summary>
    /// <param name="isDefending">방어 중 여부</param>
    public void SetDefending(bool isDefending)
    {
        if (_animator != null && _hashBoolIsDefending != 0)
        {
            _animator.SetBool(_hashBoolIsDefending, isDefending);
            Debug.Log($"[MonsterAnimationController] {name}: 방어 상태 변경 -> {isDefending}");
        }
    }

    /// <summary>
    /// 방어 중 피격(DefenseGetHit) 애니메이션을 트리거한다. (방어 파라미터가 없는 몬스터는 무시됨)
    /// </summary>
    public void PlayDefenseGetHint()
    {
        if (_animator != null && _hashTriggerDefenseGetHit != 0)
        {
            _animator.SetTrigger(_hashTriggerDefenseGetHit);
            Debug.Log($"[MonsterAnimationController] {name}: 방어 중 피격 애니메이션 재생");
        }
    }

    /// <summary>
    /// 도발(Taunt) 애니메이션을 트리거한다. (도발 파라미터가 없는 몬스터는 무시됨)
    /// </summary>
    public void PlayTaunt()
    {
        if (_animator != null && _hashTriggerTaunt != 0)
        {
            _animator.SetTrigger(_hashTriggerTaunt);
            Debug.Log($"[MonsterAnimationController] {name}: 도발 애니메이션 재생");
        }
    }

    /// <summary>
    /// 상태이상(Dizzy) 상태를 설정한다.
    /// </summary>
    /// <param name="isDizzy">기절/혼란 상태 여부</param>
    public void SetDizzy(bool isDizzy)
    {
        if (_animator != null)
        {
            _animator.SetBool(_hashBoolIsDizzy, isDizzy);
            Debug.Log($"[MonsterAnimationController] {name}: 상태이상(Dizzy) 상태 변경 -> {isDizzy}");
        }
    }

    /// <summary>
    /// 사망(Die) 애니메이션을 트리거한다.
    /// </summary>
    public void PlayDie()
    {
        if (_animator != null)
        {
            _animator.SetTrigger(_hashTriggerDie);
            Debug.Log($"[MonsterAnimationController] {name}: 사망 애니메이션 재생");
        }
    }
}