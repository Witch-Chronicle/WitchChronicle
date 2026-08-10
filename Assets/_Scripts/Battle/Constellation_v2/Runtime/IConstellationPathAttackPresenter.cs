using System;

/// <summary>
/// 별자리 강공격 전용 적 연출 인터페이스
/// 위협 동작과 실제 공격 동작 분리
/// </summary>
public interface IConstellationPathAttackPresenter
{
    /// <summary>
    /// 강공격 위협 연출 재생
    /// </summary>
    /// <param name="onComplete">위협 연출 완료 콜백</param>
    void PlayConstellationThreat(Action onComplete = null);

    /// <summary>
    /// 강공격 실제 공격 연출 재생
    /// </summary>
    /// <param name="onLaunch">공격 발사 시점 콜백</param>
    /// <param name="onComplete">공격 연출 완료 콜백</param>
    void PlayConstellationAttack(Action onLaunch = null, Action onComplete = null);
}