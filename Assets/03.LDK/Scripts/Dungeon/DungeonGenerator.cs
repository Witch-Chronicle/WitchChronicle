using System.Collections.Generic;
using UnityEngine;

/// <summary> 구조
///          GenerateDungeon()
///          |
///          ├── PartitionSpace()
///          |       └── BSP 공간 분할 → Room 생성
///          |
///          ├── ConnectRooms()
///          |      ├── MST 기반 필수 연결
///          |      └── Random Cycle 추가
///          |      
///          └── AssignRoomTypes()
///                  ├── BFS 거리 계산
///                  ├── Start 지정
///                  ├── Boss 지정
///                  └── Room Type 확률 배정
/// </summary>

public class DungeonGenerator : MonoBehaviour
{
    // BSP는 하나의 큰 공간을 여러 개의 작은 영역으로 분할하는 알고리즘이다.
    // BSP 방식으로 던전 생성

    [Header("Dungeon Settings")]
    
    private DungeonData _dungeonData
    {
        get
        {
            return DungeonManager.Instance.CurrentDungeonData;
        }
    }

    public List<RoomNode> GenerateDungeon()
    {
        // 생성된 방을 저장할 Room 리스트 
        List<RoomNode> rooms = new List<RoomNode>();

        // 전체 맵 생성 -> 분할 할 큰 맵을 생성
        RectInt fullMap = new RectInt(0, 0, _dungeonData.MapSize.x, _dungeonData.MapSize.y);
        
        PartitionSpace(fullMap, _dungeonData.BSPDepth, rooms);
        ConnectRooms(rooms);
        AssignRoomTypes(rooms);
        
        // 생성된 Room 의 리스트 를 반환함
        return rooms;
    }

    
    /// <summary>
    /// BSP의 핵심 함수. 하나의 큰 공간을 여러개 의 작은 공간으로 분할하는 함수(재귀 함수 로 구현됨)
    /// 큰 공간을 재귀적으로 계속 분할하여 최종적으로 Room을 생성한다.
    /// </summary>
    /// <param name="area">현재 분할 대상 영역</param>
    /// <param name="depth">남은 분할 횟수(BSP 반복 횟수)</param>
    /// <param name="rooms">생성된 Room을 저장할 리스트</param>
    private void PartitionSpace(RectInt area, int depth, List<RoomNode> rooms)
    {
        //종료 조건, 더 이상 분할하지 않는 마지막 영역이면 Room 을 생성
        if (depth <= 0)
        {   
            // 겹침 을 방지하기 위한 틈 
            int padding = 0;
            int maxWidth = area.width - _dungeonData.MinPadding;
            int maxHeight = area.height - _dungeonData.MinPadding;

            // 분할된 구역이 최소 방 크기보다 작아지면 방을 생성하지 않고 리턴
            if (maxWidth < _dungeonData.MinRoomSize || maxHeight < _dungeonData.MinRoomSize)
            {
                return;
            }

            // Room 의 크기는 정해진 범위 안에서 랜덤으로 결정 -> 다양한 형태 가능
            int minSize = Mathf.Max(_dungeonData.MinRoomSize, Mathf.Min(area.width - padding, area.height - padding));
            int width = Random.Range(_dungeonData.MinRoomSize, Mathf.Min(_dungeonData.MaxRoomSize, area.width));
            int height = Random.Range(_dungeonData.MinRoomSize, Mathf.Min(_dungeonData.MaxRoomSize, area.height));
            // Room 의 위치 는 중앙이 아님, 랜덤하게, -1 은 틈 을 위한것
            int x = area.x + Random.Range(1, area.width - width - padding);
            int y = area.y + Random.Range(1, area.height - height - padding);
            
            rooms.Add(new RoomNode(new RectInt(x, y, width, height)));
            return;
        }

        // 분할 방향 결정
        // 기본적으로 랜덤하게(50 %), 하지만 비율 제한을 추가
        bool splitHorizontally = Random.value > 0.5f;

        // 비율 제한, 가로가 길면(세로의 1.5배) 세로 분할로 강제
        if (area.width > area.height * 1.5f)
        {
            splitHorizontally = false;
        }
        // 반대로, 세로가 길면 강제로 가로 분할
        else if (area.height > area.width * 1.5f)
        {
            splitHorizontally = true;
        }

        // 실제 분할 
        if (splitHorizontally)
        {
            // 가로 분할, 딱 절반이 아니라 삼등분 해서 딱 중간의 부분 안에서 랜덤하게
            int splitY = Random.Range(area.height / 3, area.height * 2 / 3);
            // 위쪽 부분 을 분할 depth - 1 을 해주면서, 횟수를 차감 
            PartitionSpace(new RectInt(area.x, area.y, area.width, splitY), depth - 1, rooms);
            // 아래쪽 부분을 분할
            PartitionSpace(new RectInt(area.x, area.y + splitY, area.width, area.height - splitY), depth - 1, rooms);
        }
        else
        {
            //세로 로 분할
            int splitX = Random.Range(area.width / 3, area.width * 2 / 3);
            PartitionSpace(new RectInt(area.x, area.y, splitX, area.height), depth - 1, rooms);
            PartitionSpace(new RectInt(area.x + splitX, area.y, area.width - splitX, area.height), depth - 1, rooms);
        }
    }

