using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FarmSlotUI : MonoBehaviour
{
    [Header("UI 요소")]
    public Image slotBackground;
    public Image slotImage;
    public TextMeshProUGUI stateText;
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI costText;
    public Button slotButton;
    public GameObject lockedOverlay;

    [Header("성장 게이지")]
    public GameObject progressBarRoot;
    public Image progressBarFill;

    [Header("색상")]
    public Color emptyColor = new Color(0.96f, 0.92f, 0.85f);
    public Color growingColor = new Color(0.83f, 0.91f, 0.72f);
    public Color harvestColor = new Color(0.66f, 0.84f, 0.48f);
    public Color lockedColor = new Color(0.75f, 0.72f, 0.68f);

    private int slotIndex;
    private FarmSlot slotData;

    public void Setup(int index)
    {
        slotIndex = index;
        slotButton.onClick.AddListener(OnClicked);
        FarmingManager.Instance.OnFarmUpdated += UpdateUI;
        UpdateUI();
    }

    void Update()
    {
        if (slotData != null && slotData.state == SlotState.Growing)
        {
            float remaining = slotData.GetRemainingTime();
            int min = Mathf.FloorToInt(remaining / 60f);
            int sec = Mathf.FloorToInt(remaining % 60f);
            infoText.text = $"성장 중... {min:00}:{sec:00}";

            if (progressBarFill != null)
                progressBarFill.fillAmount = slotData.GetGrowthProgress();

            // 성장 단계에 따라 이미지 실시간 교체
            if (slotImage != null)
            {
                Sprite currentSprite = slotData.GetCurrentStageSprite();
                if (currentSprite != null)
                {
                    slotImage.sprite = currentSprite;
                    slotImage.enabled = true;
                }
            }
        }
    }

    public void UpdateUI()
    {
        slotData = FarmingManager.Instance.slots[slotIndex];

        if (lockedOverlay != null) lockedOverlay.SetActive(!slotData.isUnlocked);
        if (costText != null) costText.gameObject.SetActive(!slotData.isUnlocked);

        // 게이지 바는 성장 중일 때만 표시
        if (progressBarRoot != null)
            progressBarRoot.SetActive(slotData.isUnlocked && slotData.state == SlotState.Growing);

        if (!slotData.isUnlocked)
        {
            stateText.text = "잠긴 밭";
            infoText.text = "이 밭은 잠겨 있습니다.";
            slotBackground.color = lockedColor;

            if (slotImage != null) slotImage.enabled = false;

            int idx = slotIndex - FarmingManager.Instance.initialSlots;
            if (idx >= 0 && idx < FarmingManager.Instance.unlockCosts.Length)
                costText.text = $"{FarmingManager.Instance.unlockCosts[idx]}G";
            return;
        }

        switch (slotData.state)
        {
            case SlotState.Empty:
                stateText.text = "비어있음";
                infoText.text = "아직 씨앗을 심지 않았어요.";
                slotBackground.color = emptyColor;
                if (slotImage != null)
                {
                    slotImage.sprite = null;
                    slotImage.enabled = false;
                }
                break;

            case SlotState.Growing:
                stateText.text = slotData.plantedSeed.seedData.harvestName;
                slotBackground.color = growingColor;
                if (progressBarFill != null)
                    progressBarFill.fillAmount = slotData.GetGrowthProgress();
                if (slotImage != null)
                {
                    Sprite s = slotData.GetCurrentStageSprite();
                    if (s != null)
                    {
                        slotImage.sprite = s;
                        slotImage.enabled = true;
                    }
                }
                break;

            case SlotState.Harvestable:
                stateText.text = slotData.plantedSeed.seedData.harvestName;
                infoText.text = "수확 가능!";
                slotBackground.color = harvestColor;
                if (slotImage != null)
                {
                    slotImage.sprite = slotData.plantedSeed.seedData.harvestSprite;
                    slotImage.enabled = (slotImage.sprite != null);
                }
                break;
        }
    }

    void OnClicked()
    {
        slotData = FarmingManager.Instance.slots[slotIndex];

        if (!slotData.isUnlocked)
        {
            FarmUIManager.Instance.OpenUnlockPopup(slotIndex);
            return;
        }

        switch (slotData.state)
        {
            case SlotState.Empty:
                FarmUIManager.Instance.OpenSeedSelect(slotIndex);
                break;
            case SlotState.Harvestable:
                var harvested = FarmingManager.Instance.Harvest(slotIndex);
                if (harvested != null) Debug.Log($"수확: {harvested.seedData.harvestName}");
                break;
        }
    }

    void OnDestroy()
    {
        if (FarmingManager.Instance != null)
            FarmingManager.Instance.OnFarmUpdated -= UpdateUI;
    }
}