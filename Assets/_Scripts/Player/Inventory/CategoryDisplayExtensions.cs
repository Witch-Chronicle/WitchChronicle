/// <summary>
/// MainCategory/SubCategory를 인벤토리/상점 UI에 표시할 한글 텍스트로 변환.
/// </summary>
public static class CategoryDisplayExtensions
{
    public static string ToDisplayString(this MainCategory category)
    {
        switch (category)
        {
            case MainCategory.Equip: return "장비";
            case MainCategory.Consume: return "소비";
            case MainCategory.Life: return "생활";
            case MainCategory.Material: return "재료";
            default: return category.ToString();
        }
    }

    public static string ToDisplayString(this SubCategory category)
    {
        switch (category)
        {
            case SubCategory.Weapon: return "무기";
            case SubCategory.Armor: return "방어구";
            case SubCategory.Acce: return "장신구";
            case SubCategory.Seed: return "씨앗";
            case SubCategory.Harvest: return "작물";
            case SubCategory.Fish: return "생선";
            case SubCategory.Cooked: return "요리";
            case SubCategory.Rod: return "낚싯대";
            case SubCategory.Potion: return "포션";
            case SubCategory.Book: return "마도서";
            case SubCategory.Material: return "재료";
            default: return category.ToString();
        }
    }
}