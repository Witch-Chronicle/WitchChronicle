using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WitchChronicle.Alchemy
{
    /// <summary>
    /// 레시피 리스트에 표시되는 카드 하나.
    /// 요리/포션 공통 사용.
    /// </summary>
    public class RecipeCard : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _ingredientsText;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private GameObject _selectHighlight;
        [SerializeField] private Button _button;

        private object _recipeData;
        private Action<object> _onClickCallback;

        private void Awake()
        {
            if (_button != null)
                _button.onClick.AddListener(OnCardClicked);
        }

        public void SetupCookingRecipe(CookingRecipeData recipe, Action<object> onClick)
        {
            _recipeData = recipe;
            _onClickCallback = onClick;

            if (recipe == null || recipe.result == null) return;

            if (_iconImage != null && recipe.result.icon != null)
            {
                _iconImage.sprite = recipe.result.icon;
                _iconImage.enabled = true;
            }

            if (_nameText != null)
                _nameText.text = recipe.result.itemName;

            if (_priceText != null)
                _priceText.text = $"판매가 {recipe.result.sellPrice}G";

            if (_ingredientsText != null)
                _ingredientsText.text = BuildIngredientsText(recipe.ingredients);

            SetHighlighted(false);
        }

        public void SetupPotionRecipe(PotionRecipeData recipe, Action<object> onClick)
        {
            _recipeData = recipe;
            _onClickCallback = onClick;

            if (recipe == null || recipe.resultPotion == null) return;

            if (_iconImage != null && recipe.resultPotion.icon != null)
            {
                _iconImage.sprite = recipe.resultPotion.icon;
                _iconImage.enabled = true;
            }

            if (_nameText != null)
                _nameText.text = recipe.resultPotion.itemName;

            if (_priceText != null)
                _priceText.text = $"판매가 {recipe.resultPotion.sellPrice}G";

            if (_ingredientsText != null)
                _ingredientsText.text = BuildIngredientsText(recipe.ingredients);

            SetHighlighted(false);
        }

        private string BuildIngredientsText(List<IngredientSlot> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0) return "-";

            var parts = new List<string>();
            foreach (var ing in ingredients)
            {
                if (ing == null || ing.specificItem == null) continue;
                parts.Add($"{ing.specificItem.itemName} x{ing.amount}");
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "-";
        }

        public void SetHighlighted(bool highlighted)
        {
            if (_selectHighlight != null)
                _selectHighlight.SetActive(highlighted);
        }

        private void OnCardClicked()
        {
            _onClickCallback?.Invoke(_recipeData);
        }
    }
}