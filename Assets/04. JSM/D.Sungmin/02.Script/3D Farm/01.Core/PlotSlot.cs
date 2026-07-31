using System;
using UnityEngine;

namespace WitchChronicle.IdleFarming
{
    /// <summary>
    /// 개별 밭 하나의 로직과 데이터
    /// 씬 오브젝트에 붙는 컴포넌트
    /// </summary>
    public class PlotSlot : MonoBehaviour
    {
        [Header("밭 정보")]
        [SerializeField] private int _plotIndex;
        public int PlotIndex => _plotIndex;

        [Header("컴포넌트 참조")]
        [SerializeField] private PlotVisual _visual;
        [SerializeField] private PlotFloatingUI _floatingUI;

        // 런타임 데이터
        private PlotState _state;
        private SeedData _plantedSeed;
        private DateTime _cycleStartTime;
        private int _pendingHarvestCount;

        // 이벤트
        public event Action<PlotSlot> OnStateChanged;
        public event Action<PlotSlot, SeedData, int> OnHarvested;

        // 프로퍼티
        public PlotState State => _state;
        public SeedData PlantedSeed => _plantedSeed;
        public DateTime CycleStartTime => _cycleStartTime;
        public int PendingHarvestCount => _pendingHarvestCount;

        // ====== Unity 라이프사이클 ======

        private void Update()
        {
            if (_state == PlotState.Growing && _plantedSeed != null)
            {
                _visual.Refresh(_state, _plantedSeed, GetGrowthProgress());
                if (_floatingUI != null)
                    _floatingUI.Refresh(_state, _plantedSeed, GetRemainingSeconds(), _pendingHarvestCount);
            }
        }

        // ====== 초기화 ======

        public void Initialize(PlotSaveData saveData, SeedData plantedSeed)
        {
            _plotIndex = saveData.plotIndex;
            _state = saveData.state;
            _plantedSeed = plantedSeed;
            _cycleStartTime = saveData.GetCycleStartTime();
            _pendingHarvestCount = saveData.pendingHarvestCount;

            RefreshVisual();
        }

        public PlotSaveData ToSaveData()
        {
            var data = new PlotSaveData(_plotIndex);
            data.state = _state;
            data.plantedSeedItemId = _plantedSeed != null ? _plantedSeed.name : "";
            data.SetCycleStartTime(_cycleStartTime);
            data.pendingHarvestCount = _pendingHarvestCount;
            return data;
        }

        // ====== 상태 전환 액션 ======

        public bool Unlock()
        {
            if (_state != PlotState.Locked) return false;

            _state = PlotState.Empty;
            OnStateChanged?.Invoke(this);
            RefreshVisual();
            return true;
        }

        public bool PlantSeed(SeedData seed)
        {
            if (_state == PlotState.Locked || seed == null) return false;

            _plantedSeed = seed;
            _cycleStartTime = DateTime.Now;
            _pendingHarvestCount = 0;
            _state = PlotState.Growing;

            OnStateChanged?.Invoke(this);
            RefreshVisual();
            return true;
        }

        public bool Harvest()
        {
            if (_state != PlotState.ReadyToHarvest || _plantedSeed == null) return false;

            int harvested = _pendingHarvestCount;
            _pendingHarvestCount = 0;

            OnHarvested?.Invoke(this, _plantedSeed, harvested);

            _cycleStartTime = DateTime.Now;
            _state = PlotState.Growing;

            OnStateChanged?.Invoke(this);
            RefreshVisual();
            return true;
        }

        // ====== 사이클 업데이트 (온라인/오프라인 통합) ======

        public void UpdateCycle()
        {
            if (_plantedSeed == null) return;
            if (_state == PlotState.Locked || _state == PlotState.Empty) return;

            float cycleSeconds = _plantedSeed.growthTime;
            if (cycleSeconds <= 0f) return;

            double elapsed = (DateTime.Now - _cycleStartTime).TotalSeconds;
            if (elapsed < cycleSeconds) return;

            int completedCycles = (int)(elapsed / cycleSeconds);
            if (completedCycles <= 0) return;

            // 최대 누적 제한
            int maxStack = PlotManager.Instance != null ? PlotManager.Instance.MaxOfflineCycles : 5;
            int harvestPerCycle = Mathf.Max(1, _plantedSeed.harvestAmount);
            int currentStacks = _pendingHarvestCount / harvestPerCycle;
            int allowedCycles = Mathf.Min(completedCycles, maxStack - currentStacks);

            if (allowedCycles <= 0)
            {
                // 최대 도달, cycleStartTime 유지 (시간 소비 X)
                if (_state != PlotState.ReadyToHarvest)
                {
                    _state = PlotState.ReadyToHarvest;
                    OnStateChanged?.Invoke(this);
                    RefreshVisual();
                }
                return;
            }

            _pendingHarvestCount += allowedCycles * harvestPerCycle;
            _cycleStartTime = _cycleStartTime.AddSeconds(allowedCycles * cycleSeconds);
            _state = PlotState.ReadyToHarvest;

            OnStateChanged?.Invoke(this);
            RefreshVisual();
        }

        /// <summary>
        /// 오프라인 처리 — 현재 UpdateCycle이 통합 처리하므로 이 메서드는 호환용으로만 남김
        /// </summary>
        public void ProcessOfflineTime(int maxCycles)
        {
            UpdateCycle();
        }

        // ====== 유틸리티 ======

        public float GetGrowthProgress()
        {
            if (_state != PlotState.Growing || _plantedSeed == null) return 0f;

            float cycleSeconds = _plantedSeed.growthTime;
            double elapsed = (DateTime.Now - _cycleStartTime).TotalSeconds;
            return Mathf.Clamp01((float)(elapsed / cycleSeconds));
        }

        public float GetRemainingSeconds()
        {
            if (_state != PlotState.Growing || _plantedSeed == null) return 0f;

            float cycleSeconds = _plantedSeed.growthTime;
            double elapsed = (DateTime.Now - _cycleStartTime).TotalSeconds;
            return Mathf.Max(0f, cycleSeconds - (float)elapsed);
        }

        // ====== 시각화 ======

        private void RefreshVisual()
        {
            float progress = _state switch
            {
                PlotState.Growing => GetGrowthProgress(),
                PlotState.ReadyToHarvest => 1f,
                _ => 0f
            };

            if (_visual != null)
                _visual.Refresh(_state, _plantedSeed, progress);

            if (_floatingUI != null)
                _floatingUI.Refresh(_state, _plantedSeed, GetRemainingSeconds(), _pendingHarvestCount);
        }
    }
}