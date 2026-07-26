using UnityEngine;

/// <summary>
/// 낚시터. 플레이어가 트리거 안에 있을 때 F키로 낚시 UI 토글.
/// </summary>
[RequireComponent(typeof(Collider))]
public class FishingSpot : MonoBehaviour, ITFInteractable
{
    [Header("낚시터 설정")]
    [SerializeField] private string spotName = "잔잔한 호수";

    [Header("낚시 UI 패널")]
    public GameObject fishingUIPanel;


    public string SpotName => spotName;

    public string Prompt => $"[F] {spotName} 에서 낚시 하기";

    void Start()
    {
        if (fishingUIPanel != null) fishingUIPanel.SetActive(false);
    }

    void ToggleFishingUI()
    {
        if (fishingUIPanel == null) return;
        bool isActive = fishingUIPanel.activeSelf;
        fishingUIPanel.SetActive(!isActive);

        // FishingPanel도 켜기 (자식이 꺼져있을 수도 있으니)
        if (!isActive)
        {
            var childPanel = fishingUIPanel.transform.Find("FishingPanel");
            if (childPanel != null) childPanel.gameObject.SetActive(true);
        }

        if (isActive)
        {
            CursorLocker.Instance.ExitUIMode();
        }
        else
        {
            CursorLocker.Instance.EnterUIMode();
        }
    }


    public void Interact(GameObject interactor)
    {
        ToggleFishingUI();
    }
}