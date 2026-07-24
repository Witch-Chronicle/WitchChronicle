using System.Collections.Generic;
using UnityEngine;

public enum RoomType { Start, Battle, Treasure, Shop, Event, Boss, Exit }

public class RoomNode 
{
    // 실제 room 이 차지하는 영역
    // Unity 에 RectInt -> 실제 크기와 위치
    // x, y 좌표 와 width, height 까지 포함된 변수, 겹치는지 판별 하기 쉬움
    public RectInt Bounds;
    public RoomType Type;

    // 그래프의 연결 정보를 저장, 그래프 구조의 핵심 -> 서로 연결 시킴
    public List<RoomNode> ConnectedRooms;
    
    public RoomController RoomControllerInstance { get; set; }

    // 길찾기 및 타입 배정용 (시작점으로부터의 거리, 깊이)
    public int Depth;

    /// <summary>
    /// RoomNode 의 생성자
    /// </summary>
    /// <param name="bounds">실제 RoomNode 가 차지하는 영역</param>
    public RoomNode(RectInt bounds) 
    {
        Bounds = bounds;
        Type = RoomType.Battle;
        ConnectedRooms = new List<RoomNode>();
        Depth = 0;
    }

    public Vector2Int Center => new Vector2Int(Bounds.x + Bounds.width / 2, Bounds.y + Bounds.height / 2);
}