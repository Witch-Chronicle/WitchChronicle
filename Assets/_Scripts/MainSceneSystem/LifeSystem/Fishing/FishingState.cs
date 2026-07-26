/// <summary>
/// 낚시 상태 머신
/// </summary>
public enum FishingState
{
    Idle,       // 낚시 UI 열림, 대기 전
    Casting,    // 낚싯대 던지는 중 (0.5초)
    Waiting,    // 물기 대기 (3~8초 랜덤)
    Bite,       // 물었음, 낚아채기 판정 (1.5초 내)
    Reeling,    // 텐션 미니게임 진행 중
    Result      // 결과 표시
}