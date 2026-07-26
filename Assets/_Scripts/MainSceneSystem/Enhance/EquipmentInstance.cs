using System;

/// <summary>
/// 플레이어가 보유한 장비 "개체" 데이터.
/// EquipItemData(SO)는 아이템 종류(원본) 데이터일 뿐이고,
/// 실제 강화 단계는 이 클래스가 개체별로 따로 들고 있음.
/// 같은 종류의 장비를 여러 개 보유해도 각각 독립적으로 강화 가능.
///
/// 최종 스탯(cachedStats)은 매번 계산하지 않고, 강화에 성공해서
/// enhanceLevel이 바뀔 때만 RefreshStats()로 다시 계산해서 캐싱해둔다.
/// 장착/전투 중에는 이 캐시된 값만 읽으면 됨.
/// </summary>
[Serializable]
public class EquipmentInstance
{
    public EquipItemData baseData;                 // 원본 장비 종류 (SO)
    public int enhanceLevel;                       // 현재 강화 단계 (0 = 강화 안 함)
    public EquipStatCalculator.StatSet cachedStats; // 강화 단계가 반영된 최종 스탯 (캐시)

    // 현재 단계에서 연속으로 시도(실패)한 횟수. 천장 시스템용. 강화 성공하면 0으로 리셋.
    public int enhanceAttemptCount;

    public EquipmentInstance(EquipItemData baseData, int enhanceLevel, EnhanceTableData enhanceTable)
    {
        this.baseData = baseData;
        this.enhanceLevel = enhanceLevel;
        RefreshStats(enhanceTable);
    }

    /// <summary>
    /// 강화 단계 변경 등으로 cachedStats를 다시 계산해야 할 때 호출.
    /// (강화 성공 시에만 호출하면 됨. 장착/전투 중에는 호출할 필요 없음)
    /// </summary>
    public void RefreshStats(EnhanceTableData enhanceTable)
    {
        cachedStats = EquipStatCalculator.GetCurrentStats(baseData, enhanceLevel, enhanceTable);
    }
}