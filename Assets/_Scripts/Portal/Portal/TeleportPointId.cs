/// <summary>
/// 빠른 이동 목적지 식별자. TeleportDestination과 TeleportPanel의 고정 버튼을
/// 서로 매칭시키는 데 사용한다.
/// </summary>
public enum TeleportPointId
{
    None,
    Main,
    Street,
    Farm,
    Fishing,
}
/// <summary>
/// TeleportPointId를 사용자에게 보여줄 한글 이름으로 변환한다.
/// </summary>
public static class TeleportPointIdExtensions
{
    public static string ToDisplayName(this TeleportPointId id)
    {
        switch (id)
        {
            case TeleportPointId.Main:
                return "광장";
            case TeleportPointId.Street:
                return "번화가";
            case TeleportPointId.Farm:
                return "농장";
            case TeleportPointId.Fishing:
                return "낚시터";
            default:
                return string.Empty;
        }
    }
}