using System.Collections.Generic;

/// <summary>보유 중인 마도서 한 종류.</summary>
public struct OwnedSkillBook
{
    public SkillBookItemData Book;
    public int Count;

    public OwnedSkillBook(SkillBookItemData book, int count)
    {
        Book = book;
        Count = count;
    }
}

/// <summary>
/// 인벤토리에서 마도서만 조회하는 헬퍼.
/// PlayerInventory는 건드리지 않고 읽기만 한다.
/// </summary>
public static class SkillBookQuery
{
    /// <summary>
    /// 보유 중인 마도서를 종류별로 모아 result에 담는다(수량 0은 제외).
    /// </summary>
    /// <param name="result">결과를 담을 리스트</param>
    public static void GetOwnedBooks(List<OwnedSkillBook> result)
    {
        if (result == null)
        {
            return;
        }

        result.Clear();

        if (PlayerInventory.Instance == null)
        {
            return;
        }

        IReadOnlyList<PlayerInventory.InventorySlot> slots =
            PlayerInventory.Instance.InventorySlots;

        for (int i = 0; i < slots.Count; i++)
        {
            PlayerInventory.InventorySlot slot = slots[i];

            if (slot == null || slot.Quantity <= 0)
            {
                continue;
            }

            if (slot.ItemData is not SkillBookItemData book)
            {
                continue;
            }

            // 같은 마도서가 여러 슬롯에 나뉘어 있으면 합산
            int existing = IndexOf(result, book);

            if (existing >= 0)
            {
                OwnedSkillBook merged = result[existing];
                merged.Count += slot.Quantity;
                result[existing] = merged;
            }
            else
            {
                result.Add(new OwnedSkillBook(book, slot.Quantity));
            }
        }
    }

    /// <summary>해당 마도서의 보유 개수.</summary>
    public static int GetCount(SkillBookItemData book)
    {
        if (book == null || PlayerInventory.Instance == null)
        {
            return 0;
        }

        return PlayerInventory.Instance.GetTotalQuantity(book);
    }

    private static int IndexOf(List<OwnedSkillBook> list, SkillBookItemData book)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Book == book)
            {
                return i;
            }
        }

        return -1;
    }
}
