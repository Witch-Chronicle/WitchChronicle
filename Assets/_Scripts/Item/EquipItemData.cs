using UnityEngine;

/// <summary>
/// 장착 가능한 장비의 부모 클래스.
/// 실제로는 생성하지 않고 WeaponItemData / ArmorItemData / AccessoryItemData 로 상속해서 사용.
/// </summary>
public abstract class EquipItemData : ItemData
{
    [Header("장착 보너스 스탯")]
    public int hpBonus;            // 체력
    public int mpBonus;
    public int spellPowerBonus;    // 마력(공격력)
    public int intelligenceBonus;  // 지능
    public int defenseBonus;       // 방어력
    public int speedBonus;         // 속도
    public int luckBonus;          // 행운

    [Header("착용 조건")]
    public int requiredLevel;      // 착용 가능 레벨

    public EquipSlotType equipSlotType; // 장착 슬롯
}