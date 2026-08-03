using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
namespace WitchChronicle.Alchemy
{
    /// <summary>
    /// 가마솥 UI 패널 (요리/포션 겸용).
    /// 모드 탭 클릭 시 3D 가마솥 + UI 요소가 함께 스왑됨.
    /// </summary>
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
        [SerializeField] private AlchemyIngredientSlot[] _ingredientSlots;

        [Header("완성 예상 표시")]
        [SerializeField] private GameObject _previewRoot;
        [SerializeField] private Image _previewIcon;
        [SerializeField] private TextMeshProUGUI _previewNameText;
        [SerializeField] private GameObject _previewFailIcon;

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

        [Header("성공 팝업")]
[SerializeField] private AlchemySuccessPopup _successPopup;

[Header("애니메이션 & 지연")]
[SerializeField] private Animator _playerAnimator;
[SerializeField] private string _cookingTriggerName = "Cook";
[SerializeField] private string _potionTriggerName = "Brew";

[Header("나가기 버튼")]
[SerializeField] private Button _closeButton;
[SerializeField] private float _resultDelay = 3f;
        private Action _onClosedCallback;
        private AlchemyMode _currentMode;
        private int _currentGradeIndex = 0;

        private List<RecipeCard> _spawnedCards = new List<RecipeCard>();
        private RecipeCard _selectedCard;

        private List<MaterialSlot> _spawnedInventorySlots = new List<MaterialSlot>();

        private CookingRecipeData _matchedCookingRecipe;
        private PotionRecipeData _matchedPotionRecipe;

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

            if (_ingredientSlots != null)
            {
                foreach (var slot in _ingredientSlots)
                {
                    if (slot != null)
                        slot.OnSlotChanged += OnIngredientSlotChanged;
                }
            }

            if (_startButton != null)
                _startButton.onClick.AddListener(OnStartButtonClicked);
                if (_closeButton != null)
    _closeButton.onClick.AddListener(Close);
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

