using UnityEngine;

/// <summary>
/// 구매의 "규칙"을 전담하는 컨트롤러 (EnhanceController와 동일한 패턴).
/// - 가격 계산, 골드 검증, 구매 성공 시 아이템/장비 추가까지 담당
/// - 실제 골드 소모/아이템 추가는 PlayerInventory의 저수준 메서드에 위임
/// * PlayerInventory는 "얼마 갖고 있는지"만 알고, 구매 규칙 자체는 전혀 모름
/// * 판매(TrySell/TrySellEquipment)는 당장은 PlayerInventory에 그대로 둠
/// </summary>
public class ShopController : MonoBehaviour
{
    /// <summary>
    /// 아이템 구매 시도. 골드가 충분하면 차감 후 아이템을 인벤토리에 추가.
    /// 장비(EquipItemData)면 개수만큼 EquipmentInstance를 새로 생성(항상 0강)하고,
    /// 그 외에는 기존처럼 수량 기반으로 추가.
    /// </summary>
    /// <returns>구매 성공 여부</returns>
    public bool TryPurchase(ItemData itemData, int amount)
    {
        if (itemData == null || amount <= 0 || PlayerInventory.Instance == null)
        {
            return false;
        }

        int totalPrice = itemData.buyPrice * amount;

        if (!PlayerInventory.Instance.TrySpendGold(totalPrice))
        {
            Debug.Log($"[ShopController] 골드 부족 (필요: {totalPrice})");
            AlertManager.Instance?.Enqueue(AlertType.NotEnoughGold);
            return false;
        }

        if (itemData is EquipItemData equipItemData)
        {
            PlayerInventory.Instance.AddEquipment(equipItemData, amount);
        }
        else
        {
            PlayerInventory.Instance.AddItem(itemData, amount);
        }

        PlayerInventory.Instance.RaiseInventoryChanged();

        Debug.Log($"[ShopController] 구매 완료: {itemData.itemName} x{amount}");
        AlertManager.Instance?.Enqueue(AlertType.BuyItems, itemData.itemName, amount);
        return true;
    }
}