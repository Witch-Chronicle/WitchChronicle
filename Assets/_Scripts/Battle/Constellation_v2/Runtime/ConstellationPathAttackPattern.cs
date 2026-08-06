/// <summary>
/// 별자리 공격 대상 분배 방식
/// </summary>
public enum ConstellationPathAttackPattern
{
    /// <summary>모든 대상에게 라운드별 동시 타격</summary>
    AllTargetsSimultaneous,

    /// <summary>모든 대상에게 균등하게 분배된 순차 타격</summary>
    AllTargetsBalancedSequence,

    /// <summary>단일 대상에게 연속 타격</summary>
    SingleTargetSequence
}