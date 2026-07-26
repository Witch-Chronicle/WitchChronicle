using UnityEngine;

/// 경험치 곡선 파라미터. 밸런스 시트(경험치곡선 시트)와 동일한 값 유지.
[CreateAssetMenu(menuName = "Witch Chronicle/GrowthConfig")]
public class GrowthConfig : ScriptableObject
{
    [Header("경험치 곡선 — 밸런스 시트 파라미터와 동일하게")]
    public int MaxLevel = 30;
    public float BaseBattles = 3f;          // 기본 필요 전투 수
    public float BattlesPerLevel = 0.4f;    // 레벨당 전투 수 증가
    public float BaseMonsterExp = 20f;      // 몬스터 기본 경험치
    public float MonsterExpGrowth = 0.1f;   // 몬스터 경험치 증가율/레벨

    /// level → level+1 에 필요한 경험치. 시트 수식과 동일: ROUND(전투수 × 몬스터경험치, -1)
    public int ExpToNext(int level)
    {
        if (level >= MaxLevel) return 0;
        float battles = BaseBattles + BattlesPerLevel * (level - 1);
        float monsterExp = Mathf.Round(BaseMonsterExp * Mathf.Pow(1f + MonsterExpGrowth, level - 1));
        return Mathf.RoundToInt(battles * monsterExp / 10f) * 10;
    }
}
