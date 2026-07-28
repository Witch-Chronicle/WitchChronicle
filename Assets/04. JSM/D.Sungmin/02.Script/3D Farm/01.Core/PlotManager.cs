using System.Collections.Generic;
using UnityEngine;

namespace WitchChronicle.IdleFarming
{
    /// <summary>
    /// 방치형 파밍 전체 관리 싱글톤 (저장 없는 임시 버전)
    /// </summary>
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

        private readonly List<PlotSlot> _slots = new List<PlotSlot>();
        private int _openPanelCount = 0;  // 열린 팝업 카운트 (커서 제어용)

        // 프로퍼티
        public PlotConfig Config => _config;
        public int AllSeedsCount => _allSeeds.Count;
        public PlotUnlockPanel UnlockPanel => _unlockPanel;
        public PlotSeedSelectPanel SeedSelectPanel => _seedSelectPanel;

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
            var found = FindObjectsOfType<PlotSlot>();
            System.Array.Sort(found, (a, b) => a.PlotIndex.CompareTo(b.PlotIndex));
            _slots.AddRange(found);
            Debug.Log($"[PlotManager] {_slots.Count}개 슬롯 등록됨");
        }

        // ====== 초기화 (저장 없이 매번 새로) ======

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