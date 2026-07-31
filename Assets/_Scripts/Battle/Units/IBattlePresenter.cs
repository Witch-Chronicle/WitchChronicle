using System;

/// <summary>
/// 전투 유닛의 연출(애니메이션) 재생 공용 인터페이스.
/// 플레이어(상태 이름 방식)와 적(트리거 방식) Presenter가 함께 구현하여
/// 바인더가 팀 구분 없이 동일하게 호출한다.
/// </summary>
public interface IBattlePresenter
{
    /// <summary>Idle 상태로 초기화 (전투 시작 시).</summary>
    void ResetToIdle();

    /// <summary>일반 공격. index 생략 시 임의.</summary>
    void PlayAttack(int index = -1, Action onImpact = null, Action onComplete = null);

    /// <summary>공격형 스킬 캐스팅.</summary>
    void PlaySkill(Action onComplete = null);

    /// <summary>지원형(힐/버프) 스킬 캐스팅.</summary>
    void PlaySkillSupport(Action onComplete = null);

    /// <summary>방어/패리.</summary>
    void PlayParry(Action onComplete = null);

    /// <summary>피격.</summary>
    void PlayHit(Action onComplete = null);

    /// <summary>사망.</summary>
    void PlayDeath(Action onComplete = null);
}