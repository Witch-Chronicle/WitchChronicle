using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class SeedSelectPopup : MonoBehaviour
{
    [Header("팝업")]
    public GameObject popupPanel;
    public Button closeButton;

    [Header("탭")]
    public Button cropTabButton;
    public Button herbTabButton;
    public Button rareTabButton;
    public Color activeTabColor = new Color(0.66f, 0.84f, 0.48f, 1f);
    public Color inactiveTabColor = new Color(0.85f, 0.78f, 0.65f, 1f);

    [Header("씨앗 그리드")]
    public Transform seedGridContainer;
    public GameObject seedCardPrefab;

    [Header("상세 정보")]
    public Image detailIcon;
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailDescriptionText;
    public TextMeshProUGUI growthTimeText;
    public TextMeshProUGUI sellPriceText;

    [Header("심기 버튼")]
    public Button plantButton;
    public TextMeshProUGUI plantButtonText;

    [Header("씨앗 데이터")]
    public List<SeedItemData> allSeeds;

    private int targetSlotIndex;
    private SeedCategory currentCategory = SeedCategory.Jagmul;
    private SeedItemData selectedSeed;
    private List<SeedCardUI> spawnedCards = new List<SeedCardUI>();

    void Start()
    {
        closeButton.onClick.AddListener(Hide);
        cropTabButton.onClick.AddListener(() => SwitchCategory(SeedCategory.Jagmul));
        herbTabButton.onClick.AddListener(() => SwitchCategory(SeedCategory.Yakcho));
        rareTabButton.onClick.AddListener(() => SwitchCategory(SeedCategory.Rare));
        plantButton.onClick.AddListener(OnPlantClicked);

        ClearDetail();
        popupPanel.SetActive(false);
    }

    public void Show(int slotIndex)
    {
        targetSlotIndex = slotIndex;
        popupPanel.SetActive(true);
        SwitchCategory(SeedCategory.Jagmul);
        ClearDetail();
    }

    public void Hide()
    {
        popupPanel.SetActive(false);
        selectedSeed = null;
    }

    void SwitchCategory(SeedCategory category)
    {
        currentCategory = category;
        UpdateTabVisuals();
        RefreshSeedGrid();
        ClearDetail();
    }

    void UpdateTabVisuals()
    {
        SetTabColor(cropTabButton, currentCategory == SeedCategory.Jagmul);
        SetTabColor(herbTabButton, currentCategory == SeedCategory.Yakcho);
        SetTabColor(rareTabButton, currentCategory == SeedCategory.Rare);
    }

    void SetTabColor(Button button, bool active)
    {
        Image img = button.GetComponent<Image>();
        if (img != null) img.color = active ? activeTabColor : inactiveTabColor;
    }

    void RefreshSeedGrid()
    {
        // 기존 카드 제거
        foreach (Transform child in seedGridContainer)
            Destroy(child.gameObject);
        spawnedCards.Clear();

        // 현재 카테고리 씨앗만 표시
        foreach (var seed in allSeeds)
        {
            if (seed == null || seed.seedData == null) continue;
            if (seed.seedData.category != currentCategory) continue;

            var cardObj = Instantiate(seedCardPrefab, seedGridContainer);
            var card = cardObj.GetComponent<SeedCardUI>();
            if (card != null)
            {
                card.Setup(seed, OnCardSelected);
                spawnedCards.Add(card);
            }
        }
    }

    void OnCardSelected(SeedItemData seed)
    {
        selectedSeed = seed;
        UpdateDetail(seed);

        // 카드 하이라이트 갱신
        foreach (var card in spawnedCards)
            card.SetSelected(card.SeedData == seed);
    }

    void UpdateDetail(SeedItemData seed)
{
    if (seed == null) { ClearDetail(); return; }

    if (detailIcon != null)
    {
        detailIcon.gameObject.SetActive(true);
        if (seed.icon != null) detailIcon.sprite = seed.icon;
    }
    if (detailNameText != null) detailNameText.text = seed.itemName;
    if (detailDescriptionText != null) detailDescriptionText.text = seed.description;
    if (growthTimeText != null)
    {
        int min = Mathf.FloorToInt(seed.seedData.growthTime / 60f);
        growthTimeText.text = $"성장 시간: {min}분";
    }
    if (sellPriceText != null) sellPriceText.text = $"판매 가격: {seed.sellPrice}G";
    if (plantButton != null) plantButton.interactable = true;
    if (plantButtonText != null) plantButtonText.text = "심기";
}

    void ClearDetail()
{
    selectedSeed = null;

    if (detailIcon != null) detailIcon.gameObject.SetActive(false);
    if (detailNameText != null) detailNameText.text = "";
    if (detailDescriptionText != null) detailDescriptionText.text = "씨앗을 선택하세요.";
    if (growthTimeText != null) growthTimeText.text = "";
    if (sellPriceText != null) sellPriceText.text = "";
    if (plantButton != null) plantButton.interactable = false;
    if (plantButtonText != null) plantButtonText.text = "심기";
}

    void OnPlantClicked()
    {
        if (selectedSeed == null) return;

        // 보유 개수 확인
        int owned = GetOwnedCount(selectedSeed);
        if (owned <= 0)
        {
            Debug.LogWarning($"{selectedSeed.itemName} 부족");
            return;
        }

        // 심기
        bool success = FarmingManager.Instance.PlantSeed(targetSlotIndex, selectedSeed);
        if (success)
        {
            // TODO: PlayerInventory.Instance.ConsumeItem(selectedSeed, 1) - 3번 메서드 필요
            Debug.Log($"슬롯 {targetSlotIndex}에 {selectedSeed.itemName} 심기 완료");
            Hide();
        }
    }

    public static int GetOwnedCount(SeedItemData seed)
    {
        if (PlayerInventory.Instance == null || seed == null) return 0;
        return PlayerInventory.Instance.InventorySlots
            .Where(s => s.ItemData == seed)
            .Sum(s => s.Quantity);
    }
}