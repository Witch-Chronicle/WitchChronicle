using System.Collections.Generic;
using UnityEngine;
using System;
/// <summary>
/// 장비 장착/해제를 담당하고, 장착 중인 장비들의 스탯을 합산해서 StatBlock으로 공개하는 컨트롤러.
/// - 슬롯당 하나씩(EquipSlotType 8종) EquipmentInstance를 들고 있음
/// - 장착 중인 장비가 판매 등으로 PlayerInventory에서 사라지면 자동으로 해제 (OnInventoryChanged 구독)
/// * 이 스크립트는 "장비 장착 + 장비발 스탯 합산"만 담당. 최종 캐릭터 스탯 계산은 StatController가 담당.
/// </summary>
public class EquipmentController : MonoBehaviour
{
    private readonly Dictionary<EquipSlotType, EquipmentInstance> _equipped = new Dictionary<EquipSlotType, EquipmentInstance>();

    private StatBlock _totalStats = new StatBlock();

    /// <summary>
    /// 현재 장착 중인 장비들의 스탯 총합. StatController가 이 값을 그대로 가져다 쓰면 됨.
    /// </summary>
    public StatBlock TotalEquipmentStats => _totalStats;

    /// <summary>
    /// 장착/해제 등으로 TotalEquipmentStats가 바뀔 때마다 호출됨. StatController가 구독해서 갱신하는 용도.
    /// </summary>
    public event Action OnEquipmentChanged;

    private void OnEnable()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged += HandleInventoryChanged;
        }
    }

    private void OnDisable()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    /// <summary>
    /// 장비 장착. instance.baseData.equipSlotType을 보고 슬롯을 자동으로 판단.
    /// 해당 슬롯에 이미 다른 장비가 있으면 자동으로 해제 후 장착.
    /// </summary>
    public void Equip(EquipmentInstance instance)
    {
        if (instance == null || instance.baseData == null) return;

        EquipSlotType slot = instance.baseData.equipSlotType;

        // 이미 그 슬롯에 같은 장비가 장착되어 있으면 아무것도 안 함
        if (_equipped.TryGetValue(slot, out var current) && current == instance)
        {
            return;
        }

        _equipped[slot] = instance;
        RecalculateTotalStats();
    }

    /// <summary>
    /// 지정한 슬롯의 장비를 해제.
    /// </summary>
    public void Unequip(EquipSlotType slot)
    {
        if (_equipped.Remove(slot))
        {
            RecalculateTotalStats();
        }
    }

    /// <summary>
    /// 지정한 슬롯에 장착된 장비 반환. 없으면 null.
    /// </summary>
    public EquipmentInstance GetEquipped(EquipSlotType slot)
    {
        return _equipped.TryGetValue(slot, out var instance) ? instance : null;
    }

    /// <summary>
    /// 이 장비 개체가 지금 어딘가에 장착되어 있는지 여부.
    /// </summary>
    public bool IsEquipped(EquipmentInstance instance)
    {
        return instance != null && _equipped.ContainsValue(instance);
    }

    /// <summary>
    /// PlayerInventory 내용이 바뀔 때마다 호출됨 (구매/판매/강화 등).
    /// 장착 중인 장비가 인벤토리에서 사라졌으면(판매 등) 자동으로 해제.
    /// </summary>
    private void HandleInventoryChanged()
    {
        if (PlayerInventory.Instance == null) return;

        List<EquipSlotType> slotsToUnequip = null;

        foreach (var kvp in _equipped)
        {
            bool stillOwned = false;

            foreach (var owned in PlayerInventory.Instance.EquipmentInstances)
            {
                if (owned == kvp.Value)
                {
                    stillOwned = true;
                    break;
                }
            }

            if (!stillOwned)
            {
                slotsToUnequip ??= new List<EquipSlotType>();
                slotsToUnequip.Add(kvp.Key);
            }
        }

        if (slotsToUnequip == null) return;

        foreach (var slot in slotsToUnequip)
        {
            _equipped.Remove(slot);
        }

        RecalculateTotalStats();
    }

    /// <summary>
    /// 장착 중인 장비 전체를 순회하며 스탯 합산. 우리 쪽 필드명(hp/mp/spellPower...) -> StatType 매핑이 여기서만 일어남.
    /// </summary>
    private void RecalculateTotalStats()
    {
        var newTotal = new StatBlock();

        foreach (var instance in _equipped.Values)
        {
            EquipStatCalculator.StatSet stats = instance.cachedStats;

            newTotal.Add(StatType.MaxHP, stats.hp);
            newTotal.Add(StatType.MaxMP, stats.mp);
            newTotal.Add(StatType.SpellPower, stats.spellPower);
            newTotal.Add(StatType.Intelligence, stats.intelligence);
            newTotal.Add(StatType.Defense, stats.defense);
            newTotal.Add(StatType.Speed, stats.speed);
            newTotal.Add(StatType.Luck, stats.luck);
        }

        _totalStats = newTotal;

        Debug.Log($"[EquipmentController] 장착 스탯 갱신 - HP:{_totalStats.maxHP} MP:{_totalStats.maxMP} " +
            $"마력:{_totalStats.magicPower} 지능:{_totalStats.intelligence} 방어력:{_totalStats.defense} " +
            $"속도:{_totalStats.speed} 행운:{_totalStats.luck}");

        OnEquipmentChanged?.Invoke();
    }
}