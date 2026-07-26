using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 요리 레시피 데이터.
/// 재료 조합과 결과물(등급별)을 정의.
/// 생선 슬롯이 있는 레시피는 사용된 생선의 최고 등급에 따라 결과물이 파생됨.
/// </summary>
[CreateAssetMenu(fileName = "NewRecipe", menuName = "WitchChronicle/Recipe/CookingRecipeData")]
public class CookingRecipeData : ScriptableObject
{
    [Header("레시피 기본 정보")]
    public string recipeName;
    [TextArea] public string description;
    public FoodCategory category;

    [Header("재료 목록")]
    public List<IngredientSlot> ingredients = new List<IngredientSlot>();

    [Header("결과물 (등급별)")]
    [Tooltip("파생 없는 요리(야채 전용)는 이것만 설정. 파생 요리는 3개 다 설정")]
    public CookedFoodItemData resultCommon;

    [Tooltip("사용 생선 최고 등급이 희귀일 때 결과물. null이면 Common으로 대체")]
    public CookedFoodItemData resultRare;

    [Tooltip("사용 생선 최고 등급이 전설일 때 결과물. null이면 Common으로 대체")]
    public CookedFoodItemData resultLegendary;

    [Header("생선 등급 파생 여부")]
    [Tooltip("체크 시 사용된 생선 최고 등급에 따라 결과 결정. 미체크 시 resultCommon 고정")]
    public bool useFishGradeDerivation = false;

    /// <summary>
    /// 사용된 재료 리스트를 받아 결과물을 결정.
    /// 파생 요리인 경우 사용된 생선 최고 등급 기준.
    /// </summary>
    public CookedFoodItemData DetermineResult(List<ItemData> usedIngredients)
    {
        if (!useFishGradeDerivation)
        {
            return resultCommon;
        }

        FoodGrade highestFishGrade = FoodGrade.Common;

        foreach (var item in usedIngredients)
        {
            if (item is FishItemData fish)
            {
                FoodGrade g = FishGradeToFoodGrade(fish.grade);
                if (g > highestFishGrade) highestFishGrade = g;
            }
        }

        return highestFishGrade switch
        {
            FoodGrade.Legendary => resultLegendary != null ? resultLegendary : resultCommon,
            FoodGrade.Rare => resultRare != null ? resultRare : resultCommon,
            _ => resultCommon
        };
    }

    private FoodGrade FishGradeToFoodGrade(FishGrade fg)
    {
        return fg switch
        {
            FishGrade.Legendary => FoodGrade.Legendary,
            FishGrade.Rare => FoodGrade.Rare,
            _ => FoodGrade.Common
        };
    }
}

/// <summary>
/// 레시피 재료 슬롯 하나.
/// SpecificItem: 특정 아이템 지정 (감자, 밀 등)
/// FishByGrade: 등급 조건으로 임의의 생선 허용 (min~max 범위)
/// </summary>
[System.Serializable]
public class IngredientSlot
{
    public IngredientSlotType slotType = IngredientSlotType.SpecificItem;

    [Header("SpecificItem 타입일 때")]
    [Tooltip("특정 재료 (감자, 밀, 허브, 특정 생선 등)")]
    public MaterialItemData specificItem;

    [Header("FishByGrade 타입일 때")]
    [Tooltip("최소 요구 등급 (이 등급 이상만 사용 가능)")]
    public FishGrade minFishGrade = FishGrade.Common;
    [Tooltip("최대 허용 등급. 특정 등급만 원하면 min=max로 설정 (예: 희귀생선 x1)")]
    public FishGrade maxFishGrade = FishGrade.Legendary;

    [Header("공통")]
    public int amount = 1;

    public bool Matches(ItemData item)
    {
        if (item == null) return false;

        if (slotType == IngredientSlotType.SpecificItem)
        {
            return item == specificItem;
        }
        else
        {
            if (item is FishItemData fish)
            {
                return fish.grade >= minFishGrade && fish.grade <= maxFishGrade;
            }
            return false;
        }
    }
}

public enum IngredientSlotType
{
    SpecificItem,
    FishByGrade
}