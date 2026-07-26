using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 장비가 공유하는 전역 강화 테이블.
/// EnhanceLevelEntry들을 강화 단계 순서대로 담고 있음.
/// </summary>
[CreateAssetMenu(fileName = "NewEnhanceTable", menuName = "Witch Chronicle/Enhance/EnhanceTableData")]
public class EnhanceTableData : ScriptableObject
{
    [Tooltip("강화 단계 순서대로 등록 (level 오름차순 권장)")]
    public List<EnhanceLevelEntry> levels = new List<EnhanceLevelEntry>();

    /// <summary>
    /// 지정한 단계의 데이터를 찾아서 반환. 없으면 null.
    /// </summary>
    public EnhanceLevelEntry GetLevelData(int level)
    {
        return levels.Find(entry => entry.level == level);
    }

    /// <summary>
    /// 테이블에 등록된 가장 높은 강화 단계 (더 이상 강화 불가능한 상한선 체크용)
    /// </summary>
    public int MaxLevel => levels.Count > 0 ? levels[levels.Count - 1].level : 0;
}