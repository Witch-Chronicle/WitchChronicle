using System;

namespace WitchChronicle.IdleFarming
{
    /// <summary>
    /// 밭 하나의 저장 데이터 (JSON 직렬화용)
    /// </summary>
    [Serializable]
    public class PlotSaveData
    {
        public int plotIndex;                  // 0~9 밭 번호
        public PlotState state;                // 현재 상태
        public string plantedSeedItemId;       // 심은 씨앗 SO의 itemId (없으면 빈 문자열)
        public string cycleStartTimeISO;       // 사이클 시작 시각 (ISO8601 문자열)
        public int pendingHarvestCount;        // 수확 대기 중인 개수 (수확 안 하면 쌓임)
        
        public PlotSaveData()
        {
            plotIndex = 0;
            state = PlotState.Locked;
            plantedSeedItemId = "";
            cycleStartTimeISO = "";
            pendingHarvestCount = 0;
        }
        
        public PlotSaveData(int index)
        {
            plotIndex = index;
            state = PlotState.Locked;
            plantedSeedItemId = "";
            cycleStartTimeISO = "";
            pendingHarvestCount = 0;
        }
        
        /// <summary>
        /// 사이클 시작 시각을 DateTime으로 변환
        /// </summary>
        public DateTime GetCycleStartTime()
        {
            if (string.IsNullOrEmpty(cycleStartTimeISO))
                return DateTime.MinValue;
            return DateTime.Parse(cycleStartTimeISO, null, System.Globalization.DateTimeStyles.RoundtripKind);
        }
        
        /// <summary>
        /// 사이클 시작 시각 저장
        /// </summary>
        public void SetCycleStartTime(DateTime time)
        {
            cycleStartTimeISO = time.ToString("o"); // ISO8601 라운드트립 포맷
        }
    }
    
    /// <summary>
    /// 전체 밭 저장 데이터 (파일로 저장)
    /// </summary>
    [Serializable]
    public class FarmSaveData
    {
        public PlotSaveData[] plots;
        public string lastSaveTimeISO;
        
        public FarmSaveData(int plotCount)
        {
            plots = new PlotSaveData[plotCount];
            for (int i = 0; i < plotCount; i++)
                plots[i] = new PlotSaveData(i);
            lastSaveTimeISO = DateTime.Now.ToString("o");
        }
    }
}