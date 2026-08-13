using System;

/// <summary>
/// InventoryItemSlot 하나를 그리는 데 필요한 데이터를 한데 묶은 구조체입니다.
/// - 소비/재료 등 수량 기반 아이템: ItemData + Quantity 사용 (EquipmentInstance는 null)
/// - 장비 개체: EquipmentInstance 사용 (Quantity는 의미 없음)
/// RecycledScrollView는 이 구조체 단위로 데이터를 넘기고, 셀은 이걸 보고 스스로 어느 쪽인지 판단합니다.
/// </summary>
public readonly struct InventorySlotEntry
{
    public readonly ItemData ItemData;
    public readonly EquipmentInstance EquipmentInstance;
    public readonly int Quantity;
    public readonly UnityEngine.Sprite GradeIcon;
    public readonly Action<ItemData> OnItemClicked;
    public readonly Action<EquipmentInstance> OnEquipmentClicked;

    public bool IsEquipment => EquipmentInstance != null;

    /// <summary>수량 기반 아이템(소비/재료 등)용 생성자입니다.</summary>
    public InventorySlotEntry(
        ItemData itemData,
        int quantity,
        UnityEngine.Sprite gradeIcon,
        Action<ItemData> onItemClicked)
    {
        ItemData = itemData;
        EquipmentInstance = null;
        Quantity = quantity;
        GradeIcon = gradeIcon;
        OnItemClicked = onItemClicked;
        OnEquipmentClicked = null;
    }

    /// <summary>장비 개체용 생성자입니다.</summary>
    public InventorySlotEntry(
        EquipmentInstance equipmentInstance,
        UnityEngine.Sprite gradeIcon,
        Action<EquipmentInstance> onEquipmentClicked)
    {
        EquipmentInstance = equipmentInstance;
        ItemData = equipmentInstance != null ? equipmentInstance.baseData : null;
        Quantity = 0;
        GradeIcon = gradeIcon;
        OnItemClicked = null;
        OnEquipmentClicked = onEquipmentClicked;
    }
}