using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 요리 레시피 데이터.
/// 재료 조합과 결과물을 정의. 등급 파생 없음.
/// </summary>
[CreateAssetMenu(fileName = "NewRecipe", menuName = "WitchChronicle/Recipe/CookingRecipeData")]
public class CookingRecipeData : ScriptableObject
{
    [Header("레시피 기본 정보")]
    public string recipeName;
    [TextArea] public string description;

    [Header("재료 목록")]
    public List<IngredientSlot> ingredients = new List<IngredientSlot>();

    [Header("결과물")]
    public CookedFoodItemData result;
}

/// <summary>
/// 레시피 재료 슬롯 하나.
/// 특정 재료 지정 방식만 사용.
/// </summary>
[System.Serializable]
public class IngredientSlot
{
    [Tooltip("재료 (작물/생선/약초)")]
    public MaterialItemData specificItem;

    [Tooltip("필요 개수")]
    public int amount = 1;
}