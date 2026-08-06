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
        /// [사용 안 함, 남겨둠] 팜 존 전체의 FloatingUI 일괄 표시/숨김
        /// </summary>
        public void SetAllFloatingUIsPlayerNear(bool near)
        {
            for (int i = 0; i < _floatingUIs.Count; i++)
            {
                if (_floatingUIs[i] != null)
                    _floatingUIs[i].SetPlayerNear(near);
            }
        }

        /// <summary>
        /// 특정 PlotSlot에 해당하는 FloatingUI만 표시/숨김
        /// </summary>
        public void SetFloatingUINearBySlot(PlotSlot slot, bool near)
        {
            if (slot == null) return;

            int index = _slots.IndexOf(slot);
            if (index < 0 || index >= _floatingUIs.Count) return;

            if (_floatingUIs[index] != null)
                _floatingUIs[index].SetPlayerNear(near);
        }

        /// <summary>
        /// 특정 PlotIndex에 해당하는 FloatingUI만 표시/숨김
        /// </summary>
        public void SetFloatingUINearByIndex(int plotIndex, bool near)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] != null && _slots[i].PlotIndex == plotIndex)
                {
                    if (i < _floatingUIs.Count && _floatingUIs[i] != null)
                        _floatingUIs[i].SetPlayerNear(near);
                    return;
                }
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

        public List<PlotSaveData> GetFarmSaveData()
        {
            List<PlotSaveData> saveDataList = new List<PlotSaveData>();
            foreach (var slot in _slots)
            {
                if (slot != null)
                    saveDataList.Add(slot.ToSaveData());
            }
            return saveDataList;
        }

        public void LoadFarmSaveData(List<PlotSaveData> savedPlots)
        {
            // 저장된 데이터가 없으면 신규 시작 (기본 밭 해제)
            if (savedPlots == null || savedPlots.Count == 0)
            {
                InitializeFresh();
                return;
            }

            Dictionary<int, PlotSaveData> dataMap = new Dictionary<int, PlotSaveData>();
            foreach (var p in savedPlots)
            {
                if (p != null) dataMap[p.plotIndex] = p;
            }

            foreach (var slot in _slots)
            {
                if (slot == null) continue;

                if (dataMap.TryGetValue(slot.PlotIndex, out var saveData))
                {
                    SeedData seed = FindSeedByName(saveData.plantedSeedItemId);
                    // 💡 Initialize() 안에서 UpdateCycle()이 불려 오프라인 지난 시간만큼 작물이 자랍니다!
                    slot.Initialize(saveData, seed);
                }
                else
                {
                    var fresh = new PlotSaveData(slot.PlotIndex);
                    fresh.state = slot.PlotIndex < _config.initialUnlockedCount ? PlotState.Empty : PlotState.Locked;
                    fresh.SetCycleStartTime(System.DateTime.Now);
                    slot.Initialize(fresh, null);
                }
            }

            Debug.Log($"[PlotManager] 농사 데이터 및 오프라인 시간 복원 완료 (총 {_slots.Count}개 밭)");
        }
    }
}