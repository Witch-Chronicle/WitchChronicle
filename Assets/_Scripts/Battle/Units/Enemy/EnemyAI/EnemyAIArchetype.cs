/// <summary>
/// 적 AI 성향 타입
/// </summary>
public enum EnemyAIArchetype
{
    Aggressive,     // 공격 위주, 낮은 HP대상과 높은 피해 스킬 선호
    Defensive,      // 자기 생존 우선, HP가 낮으면 방어/회복/보조 행동 선호
    Support,        // 아군 회복, 버프, 상태 이상 보조 선호
    Cunning,        // 약점, 막타, 상태이상, 위협 대상 제거 선호
    Berserker,      // 체력이 낮을수록 더 공격적으로 대응
    Random          // 랜덤
}