/// <summary>
/// 스탯 보정값이 어떤 시스템에서 발생했는지 나타냄
/// </summary>
public enum StatModifierSourceType
{
    Equipment,
    EquipmentEnhancement,   // 강화 보너스를 장비 기본 스탯과 분리해서 관리하면 사용
    Buff,
    Debuff,
    Passive,
    Food,
    StatusEffect
}