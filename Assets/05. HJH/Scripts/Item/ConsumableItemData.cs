using UnityEngine;

/// <summary>
/// 사용 시 소모되는 소비 아이템.
/// 예: HP/AP 포션, 상태 회복 포션, 음식, 마도서(Grimoire) 등
/// </summary>
[CreateAssetMenu(fileName = "NewConsumableItem", menuName = "Witch Chronicle/Item/ConsumableItemData")]
public class ConsumableItemData : ItemData
{
    [Header("소비 아이템 데이터")]
    public ConsumableType consumableType; // 소비 아이템 종류
    public float value;                   // 회복량 / 효과 수치

    // Grimoire(마도서) 전용 데이터가 늘어난다면 여기에 추가
    // 예: public SkillData[] possibleSkills; public float[] dropWeights;
}