using UnityEngine;

/// <summary>
/// ItemType / ItemGradeType을 UI에 표시할 한글 문자열로 변환.
/// Shop / Inventory 상세정보에서 공용으로 사용.
/// </summary>
public static class ItemDisplayNameUtil
{
    public static string ToDisplayString(this ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Equipment: return "장비";
            case ItemType.Consumable: return "소비";
            case ItemType.Material: return "재료";
            case ItemType.SeedItem: return "씨앗";
            case ItemType.KeyItem: return "퀘스트";
            default:
                Debug.LogWarning($"[ItemDisplayNameUtil] 매핑되지 않은 ItemType: {itemType}");
                return itemType.ToString();
        }
    }

    public static string ToDisplayString(this ItemGradeType itemGrade)
    {
        switch (itemGrade)
        {
            case ItemGradeType.Common: return "일반";
            case ItemGradeType.UnCommon: return "고급";
            case ItemGradeType.Rare: return "레어";
            case ItemGradeType.Unique: return "희귀";
            case ItemGradeType.Legendary: return "전설";
            default:
                Debug.LogWarning($"[ItemDisplayNameUtil] 매핑되지 않은 ItemGradeType: {itemGrade}");
                return itemGrade.ToString();
        }
    }
}