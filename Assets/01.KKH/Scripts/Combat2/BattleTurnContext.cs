/// <summary>
/// 한 유닛의 현재 턴 정보를 관리
/// 남은 행동 횟수를 통해 한 턴에 여러 번 행동하는 확장 가능성 염두
/// </summary>
public class BattleTurnContext
{
    private readonly BattleUnit _unit;
    private readonly int _maxActionCount;
    private int _remainingActionCount;

    public BattleUnit Unit => _unit;
    public int MaxActionCount => _maxActionCount;
    public int RemainingActionCount => _remainingActionCount;

    public bool CanAct => _unit != null && _unit.IsAlive && _remainingActionCount > 0;

    /// <summary>
    /// 턴 정보를 생성
    /// </summary>
    /// <param name="unit">이번 턴을 진행할 유닛</param>
    /// <param name="actionCount">이번 턴에 가능한 행동 횟수</param>
    public BattleTurnContext(BattleUnit unit, int actionCount)
    {
        _unit = unit;
        _maxActionCount = actionCount;
        _remainingActionCount = actionCount;
    }

    /// <summary>
    /// 행동 1회를 소모
    /// </summary>
    public void ConsumeAction()
    {
        if (_remainingActionCount <= 0)
        {
            return;
        }

        _remainingActionCount--;
    }
}