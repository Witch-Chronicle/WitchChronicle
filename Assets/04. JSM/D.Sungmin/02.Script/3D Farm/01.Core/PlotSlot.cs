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
            // Growing 중엔 매 프레임 진행률 반영 (새싹 ↔ 자라는 나무 전환용)
            if (_state == PlotState.Growing && _plantedSeed != null)
            {
                _visual.Refresh(_state, _plantedSeed, GetGrowthProgress());
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
            
            // 자동으로 다음 사이클 시작 (progress 0 → 새싹부터)
            _cycleStartTime = DateTime.Now;
            _state = PlotState.Growing;
            
            OnStateChanged?.Invoke(this);
            RefreshVisual();
            return true;
        }
        
        // ====== 사이클 업데이트 ======
        
        public void UpdateCycle()
        {
            if (_state != PlotState.Growing || _plantedSeed == null) return;
            
            float cycleSeconds = _plantedSeed.growthTime;
            double elapsed = (DateTime.Now - _cycleStartTime).TotalSeconds;
            
            if (elapsed >= cycleSeconds)
            {
                _pendingHarvestCount += _plantedSeed.harvestAmount;
                _state = PlotState.ReadyToHarvest;
                
                OnStateChanged?.Invoke(this);
                RefreshVisual();
            }
        }
        
        /// <summary>
        /// 오프라인 시간 계산 (옵션 B: 최대 maxCycles 누적)
        /// </summary>
        public void ProcessOfflineTime(int maxCycles)
        {
            if (_state != PlotState.Growing || _plantedSeed == null) return;
            
            float cycleSeconds = _plantedSeed.growthTime;
            double elapsed = (DateTime.Now - _cycleStartTime).TotalSeconds;
            
            if (elapsed >= cycleSeconds)
            {
                int completedCycles = (int)(elapsed / cycleSeconds);
                int cyclesToApply = Mathf.Min(completedCycles, maxCycles);
                
                _pendingHarvestCount += cyclesToApply * _plantedSeed.harvestAmount;
                _state = PlotState.ReadyToHarvest;
                
                _cycleStartTime = _cycleStartTime.AddSeconds(cyclesToApply * cycleSeconds);
                
                OnStateChanged?.Invoke(this);
                RefreshVisual();
            }
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
            if (_visual == null) return;
            
            float progress = _state switch
            {
                PlotState.Growing => GetGrowthProgress(),
                PlotState.ReadyToHarvest => 1f,
                _ => 0f
            };
            
            _visual.Refresh(_state, _plantedSeed, progress);
        }
    }
}