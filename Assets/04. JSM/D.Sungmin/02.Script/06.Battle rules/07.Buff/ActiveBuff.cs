namespace Battle.Rules
{
    /// <summary>
    /// 런타임 버프 인스턴스 (한 유닛에 걸린 버프 하나)
    /// </summary>
    public class ActiveBuff
    {
        public BuffData Data { get; private set; }
        public int RemainingTurns { get; private set; }
        public int StackCount { get; private set; }

        public ActiveBuff(BuffData data)
        {
            Data = data;
            RemainingTurns = data.Duration;
            StackCount = 1;
        }

        /// <summary>
        /// 턴 종료 시 남은 턴 감소
        /// </summary>
        /// <returns>true = 만료됨(제거 대상)</returns>
        public bool DecreaseTurn()
        {
            RemainingTurns--;
            return RemainingTurns <= 0;
        }

        /// <summary>
        /// 같은 버프 재적용 시 지속시간 갱신
        /// </summary>
        public void RefreshDuration()
        {
            RemainingTurns = Data.Duration;
        }

        /// <summary>
        /// 스택 증가 (스택 가능한 버프만)
        /// </summary>
        public void AddStack()
        {
            if (!Data.CanStack) return;
            if (StackCount < Data.MaxStack)
            {
                StackCount++;
            }
        }

        /// <summary>
        /// 실효 배율 계산 (스택 반영)
        /// 예: Multiplier=1.2, Stack=2 → delta 0.2 × 2 = 0.4 → 1.4배
        ///     Multiplier=0.8, Stack=2 → delta -0.2 × 2 = -0.4 → 0.6배
        /// </summary>
        public float GetEffectiveMultiplier()
        {
            if (!Data.CanStack || StackCount <= 1)
                return Data.Multiplier;

            float delta = Data.Multiplier - 1f;
            return 1f + (delta * StackCount);
        }
    }
}