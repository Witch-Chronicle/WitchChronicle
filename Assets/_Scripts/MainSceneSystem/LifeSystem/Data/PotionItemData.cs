using UnityEngine;

/// <summary>
/// 전투용 포션 아이템 데이터
/// 가마솥에서 제작되며, 전투 중 사용 시 회복/상태이상 해제 효과를 발동
/// MaterialItemData를 상속받아 재료로도 사용 가능
/// </summary>
[CreateAssetMenu(menuName = "WitchChronicle/PotionItemData")]
public class PotionItemData : ConsumableItemData
{
    [Header("Potion Info")]
    [SerializeField] private PotionGrade _potionGrade;
    [SerializeField] private PotionEffect _potionEffect;

    [Header("Heal Effect (HP/MP 회복 포션 전용)")]
    [Tooltip("최대 HP/MP 대비 회복 비율 (0.3 = 30%, 1.0 = 100%)")]
    [Range(0f, 1f)]
    [SerializeField] private float _healRatio = 0f;

    [Header("Status Cure Effect (상태이상 해제 포션 전용)")]
    [Tooltip("해제할 상태이상 종류")]
    [SerializeField] private StatusEffectType _cureStatusEffectType = StatusEffectType.None;

    public PotionGrade PotionGrade => _potionGrade;
    public PotionEffect PotionEffect => _potionEffect;
    public float HealRatio => _healRatio;
    public StatusEffectType CureStatusEffectType => _cureStatusEffectType;
}

/// <summary>
/// 포션 등급
/// </summary>
public enum PotionGrade
{
    Common,     // 일반 (100G)
    Rare        // 강화 (500G+)
}

/// <summary>
/// 포션 효과 종류
/// </summary>
public enum PotionEffect
{
    HealHp,             // HP 회복
    HealMp,             // MP 회복
    CureStatusEffect,
    CureAllStatusEffects    // 상태이상 해제
}
