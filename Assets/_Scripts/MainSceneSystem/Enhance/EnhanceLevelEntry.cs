using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 강화 재료 하나와 필요 수량.
/// </summary>
[Serializable]
public class RequiredMaterialEntry
{
    public MaterialItemData material;

    [Min(1)]
    public int amount = 1;
}

/// <summary>
/// 강화 한 단계에 대한 설정 데이터.
///
/// increaseRate는 이 단계에서 추가되는 증가율이다.
/// 최종 스탯은 현재 단계까지의 increaseRate를 전부 더한 뒤
/// 원본 스탯에 한 번만 적용한다.
///
/// 권장 증가율:
/// +1: 10%, +2: 15%, +3: 20%, +4: 25%, +5: 30%
/// 누적 증가율:
/// +1: 10%, +2: 25%, +3: 45%, +4: 70%, +5: 100%
/// </summary>
[Serializable]
public class EnhanceLevelEntry
{
    [Header("단계")]
    [Min(1)]
    public int level = 1;

    [Header("증가율")]
    [Tooltip("이 단계에서 추가되는 증가율(%). 현재 단계까지 합산한 뒤 원본 스탯에 한 번 적용한다.")]
    [Min(0f)]
    public float increaseRate;

    [Header("강화 비용/확률")]
    [Range(0f, 100f)]
    public float successRate = 100f;

    [Min(0)]
    public int requiredGold;

    [Tooltip("필요한 재료들. 비워두면 재료 없이 골드만 사용한다.")]
    public List<RequiredMaterialEntry> requiredMaterials = new List<RequiredMaterialEntry>();

    [Header("천장 시스템")]
    [Tooltip("이 횟수만큼 연속 실패하면 그 다음 시도가 확정 성공한다. 0이면 천장이 없다.")]
    [Min(0)]
    public int pityCount;
}