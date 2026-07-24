using UnityEngine;
using System;

public enum SlotState { Empty, Growing, Harvestable }

[System.Serializable]
public class FarmSlot
{
    public int slotIndex;
    public SlotState state = SlotState.Empty;
    public SeedItemData plantedSeed;
    public DateTime plantedTime;
    public bool isUnlocked;

    public float GetRemainingTime()
    {
        if (state != SlotState.Growing || plantedSeed == null) return 0f;
        double elapsed = (DateTime.Now - plantedTime).TotalSeconds;
        return Mathf.Max(0f, plantedSeed.seedData.growthTime - (float)elapsed);
    }

    public float GetGrowthProgress()
    {
        if (state == SlotState.Harvestable) return 1f;
        if (state != SlotState.Growing || plantedSeed == null) return 0f;
        double elapsed = (DateTime.Now - plantedTime).TotalSeconds;
        return Mathf.Clamp01((float)elapsed / plantedSeed.seedData.growthTime);
    }

    public bool IsGrowthComplete()
    {
        if (state != SlotState.Growing || plantedSeed == null) return false;
        double elapsed = (DateTime.Now - plantedTime).TotalSeconds;
        return elapsed >= plantedSeed.seedData.growthTime;
    }

    public Sprite GetCurrentStageSprite()
    {
        if (plantedSeed == null || plantedSeed.seedData == null) return null;

        var data = plantedSeed.seedData;

        if (state == SlotState.Harvestable)
            return data.harvestSprite;

        float progress = GetGrowthProgress();
        if (progress < 0.5f)
            return data.seedSprite;
        else
            return data.sproutSprite;
    }
}