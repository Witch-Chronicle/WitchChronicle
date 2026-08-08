using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BSP 기반 던전 생성기 (6x6 모듈 규격 스냅 및 스택 오버플로우 방지 적용)
/// </summary>
public class DungeonGenerator : MonoBehaviour
{
    private const int ModuleSize = 6;

    [Header("Dungeon Settings")]
    private DungeonData _dungeonData
    {
        get
        {
            return DungeonManager.Instance.CurrentDungeonData;
        }
    }

    /// <summary>
    /// BSP Leaf
    /// </summary>
    private class Leaf
    {
        public RectInt Area;

        public Leaf Left;
        public Leaf Right;

        public bool IsLeaf => Left == null && Right == null;

        public Leaf(RectInt area)
        {
            Area = area;
        }
    }

    /// <summary>
    /// 던전 생성 메인 함수 (최대 20회 재시도 제한으로 StackOverflowException 방지)
    /// </summary>
    public List<RoomNode> GenerateDungeon()
    {
        int maxAttempts = 20; // 최대 재시도 횟수 제한 (무한 재귀 호출 방지)
        int attempt = 0;

        while (attempt < maxAttempts)
        {
            attempt++;
            List<RoomNode> rooms = TryGenerateRooms();

            if (rooms != null && rooms.Count >= _dungeonData.MinRoomCount)
            {
                ConnectRooms(rooms);
                AssignRoomTypes(rooms);
                Debug.Log($"[DungeonGenerator] 던전 생성 성공 (시도 {attempt}회) : {rooms.Count}개 방");
                return rooms;
            }
        }

        // 최대 시도 횟수 초과 시 현재까지 생성된 방들로 진행 (에러 방지)
        Debug.LogWarning("[DungeonGenerator] 최대 시도 횟수 초과. 현재 생성된 방들로 진행합니다.");
        List<RoomNode> fallbackRooms = TryGenerateRooms();
        ConnectRooms(fallbackRooms);
        AssignRoomTypes(fallbackRooms);
        return fallbackRooms;
    }

    /// <summary>
    /// 실제 방 시도 생성
    /// </summary>
    private List<RoomNode> TryGenerateRooms()
    {
        List<RoomNode> rooms = new List<RoomNode>();

        int targetRoomCount = Random.Range(
            _dungeonData.MinRoomCount,
            _dungeonData.MaxRoomCount + 1);

        RectInt fullArea = new RectInt(
            0,
            0,
            _dungeonData.MapSize.x,
            _dungeonData.MapSize.y);

        Leaf root = new Leaf(fullArea);

        List<Leaf> leaves = new List<Leaf>
        {
            root
        };

        while (leaves.Count < targetRoomCount)
        {
            Leaf leaf = GetLargestLeaf(leaves);

            if (leaf == null)
            {
                break;
            }

            if (!SplitLeaf(leaf))
            {
                break;
            }

            leaves.Remove(leaf);

            leaves.Add(leaf.Left);
            leaves.Add(leaf.Right);
        }

        foreach (Leaf leaf in leaves)
        {
            CreateRoom(leaf.Area, rooms);
        }

        return rooms;
    }

    /// <summary>
    /// 가장 큰 Leaf 선택
    /// </summary>
    private Leaf GetLargestLeaf(List<Leaf> leaves)
    {
        Leaf largest = null;

        int largestArea = -1;

        foreach (Leaf leaf in leaves)
        {
            int area = leaf.Area.width * leaf.Area.height;

            if (area > largestArea)
            {
                largestArea = area;
                largest = leaf;
            }
        }

        return largest;
    }

