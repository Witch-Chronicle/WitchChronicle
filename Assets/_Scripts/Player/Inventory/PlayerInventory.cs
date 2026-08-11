using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// - 보유 골드와 보유 아이템(+수량)을 인스펙터에서 확인 가능
/// - 순수 자원 저장소 역할만 담당. 구매 판정/규칙은 ShopController, 강화 판정/규칙은 EnhanceController가 담당
/// - 소비/재료/씨앗/퀘스트 아이템은 수량 기반(InventorySlot)으로,
///   장비는 개체마다 강화 단계가 다를 수 있어 개별 인스턴스(EquipmentInstance)로 관리
/// * 패널 열기/닫기 입력 처리는 PlayerUIInputReader가 담당 (여기서는 순수 자원 로직만)
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [Serializable]
    public class InventorySlot
    {
        public ItemData ItemData;
        public int Quantity;

        public InventorySlot(ItemData itemData, int quantity)
        {
            ItemData = itemData;
            Quantity = quantity;
        }
    }

    [Header("보유 골드 (테스트용 초기값)")]
    [SerializeField] private int _gold = 1000;

    [Header("보유 아이템 목록 (확인용) - 소비/재료/씨앗/퀘스트")]
    [SerializeField] private List<InventorySlot> _inventorySlots = new List<InventorySlot>();

    [Header("보유 장비 목록 (확인용) - 개체별로 강화 단계가 다를 수 있음")]
    [SerializeField] private List<EquipmentInstance> _equipmentInstances = new List<EquipmentInstance>();

    public int Gold => _gold;

    // InventoryUIController가 목록을 읽어갈 수 있도록 읽기 전용으로 공개
    public IReadOnlyList<InventorySlot> InventorySlots => _inventorySlots;

    // 장비는 별도 리스트로 공개 (개체별 강화 단계 보존)
    public IReadOnlyList<EquipmentInstance> EquipmentInstances => _equipmentInstances;

    /// <summary>
    /// 골드가 변경될 때마다 호출됨. UI 쪽에서 구독해서 실시간으로 텍스트 갱신하는 용도.
    /// </summary>
    public event Action<int> OnGoldChanged;

    /// <summary>
    /// 보유 아이템 목록이 변경될 때마다 호출됨 (구매/판매 등). UI 쪽에서 구독해서 목록 갱신하는 용도.
    /// </summary>
    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// 특정 아이템을 총 몇 개 보유하고 있는지 (여러 슬롯에 나뉘어 있으면 합산)
    /// * 장비(EquipItemData)는 이 메서드로 세지 않음 - EquipmentInstances 쪽을 따로 확인할 것
    /// </summary>
    public int GetTotalQuantity(ItemData itemData)
    {
        if (itemData == null) return 0;

        return _inventorySlots
            .Where(slot => slot.ItemData == itemData)
            .Sum(slot => slot.Quantity);
    }


    /// <summary>
    /// 아이템 판매 시도. 보유 수량이 충분하고 canSell이 true면 수량만큼 차감하고 판매 금액을 골드에 더한다.
    /// * 장비(EquipItemData)는 이 메서드로 팔지 않음 - TrySellEquipment() 사용
    /// </summary>
    /// <returns>판매 성공 여부</returns>
    public bool TrySell(ItemData itemData, int amount)
    {
        if (itemData == null || amount <= 0)
        {
            return false;
        }

        if (!itemData.canSell)
        {
            Debug.Log($"[PlayerInventory] 판매 불가 아이템: {itemData.itemName}");
            return false;
        }

        int totalOwned = GetTotalQuantity(itemData);
        if (totalOwned < amount)
        {
            Debug.Log($"[PlayerInventory] 판매 수량 부족 (보유: {totalOwned}, 요청: {amount})");
            return false;
        }

        RemoveItem(itemData, amount);

        int totalSellPrice = itemData.sellPrice * amount;
        _gold += totalSellPrice;
        OnGoldChanged?.Invoke(_gold);
        OnInventoryChanged?.Invoke();

        // 퀘스트 진행도 반영
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.AddProgress(QuestObjectiveType.SellItem, itemData.itemName, amount);
        }

        Debug.Log($"[PlayerInventory] 판매 완료: {itemData.itemName} x{amount} (+{totalSellPrice} 골드, 현재 골드: {_gold})");
        AlertManager.Instance?.Enqueue(AlertType.GoldAcquired, totalSellPrice);
        return true;
    }

    /// <summary>
    /// 여러 슬롯에 나뉘어 있어도 지정한 수량만큼 뒤에서부터 차감. 다 빠진 슬롯은 제거.
    /// </summary>
    private void RemoveItem(ItemData itemData, int amount)
    {
        int remaining = amount;

        for (int i = _inventorySlots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var slot = _inventorySlots[i];
            if (slot.ItemData != itemData) continue;

            int removeAmount = Mathf.Min(slot.Quantity, remaining);
            slot.Quantity -= removeAmount;
            remaining -= removeAmount;

            if (slot.Quantity <= 0)
            {
                _inventorySlots.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 아이템을 인벤토리에 추가.
    /// itemData.maxStack을 넘어가면 기존 슬롯을 꽉 채우고, 남은 수량은 새 슬롯을 만들어서 담는다.
    /// 예: maxStack 99, 250개 획득 -> 슬롯 99 + 99 + 52 (3개 슬롯)
    /// * 가격 계산/검증 없이 그냥 추가만 함 (구매 등 상위 로직은 ShopController에서 처리)
    /// </summary>
    public void AddItem(ItemData itemData, int amount)
    {
        int maxStack = Mathf.Max(1, itemData.maxStack); // maxStack이 0 이하로 잘못 설정된 경우 방어
        int remaining = amount;

        // 1. 기존에 덜 채워진 슬롯들부터 채우기
        foreach (var slot in _inventorySlots.Where(s => s.ItemData == itemData && s.Quantity < maxStack))
        {
            if (remaining <= 0) break;

            int spaceInSlot = maxStack - slot.Quantity;
            int amountToAdd = Mathf.Min(spaceInSlot, remaining);

            slot.Quantity += amountToAdd;
            remaining -= amountToAdd;
        }

        // 2. 그래도 남은 수량은 새 슬롯을 만들어서 채우기
        while (remaining > 0)
        {
            int amountToAdd = Mathf.Min(maxStack, remaining);
            _inventorySlots.Add(new InventorySlot(itemData, amountToAdd));
            remaining -= amountToAdd;
        }
    }

    /// <summary>
    /// 장비를 개수만큼 EquipmentInstance로 새로 생성해서 추가. 항상 0강으로 시작.
    /// 장비는 개체마다 강화 단계가 다를 수 있어 수량으로 뭉치지 않고 하나씩 따로 추가한다.
    /// * 가격 계산/검증 없이 그냥 추가만 함 (구매 등 상위 로직은 ShopController에서 처리)
    /// * 0강 생성이라 강화 테이블이 필요 없음 (enhanceLevel 0이면 어차피 테이블을 안 봄) -> null 전달
    /// </summary>
    public void AddEquipment(EquipItemData equipItemData, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            _equipmentInstances.Add(new EquipmentInstance(equipItemData, 0, null));
        }
    }

    /// <summary>
    /// 장비 개체 하나를 판매. 수량 기반이 아니라 "이 개체를 정확히 지정해서" 판매하는 방식.
    /// * 강화 단계와 무관하게 판매가는 baseData.sellPrice 고정 (TODO: 강화 반영한 판매가로 확장 가능)
    /// </summary>
    /// <returns>판매 성공 여부</returns>
    public bool TrySellEquipment(EquipmentInstance instance)
    {
        if (instance == null || instance.baseData == null)
        {
            return false;
        }

        if (!instance.baseData.canSell)
        {
            Debug.Log($"[PlayerInventory] 판매 불가 장비: {instance.baseData.itemName}");
            return false;
        }

        bool removed = _equipmentInstances.Remove(instance);
        if (!removed)
        {
            Debug.Log("[PlayerInventory] 인벤토리에 없는 장비 인스턴스입니다.");
            return false;
        }

        int sellPrice = instance.baseData.sellPrice;
        _gold += sellPrice;
        OnGoldChanged?.Invoke(_gold);
        OnInventoryChanged?.Invoke();


        // 퀘스트 진행도 반영
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.AddProgress(QuestObjectiveType.SellEquipment, instance.baseData.itemName, 1);
        }

        Debug.Log($"[PlayerInventory] 장비 판매 완료: {instance.baseData.itemName} (+{sellPrice} 골드, 현재 골드: {_gold})");
        return true;
    }


    /// <summary>
    /// 골드 소모 시도. 충분하면 차감하고 true, 부족하면 아무것도 안 하고 false.
    /// </summary>
    public bool TrySpendGold(int amount)
    {
        if (amount <= 0) return true;

        if (_gold < amount)
        {
            return false;
        }

        _gold -= amount;
        OnGoldChanged?.Invoke(_gold);
        return true;
    }

    /// <summary>
    /// 골드 추가 (판매/환불 등에서 사용).
    /// </summary>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        _gold += amount;
        OnGoldChanged?.Invoke(_gold);
    }

    /// <summary>
    /// 수량 기반 아이템(소비/재료/씨앗) 소모 시도. 보유량 충분하면 차감하고 true, 부족하면 false.
    /// 
    /// bool success = PlayerInventory.Instance.TryConsumeItem(seedItemData, 1);
    /// 
    /// * itemData 자리에 seedItemData(SeedItemData)를 그대로 넣으면 됨 (SeedItemData도 ItemData를 상속받으므로 호환됨).
    /// * 이 메서드 자체는 OnInventoryChanged를 자동으로 쏘지 않음 -> 필요하면 호출한 쪽에서 RaiseInventoryChanged()를 직접 불러줄 것.
    /// </summary>
    public bool TryConsumeItem(ItemData itemData, int amount)
    {
        if (itemData == null || amount <= 0) return true;

        int owned = GetTotalQuantity(itemData);
        if (owned < amount)
        {
            return false;
        }

        RemoveItem(itemData, amount);
        return true;
    }

    /// <summary>
    /// 외부(EnhanceController 등)에서 직접 데이터를 바꾼 뒤, UI 갱신을 위해 변경 이벤트만 수동으로 발생시킬 때 사용.
    /// </summary>
    public void RaiseInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }
}