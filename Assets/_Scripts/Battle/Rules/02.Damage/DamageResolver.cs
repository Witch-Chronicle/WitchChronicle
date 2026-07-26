using UnityEngine;

namespace Battle.Rules
{
    /// <summary>
    /// 최종 데미지 계산
    /// 공격력·방어력·스킬 배율·속성 배율·크리티컬을 종합하여 DamageResult 반환
    /// 버프/디버프 적용된 실효 스탯으로 계산
    /// </summary>
    public static class DamageResolver
    {
        // 계산 상수
        private const float CriticalMultiplier = 1.5f;      // 크리티컬 배율
        private const float BaseCriticalChance = 0.05f;     // 기본 크리티컬 확률 (5%)
        private const float BaseHitChance = 0.95f;          // 기본 명중률 (95%)
        private const float MinDamageRatio = 0.1f;          // 최소 데미지 비율 (공격력의 10%)

        /// <summary>
        /// 스킬 사용 시 데미지 계산
        /// </summary>
        /// <param name="attacker">공격자</param>
        /// <param name="target">대상</param>
        /// <param name="skillData">사용 스킬</param>
        /// <param name="buffController">버프 관리자 (null이면 배율 미적용)</param>
        /// <returns>데미지 결과</returns>
        public static DamageResult Calculate(
            BattleUnit attacker,
            BattleUnit target,
            SkillData skillData,
            BuffController buffController = null)
        {
            if (attacker == null || target == null || skillData == null)
            {
                Debug.LogWarning("[DamageResolver] 인자가 null입니다");
                return DamageResult.Miss();
            }

            // 1. 명중 판정
            if (RollHit(attacker, target) == false)
            {
                return DamageResult.Miss();
            }

            // 2. 속성 상성 판정
            ElementResolveResult elementResult = ElementResolver.Resolve(target, skillData.ElementType);

            // 무효인 경우 데미지 0으로 처리 (미스 아님)
            if (elementResult.Affinity == ElementAffinity.Null)
            {
                return DamageResult.Damage(0, ElementAffinity.Null, false);
            }

            // 3. 기본 데미지 계산 (버프 배율 반영)
            float baseDamage = CalculateBaseDamage(attacker, target, skillData, buffController);

            // 4. 속성 배율 적용
            float elementalDamage = baseDamage * elementResult.Multiplier;

            // 5. 크리티컬 판정 및 적용
            bool isCritical = RollCritical(attacker);
            float finalDamage = isCritical ? elementalDamage * CriticalMultiplier : elementalDamage;

            // 6. 최소 데미지 보정 (0이 되지 않도록)
            int roundedDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage));

            return DamageResult.Damage(roundedDamage, elementResult.Affinity, isCritical);
        }

        /// <summary>
        /// 기본 데미지 계산 (공격력·방어력·스킬 배율)
        /// DamageType에 따라 물리·마법·고정 분기
        /// 버프/디버프 배율 적용
        /// </summary>
        private static float CalculateBaseDamage(
            BattleUnit attacker,
            BattleUnit target,
            SkillData skillData,
            BuffController buffController)
        {
            float attackPower;
            float defensePower;

            switch (skillData.DamageType)
            {
                case DamageType.Physical:
                    attackPower = attacker.AttackPower;
                    defensePower = target.DefensePower;
                    // 물리 계열 버프는 현재 시스템에 없음
                    break;

                case DamageType.Magical:
                    attackPower = attacker.MagicPower;
                    defensePower = target.MagicDefensePower;
                    // 마공/마방 버프 배율 적용
                    if (buffController != null)
                    {
                        attackPower *= buffController.GetMagicAttackMultiplier(attacker);
                        defensePower *= buffController.GetMagicDefenseMultiplier(target);
                    }
                    break;

                case DamageType.Fixed:
                    // 고정 피해는 방어력 무시, 스킬 파워만 사용
                    return skillData.Power;

                default:
                    attackPower = attacker.AttackPower;
                    defensePower = target.DefensePower;
                    break;
            }

            // 데미지 공식: (공격력 × 스킬 배율) - 방어력
            // 스킬 Power를 배율로 사용 (100 = 1.0배)
            float skillMultiplier = skillData.Power / 100f;
            float rawDamage = (attackPower * skillMultiplier) - defensePower;

            // 최소 데미지 보정 (공격력의 10%는 무조건 들어감)
            float minDamage = attackPower * skillMultiplier * MinDamageRatio;

            return Mathf.Max(minDamage, rawDamage);
        }

        /// <summary>
        /// 명중 판정
        /// 현재는 기본 명중률만 사용, 추후 회피 스탯 추가 시 확장
        /// </summary>
        private static bool RollHit(BattleUnit attacker, BattleUnit target)
        {
            float hitChance = BaseHitChance;
            return Random.value <= hitChance;
        }

        /// <summary>
        /// 크리티컬 판정
        /// 현재는 기본 확률만 사용, 추후 행운 스탯 추가 시 확장
        /// </summary>
        private static bool RollCritical(BattleUnit attacker)
        {
            float criticalChance = BaseCriticalChance;
            return Random.value <= criticalChance;
        }
    }
}