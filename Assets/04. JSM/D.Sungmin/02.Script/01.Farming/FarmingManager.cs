using UnityEngine;
using System.Collections.Generic;
using System;

public class FarmingManager : MonoBehaviour
{
    public static FarmingManager Instance;

    public int maxSlots = 8;
    public int initialSlots = 2;
    public int[] unlockCosts = { 300, 700, 1500, 3000, 5000, 8000 };

    public List<FarmSlot> slots = new List<FarmSlot>();

    public event Action OnFarmUpdated;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        for (int i = 0; i < maxSlots; i++)
        {
            slots.Add(new FarmSlot { slotIndex = i, isUnlocked = i < initialSlots });
        }
    }

    void Update()
    {
        bool updated = false;
        foreach (var slot in slots)
        {
            if (slot.state == SlotState.Growing && slot.IsGrowthComplete())
            {
                slot.state = SlotState.Harvestable;
                updated = true;
            }
        }
        if (updated) OnFarmUpdated?.Invoke();
    }

    public bool PlantSeed(int slotIndex, SeedItemData seed)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return false;
        var slot = slots[slotIndex];
        if (!slot.isUnlocked || slot.state != SlotState.Empty) return false;

        slot.plantedSeed = seed;
        slot.plantedTime = DateTime.Now;
        slot.state = SlotState.Growing;
        OnFarmUpdated?.Invoke();
        return true;
    }

    public SeedItemData Harvest(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return null;
        var slot = slots[slotIndex];
        if (slot.state != SlotState.Harvestable) return null;

        var harvested = slot.plantedSeed;

        // 수확물 지급
        if (harvested != null && harvested.seedData != null && harvested.seedData.harvestItem != null)
        {
            // TODO: PlayerInventory.Instance.AddItem(harvested.seedData.harvestItem, harvested.seedData.harvestAmount) - 3번 팀원 메서드 확인 필요
            Debug.Log($"{harvested.seedData.harvestItem.itemName} x{harvested.seedData.harvestAmount} 획득!");
        }

        slot.plantedSeed = null;
        slot.state = SlotState.Empty;
        OnFarmUpdated?.Invoke();
        return harvested;
    }

    public bool UnlockSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return false;
        var slot = slots[slotIndex];
        if (slot.isUnlocked) return false;

        slot.isUnlocked = true;
        OnFarmUpdated?.Invoke();
        return true;
    }
}