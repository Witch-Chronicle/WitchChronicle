/// <summary>
/// 별자리 공격 데미지 적용 방식
/// </summary>
public enum ConstellationPathDamageDeliveryType
{
    /// <summary>공격 단위당 데미지 일괄 적용</summary>
    SingleHit,

    /// <summary>공격 단위의 데미지를 여러 틱으로 분할 적용</summary>
    Tick
}