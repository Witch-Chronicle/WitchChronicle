using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 포션 레시피 데이터.
/// 재료 조합(약초)과 결과 포션을 정의.
/// 요리와 달리 등급 파생 없음 (모든 재료가 SpecificItem).
/// </summary>
[CreateAssetMenu(fileName = "NewPotionRecipe", menuName = "WitchChronicle/Recipe/PotionRecipeData")]
public class PotionRecipeData : ScriptableObject
{
    [Header("레시피 기본 정보")]
    public string recipeName;
    [TextArea] public string description;

    [Header("재료 목록")]
    [Tooltip("모두 SpecificItem 타입 사용 (특정 약초)")]
    public List<IngredientSlot> ingredients = new List<IngredientSlot>();

    [Header("결과물")]
    public PotionItemData resultPotion;
}