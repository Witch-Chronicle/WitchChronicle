using UnityEngine;

namespace WitchChronicle.IdleFarming
{
    /// <summary>
    /// 방치형 파밍 전역 설정 (SO)
    /// PlotManager가 참조해서 사용
    /// </summary>
    [CreateAssetMenu(fileName = "PlotConfig", menuName = "Witch Chronicle/IdleFarming/PlotConfig")]
    public class PlotConfig : ScriptableObject
    {
        [Header("밭 개수")]
        [Tooltip("전체 밭 개수")]
        public int totalPlotCount = 10;
        
        [Tooltip("게임 시작 시 자동 해제된 밭 개수")]
        public int initialUnlockedCount = 2;
        
        [Header("잠금 해제 가격")]
        [Tooltip("각 밭 인덱스별 해제 가격 (초기 해제 밭은 무시)")]
        public int[] unlockPrices = new int[]
        {
            0, 0,           // Plot 0, 1: 초기 지급
            500,            // Plot 2
            1000,           // Plot 3
            2000,           // Plot 4
            4000,           // Plot 5
            7000,           // Plot 6
            12000,          // Plot 7
            20000,          // Plot 8
            35000           // Plot 9
        };
        
        [Header("저장 파일")]
        [Tooltip("PlayerPrefs 저장 키")]
        public string saveKey = "IdleFarming_SaveData";
        
        /// <summary>
        /// 특정 밭의 해제 가격 조회
        /// </summary>
        public int GetUnlockPrice(int plotIndex)
        {
            if (plotIndex < 0 || plotIndex >= unlockPrices.Length)
                return 0;
            return unlockPrices[plotIndex];
        }
        
        /// <summary>
        /// 해당 밭이 초기 해제 대상인지
        /// </summary>
        public bool IsInitiallyUnlocked(int plotIndex)
        {
            return plotIndex < initialUnlockedCount;
        }
    }
}