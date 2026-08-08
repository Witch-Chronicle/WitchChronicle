/// <summary>
/// 별자리 공격 단위 내부 데미지 조각
/// </summary>
public readonly struct ConstellationPathDamageSlice
{
    public int Damage { get; }
    public int TickIndex { get; }
    public int TickCount { get; }

    /// <summary>
    /// 데미지 조각 생성
    /// </summary>
    /// <param name="damage">적용 데미지</param>
    /// <param name="tickIndex">현재 틱 인덱스</param>
    /// <param name="tickCount">전체 틱 수</param>
    public ConstellationPathDamageSlice(int damage, int tickIndex, int tickCount)
    {
        Damage = damage;
        TickIndex = tickIndex;
        TickCount = tickCount;
    }
}