using UnityEngine;

/// <summary>
/// 요리 결과물 아이템 데이터.
/// MaterialItemData 상속받아 인벤토리에 수량 기반으로 저장됨.
/// 사용 시 플레이어 최대 HP의 healPercent만큼 회복.
/// </summary>
[CreateAssetMenu(fileName = "NewCookedFood", menuName = "WitchChronicle/Item/CookedFoodItemData")]
public class CookedFoodItemData : MaterialItemData
{
    [Header("요리 데이터")]
    [Tooltip("HP 회복률 (0.0 ~ 1.0). 예: 0.25 = 최대 HP의 25% 회복")]
    [Range(0f, 1f)]
    public float healPercent = 0.15f;

    [Tooltip("요리 등급 (일반/희귀/전설). 아이콘 색상, 판매가 등에 사용")]
    public FoodGrade foodGrade = FoodGrade.Common;

    [Tooltip("요리 카테고리 (빵/수프/구이/스튜/특별)")]
    public FoodCategory foodCategory = FoodCategory.Bread;
}

/// <summary>
/// 요리 등급. HP 회복량 티어와 UI 색상 구분에 사용.
/// </summary>
public enum FoodGrade
{
    Common,     // 일반 (HP 소~중 15~25%)
    Rare,       // 희귀 (HP 대 40%)
    Legendary   // 전설 (HP 특대 60%)
}

/// <summary>
/// 요리 카테고리. UI 탭 분류 및 필터링에 사용.
/// </summary>
public enum FoodCategory
{
    Bread,      // 빵
    Soup,       // 수프
    Grilled,    // 구이
    Stew,       // 스튜
    Special     // 특별
}