    // 나가기 시 요리 가마솥만 기본으로 표시 (MainField의 기본 상태로)
    if (_cookingCauldron != null) _cookingCauldron.SetActive(true);
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
            ClearAllIngredientSlots();

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
                    _ingredientSlots[i].gameObject.SetActive(i < visibleCount);
            }
        }

        private void UpdateStartButtonText(AlchemyMode mode)
        {
            if (_startButtonText == null) return;
            _startButtonText.text = (mode == AlchemyMode.Cooking) ? " 요리 시작" : " 제조 시작";
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

        // ====== 재료 슬롯 관리 ======

        private void ClearAllIngredientSlots()
        {
            if (_ingredientSlots == null) return;

            for (int i = 0; i < _ingredientSlots.Length; i++)
            {
                if (_ingredientSlots[i] != null)
                    _ingredientSlots[i].ClearSlot();
            }
        }

        /// <summary>
        /// 인벤 재료 클릭 시 → 같은 재료가 이미 있으면 스택, 없으면 첫 번째 빈 슬롯에 담기
        /// </summary>
        /// <summary>
/// 인벤 재료 클릭 시 → 인벤 개수 검사 후 슬롯에 담기
/// </summary>
private void TryPlaceMaterialInSlot(MaterialItemData material)
{
    if (material == null || _ingredientSlots == null) return;
    if (PlayerInventory.Instance == null) return;

    // 인벤 보유량 확인
    int ownedInInventory = PlayerInventory.Instance.GetTotalQuantity(material);

    // 이미 슬롯에 담긴 이 재료 개수 합산
    int alreadyInSlots = GetMaterialCountInSlots(material);

    // 인벤 개수 초과 시 담기 거부
    if (alreadyInSlots >= ownedInInventory)
    {
        Debug.LogWarning($"[AlchemyPanel] {material.itemName} 인벤 부족 (보유:{ownedInInventory}, 슬롯:{alreadyInSlots})");
        return;
    }

    int visibleCount = (_currentMode == AlchemyMode.Cooking) ? 5 : 6;

    // 1단계: 같은 재료가 이미 있으면 그 슬롯에 개수 +1
    for (int i = 0; i < visibleCount && i < _ingredientSlots.Length; i++)
    {
        var slot = _ingredientSlots[i];
        if (slot == null) continue;
        if (slot.IsEmpty) continue;

        if (slot.CurrentMaterial == material)
        {
            slot.SetMaterial(material, slot.CurrentCount + 1);
            Debug.Log($"[AlchemyPanel] 재료 스택: {material.itemName} → Slot {i} (총 {slot.CurrentCount}개)");
            return;
        }
    }

    // 2단계: 없으면 첫 번째 빈 슬롯에 담기
    for (int i = 0; i < visibleCount && i < _ingredientSlots.Length; i++)
    {
        var slot = _ingredientSlots[i];
        if (slot == null) continue;

        if (slot.IsEmpty)
        {
            slot.SetMaterial(material, 1);
            Debug.Log($"[AlchemyPanel] 재료 담김: {material.itemName} → Slot {i}");
            return;
        }
    }

    Debug.Log("[AlchemyPanel] 빈 슬롯 없음");
}

/// <summary>
/// 특정 재료가 지금 재료 슬롯들에 얼마나 담겨있는지 합산
/// </summary>
private int GetMaterialCountInSlots(MaterialItemData material)
{
    if (material == null || _ingredientSlots == null) return 0;

    int total = 0;
    foreach (var slot in _ingredientSlots)
    {
        if (slot == null || slot.IsEmpty) continue;
        if (!slot.gameObject.activeSelf) continue;
        if (slot.CurrentMaterial != material) continue;

        total += slot.CurrentCount;
    }
    return total;
}

        private void OnIngredientSlotChanged()
        {
            RefreshRecipeMatch();
        }

        // ====== 레시피 매칭 ======

        private void RefreshRecipeMatch()
        {
            _matchedCookingRecipe = null;
            _matchedPotionRecipe = null;

            var slotMaterials = GetSlotMaterials();
            if (slotMaterials.Count == 0)
            {
                UpdatePreview(null, null);
                return;
            }

            if (_currentMode == AlchemyMode.Cooking)
            {
                _matchedCookingRecipe = FindMatchingCookingRecipe(slotMaterials);
                UpdatePreview(_matchedCookingRecipe?.result?.icon, _matchedCookingRecipe?.result?.itemName);
            }
            else
            {
                _matchedPotionRecipe = FindMatchingPotionRecipe(slotMaterials);
                UpdatePreview(_matchedPotionRecipe?.resultPotion?.icon, _matchedPotionRecipe?.resultPotion?.itemName);
            }
        }

        private Dictionary<MaterialItemData, int> GetSlotMaterials()
        {
            var result = new Dictionary<MaterialItemData, int>();
            if (_ingredientSlots == null) return result;

            foreach (var slot in _ingredientSlots)
            {
                if (slot == null || slot.IsEmpty) continue;
                if (!slot.gameObject.activeSelf) continue;

                if (result.ContainsKey(slot.CurrentMaterial))
                    result[slot.CurrentMaterial] += slot.CurrentCount;
                else
                    result[slot.CurrentMaterial] = slot.CurrentCount;
            }

            return result;
        }

        private CookingRecipeData FindMatchingCookingRecipe(Dictionary<MaterialItemData, int> slotMaterials)
        {
            foreach (var recipe in _cookingRecipes)
            {
                if (recipe == null) continue;
                if (IsRecipeMatch(recipe.ingredients, slotMaterials))
                    return recipe;
            }
            return null;
        }

        private PotionRecipeData FindMatchingPotionRecipe(Dictionary<MaterialItemData, int> slotMaterials)
        {
            foreach (var recipe in _potionRecipes)
            {
                if (recipe == null) continue;
                if (IsRecipeMatch(recipe.ingredients, slotMaterials))
                    return recipe;
            }
            return null;
        }

        private bool IsRecipeMatch(List<IngredientSlot> recipeIngredients, Dictionary<MaterialItemData, int> slotMaterials)
        {
            if (recipeIngredients == null) return false;

            var recipeMap = new Dictionary<MaterialItemData, int>();
            foreach (var ing in recipeIngredients)
            {
                if (ing == null || ing.specificItem == null) continue;

                if (recipeMap.ContainsKey(ing.specificItem))
                    recipeMap[ing.specificItem] += ing.amount;
                else
                    recipeMap[ing.specificItem] = ing.amount;
            }

            if (recipeMap.Count != slotMaterials.Count) return false;

            foreach (var kvp in recipeMap)
            {
                if (!slotMaterials.TryGetValue(kvp.Key, out int slotCount))
                    return false;
                if (slotCount != kvp.Value)
                    return false;
            }

            return true;
        }

        // ====== 완성 예상 표시 ======

        private void UpdatePreview(Sprite icon, string name)
        {
            bool hasMatch = icon != null;

            if (_previewRoot != null)
                _previewRoot.SetActive(true);

            if (_previewIcon != null)
            {
                _previewIcon.gameObject.SetActive(hasMatch);
                if (hasMatch)
                {
                    _previewIcon.sprite = icon;
                    _previewIcon.preserveAspect = true;
                }
            }

            if (_previewNameText != null)
            {
                _previewNameText.gameObject.SetActive(hasMatch);
                if (hasMatch) _previewNameText.text = name;
            }

            if (_previewFailIcon != null)
                _previewFailIcon.SetActive(!hasMatch && HasAnySlotFilled());

            if (_startButton != null)
                _startButton.interactable = hasMatch;
        }

        private bool HasAnySlotFilled()
        {
            if (_ingredientSlots == null) return false;
            foreach (var slot in _ingredientSlots)
            {
                if (slot != null && slot.gameObject.activeSelf && !slot.IsEmpty)
                    return true;
            }
            return false;
        }

        // ====== 요리 시작 버튼 ======

        private void OnStartButtonClicked()
        
        {
            Debug.Log($"[AlchemyPanel] 시작 버튼 클릭! 모드={_currentMode}, 요리매칭={_matchedCookingRecipe != null}, 포션매칭={_matchedPotionRecipe != null}");
    
            if (_currentMode == AlchemyMode.Cooking && _matchedCookingRecipe != null)
            {
                ExecuteCookingRecipe(_matchedCookingRecipe);
            }
            else if (_currentMode == AlchemyMode.Potion && _matchedPotionRecipe != null)
            {
                ExecutePotionRecipe(_matchedPotionRecipe);
            }
        }

        private void ExecuteCookingRecipe(CookingRecipeData recipe)
{
    if (recipe == null || recipe.result == null) return;
    if (PlayerInventory.Instance == null) return;

    if (!HasEnoughMaterials(recipe.ingredients))
    {
        Debug.LogWarning("[AlchemyPanel] 재료 부족");
        return;
    }

    ConsumeIngredients(recipe.ingredients);

    PlayerInventory.Instance.AddItem(recipe.result, 1);
    PlayerInventory.Instance.RaiseInventoryChanged();

    Debug.Log($"[AlchemyPanel] 요리 완성: {recipe.result.itemName}");

    ClearAllIngredientSlots();
    RefreshInventory();

    // 애니메이션 재생 + 지연 후 팝업
    StartCoroutine(Co_ShowResultAfterAnimation(recipe.result.icon, AlchemyMode.Cooking, _cookingTriggerName));
}

private void ExecutePotionRecipe(PotionRecipeData recipe)
{
    if (recipe == null || recipe.resultPotion == null) return;
    if (PlayerInventory.Instance == null) return;

    if (!HasEnoughMaterials(recipe.ingredients))
    {
        Debug.LogWarning("[AlchemyPanel] 재료 부족");
        return;
    }

    ConsumeIngredients(recipe.ingredients);

    PlayerInventory.Instance.AddItem(recipe.resultPotion, 1);
    PlayerInventory.Instance.RaiseInventoryChanged();

    Debug.Log($"[AlchemyPanel] 포션 완성: {recipe.resultPotion.itemName}");

    ClearAllIngredientSlots();
    RefreshInventory();

    // 애니메이션 재생 + 지연 후 팝업
    StartCoroutine(Co_ShowResultAfterAnimation(recipe.resultPotion.icon, AlchemyMode.Potion, _potionTriggerName));
}

private IEnumerator Co_ShowResultAfterAnimation(Sprite resultIcon, AlchemyMode mode, string triggerName)
{
    // 시작 버튼 잠금 (연타 방지)
    if (_startButton != null) _startButton.interactable = false;

    // 캐릭터 애니메이션 재생
    if (_playerAnimator != null && !string.IsNullOrEmpty(triggerName))
    {
        _playerAnimator.SetTrigger(triggerName);
        Debug.Log($"[AlchemyPanel] 애니메이션 트리거: {triggerName}");
    }

    // 결과 팝업 대기
    yield return new WaitForSeconds(_resultDelay);

    // 성공 팝업 표시
    if (_successPopup != null)
        _successPopup.Show(resultIcon, mode, null);
}
        private bool HasEnoughMaterials(List<IngredientSlot> ingredients)
        {
            if (ingredients == null) return false;
            if (PlayerInventory.Instance == null) return false;

            var required = new Dictionary<MaterialItemData, int>();
            foreach (var ing in ingredients)
            {
                if (ing == null || ing.specificItem == null) continue;

                if (required.ContainsKey(ing.specificItem))
                    required[ing.specificItem] += ing.amount;
                else
                    required[ing.specificItem] = ing.amount;
            }

            foreach (var kvp in required)
            {
                int owned = PlayerInventory.Instance.GetTotalQuantity(kvp.Key);
                if (owned < kvp.Value)
                {
                    Debug.LogWarning($"[AlchemyPanel] 재료 부족: {kvp.Key.itemName} 필요 {kvp.Value}, 보유 {owned}");
                    return false;
                }
            }

            return true;
        }

        private void ConsumeIngredients(List<IngredientSlot> ingredients)
        {
            if (ingredients == null) return;
            if (PlayerInventory.Instance == null) return;

            var toConsume = new Dictionary<MaterialItemData, int>();
            foreach (var ing in ingredients)
            {
                if (ing == null || ing.specificItem == null) continue;

                if (toConsume.ContainsKey(ing.specificItem))
                    toConsume[ing.specificItem] += ing.amount;
                else
                    toConsume[ing.specificItem] = ing.amount;
            }

            foreach (var kvp in toConsume)
            {
                PlayerInventory.Instance.TryConsumeItem(kvp.Key, kvp.Value);
            }
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

    // 레시피 타입에 따라 자동 담기
    if (recipeData is CookingRecipeData cookingRecipe)
    {
        AutoFillIngredients(cookingRecipe.ingredients);
        Debug.Log($"[AlchemyPanel] 레시피 자동 담기: {cookingRecipe.result?.itemName}");
    }
    else if (recipeData is PotionRecipeData potionRecipe)
    {
        AutoFillIngredients(potionRecipe.ingredients);
        Debug.Log($"[AlchemyPanel] 포션 레시피 자동 담기: {potionRecipe.resultPotion?.itemName}");
    }
}

/// <summary>
/// 레시피의 재료를 재료 슬롯에 자동 배치.
/// 슬롯 먼저 다 비운 후 필요한 재료를 순서대로 담음.
/// 인벤 부족 시 담을 수 있는 만큼만.
/// </summary>
private void AutoFillIngredients(List<IngredientSlot> ingredients)
{
    if (ingredients == null || _ingredientSlots == null) return;
    if (PlayerInventory.Instance == null) return;

    // 기존 슬롯 다 비우기
    ClearAllIngredientSlots();

    // 재료별 필요 개수 병합 (같은 재료 여러 슬롯이면 합침)
    var required = new Dictionary<MaterialItemData, int>();
    foreach (var ing in ingredients)
    {
        if (ing == null || ing.specificItem == null) continue;

        if (required.ContainsKey(ing.specificItem))
            required[ing.specificItem] += ing.amount;
        else
            required[ing.specificItem] = ing.amount;
    }

    // 각 재료를 슬롯에 배치
    int slotIndex = 0;
    int visibleCount = (_currentMode == AlchemyMode.Cooking) ? 5 : 6;

    foreach (var kvp in required)
    {
        if (slotIndex >= visibleCount) break; // 슬롯 부족

        var material = kvp.Key;
        int needed = kvp.Value;

        // 인벤 개수와 비교해서 실제 담을 수 있는 만큼만
        int owned = PlayerInventory.Instance.GetTotalQuantity(material);
        int toPlace = Mathf.Min(needed, owned);

        if (toPlace <= 0)
        {
            Debug.LogWarning($"[AlchemyPanel] {material.itemName} 인벤 없음 (필요:{needed})");
            slotIndex++;
            continue;
        }

        // 슬롯에 배치
        if (_ingredientSlots[slotIndex] != null)
        {
            _ingredientSlots[slotIndex].SetMaterial(material, toPlace);

            if (toPlace < needed)
                Debug.LogWarning($"[AlchemyPanel] {material.itemName} 부족 (필요:{needed}, 담김:{toPlace})");
            else
                Debug.Log($"[AlchemyPanel] {material.itemName} x{toPlace} → Slot {slotIndex}");
        }

        slotIndex++;
    }
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

                var material = slot.ItemData as MaterialItemData;
                if (material == null) continue;

                if (material is CookedFoodItemData) continue;
                if (material is PotionItemData) continue;

                SpawnInventorySlot(material, slot.Quantity);
                totalCount += slot.Quantity;
            }

            if (_inventoryCountText != null)
                _inventoryCountText.text = totalCount.ToString();
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
            TryPlaceMaterialInSlot(material);
        }
    }
}