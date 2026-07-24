/// <summary>
/// 장비가 실제로 장착되는 슬롯.
/// EquipItemData에 이 값을 지정해서 어느 슬롯에 꽂히는지 표시.
/// </summary>
public enum EquipSlotType
{
    Weapon,     // 무기 (1부위)

    // 방어구 (4부위)
    Robe,
    Cloak,
    Gloves,
    Shoes,

    // 장신구 (3부위)
    Ring,
    Necklace,
    Earring
}