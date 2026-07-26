using UnityEngine;

/// <summary>
/// EquipItemData(원본 스탯) + EnhanceTableData(강화 테이블) + 강화 단계를 조합해서
/// 최종 스탯을 계산하는 유틸리티.
/// - 원래 0이었던 스탯은 강화해도 계속 0 (스탯별 성장 폭이 다른 무기/방어구 대응)
/// - 강화는 단계별로 누적 적용 (1강 적용된 값에 2강 비율이 다시 적용되는 복리 방식)
/// - 소수점은 올림 처리 (강화했는데 하나도 안 오르는 상황 방지)
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
    /// 강화 단계가 반영된 최종 스탯을 계산해서 반환.
    /// </summary>
    public static StatSet GetCurrentStats(EquipItemData baseData, int enhanceLevel, EnhanceTableData enhanceTable)
    {
        var stats = new StatSet
        {
            hp = baseData.hpBonus,
            mp = baseData.mpBonus,
            spellPower = baseData.spellPowerBonus,
            intelligence = baseData.intelligenceBonus,
            defense = baseData.defenseBonus,
            speed = baseData.speedBonus,
            luck = baseData.luckBonus
        };

        if (enhanceTable == null || enhanceLevel <= 0)
        {
            return stats;
        }

        // 1강부터 현재 강화 단계까지 순서대로 누적 적용
        for (int level = 1; level <= enhanceLevel; level++)
        {
            EnhanceLevelEntry entry = enhanceTable.GetLevelData(level);
            if (entry == null) continue;

            stats.hp = ApplyIncrease(stats.hp, entry.increaseRate);
            stats.mp = ApplyIncrease(stats.mp, entry.increaseRate);
            stats.spellPower = ApplyIncrease(stats.spellPower, entry.increaseRate);
            stats.intelligence = ApplyIncrease(stats.intelligence, entry.increaseRate);
            stats.defense = ApplyIncrease(stats.defense, entry.increaseRate);
            stats.speed = ApplyIncrease(stats.speed, entry.increaseRate);
            stats.luck = ApplyIncrease(stats.luck, entry.increaseRate);
        }

        return stats;
    }

    /// <summary>
    /// 원래 0이었던 스탯은 그대로 0 유지. 0이 아니면 올림 처리된 증가분을 더함.
    /// </summary>
    private static int ApplyIncrease(int currentValue, float increaseRate)
    {
        if (currentValue == 0)
        {
            return 0;
        }

        int increase = Mathf.CeilToInt(currentValue * increaseRate / 100f);
        return currentValue + increase;
    }
}