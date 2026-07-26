using UnityEngine;

/// <summary>
/// 장신구.
/// 예: 반지, 목걸이, 귀걸이
/// </summary>
[CreateAssetMenu(fileName = "NewAccessoryItem", menuName = "Witch Chronicle/Item/AccessoryItemData")]
public class AccessoryItemData : EquipItemData
{
    // 장신구 전용 추가 데이터가 필요하면 여기에 작성
    // 예: public AccessorySlotType accessorySlotType; (반지/목걸이/귀걸이 구분이 필요할 경우)
}