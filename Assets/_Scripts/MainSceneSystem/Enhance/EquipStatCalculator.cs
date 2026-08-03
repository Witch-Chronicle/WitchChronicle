using UnityEngine;

/// <summary>
/// 원본 장비 스탯과 강화 테이블을 조합해 최종 스탯을 계산한다.
///
/// 계산 규칙:
/// 1. 현재 강화 단계까지의 increaseRate를 모두 더한다.
/// 2. 합산된 증가율을 원본 스탯에 한 번만 적용한다.
/// 3. 증가량의 소수점은 마지막에 한 번만 올림 처리한다.
/// 4. 원래 0이었던 스탯은 강화 후에도 0을 유지한다.
///
/// 단계별 현재 값에 반복 적용하는 복리 계산은 사용하지 않는다.
/// </summary>
public static class EquipStatCalculator
{
    [System.Serializable]
    public struct StatSet
    {
        public int hp;
        public int mp;
        public int spellPower;
        public int intelligence;
        public int defense;
        public int speed;
        public int luck;
    }

    /// <summary>
    /// 강화 단계가 반영된 최종 정수 스탯을 계산한다.
    /// </summary>
    public static StatSet GetCurrentStats(
        EquipItemData baseData,
        int enhanceLevel,
        EnhanceTableData enhanceTable)
    {
        if (baseData == null)
        {
            Debug.LogWarning("[EquipStatCalculator] baseData가 null이므로 빈 스탯을 반환합니다.");
            return default;
        }

        if (enhanceTable == null || enhanceLevel <= 0)
        {
            return CreateBaseStats(baseData);
        }

        float totalIncreaseRate = enhanceTable.GetTotalIncreaseRate(enhanceLevel);

        return new StatSet
        {
            hp = ApplyTotalIncrease(baseData.hpBonus, totalIncreaseRate),
            mp = ApplyTotalIncrease(baseData.mpBonus, totalIncreaseRate),
            spellPower = ApplyTotalIncrease(baseData.spellPowerBonus, totalIncreaseRate),
            intelligence = ApplyTotalIncrease(baseData.intelligenceBonus, totalIncreaseRate),
            defense = ApplyTotalIncrease(baseData.defenseBonus, totalIncreaseRate),
            speed = ApplyTotalIncrease(baseData.speedBonus, totalIncreaseRate),
            luck = ApplyTotalIncrease(baseData.luckBonus, totalIncreaseRate)
        };
    }

    /// <summary>
    /// 강화가 적용되지 않은 원본 스탯 세트를 생성한다.
    /// </summary>
    private static StatSet CreateBaseStats(EquipItemData baseData)
    {
        return new StatSet
        {
            hp = baseData.hpBonus,
            mp = baseData.mpBonus,
            spellPower = baseData.spellPowerBonus,
            intelligence = baseData.intelligenceBonus,
            defense = baseData.defenseBonus,
            speed = baseData.speedBonus,
            luck = baseData.luckBonus
        };
    }

    /// <summary>
    /// 누적 증가율을 원본 스탯에 한 번 적용한다.
    /// 증가량의 소수점은 마지막에 한 번만 올린다.
    /// </summary>
    private static int ApplyTotalIncrease(int baseValue, float totalIncreaseRate)
    {
        if (baseValue == 0 || totalIncreaseRate <= 0f)
        {
            return baseValue;
        }

        int increase = Mathf.CeilToInt(baseValue * totalIncreaseRate / 100f);
        return baseValue + increase;
    }
}
