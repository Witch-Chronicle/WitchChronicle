using System.Collections.Generic;
using UnityEngine;

namespace WitchChronicle.IdleFarming
{
    public class PlotManager : MonoBehaviour
    {
        public static PlotManager Instance { get; private set; }

        [Header("설정")]
        [SerializeField] private PlotConfig _config;
        [SerializeField] private List<SeedData> _allSeeds = new List<SeedData>();
        [SerializeField] private int _maxOfflineCycles = 5;

        [Header("UI Panels")]
        [SerializeField] private PlotUnlockPanel _unlockPanel;
        [SerializeField] private PlotSeedSelectPanel _seedSelectPanel;
        [SerializeField] private PlotHarvestPanel _harvestPanel;

        [Header("Floating UI (Screen Space)")]
        [SerializeField] private PlotFloatingUI _floatingUIPrefab;
        [SerializeField] private RectTransform _hudCanvasRoot;

        private readonly List<PlotSlot> _slots = new List<PlotSlot>();
        private readonly List<PlotFloatingUI> _floatingUIs = new List<PlotFloatingUI>();
        private int _openPanelCount = 0;

        public PlotConfig Config => _config;
        public int AllSeedsCount => _allSeeds.Count;
        public int MaxOfflineCycles => _maxOfflineCycles;
        public PlotUnlockPanel UnlockPanel => _unlockPanel;
        public PlotSeedSelectPanel SeedSelectPanel => _seedSelectPanel;
        public PlotHarvestPanel HarvestPanel => _harvestPanel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            AutoRegisterPlots();
            InitializeFresh();
        }

        // ====== 슬롯 등록 ======

        private void AutoRegisterPlots()
        {
            _slots.Clear();
            _floatingUIs.Clear();

            var found = FindObjectsOfType<PlotSlot>();
            System.Array.Sort(found, (a, b) => a.PlotIndex.CompareTo(b.PlotIndex));
            _slots.AddRange(found);

            foreach (var slot in _slots)
            {
                slot.OnHarvested += HandleSlotHarvested;
                SpawnFloatingUI(slot);
            }

            Debug.Log($"[PlotManager] {_slots.Count}개 슬롯 등록됨");
        }

        private void SpawnFloatingUI(PlotSlot slot)
        {
            if (_floatingUIPrefab == null || _hudCanvasRoot == null)
            {
                Debug.LogWarning("[PlotManager] FloatingUI 프리팹 또는 HUD Canvas 미설정");
                return;
            }

            var ui = Instantiate(_floatingUIPrefab, _hudCanvasRoot);
            ui.name = $"PlotFloatingUI_{slot.PlotIndex:D2}";
            slot.SetFloatingUI(ui);
            _floatingUIs.Add(ui);
        }

        private void HandleSlotHarvested(PlotSlot slot, SeedData seed, int amount)
        {
            if (seed == null || seed.harvestItem == null || amount <= 0) return;

            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.AddItem(seed.harvestItem, amount);
                Debug.Log($"[PlotManager] 수확: {seed.harvestName} x{amount}");
            }
            else
            {
                Debug.LogWarning("[PlotManager] PlayerInventory.Instance 없음, 수확 지급 실패");
            }
        }

        // ====== 초기화 ======

        private void InitializeFresh()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                var saveData = new PlotSaveData(_slots[i].PlotIndex);
                saveData.state = _slots[i].PlotIndex < _config.initialUnlockedCount
                    ? PlotState.Empty : PlotState.Locked;
                saveData.SetCycleStartTime(System.DateTime.Now);
                _slots[i].Initialize(saveData, null);
            }
        }

        // ====== 조회 ======

        public SeedData GetSeedAt(int index)
        {
            if (index < 0 || index >= _allSeeds.Count) return null;
            return _allSeeds[index];
        }

        public SeedData FindSeedByName(string seedName)
        {
            if (string.IsNullOrEmpty(seedName)) return null;
            foreach (var s in _allSeeds)
                if (s != null && s.name == seedName) return s;
            return null;
        }

        public int GetUnlockCost(int plotIndex)
        {
            int priceIndex = plotIndex - _config.initialUnlockedCount;
            if (priceIndex < 0) return 0;
            if (_config.unlockPrices == null || priceIndex >= _config.unlockPrices.Length)
                return 999999;
            return _config.unlockPrices[priceIndex];
        }

        // ====== 플레이어 존 진입/이탈 ======

        /// <summary>
        /// FarmZoneTrigger가 호출: 팜 존 전체의 FloatingUI 표시/숨김 일괄 처리
        /// </summary>
        public void SetAllFloatingUIsPlayerNear(bool near)
        {
            for (int i = 0; i < _floatingUIs.Count; i++)
            {
                if (_floatingUIs[i] != null)
                    _floatingUIs[i].SetPlayerNear(near);
            }
        }

        // ====== 커서 제어 ======

        public void NotifyPanelOpened()
        {
            _openPanelCount++;
            if (_openPanelCount == 1)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void NotifyPanelClosed()
        {
            _openPanelCount = Mathf.Max(0, _openPanelCount - 1);
            if (_openPanelCount == 0)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // ====== 주기적 갱신 ======

        private void Update()
        {
            foreach (var slot in _slots)
                slot.UpdateCycle();
        }
    }
}