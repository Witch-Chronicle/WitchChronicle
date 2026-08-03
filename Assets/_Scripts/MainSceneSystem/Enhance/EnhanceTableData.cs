using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 장비가 공유하는 전역 강화 테이블.
///
/// increaseRate는 각 강화 단계에서 추가되는 비율이다.
/// 최종 스탯 계산 시에는 현재 강화 단계까지의 increaseRate를 모두 더한 뒤,
/// 원본 장비 스탯에 한 번만 적용한다.
///
/// 예시:
/// +1 = 10%, +2 = 15%라면 +2의 누적 증가율은 25%다.
/// </summary>
[CreateAssetMenu(
    fileName = "NewEnhanceTable",
    menuName = "Witch Chronicle/Enhance/EnhanceTableData")]
public class EnhanceTableData : ScriptableObject
{
    [Tooltip("강화 단계 순서대로 등록 (level 오름차순 권장)")]
    public List<EnhanceLevelEntry> levels = new List<EnhanceLevelEntry>();


    /// <summary>
    /// 지정한 강화 단계의 데이터를 찾아 반환한다.
    /// 등록되지 않은 단계라면 null을 반환한다.
    /// </summary>
    public EnhanceLevelEntry GetLevelData(int level)
    {
        if (levels == null)
        {
            return null;
        }

        return levels.Find(entry => entry != null && entry.level == level);
    }

    /// <summary>
    /// +1부터 지정한 강화 단계까지의 증가율을 합산한다.
    ///
    /// 중요:
    /// 합산한 증가율은 원본 스탯에 한 번만 적용해야 한다.
    /// 단계마다 현재 스탯에 반복 적용하면 안 된다.
    /// </summary>
    public float GetTotalIncreaseRate(int enhanceLevel)
    {
        if (enhanceLevel <= 0 || levels == null || levels.Count == 0)
        {
            return 0f;
        }

        float totalIncreaseRate = 0f;
        int targetLevel = Mathf.Min(enhanceLevel, MaxLevel);

        for (int level = 1; level <= targetLevel; level++)
        {
            EnhanceLevelEntry entry = GetLevelData(level);

            if (entry != null)
            {
                totalIncreaseRate += Mathf.Max(0f, entry.increaseRate);
            }
        }

        return totalIncreaseRate;
    }

    /// <summary>
    /// 테이블에 등록된 가장 높은 강화 단계.
    /// 리스트 정렬 여부와 관계없이 실제 최댓값을 반환한다.
    /// </summary>
    public int MaxLevel
    {
        get
        {
            if (levels == null || levels.Count == 0)
            {
                return 0;
            }

            int maxLevel = 0;

            foreach (EnhanceLevelEntry entry in levels)
            {
                if (entry != null && entry.level > maxLevel)
                {
                    maxLevel = entry.level;
                }
            }

            return maxLevel;
        }
    }

    /// <summary>
    /// 테이블의 최대 강화 단계까지 적용했을 때의 총 증가율.
    /// 현재 권장 테이블에서는 100을 반환한다.
    /// </summary>
    public float MaxTotalIncreaseRate => GetTotalIncreaseRate(MaxLevel);
}
