using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WitchChronicle.Alchemy
{
    public class AlchemyPanel : MonoBehaviour
    {
        [Header("UI 루트")]
        [SerializeField] private GameObject _panelRoot;

        [Header("모드 전환 - 3D 가마솥")]
        [SerializeField] private GameObject _cookingCauldron;
        [SerializeField] private GameObject _potionCauldron;

        [Header("모드 탭 버튼")]
        [SerializeField] private Button _cookingTabButton;
        [SerializeField] private Button _potionTabButton;

        [Header("모드 탭 시각 상태")]
        [SerializeField] private Color _tabActiveColor = new Color(1f, 0.6f, 0.3f);
        [SerializeField] private Color _tabInactiveColor = new Color(0.4f, 0.4f, 0.4f);

        [Header("등급 탭")]
        [SerializeField] private Button _commonTabButton;
        [SerializeField] private Button _rareTabButton;
        [SerializeField] private Button _legendaryTabButton;

        [Header("영역 참조")]
        [SerializeField] private RectTransform _ingredientSlotContainer;
        [SerializeField] private TextMeshProUGUI _startButtonText;
        [SerializeField] private Button _startButton;

        [Header("재료 슬롯")]
        [SerializeField] private GameObject[] _ingredientSlots;

        [Header("레시피 리스트")]
        [SerializeField] private Transform _recipeListContent;
        [SerializeField] private RecipeCard _recipeCardPrefab;
        [SerializeField] private TextMeshProUGUI _recipeCountText;

        [Header("레시피 데이터")]
        [SerializeField] private List<CookingRecipeData> _cookingRecipes = new List<CookingRecipeData>();
        [SerializeField] private List<PotionRecipeData> _potionRecipes = new List<PotionRecipeData>();

        [Header("재료 인벤토리")]
        [SerializeField] private Transform _inventoryContent;
        [SerializeField] private MaterialSlot _materialSlotPrefab;
        [SerializeField] private TextMeshProUGUI _inventoryCountText;

        private Action _onClosedCallback;
        private AlchemyMode _currentMode;
        private int _currentGradeIndex = 0;

        private List<RecipeCard> _spawnedCards = new List<RecipeCard>();
        private RecipeCard _selectedCard;

        private List<MaterialSlot> _spawnedInventorySlots = new List<MaterialSlot>();

        private void Awake()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);

            if (_cookingTabButton != null)
                _cookingTabButton.onClick.AddListener(() => SwitchMode(AlchemyMode.Cooking));
            if (_potionTabButton != null)
                _potionTabButton.onClick.AddListener(() => SwitchMode(AlchemyMode.Potion));

            if (_commonTabButton != null)
                _commonTabButton.onClick.AddListener(() => OnGradeTabClicked(0));
            if (_rareTabButton != null)
                _rareTabButton.onClick.AddListener(() => OnGradeTabClicked(1));
            if (_legendaryTabButton != null)
                _legendaryTabButton.onClick.AddListener(() => OnGradeTabClicked(2));
        }

        private void Update()
        {
            if (_panelRoot == null || !_panelRoot.activeSelf) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        public void Open(AlchemyMode mode, Action onClosed)
        {
            _onClosedCallback = onClosed;
            if (_panelRoot != null) _panelRoot.SetActive(true);

            _currentGradeIndex = 0;
            SwitchMode(mode);
            Debug.Log($"[AlchemyPanel] 열림 (모드: {mode})");
        }

        public void Close()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);

            if (_cookingCauldron != null) _cookingCauldron.SetActive(false);
            if (_potionCauldron != null) _potionCauldron.SetActive(false);

            _onClosedCallback?.Invoke();
            _onClosedCallback = null;
        }

        private void SwitchMode(AlchemyMode mode)
        {
            _currentMode = mode;
            _currentGradeIndex = 0;
            _selectedCard = null;

            if (_cookingCauldron != null)
                _cookingCauldron.SetActive(mode == AlchemyMode.Cooking);
            if (_potionCauldron != null)
                _potionCauldron.SetActive(mode == AlchemyMode.Potion);

            UpdateModeTabVisual();
            UpdateIngredientSlotCount(mode);
            UpdateStartButtonText(mode);
            UpdateGradeTabVisibility(mode);

            RefreshRecipeList();
            RefreshInventory();

            Debug.Log($"[AlchemyPanel] 모드 전환: {mode}");
        }

        private void UpdateModeTabVisual()
        {
            SetButtonColor(_cookingTabButton, _currentMode == AlchemyMode.Cooking ? _tabActiveColor : _tabInactiveColor);
            SetButtonColor(_potionTabButton, _currentMode == AlchemyMode.Potion ? _tabActiveColor : _tabInactiveColor);
        }

        private void SetButtonColor(Button btn, Color color)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = color;
        }

        private void UpdateIngredientSlotCount(AlchemyMode mode)
        {
            if (_ingredientSlots == null) return;

            int visibleCount = (mode == AlchemyMode.Cooking) ? 5 : 6;

            for (int i = 0; i < _ingredientSlots.Length; i++)
            {
                if (_ingredientSlots[i] != null)
                    _ingredientSlots[i].SetActive(i < visibleCount);
            }
        }

        private void UpdateStartButtonText(AlchemyMode mode)
        {
            if (_startButtonText == null) return;
            _startButtonText.text = (mode == AlchemyMode.Cooking) ? "🔥 요리 시작" : "🧪 제조 시작";
        }

        private void UpdateGradeTabVisibility(AlchemyMode mode)
        {
            if (_legendaryTabButton != null)
                _legendaryTabButton.gameObject.SetActive(mode == AlchemyMode.Cooking);
        }

        private void OnGradeTabClicked(int gradeIndex)
        {
            _currentGradeIndex = gradeIndex;
            RefreshRecipeList();
            Debug.Log($"[AlchemyPanel] 등급 탭 클릭: {gradeIndex}");
        }

        // ====== 레시피 리스트 ======

        private void RefreshRecipeList()
        {
            ClearRecipeCards();

            if (_currentMode == AlchemyMode.Cooking)
                SpawnCookingRecipes();
            else
                SpawnPotionRecipes();
        }

        private void ClearRecipeCards()
        {
            foreach (var card in _spawnedCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            _spawnedCards.Clear();
            _selectedCard = null;
        }

        private void SpawnCookingRecipes()
        {
            if (_recipeCardPrefab == null || _recipeListContent == null) return;

            int count = 0;
            foreach (var recipe in _cookingRecipes)
            {
                if (recipe == null || recipe.result == null) continue;

                if ((int)recipe.result.foodGrade != _currentGradeIndex) continue;

                var card = Instantiate(_recipeCardPrefab, _recipeListContent);
                card.SetupCookingRecipe(recipe, OnRecipeCardClicked);
                _spawnedCards.Add(card);
                count++;
            }

            UpdateRecipeCountText(count);
        }

        private void SpawnPotionRecipes()
        {
            if (_recipeCardPrefab == null || _recipeListContent == null) return;

            int count = 0;
            foreach (var recipe in _potionRecipes)
            {
                if (recipe == null || recipe.resultPotion == null) continue;

                if ((int)recipe.resultPotion.PotionGrade != _currentGradeIndex) continue;

                var card = Instantiate(_recipeCardPrefab, _recipeListContent);
                card.SetupPotionRecipe(recipe, OnRecipeCardClicked);
                _spawnedCards.Add(card);
                count++;
            }

            UpdateRecipeCountText(count);
        }

        private void UpdateRecipeCountText(int count)
        {
            if (_recipeCountText != null)
                _recipeCountText.text = $"{count}종";
        }

        private void OnRecipeCardClicked(object recipeData)
        {
            if (_selectedCard != null)
                _selectedCard.SetHighlighted(false);

            Debug.Log($"[AlchemyPanel] 레시피 클릭됨: {recipeData}");
        }

        // ====== 재료 인벤토리 ======

        private void RefreshInventory()
        {
            ClearInventorySlots();

            if (PlayerInventory.Instance == null)
            {
                Debug.LogWarning("[AlchemyPanel] PlayerInventory.Instance 없음");
                return;
            }

            int totalCount = 0;

            foreach (var slot in PlayerInventory.Instance.InventorySlots)
            {
                if (slot == null || slot.ItemData == null) continue;

                // 재료만 필터링
                var material = slot.ItemData as MaterialItemData;
                if (material == null) continue;

                // 요리 결과물이나 포션은 인벤토리에서 제외 (재료 아님)
                if (material is CookedFoodItemData) continue;
                if (material is PotionItemData) continue;

                SpawnInventorySlot(material, slot.Quantity);
                totalCount += slot.Quantity;
            }

            if (_inventoryCountText != null)
                _inventoryCountText.text = totalCount.ToString();

            Debug.Log($"[AlchemyPanel] 재료 인벤토리 갱신: {_spawnedInventorySlots.Count}종, 총 {totalCount}개");
        }

        private void ClearInventorySlots()
        {
            foreach (var slot in _spawnedInventorySlots)
            {
                if (slot != null) Destroy(slot.gameObject);
            }
            _spawnedInventorySlots.Clear();
        }

        private void SpawnInventorySlot(MaterialItemData material, int quantity)
        {
            if (_materialSlotPrefab == null || _inventoryContent == null) return;

            var slot = Instantiate(_materialSlotPrefab, _inventoryContent);
            slot.Setup(material, quantity, OnMaterialSlotClicked);
            _spawnedInventorySlots.Add(slot);
        }

        private void OnMaterialSlotClicked(MaterialItemData material)
        {
            Debug.Log($"[AlchemyPanel] 재료 클릭됨: {material?.itemName}");
        }
    }
}