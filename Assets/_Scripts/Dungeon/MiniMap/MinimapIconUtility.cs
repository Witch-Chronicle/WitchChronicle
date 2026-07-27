/// <summary>
/// 방 상태에 따른 미니맵 아이콘 타입을 판별하는 유틸리티 클래스이다.
/// </summary>
public static class MinimapIconUtility
{
    /// <summary>
    /// 방 노드의 상태와 타입에 맞는 아이콘 타입을 반환한다.
    /// </summary>
    /// <param name="room">방 노드 데이터</param>
    /// <returns>미니맵 아이콘 타입</returns>
    public static MinimapIconType GetIconType(RoomNode room)
    {
        if (room == null)
        {
            return MinimapIconType.Unknown;
        }

        if (!room.IsDiscovered)
        {
            return MinimapIconType.Unknown;
        }

        return room.Type switch
        {
            RoomType.Start => MinimapIconType.Start,
            RoomType.Battle => MinimapIconType.Battle,
            RoomType.Treasure => MinimapIconType.Treasure,
            RoomType.Shop => MinimapIconType.Shop,
            RoomType.Event => MinimapIconType.Event,
            RoomType.Boss => MinimapIconType.Boss,
            RoomType.Exit => MinimapIconType.Exit,
            _ => MinimapIconType.Unknown
        };
    }
}


/// <summary>
/// 미니맵에 표시되는 아이콘의 종류를 정의한다.
/// </summary>
public enum MinimapIconType
{
    Unknown,
    Start,
    Battle,
    Treasure,
    Shop,
    Event,
    Boss,
    Exit
}