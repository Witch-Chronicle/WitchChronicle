using System.Collections.Generic;

/// <summary>
/// 별자리 공격 방어막 상태
/// 대상별 남은 방어 횟수 관리
/// </summary>
public class ConstellationPathShieldState
{
    private readonly Dictionary<BattleUnit, int> _remainingCharges = new Dictionary<BattleUnit, int>();

    public int ChargesPerTarget { get; private set; }

    /// <summary>
    /// 대상별 방어막 내구도 초기화
    /// </summary>
    /// <param name="targets">방어막 적용 대상</param>
    /// <param name="chargesPerTarget">대상별 방어 가능 횟수</param>
    public void Initialize(IReadOnlyList<BattleUnit> targets, int chargesPerTarget)
    {
        Clear();

        ChargesPerTarget = System.Math.Max(0, chargesPerTarget);

        if (targets == null || ChargesPerTarget <= 0)
        {
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            BattleUnit target = targets[i];

            if (target == null || target.IsAlive == false || _remainingCharges.ContainsKey(target))
            {
                continue;
            }

            _remainingCharges.Add(target, ChargesPerTarget);
        }
    }

    /// <summary>
    /// 대상 공격 방어 시도
    /// </summary>
    /// <param name="target">공격 대상</param>
    /// <param name="remainingCharge">방어 후 남은 횟수</param>
    /// <returns>방어 성공 여부</returns>
    public bool TryBlock(BattleUnit target, out int remainingCharge)
    {
        remainingCharge = 0;

        if (target == null || _remainingCharges.TryGetValue(target, out int currentCharge) == false)
        {
            return false;
        }

        if (currentCharge <= 0)
        {
            return false;
        }

        remainingCharge = currentCharge - 1;
        _remainingCharges[target] = remainingCharge;

        return true;
    }

    /// <summary>
    /// 대상의 남은 방어 횟수 반환
    /// </summary>
    /// <param name="target">확인 대상</param>
    /// <returns>남은 방어 횟수</returns>
    public int GetRemainingCharge(BattleUnit target)
    {
        if (target == null || _remainingCharges.TryGetValue(target, out int remainingCharge) == false)
        {
            return 0;
        }

        return remainingCharge;
    }

    /// <summary>
    /// 대상의 방어막 유지 여부 반환
    /// </summary>
    /// <param name="target">확인 대상</param>
    /// <returns>방어막 유지 여부</returns>
    public bool HasShield(BattleUnit target)
    {
        return GetRemainingCharge(target) > 0;
    }

    /// <summary>
    /// 방어막 상태 초기화
    /// </summary>
    public void Clear()
    {
        _remainingCharges.Clear();
        ChargesPerTarget = 0;
    }
}