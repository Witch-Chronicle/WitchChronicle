using UnityEngine;

/// <summary>
/// 방어구.
/// 예: 로브, 마법 망토, 장갑, 신발
/// </summary>
[CreateAssetMenu(fileName = "NewArmorItem", menuName = "Witch Chronicle/Item/ArmorItemData")]
public class ArmorItemData : EquipItemData
{
    // 방어구 전용 추가 데이터가 필요하면 여기에 작성
    // 예: public ArmorSlotType armorSlotType; (머리/몸/손/발 등 부위 구분이 필요할 경우)
}