using UnityEngine;

/// <summary>
/// 요리 결과물 아이템 데이터.
/// MaterialItemData 상속받아 인벤토리에 수량 기반으로 저장됨.
/// 판매 전용 - 회복 효과 없음, 오직 상점에서 골드로 팔기 위한 아이템.
/// </summary>
[CreateAssetMenu(fileName = "NewCookedFood", menuName = "WitchChronicle/Item/CookedFoodItemData")]
public class CookedFoodItemData : MaterialItemData
{
    [Header("요리 데이터")]
    [Tooltip("요리 등급 (평범/비법/전설). 아이콘 색상, 판매가 등에 사용")]
    public FoodGrade foodGrade = FoodGrade.Common;
}

/// <summary>
/// 요리 등급. UI 탭 분류 및 판매가 티어에 사용.
/// </summary>
public enum FoodGrade
{
    Common,     // 평범
    Rare,       // 비법
    Legendary   // 전설
}