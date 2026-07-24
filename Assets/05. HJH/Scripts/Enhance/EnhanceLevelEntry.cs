using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 강화 재료 하나(재료 종류 + 필요 수량). EnhanceLevelEntry.requiredMaterials 안에서 여러 개 등록 가능.
/// </summary>
[Serializable]
public class RequiredMaterialEntry
{
    public MaterialItemData material;
    public int amount;
}

/// <summary>
/// 강화 1단계에 대한 데이터.
/// EnhanceTableData 안에서 리스트 형태로 관리됨.
/// - 스탯별 고정 증가량이 아니라, "원래 0이 아니었던 스탯"에 적용되는 증가율(%) 하나만 가짐.
/// - 실제 적용 시: 새 값 = 기존값 + Ceil(기존값 * increaseRate / 100)
/// </summary>
[Serializable]
public class EnhanceLevelEntry
{
    [Header("단계")]
    public int level; // 몇 단계인지 (예: 1강, 2강...)

    [Header("증가율")]
    [Tooltip("이 단계에서 적용되는 스탯 증가율(%). 원래 0이 아니었던 스탯에만 적용됨. 예: 10이면 10% 증가")]
    public float increaseRate;

    [Header("강화 비용/확률")]
    [Range(0, 100)]
    public float successRate = 100f;
    public int requiredGold;
    [Tooltip("필요한 재료들. 여러 종류 등록 가능. 비워두면 재료 없이 골드만으로 강화")]
    public List<RequiredMaterialEntry> requiredMaterials = new List<RequiredMaterialEntry>();

    [Header("천장 시스템")]
    [Tooltip("이 단계에서 연속으로 몇 번 시도(실패)하면 다음 강화가 100% 성공하는지. 0이면 천장 없음")]
    public int pityCount;
}