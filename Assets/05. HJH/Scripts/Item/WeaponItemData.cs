using UnityEngine;

/// <summary>
/// 캐릭터 전용 무기.
/// 예: 아리엘의 지팡이, 라이아의 완드, 페이의 마검
/// </summary>
[CreateAssetMenu(fileName = "NewWeaponItem", menuName = "Witch Chronicle/Item/WeaponItemData")]
public class WeaponItemData : EquipItemData
{
    [Header("무기 전용 데이터")]

    [Tooltip("전용 무기가 아니라면 None으로 두거나 필드 자체를 삭제해도 됨")]
    public OwnerCharacter ownerCharacter; // 장착 가능한 캐릭터

    public WeaponType weaponType;  // 무기 종류
}