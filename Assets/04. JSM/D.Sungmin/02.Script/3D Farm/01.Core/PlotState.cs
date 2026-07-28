namespace WitchChronicle.IdleFarming
{
    /// <summary>
    /// 방치형 파밍 밭의 상태
    /// </summary>
    public enum PlotState
    {
        Locked,             // 잠긴 상태 (골드로 해제 필요)
        Empty,              // 해제됐지만 씨앗 없음
        Growing,            // 씨앗 심어져 자라는 중
        ReadyToHarvest      // 수확 대기 (열매 완성)
    }
}