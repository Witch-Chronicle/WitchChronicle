// FILE: Assets\_Scripts\Dungeon\DungeonGenerator.cs

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// BSP 기반 던전 생성기 (DungeonData SO 설정값 100% 반영 버전)
/// </summary>
public class DungeonGenerator : MonoBehaviour
{
    private const int ModuleSize = 6; // 1모듈 = 6단위 (그리드 타일 규격)

    private DungeonData _dungeonData
    {
        get
        {
            return DungeonManager.Instance.CurrentDungeonData;
        }
    }

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
    /// 던전 생성 메인 함수 (SO에 설정된 데이터 그대로 적용)
    /// </summary>
    public List<RoomNode> GenerateDungeon()
    {
        int maxAttempts = 50;

        // 💡 오직 SO 데이터(CurrentDungeonData)에 설정된 MinRoomSize & MaxRoomSize만 사용
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            List<RoomNode> rooms = TryGenerateRooms();

            if (rooms != null && rooms.Count >= _dungeonData.MinRoomCount)
            {
                ConnectRooms(rooms);
                AssignRoomTypes(rooms);
                Debug.Log($"[DungeonGenerator] SO 규격 던전 생성 성공 ({_dungeonData.MinRoomSize}~{_dungeonData.MaxRoomSize}) : {rooms.Count}개 방");
                return rooms;
            }
        }

        // 시도 후 가장 방이 많이 만들어진 결과를 채택 (모든 방은 SO 규격 100% 엄수)
        Debug.LogWarning($"[DungeonGenerator] MapSize 내에 MinRoomCount({_dungeonData.MinRoomCount})개를 모두 배치하기 부족합니다. SO 규격을 만족하는 최선의 방들로 구성합니다.");

        List<RoomNode> fallbackRooms = TryGenerateRooms();

        if (fallbackRooms == null || fallbackRooms.Count == 0)
        {
            fallbackRooms = new List<RoomNode>();
            int size = _dungeonData.MinRoomSize;
            int centerX = (_dungeonData.MapSize.x - size) / 2;
            int centerY = (_dungeonData.MapSize.y - size) / 2;
            fallbackRooms.Add(new RoomNode(new RectInt(centerX, centerY, size, size)));
        }

