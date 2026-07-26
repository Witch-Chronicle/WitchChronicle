using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FarmUIManager : MonoBehaviour
{
    public static FarmUIManager Instance;

    [Header("루트 패널")]
    [Tooltip("자식 FarmPanel 오브젝트 참조. 실제 활성/비활성 토글은 부모 FarmUI에서 이뤄짐 (낚시와 동일 구조)")]
    public GameObject farmPanel;

    [Header("슬롯 UI")]
    public Transform slotContainer;
    public GameObject farmSlotPrefab;

    [Header("상단")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI goldText;

    [Header("팝업")]
    public SeedSelectPopup seedSelectPopup;
    public SlotUnlockPopup slotUnlockPopup;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        for (int i = 0; i < FarmingManager.Instance.maxSlots; i++)
        {
            var slotObj = Instantiate(farmSlotPrefab, slotContainer);
            slotObj.GetComponent<FarmSlotUI>().Setup(i);
        }

        if (PlayerInventory.Instance != null && goldText != null)
        {
            goldText.text = $"{PlayerInventory.Instance.Gold}G";
            PlayerInventory.Instance.OnGoldChanged += (g) => goldText.text = $"{g}G";
        }
    }

    public void OpenPanel()
    {
        // 부모 FarmUI 오브젝트를 켬. 자식 farmPanel은 이미 활성 상태이므로 함께 표시됨
        if (farmPanel != null && farmPanel.transform.parent != null)
        {
            farmPanel.transform.parent.gameObject.SetActive(true);
        }
        else if (farmPanel != null)
        {
            farmPanel.SetActive(true);
        }
    }

    public void ClosePanel()
    {
        // 부모 FarmUI 오브젝트만 끔. 자식 farmPanel은 활성 상태 유지
        // → 재진입 시 F키가 부모만 켜도 자식이 자동으로 표시됨 (낚시와 동일 방식)
        if (farmPanel != null && farmPanel.transform.parent != null)
        {
            farmPanel.transform.parent.gameObject.SetActive(false);
        }
        else if (farmPanel != null)
        {
            farmPanel.SetActive(false);
        }

        CursorLocker.Instance.ExitUIMode();
    }

    public void OpenSeedSelect(int slotIndex)
    {
        if (seedSelectPopup != null)
            seedSelectPopup.Show(slotIndex);
        else
            Debug.LogWarning("SeedSelectPopup이 연결되지 않았습니다.");
    }

    public void OpenUnlockPopup(int slotIndex)
    {
        if (slotUnlockPopup == null) return;

        int idx = slotIndex - FarmingManager.Instance.initialSlots;
        if (idx < 0 || idx >= FarmingManager.Instance.unlockCosts.Length) return;

        int cost = FarmingManager.Instance.unlockCosts[idx];
        slotUnlockPopup.Show(slotIndex, cost);
    }
}