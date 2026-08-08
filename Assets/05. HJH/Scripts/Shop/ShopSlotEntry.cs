using System;

/// <summary>
/// ShopItemSlot 하나를 그리는 데 필요한 데이터를 한데 묶은 구조체입니다.
/// IsSelected는 셀 재사용 시 "이 데이터가 지금 선택된 아이템인지"를 매번 다시 계산해서 넣어줘야 합니다.
/// (셀 오브젝트 자체는 재사용되므로, 셀 참조로 선택 상태를 기억하면 스크롤 후 어긋납니다.)
/// </summary>
public readonly struct ShopSlotEntry
{
    public readonly ItemData ItemData;
    public readonly bool IsSelected;
    public readonly Action<ItemData> OnClicked;

    public ShopSlotEntry(ItemData itemData, bool isSelected, Action<ItemData> onClicked)
    {
        ItemData = itemData;
        IsSelected = isSelected;
        OnClicked = onClicked;
    }
}