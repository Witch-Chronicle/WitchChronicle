using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터의 애니메이션 상태를 표준화된 파라미터 이름으로 통합 관리하는 컨트롤러 클래스.
/// </summary>
public class MonsterAnimationController : MonoBehaviour
{
    private Animator _animator;

    // Animator가 늦게 붙더라도 안전하게 파라미터 해시를 받아오도록 지연 로딩
    private Animator ResolvedAnimator
    {
        get
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
                if (_animator == null)
                {
                    _animator = GetComponentInChildren<Animator>();
                }

                // Animator를 새로 찾아낸 시점에 해시값 재계산
                if (_animator != null)
                {
                    InitializeHashes();
                }
            }

            return _animator;
        }
    }

    // C# 연동 이벤트
    public event Action OnGetHit;
    public event Action OnDie;

    // 성능 최적화를 위한 Animator Hash 캐싱
    private int _hashBoolIsMoving;
    private int _hashTriggerAttack1;
    private int _hashTriggerAttack2;
    private int _hashTriggerAttack3;
    private int _hashTriggerAttack4;
    private int _hashTriggerAttack5;

    [SerializeField] private readonly List<int> _availableAttackHashes = new List<int>();
    
    private int _hashTriggerGetHit;
    private int _hashBoolIsDizzy;
    private int _hashTriggerDie;

    // 방어 관련 파라미터
    [Header("방어형 몬스터, 방어 애니메이션")]
    [SerializeField] private int _hashBoolIsDefending;
    private int _hashTriggerDefenseGetHit;

    // 도발 관련 파라미터
    [Header("도발, 공포, 등등 상태이상 용 애니메이션")]
    [SerializeField] private int _hashTriggerTaunt;

    public bool IsDefending { get; private set; }
    public bool IsDizzy { get; private set; }

    private void Awake()
    {
        InitializeAnimator();
    }

    public void InitializeAnimator()
    {
        // ResolvedAnimator 속성을 호출하면 자동으로 Animator 탐색 및 해시 초기화가 진행됨
        _ = ResolvedAnimator;
    }

    private void InitializeHashes()
    {
        if (_animator == null) return;

        // 파라미터 존재 여부 확인 및 다양한 별칭(Alias) 매칭
        _hashBoolIsMoving = FindParameterHash("IsMoving", "Moving", "Move", "Walk");

        _hashTriggerAttack1 = FindParameterHash("TriggerAttack1", "Attack1", "Attack_1", "Attack");
        _hashTriggerAttack2 = FindParameterHash("TriggerAttack2", "Attack2", "Attack_2");
        _hashTriggerAttack3 = FindParameterHash("TriggerAttack3", "Attack3", "Attack_3");
        _hashTriggerAttack4 = FindParameterHash("TriggerAttack4", "Attack4", "Attack_4");
        _hashTriggerAttack5 = FindParameterHash("TriggerAttack5", "Attack5", "Attack_5");

        _hashTriggerGetHit = FindParameterHash("TriggerGetHit", "GetHit", "Hit", "TakeHit", "Damage");
        _hashBoolIsDizzy = FindParameterHash("IsDizzy", "Dizzy", "Stun", "Stunned");
        _hashTriggerDie = FindParameterHash("TriggerDie", "Die", "Death", "Dead");

        // 방어 관련 파라미터 동적 할당
        _hashBoolIsDefending = FindParameterHash("IsDefending", "Defending", "Defense", "Guard");
        _hashTriggerDefenseGetHit = FindParameterHash("TriggerDefenseGetHit", "DefenseGetHit", "GuardHit");

        // 도발 관련 파라미터 동적 할당
        _hashTriggerTaunt = FindParameterHash("TriggerTaunt", "Taunt");

        // 공격 파라미터 추가
        _availableAttackHashes.Clear();
        if (_hashTriggerAttack1 != 0) _availableAttackHashes.Add(_hashTriggerAttack1);
        if (_hashTriggerAttack2 != 0) _availableAttackHashes.Add(_hashTriggerAttack2);
        if (_hashTriggerAttack3 != 0) _availableAttackHashes.Add(_hashTriggerAttack3);
        if (_hashTriggerAttack4 != 0) _availableAttackHashes.Add(_hashTriggerAttack4);
        if (_hashTriggerAttack5 != 0) _availableAttackHashes.Add(_hashTriggerAttack5);
    }

    private int FindParameterHash(params string[] candidateNames)
    {
        Animator anim = ResolvedAnimator;
        if (anim == null) return 0;

        foreach (string candidate in candidateNames)
        {
            if (HasParameter(candidate))
            {
                return Animator.StringToHash(candidate);
            }
        }

        return 0;
    }

    private bool HasParameter(string paramName)
    {
        Animator anim = ResolvedAnimator;
        if (anim == null) return false;

        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName)
            {
                return true;
            }
        }

        return false;
    }

    public void ResetAllTriggers()
    {
        Animator anim = ResolvedAnimator;
        if (anim == null) return;

        if (_hashTriggerAttack1 != 0) anim.ResetTrigger(_hashTriggerAttack1);
        if (_hashTriggerAttack2 != 0) anim.ResetTrigger(_hashTriggerAttack2);
        if (_hashTriggerAttack3 != 0) anim.ResetTrigger(_hashTriggerAttack3);
        if (_hashTriggerAttack4 != 0) anim.ResetTrigger(_hashTriggerAttack4);
        if (_hashTriggerAttack5 != 0) anim.ResetTrigger(_hashTriggerAttack5);
        if (_hashTriggerGetHit != 0) anim.ResetTrigger(_hashTriggerGetHit);
        if (_hashTriggerDefenseGetHit != 0) anim.ResetTrigger(_hashTriggerDefenseGetHit);
        if (_hashTriggerTaunt != 0) anim.ResetTrigger(_hashTriggerTaunt);
        if (_hashTriggerDie != 0) anim.ResetTrigger(_hashTriggerDie);
    }

    public void ResetToIdle()
    {
        SetIsMoving(false);
        SetDefending(false);
        SetDizzy(false);
        ResetAllTriggers();
    }

    public void SetIsMoving(bool isMoving)
    {
        Animator anim = ResolvedAnimator;
        if (anim != null && _hashBoolIsMoving != 0)
        {
            anim.SetBool(_hashBoolIsMoving, isMoving);
        }
    }

    public void PlayRandomAttack()
    {
        Animator anim = ResolvedAnimator;
        if (anim == null || _availableAttackHashes.Count == 0) return;

        int randomIndex = UnityEngine.Random.Range(0, _availableAttackHashes.Count);
        int selectedAttackHash = _availableAttackHashes[randomIndex];

        ResetAllTriggers();
        anim.SetTrigger(selectedAttackHash);
        Debug.Log($"[MonsterAnimationController] {name}: 무작위 공격 애니메이션 재생 (타입 인덱스: {randomIndex})");
    }

    public void PlayAttack(int attackIndex)
    {
        Animator anim = ResolvedAnimator;
        if (anim == null) return;

        int targetHash = 0;

        if (attackIndex == 1) targetHash = _hashTriggerAttack1;
        else if (attackIndex == 2) targetHash = _hashTriggerAttack2;
        else if (attackIndex == 3) targetHash = _hashTriggerAttack3;
        else if (attackIndex == 4) targetHash = _hashTriggerAttack4;
        else if (attackIndex == 5) targetHash = _hashTriggerAttack5;

        if (targetHash != 0)
        {
            ResetAllTriggers();
            anim.SetTrigger(targetHash);
            Debug.Log($"[MonsterAnimationController] {name}: {attackIndex}번 공격 애니메이션 재생");
        }
        else
        {
            Debug.LogWarning($"[MonsterAnimationController] {name}: 존재하지 않거나 유효하지 않은 공격 인덱스입니다 ({attackIndex}).");
        }
    }

    public void PlayGetHit()
    {
        Animator anim = ResolvedAnimator;

        if (anim == null)
        {
            Debug.LogError($"[MonsterAnimationController] {name}: Animator를 찾을 수 없어 피격 애니메이션을 재생할 수 없습니다.");
            return;
        }

        ResetAllTriggers();

        if (IsDefending && _hashTriggerDefenseGetHit != 0)
        {
            anim.SetTrigger(_hashTriggerDefenseGetHit);
            Debug.Log($"[MonsterAnimationController] {name}: 방어 중 피격 애니메이션 재생");
        }
        else if (_hashTriggerGetHit != 0)
        {
            anim.SetTrigger(_hashTriggerGetHit);
            Debug.Log($"[MonsterAnimationController] {name}: 피격 애니메이션 재생");
        }
        else
        {
            Debug.LogWarning($"[MonsterAnimationController] {name}: 피격 파라미터(TriggerGetHit/GetHit/Hit 등)를 Animator에서 찾을 수 없습니다.");
        }

        OnGetHit?.Invoke();
    }

    public void SetDefending(bool isDefending)
    {
        IsDefending = isDefending;
        Animator anim = ResolvedAnimator;

        if (anim != null && _hashBoolIsDefending != 0)
        {
            anim.SetBool(_hashBoolIsDefending, isDefending);
            Debug.Log($"[MonsterAnimationController] {name}: 방어 상태 변경 -> {isDefending}");
        }
    }

    public void PlayDefenseGetHit()
    {
        Animator anim = ResolvedAnimator;
        if (anim != null && _hashTriggerDefenseGetHit != 0)
        {
            ResetAllTriggers();
            anim.SetTrigger(_hashTriggerDefenseGetHit);
            Debug.Log($"[MonsterAnimationController] {name}: 방어 중 피격 애니메이션 재생");
        }
    }

    [Obsolete("PlayDefenseGetHit()을 사용하세요.")]
    public void PlayDefenseGetHint()
    {
        PlayDefenseGetHit();
    }

    public void PlayTaunt()
    {
        Animator anim = ResolvedAnimator;
        if (anim != null && _hashTriggerTaunt != 0)
        {
            ResetAllTriggers();
            anim.SetTrigger(_hashTriggerTaunt);
            Debug.Log($"[MonsterAnimationController] {name}: 도발 애니메이션 재생");
        }
    }

    public void SetDizzy(bool isDizzy)
    {
        IsDizzy = isDizzy;
        Animator anim = ResolvedAnimator;

        if (anim != null && _hashBoolIsDizzy != 0)
        {
            anim.SetBool(_hashBoolIsDizzy, isDizzy);
            Debug.Log($"[MonsterAnimationController] {name}: 상태이상(Dizzy) 상태 변경 -> {isDizzy}");
        }
    }

    public void PlayDie()
    {
        Animator anim = ResolvedAnimator;
        if (anim == null) return;

        SetIsMoving(false);
        SetDefending(false);
        SetDizzy(false);
        ResetAllTriggers();

        if (_hashTriggerDie != 0)
        {
            anim.SetTrigger(_hashTriggerDie);
            Debug.Log($"[MonsterAnimationController] {name}: 사망 애니메이션 재생");
        }

        OnDie?.Invoke();
    }
}