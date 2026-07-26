using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotUnlockPopup : MonoBehaviour
{
    [Header("팝업")]
    public GameObject popupPanel;
    public TextMeshProUGUI messageText;
    public Button confirmButton;
    public Button cancelButton;

    private int targetSlotIndex;
    private int unlockCost;

    void Start()
    {
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(Hide);
        popupPanel.SetActive(false);
    }

    public void Show(int slotIndex, int cost)
    {
        targetSlotIndex = slotIndex;
        unlockCost = cost;
        messageText.text = $"{cost}G로 밭을 해금하시겠습니까?";
        popupPanel.SetActive(true);
    }

    public void Hide()
    {
        popupPanel.SetActive(false);
    }

    void OnConfirm()
    {
        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("PlayerInventory 없음 - 임시로 해금만 처리");
            FarmingManager.Instance.UnlockSlot(targetSlotIndex);
            Hide();
            return;
        }

        if (PlayerInventory.Instance.Gold < unlockCost)
        {
            messageText.text = $"골드 부족! (보유: {PlayerInventory.Instance.Gold}G)";
            return;
        }

        // TODO: 3번한테 TrySpendGold 받으면 이거로 교체
        // if (!PlayerInventory.Instance.TrySpendGold(unlockCost))
        // {
        //     messageText.text = "골드 부족!";
        //     return;
        // }

        FarmingManager.Instance.UnlockSlot(targetSlotIndex);
        Debug.Log($"슬롯 {targetSlotIndex} 해금 (비용 {unlockCost}G - 골드 차감은 3번 메서드 필요)");
        Hide();
    }
}