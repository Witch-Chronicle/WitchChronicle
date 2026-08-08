using System.Collections.Generic;

/// <summary>
/// 별자리 단일 공격 단위 판정 결과
/// </summary>
public readonly struct ConstellationPathHitResolution
{
    public BattleUnit Target { get; }
    public bool IsBlocked { get; }
    public int RemainingShieldCharge { get; }
    public IReadOnlyList<ConstellationPathDamageSlice> DamageSlices { get; }

    public bool IsShieldBroken => IsBlocked && RemainingShieldCharge == 0;

    /// <summary>
    /// 공격 단위 판정 결과 생성
    /// </summary>
    /// <param name="target">공격 대상</param>
    /// <param name="isBlocked">방어 성공 여부</param>
    /// <param name="remainingShieldCharge">남은 방어 횟수</param>
    /// <param name="damageSlices">적용 데미지 조각</param>
    public ConstellationPathHitResolution(
        BattleUnit target,
        bool isBlocked,
        int remainingShieldCharge,
        IReadOnlyList<ConstellationPathDamageSlice> damageSlices)
    {
        Target = target;
        IsBlocked = isBlocked;
        RemainingShieldCharge = remainingShieldCharge;
        DamageSlices = damageSlices;
    }
}