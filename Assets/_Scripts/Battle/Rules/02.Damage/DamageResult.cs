namespace Battle.Rules
{
    /// <summary>
    /// 데미지 계산 결과 데이터
    /// UI, 이펙트, 로그가 이 결과를 받아 연출
    /// </summary>
    public struct DamageResult
    {
        /// <summary>실제 적용된 최종 데미지 (0 이상)</summary>
        public int FinalDamage;

        /// <summary>속성 상성 결과</summary>
        public ElementAffinity Affinity;

        /// <summary>크리티컬 발생 여부</summary>
        public bool IsCritical;

        /// <summary>공격 빗나감 여부 (true면 FinalDamage = 0)</summary>
        public bool IsMiss;

        /// <summary>데미지가 아닌 회복 처리 여부 (현재 사용 안 함, 확장용)</summary>
        public bool IsHeal;

        /// <summary>
        /// 미스 결과 생성
        /// </summary>
        public static DamageResult Miss()
        {
            return new DamageResult
            {
                FinalDamage = 0,
                Affinity = ElementAffinity.Normal,
                IsCritical = false,
                IsMiss = true,
                IsHeal = false
            };
        }

        /// <summary>
        /// 일반 데미지 결과 생성
        /// </summary>
        public static DamageResult Damage(int damage, ElementAffinity affinity, bool isCritical)
        {
            return new DamageResult
            {
                FinalDamage = damage,
                Affinity = affinity,
                IsCritical = isCritical,
                IsMiss = false,
                IsHeal = false
            };
        }
    }
}