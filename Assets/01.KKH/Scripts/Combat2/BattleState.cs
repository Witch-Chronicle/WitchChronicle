/// <summary>
/// 전투의 현재 진행 상태
/// </summary>
public enum BattleState
{
    None,
    Starting,
    RoundStart,
    TurnStart,
    ExecutingAction,
    TurnEnd,
    BattleEnd
}