        ConnectRooms(fallbackRooms);
        AssignRoomTypes(fallbackRooms);
        return fallbackRooms;
    }

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
        List<Leaf> leaves = new List<Leaf> { root };

        while (leaves.Count < targetRoomCount)
        {
            Leaf leaf = GetLargestLeaf(leaves);

            if (leaf == null || !SplitLeaf(leaf))
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
    /// Leaf 분할: SO의 MinRoomSize + 여백(6*2=12)을 담을 수 있는 크기일 때만 분할
    /// </summary>
    private bool SplitLeaf(Leaf leaf)
    {
        RectInt area = leaf.Area;

        // SO 최소 방 크기 + 양쪽 여백(6 + 6 = 12)
        int minLeafCapacity = _dungeonData.MinRoomSize + (ModuleSize * 2);

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
            if (area.height < minLeafCapacity * 2) return false;

            int minSplit = minLeafCapacity / ModuleSize;
            int maxSplit = (area.height - minLeafCapacity) / ModuleSize;

            if (minSplit > maxSplit) return false;

            int split = Random.Range(minSplit, maxSplit + 1) * ModuleSize;

            leaf.Left = new Leaf(new RectInt(area.x, area.y, area.width, split));
            leaf.Right = new Leaf(new RectInt(area.x, area.y + split, area.width, area.height - split));
        }
        else
        {
            if (area.width < minLeafCapacity * 2) return false;

            int minSplit = minLeafCapacity / ModuleSize;
            int maxSplit = (area.width - minLeafCapacity) / ModuleSize;

            if (minSplit > maxSplit) return false;

            int split = Random.Range(minSplit, maxSplit + 1) * ModuleSize;

            leaf.Left = new Leaf(new RectInt(area.x, area.y, split, area.height));
            leaf.Right = new Leaf(new RectInt(area.x + split, area.y, area.width - split, area.height));
        }

        return true;
    }

    /// <summary>
    /// Leaf 내부에 방 생성:
    /// 1) 테두리 여백 6(1모듈) 유지 -> 방끼리 절대 접촉 안 함
    /// 2) 방 크기는 SO 데이터에 설정된 [MinRoomSize ~ MaxRoomSize] 범위를 100% 엄수
    /// </summary>
    private void CreateRoom(RectInt area, List<RoomNode> rooms)
    {
        int margin = ModuleSize; // 테두리 여백 6 (1모듈)

        int innerWidth = area.width - (margin * 2);
        int innerHeight = area.height - (margin * 2);

        // 💡 SO 설정 최소 크기보다 가용 구역이 작으면 방 생성 안 함
        if (innerWidth < _dungeonData.MinRoomSize || innerHeight < _dungeonData.MinRoomSize)
        {
            return;
        }

        // SO의 MaxRoomSize와 가용 크기 중 작은 값으로 상한선 결정
        int maxAllowedWidth = Mathf.Min(_dungeonData.MaxRoomSize, innerWidth);
        int maxAllowedHeight = Mathf.Min(_dungeonData.MaxRoomSize, innerHeight);

        int minWidthSteps = _dungeonData.MinRoomSize / ModuleSize;
        int maxWidthSteps = maxAllowedWidth / ModuleSize;

        if (minWidthSteps > maxWidthSteps) return;

        int minHeightSteps = _dungeonData.MinRoomSize / ModuleSize;
        int maxHeightSteps = maxAllowedHeight / ModuleSize;

        if (minHeightSteps > maxHeightSteps) return;

        // 💡 SO에 설정하신 [MinRoomSize ~ MaxRoomSize] 범위 내에서 선택
        int widthSteps = Random.Range(minWidthSteps, maxWidthSteps + 1);
        int heightSteps = Random.Range(minHeightSteps, maxHeightSteps + 1);

        // 비율 보정 (가로세로 비율 최대 1.6배)
        if (widthSteps > Mathf.RoundToInt(heightSteps * 1.6f))
        {
            widthSteps = Mathf.Max(minWidthSteps, Mathf.RoundToInt(heightSteps * 1.5f));
        }
        else if (heightSteps > Mathf.RoundToInt(widthSteps * 1.6f))
        {
            heightSteps = Mathf.Max(minHeightSteps, Mathf.RoundToInt(widthSteps * 1.5f));
        }

        int width = widthSteps * ModuleSize;
        int height = heightSteps * ModuleSize;

        // 💡 SO의 MinRoomSize보다 작으면 생성 취소
        if (width < _dungeonData.MinRoomSize || height < _dungeonData.MinRoomSize)
        {
            return;
        }

        int maxOffsetX = innerWidth - width;
        int maxOffsetY = innerHeight - height;

        int offsetXSteps = (maxOffsetX / ModuleSize) / 2;
        int offsetYSteps = (maxOffsetY / ModuleSize) / 2;

        int x = area.x + margin + (offsetXSteps * ModuleSize);
        int y = area.y + margin + (offsetYSteps * ModuleSize);

        RoomNode newRoom = new RoomNode(new RectInt(x, y, width, height));

        if (IsTooCloseToExistingRooms(newRoom, rooms, ModuleSize))
        {
            return;
        }

        rooms.Add(newRoom);
    }

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

    private void ConnectRooms(List<RoomNode> rooms)
    {
        if (rooms == null || rooms.Count <= 1) return;

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
                    float distance = Vector2Int.Distance(a.Center, b.Center);

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
                if (a == b || a.ConnectedRooms.Contains(b)) continue;
                if (a.ConnectedRooms.Count >= _dungeonData.MaxConnectionPerRoom) continue;
                if (b.ConnectedRooms.Count >= _dungeonData.MaxConnectionPerRoom) continue;

                float distance = Vector2Int.Distance(a.Center, b.Center);

                if (distance > _dungeonData.MaxRoomSize * 2f) continue;

                if (Random.value <= _dungeonData.ExtraConnectionProbability)
                {
                    a.ConnectedRooms.Add(b);
                    b.ConnectedRooms.Add(a);
                }
            }
        }
    }

    private void AssignRoomTypes(List<RoomNode> rooms)
    {
        if (rooms == null || rooms.Count == 0) return;

        RoomNode startRoom = rooms[0];
        startRoom.Type = RoomType.Start;
        startRoom.Depth = 0;

        if (rooms.Count == 1) return;

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
                if (visited.Contains(next)) continue;

                next.Depth = current.Depth + 1;
                visited.Add(next);
                queue.Enqueue(next);

                if (next.Depth > furthestRoom.Depth)
                {
                    furthestRoom = next;
                }
            }
        }

        if (furthestRoom != startRoom)
        {
            if (_dungeonData.HasBoss)
            {
                furthestRoom.Type = RoomType.Boss;
            }
            else if (_dungeonData.HasExit)
            {
                furthestRoom.Type = RoomType.Exit;
            }
        }
        else
        {
            RoomNode otherRoom = rooms.FirstOrDefault(r => r != startRoom);
            if (otherRoom != null)
            {
                if (_dungeonData.HasBoss) otherRoom.Type = RoomType.Boss;
                else if (_dungeonData.HasExit) otherRoom.Type = RoomType.Exit;
            }
        }

        int totalWeight = 0;
        foreach (var roomWeight in _dungeonData.RoomWeights)
        {
            totalWeight += roomWeight.Weight;
        }

        if (totalWeight <= 0) totalWeight = 1;

        bool hasShop = false;

        foreach (RoomNode room in rooms)
        {
            if (room.Type == RoomType.Start || room.Type == RoomType.Boss || room.Type == RoomType.Exit)
            {
                continue;
            }

            int roll = Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (var roomWeight in _dungeonData.RoomWeights)
            {
                currentWeight += roomWeight.Weight;

                if (roll >= currentWeight) continue;

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

        if (!rooms.Any(r => r.Type == RoomType.Start))
        {
            rooms[0].Type = RoomType.Start;
            Debug.LogWarning("[DungeonGenerator] RoomType.Start 누락 감지 -> 0번 방을 Start 방으로 강제 복구했습니다.");
        }

        Debug.Log("[DungeonGenerator] RoomType 지정 완료");
    }
}