    /// <summary>
    /// Leaf 분할 (6의 배수 단위 스냅)
    /// </summary>
    private bool SplitLeaf(Leaf leaf)
    {
        RectInt area = leaf.Area;

        bool splitHorizontal = Random.value > 0.5f;

        if (area.width > area.height * 1.5f)
        {
            splitHorizontal = false;
        }
        else if (area.height > area.width * 1.5f)
        {
            splitHorizontal = true;
        }

        if (splitHorizontal)
        {
            if (area.height < _dungeonData.MinRoomSize * 2)
            {
                return false;
            }

            int minSplit = _dungeonData.MinRoomSize / ModuleSize;
            int maxSplit = (area.height - _dungeonData.MinRoomSize) / ModuleSize;

            if (minSplit > maxSplit)
            {
                return false;
            }

            int split = Random.Range(minSplit, maxSplit + 1) * ModuleSize;

            leaf.Left = new Leaf(
                new RectInt(
                    area.x,
                    area.y,
                    area.width,
                    split));

            leaf.Right = new Leaf(
                new RectInt(
                    area.x,
                    area.y + split,
                    area.width,
                    area.height - split));
        }
        else
        {
            if (area.width < _dungeonData.MinRoomSize * 2)
            {
                return false;
            }

            int minSplit = _dungeonData.MinRoomSize / ModuleSize;
            int maxSplit = (area.width - _dungeonData.MinRoomSize) / ModuleSize;

            if (minSplit > maxSplit)
            {
                return false;
            }

            int split = Random.Range(minSplit, maxSplit + 1) * ModuleSize;

            leaf.Left = new Leaf(
                new RectInt(
                    area.x,
                    area.y,
                    split,
                    area.height));

            leaf.Right = new Leaf(
                new RectInt(
                    area.x + split,
                    area.y,
                    area.width - split,
                    area.height));
        }

        return true;
    }

    /// <summary>
    /// Leaf 영역 안에 Room 생성 (방과 방 사이 최소 1모듈(6단위) 통로 간격 보장)
    /// </summary>
    private void CreateRoom(RectInt area, List<RoomNode> rooms)
    {
        int minCorridorGap = ModuleSize; 

        int padding = Mathf.Max(minCorridorGap, _dungeonData.MinPadding);

        int maxWidth = area.width - (padding * 2);
        int maxHeight = area.height - (padding * 2);

        if (maxWidth < _dungeonData.MinRoomSize ||
            maxHeight < _dungeonData.MinRoomSize)
        {
            padding = minCorridorGap;
            maxWidth = area.width - (padding * 2);
            maxHeight = area.height - (padding * 2);
        }

        if (maxWidth < _dungeonData.MinRoomSize ||
            maxHeight < _dungeonData.MinRoomSize)
        {
            return;
        }

        int minWidthSteps = _dungeonData.MinRoomSize / ModuleSize;
        int maxWidthSteps = Mathf.Min(_dungeonData.MaxRoomSize, maxWidth) / ModuleSize;

        if (minWidthSteps > maxWidthSteps)
        {
            return;
        }

        int width = Random.Range(minWidthSteps, maxWidthSteps + 1) * ModuleSize;

        int minHeightSteps = _dungeonData.MinRoomSize / ModuleSize;
        int maxHeightSteps = Mathf.Min(_dungeonData.MaxRoomSize, maxHeight) / ModuleSize;

        if (minHeightSteps > maxHeightSteps)
        {
            return;
        }

        int height = Random.Range(minHeightSteps, maxHeightSteps + 1) * ModuleSize;

        int maxOffsetX = Mathf.Max(0, maxWidth - width);
        int maxOffsetY = Mathf.Max(0, maxHeight - height);

        int randomXSteps = Random.Range(0, (maxOffsetX / ModuleSize) + 1);
        int randomYSteps = Random.Range(0, (maxOffsetY / ModuleSize) + 1);

        int x = area.x + padding + (randomXSteps * ModuleSize);
        int y = area.y + padding + (randomYSteps * ModuleSize);

        RoomNode newRoom = new RoomNode(
            new RectInt(
                x,
                y,
                width,
                height));

        if (IsTooCloseToExistingRooms(newRoom, rooms, minCorridorGap))
        {
            return;
        }

        rooms.Add(newRoom);

        Debug.Log(
            $"[DungeonGenerator] Room 생성 (통로 공간 보장됨) : {newRoom.Bounds}");
    }

