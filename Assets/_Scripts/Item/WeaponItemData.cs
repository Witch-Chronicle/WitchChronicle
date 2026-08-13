using UnityEngine;

/// <summary>
/// 캐릭터 전용 무기.
/// 예: 아리엘의 지팡이, 라이아의 완드, 페이의 마검
/// </summary>
[CreateAssetMenu(fileName = "NewWeaponItem", menuName = "Witch Chronicle/Item/WeaponItemData")]
public class WeaponItemData : EquipItemData
{
    public WeaponType weaponType;  // 무기 종류
}