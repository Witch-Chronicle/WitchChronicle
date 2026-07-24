using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FishingSidebarUI : MonoBehaviour
{
    public enum InventoryFilter { All, Common, Rare, Legendary }

    [Header("인벤토리 필터 버튼")]
    [SerializeField] private Button filterAllButton;
    [SerializeField] private Button filterCommonButton;
    [SerializeField] private Button filterRareButton;
    [SerializeField] private Button filterLegendaryButton;

    [Header("인벤토리 그리드")]
    [SerializeField] private Transform fishGridRoot;
    [SerializeField] private GameObject fishSlotPrefab;

    [Header("낚시일지 카드")]
    [SerializeField] private TextMeshProUGUI caughtCountText;
    [SerializeField] private TextMeshProUGUI fishingTimeText;

    [Header("낚시 장비 카드 슬롯 (3개 미리 배치)")]
    [SerializeField] private RodSlotUI[] rodCardSlots = new RodSlotUI[3];

    private InventoryFilter currentFilter = InventoryFilter.All;
    private float _sessionTime = 0f;
    private bool _timerRunning = false;

    private void Awake()
    {
        if (filterAllButton != null)       filterAllButton.onClick.AddListener(() => SetFilter(InventoryFilter.All));
        if (filterCommonButton != null)    filterCommonButton.onClick.AddListener(() => SetFilter(InventoryFilter.Common));
        if (filterRareButton != null)      filterRareButton.onClick.AddListener(() => SetFilter(InventoryFilter.Rare));
        if (filterLegendaryButton != null) filterLegendaryButton.onClick.AddListener(() => SetFilter(InventoryFilter.Legendary));
    }

    private void Start()
    {
        if (FishingManager.Instance != null)
        {
            FishingManager.Instance.OnFishCaught  += HandleFishCaught;
            FishingManager.Instance.OnRodEquipped += HandleRodEquipped;
        }

        SetFilter(InventoryFilter.All);
        RefreshInventory();
        RefreshEquipment();
        StartTimer();
    }

    private void OnEnable()
    {
        StartTimer();
    }

    private void OnDisable()
    {
        _timerRunning = false;
    }

    private void OnDestroy()
    {
        if (FishingManager.Instance != null)
        {
            FishingManager.Instance.OnFishCaught  -= HandleFishCaught;
            FishingManager.Instance.OnRodEquipped -= HandleRodEquipped;
        }
    }

    private void Update()
    {
        if (_timerRunning)
        {
            _sessionTime += Time.deltaTime;
            UpdateFishingTimeText();
        }
    }

    private void StartTimer()
    {
        _sessionTime = 0f;
        _timerRunning = true;
        UpdateFishingTimeText();
    }

    private void UpdateFishingTimeText()
    {
        if (fishingTimeText == null) return;
        int m = Mathf.FloorToInt(_sessionTime / 60f);
        int s = Mathf.FloorToInt(_sessionTime % 60f);
        fishingTimeText.text = $"낚시 시간: {m:00}:{s:00}";
    }

    private void SetFilter(InventoryFilter filter)
    {
        currentFilter = filter;
        RefreshInventory();
    }

    private void HandleFishCaught(FishItemData fish)
    {
        RefreshInventory();
        RefreshCaughtCount();
    }

    private void HandleRodEquipped(RodItemData rod)
    {
        RefreshEquipment();
    }

    private void RefreshInventory()
    {
        if (fishGridRoot == null || fishSlotPrefab == null) return;

        foreach (Transform child in fishGridRoot) Destroy(child.gameObject);

        var caught = FishingManager.Instance != null
            ? FishingManager.Instance.CaughtFishesThisSession
            : (IReadOnlyList<FishItemData>)new List<FishItemData>();

        var grouped = new Dictionary<FishItemData, int>();
        foreach (var f in caught)
        {
            if (f == null || !PassesFilter(f)) continue;
            if (grouped.ContainsKey(f)) grouped[f]++;
            else grouped[f] = 1;
        }

        foreach (var kv in grouped)
        {
            var go = Instantiate(fishSlotPrefab, fishGridRoot);
            var slot = go.GetComponent<FishSlotUI>();
            if (slot != null) slot.Setup(kv.Key, kv.Value);
        }

        RefreshCaughtCount();
    }

    private void RefreshCaughtCount()
    {
        if (caughtCountText == null) return;
        var count = FishingManager.Instance?.CaughtFishesThisSession.Count ?? 0;
        caughtCountText.text = $"잡은 물고기: {count}마리";
    }

    private bool PassesFilter(FishItemData fish)
    {
        switch (currentFilter)
        {
            case InventoryFilter.All:       return true;
            case InventoryFilter.Common:    return fish.grade == FishGrade.Common;
            case InventoryFilter.Rare:      return fish.grade == FishGrade.Rare;
            case InventoryFilter.Legendary: return fish.grade == FishGrade.Legendary;
        }
        return true;
    }

    private void RefreshEquipment()
    {
        var mgr = FishingManager.Instance;
        if (mgr == null) return;

        var rods = mgr.OwnedRods;
        var current = mgr.CurrentRod;

        for (int i = 0; i < rodCardSlots.Length; i++)
        {
            if (rodCardSlots[i] == null) continue;

            if (i < rods.Count)
            {
                var rod = rods[i];
                rodCardSlots[i].gameObject.SetActive(true);
                rodCardSlots[i].Setup(rod, rod == current);
            }
            else
            {
                rodCardSlots[i].gameObject.SetActive(false);
            }
        }
    }
}