    /// <summary>
    /// 기존 방들과의 거리 검사 (최소 minGap 미만으로 바짝 붙어있는지 확인)
    /// </summary>
    private bool IsTooCloseToExistingRooms(RoomNode newRoom, List<RoomNode> existingRooms, int minGap)
    {
        foreach (RoomNode existing in existingRooms)
        {
            RectInt expandedBounds = new RectInt(
                existing.Bounds.x - minGap,
                existing.Bounds.y - minGap,
                existing.Bounds.width + (minGap * 2),
                existing.Bounds.height + (minGap * 2)
            );

            if (expandedBounds.Overlaps(newRoom.Bounds))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// MST 기반 Room 연결
    /// </summary>
    private void ConnectRooms(List<RoomNode> rooms)
    {
        if (rooms.Count <= 1)
        {
            return;
        }

        List<RoomNode> connected = new List<RoomNode>();
        List<RoomNode> unconnected = new List<RoomNode>(rooms);

        connected.Add(unconnected[0]);
        unconnected.RemoveAt(0);

        while (unconnected.Count > 0)
        {
            RoomNode bestA = null;
            RoomNode bestB = null;

            float shortest = float.MaxValue;

            foreach (RoomNode a in connected)
            {
                foreach (RoomNode b in unconnected)
                {
                    float distance =
                        Vector2Int.Distance(
                            a.Center,
                            b.Center);

                    if (distance < shortest)
                    {
                        shortest = distance;
                        bestA = a;
                        bestB = b;
                    }
                }
            }

            if (bestA != null && bestB != null)
            {
                if (!bestA.ConnectedRooms.Contains(bestB)) bestA.ConnectedRooms.Add(bestB);
                if (!bestB.ConnectedRooms.Contains(bestA)) bestB.ConnectedRooms.Add(bestA);

                connected.Add(bestB);
                unconnected.Remove(bestB);
            }
        }

        foreach (RoomNode a in rooms)
        {
            foreach (RoomNode b in rooms)
            {
                if (a == b)
                {
                    continue;
                }

                if (a.ConnectedRooms.Contains(b))
                {
                    continue;
                }

                if (a.ConnectedRooms.Count >= _dungeonData.MaxConnectionPerRoom)
                {
                    continue;
                }

                if (b.ConnectedRooms.Count >= _dungeonData.MaxConnectionPerRoom)
                {
                    continue;
                }

                float distance =
                    Vector2Int.Distance(
                        a.Center,
                        b.Center);

                if (distance > _dungeonData.MaxRoomSize * 2f)
                {
                    continue;
                }

                if (Random.value <= _dungeonData.ExtraConnectionProbability)
                {
                    a.ConnectedRooms.Add(b);
                    b.ConnectedRooms.Add(a);
                }
            }
        }

        Debug.Log("[DungeonGenerator] Room 연결 완료");
    }

    /// <summary>
    /// Room Type 지정
    /// </summary>
    private void AssignRoomTypes(List<RoomNode> rooms)
    {
        if (rooms.Count == 0)
        {
            return;
        }

        RoomNode startRoom = rooms[0];
        startRoom.Type = RoomType.Start;
        startRoom.Depth = 0;

        Queue<RoomNode> queue = new Queue<RoomNode>();
        HashSet<RoomNode> visited = new HashSet<RoomNode>();

        queue.Enqueue(startRoom);
        visited.Add(startRoom);

        RoomNode furthestRoom = startRoom;

        while (queue.Count > 0)
        {
            RoomNode current = queue.Dequeue();

            foreach (RoomNode next in current.ConnectedRooms)
            {
                if (visited.Contains(next))
                {
                    continue;
                }

                next.Depth = current.Depth + 1;

                visited.Add(next);
                queue.Enqueue(next);

                if (next.Depth > furthestRoom.Depth)
                {
                    furthestRoom = next;
                }
            }
        }

        if (_dungeonData.HasBoss)
        {
            furthestRoom.Type = RoomType.Boss;
        }
        else if (_dungeonData.HasExit)
        {
            furthestRoom.Type = RoomType.Exit;
        }

        int totalWeight = 0;

        foreach (var roomWeight in _dungeonData.RoomWeights)
        {
            totalWeight += roomWeight.Weight;
        }

        if (totalWeight <= 0)
        {
            totalWeight = 1;
        }

        bool hasShop = false;

        foreach (RoomNode room in rooms)
        {
            if (room.Type == RoomType.Start)
            {
                continue;
            }

            if (room.Type == RoomType.Boss)
            {
                continue;
            }

            if (room.Type == RoomType.Exit)
            {
                continue;
            }

            int roll = Random.Range(0, totalWeight);

            int currentWeight = 0;

            foreach (var roomWeight in _dungeonData.RoomWeights)
            {
                currentWeight += roomWeight.Weight;

                if (roll >= currentWeight)
                {
                    continue;
                }

                if (roomWeight.Type == RoomType.Shop)
                {
                    if (hasShop)
                    {
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
                    room.Type = roomWeight.Type;
                }

                break;
            }
        }

        Debug.Log("[DungeonGenerator] RoomType 지정 완료");
    }
}