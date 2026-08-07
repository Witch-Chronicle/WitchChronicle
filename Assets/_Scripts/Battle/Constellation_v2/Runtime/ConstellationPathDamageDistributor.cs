using System.Collections.Generic;

/// <summary>
/// 별자리 공격 단위 데미지 분배
/// 단일 타격 및 틱 데미지 분할
/// </summary>
public static class ConstellationPathDamageDistributor
{
    /// <summary>
    /// 공격 단위 총 데미지를 전달 방식에 맞게 분배
    /// </summary>
    /// <param name="totalDamage">공격 단위 총 데미지</param>
    /// <param name="attackData">별자리 공격 데이터</param>
    /// <returns>데미지 조각 목록</returns>
    public static List<ConstellationPathDamageSlice> Build(
        int totalDamage,
        ConstellationPathAttackData attackData)
    {
        List<ConstellationPathDamageSlice> slices = new List<ConstellationPathDamageSlice>();

        if (totalDamage <= 0 || attackData == null)
        {
            return slices;
        }

        if (attackData.DamageDeliveryType == ConstellationPathDamageDeliveryType.SingleHit)
        {
            slices.Add(new ConstellationPathDamageSlice(totalDamage, 0, 1));
            return slices;
        }

        BuildTickDamage(totalDamage, attackData.TickCount, slices);
        return slices;
    }

    /// <summary>
    /// 총 데미지를 틱 단위로 균등 분배
    /// 나머지 데미지는 앞쪽 틱부터 1씩 추가
    /// </summary>
    /// <param name="totalDamage">총 데미지</param>
    /// <param name="tickCount">틱 수</param>
    /// <param name="result">분배 결과</param>
    private static void BuildTickDamage(
        int totalDamage,
        int tickCount,
        List<ConstellationPathDamageSlice> result)
    {
        tickCount = System.Math.Max(1, tickCount);

        int baseDamage = totalDamage / tickCount;
        int remainder = totalDamage % tickCount;

        for (int i = 0; i < tickCount; i++)
        {
            int damage = baseDamage;

            if (i < remainder)
            {
                damage++;
            }

            result.Add(new ConstellationPathDamageSlice(damage, i, tickCount));
        }
    }
}