    /// <summary>
    /// (모든 노드를 최소한의 간선으로 연결하는 그래프 알고리즘)MST 기반 Room 연결, 생성된 Room들을 Graph 구조로 연결한다.
    /// </summary>
    /// <param name="rooms">생성된 room 들의 리스트</param>
    private void ConnectRooms(List<RoomNode> rooms)
    {
        // 방어 코드
        if (rooms.Count == 0)
        {
            return;
        }

        // 연결된 그룹과 미연결 그룹을 분리
        // 처음에는 아무것도 연결 x, 첫 번쨰 room 을 넣는다
        List<RoomNode> connected = new List<RoomNode> { rooms[0] };
        // 모든 room을 다 넣고, 첫번째 room 을 삭제함, connected 에 있으니깐
        List<RoomNode> unconnected = new List<RoomNode>(rooms);
        unconnected.RemoveAt(0);

        // 모든 room 이 연결될 때 까지
        while (unconnected.Count > 0)
        {
            // 가장 가까운 Room 을 저장하기 위한 변수들
            RoomNode bestA = null;
            RoomNode bestB = null;
            // 처음에는 무한대로 시작
            float minDistance = float.MaxValue;

            foreach (var a in connected)
            {
                foreach (var b in unconnected)
                {
                    // 모든 조합을 비교한 후에
                    // 가장 가까운 Room 찾기, 중심 좌표끼리 거리 계산
                    float dist = Vector2Int.Distance(a.Center, b.Center);

                    // 짧은 값으로 계속 초기화
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestA = a;
                        bestB = b;
                    }
                }
            }

            // 양방향 연결
            bestA.ConnectedRooms.Add(bestB);
            bestB.ConnectedRooms.Add(bestA);
            // 연결한 Room 을 추가 하고 제거.
            connected.Add(bestB);
            unconnected.Remove(bestB);
        }

        // 추가 연결을 랜덤하게 생성, 확률에 따라서 Room 끼리 추가로 연결됨 -> 서로 연결된 경로가 더 생성됨
        foreach (var a in rooms)
        {
            foreach (var b in rooms)
            {
                // 자기 자신이거나 이미 연결되어 있으면 제외
                if (a == b || a.ConnectedRooms.Contains(b))
                {
                    continue;
                }

                // 이미 연결이 많은 방은 제외 
                if (a.ConnectedRooms.Count >= _dungeonData.MaxConnectionPerRoom || b.ConnectedRooms.Count >= _dungeonData.MaxConnectionPerRoom)
                {
                    continue;
                }

                // 너무 멀리 떨어진 방은 연결하지 않음
                if (Vector2Int.Distance(a.Center, b.Center) > _dungeonData.MaxRoomSize * 1.8f)
                {
                    continue;
                }

                // 확률적으로 추가 연결 생성
                if (Random.value < _dungeonData.ExtraConnectionProbability)
                {
                    a.ConnectedRooms.Add(b);
                    b.ConnectedRooms.Add(a);
                }
            }
        }
    }

    /// <summary>
    /// 생성된 Room 들 에게 역할 을 부여하는 함수(RoomType)
    /// </summary>
    /// <param name="rooms">생성된 room 들의 리스트</param>
    private void AssignRoomTypes(List<RoomNode> rooms)
    {
        if (rooms.Count == 0)
        {
            return;
        }

        // 첫번째 room 을 시작 방 으로 고정
        RoomNode startRoom = rooms[0];
        startRoom.Type = RoomType.Start;
        startRoom.Depth = 0; // 깊이 는 0

        // BFS 너비 우선 탐색, Queue 로 구현
        Queue<RoomNode> queue = new Queue<RoomNode>();
        // 중복 방지 를 위한 HashSet
        HashSet<RoomNode> visited = new HashSet<RoomNode> { startRoom };
        queue.Enqueue(startRoom);

        RoomNode furthestRoom = startRoom;
        int maxDist = 0;

        while (queue.Count > 0)
        {
            // Queue 에서 순차적으로 room 을 꺼냄
            RoomNode current = queue.Dequeue();
            // 가까운 순서대로 탐색
            foreach (var neighbor in current.ConnectedRooms)
            {
                if (!visited.Contains(neighbor))
                {
                    neighbor.Depth = current.Depth + 1;
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                    
                    if (neighbor.Depth > maxDist)
                    {
                        maxDist = neighbor.Depth;
                        furthestRoom = neighbor;
                    }
                }
            }
        }
        // 다 끝나면, 생성된 Room 들에게는 depth 값이 다 지정되 있음

        if (_dungeonData.HasBoss)
        {
            furthestRoom.Type = RoomType.Boss;
        }
        else if (_dungeonData.HasExit)
        {
            furthestRoom.Type = RoomType.Exit;
        }

        // 각 방의 확률 계산, 총합은 100 이 되도록
        int totalWeight = 0;

        foreach (var rw in _dungeonData.RoomWeights)
        {
            totalWeight += rw.Weight;
        }

        bool hasShop = false;

        if (totalWeight <= 0)
        {
            totalWeight = 1;
        }

        // 나머지 방 가중치 랜덤 선택 알고리즘 (누적합 알고리즘)
        foreach (var room in rooms)
        {
            // 시작 방이랑 보스 방은 제외
            if (room.Type == RoomType.Start || room.Type == RoomType.Boss || room.Type == RoomType.Exit)
            {
                continue;
            }

            // 0 ~ 100 랜덤하게
            int roll = Random.Range(0, totalWeight);
            int currentSum = 0;

            foreach (var rw in _dungeonData.RoomWeights)
            {
                currentSum += rw.Weight;

                if (roll < currentSum)
                {
                    // Shop은 최대 1개 제한
                    if (rw.Type == RoomType.Shop)
                    {
                        if (hasShop)
                        {
                            // 이미 Shop이 존재하면 Battle로 대체
                            room.Type = RoomType.Battle;
                        }
                        else
                        {
                            room.Type = RoomType.Shop;
                            hasShop = true;
                        }
                    }
                    else
                    {
                        room.Type = rw.Type;
                    }

                    break;
                }
            }
        }
    }
}