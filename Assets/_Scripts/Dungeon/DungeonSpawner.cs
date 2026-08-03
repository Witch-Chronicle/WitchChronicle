using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

/// <summary>
/// 개별 벽의 위치와 회전 정보를 담는 구조체.
/// </summary>
public struct WallData
{
    public Vector2 Position;
    public Quaternion Rotation;

    public WallData(Vector2 position, Quaternion rotation)
    {
        Position = position;
        Rotation = rotation;
    }
}

/// <summary>
/// 방과 통로 연결 지점(문)의 위치와 회전 정보를 담는 구조체.
/// </summary>
public struct DoorData
{
    public Vector2 Position;
    public Quaternion Rotation;

    public DoorData(Vector2 position, Quaternion rotation)
    {
        Position = position;
        Rotation = rotation;
    }
}

/// <summary>
/// DungeonData 및 RoomNode 기반으로 메시, 룸 컨트롤러, 문, 네비메시를 생성하는 클래스.
/// </summary>
public class DungeonSpawner : MonoBehaviour
{
    private struct ChunkWallRule
    {
        public Vector2Int ChunkOffset;
        public Vector2 WorldPosOffset;
        public float RotationY;

        public ChunkWallRule(Vector2Int chunkOffset, Vector2 worldPosOffset, float rotationY)
        {
            ChunkOffset = chunkOffset;
            WorldPosOffset = worldPosOffset;
            RotationY = rotationY;
        }
    }

    /// <summary>
    /// 방 하나가 차지하는 청크 좌표 범위를 담는 구조체 (문 배치 시 경계 판정에 사용).
    /// </summary>
    private struct RoomChunkBounds
    {
        public int Left;
        public int Right;
        public int Bottom;
        public int Top;

        public RoomChunkBounds(int left, int right, int bottom, int top)
        {
            Left = left;
            Right = right;
            Bottom = bottom;
            Top = top;
        }

        public bool Contains(Vector2Int chunk)
        {
            return chunk.x >= Left && chunk.x <= Right && chunk.y >= Bottom && chunk.y <= Top;
        }
    }

    [Header("Containers")]
    [SerializeField] private Transform _environmentContainer;

    [Header("Dungeon Settings")]
    [SerializeField] private float _wallHeight = 4f;
    [SerializeField] private float _ceilingHeight = 4f;
    [SerializeField] private float _tileSize = 1f;
    [SerializeField] private float _wallSize = 6f;

    [Header("Corridor Settings")]
    [SerializeField] private int _corridorWidth = 6;

    [Header("Navigation")]
    [SerializeField] private NavMeshSurface _navMeshSurface;

    [Header("Door Pivot Correction")]
    [Tooltip("DoorPrefab의 피벗이 문틀 정중앙이 아닐 때, 문이 바라보는 방향(로컬 X: 좌우, 로컬 Z: 전후) 기준으로 보정할 오프셋. " +
             "오른쪽으로 쏠려 있다면 X 오프셋을 조절하여 중앙으로 맞출 수 있습니다.")]
    [SerializeField] private Vector3 _doorPivotOffset = Vector3.zero;

    private GameObject _floorPrefab;
    private GameObject _wallPrefab;
    private GameObject _ceilingPrefab;
    private GameObject _doorPrefab;
    private GameObject _doorWallPrefab;

    private DungeonMeshBuilder _meshBuilder;

    private readonly Dictionary<RoomNode, RoomController> _roomControllers = new();
    private readonly HashSet<Vector2Int> _floorTiles = new();
    private readonly HashSet<Vector2> _wallTiles = new();
    private readonly List<WallData> _wallDataList = new();
    private readonly List<WallData> _doorWallDataList = new();
    private readonly List<DoorData> _doorDataList = new();
    private readonly HashSet<Vector2Int> _corridorTiles = new();

    public float GetTileSize => _tileSize;
    public IReadOnlyCollection<Vector2Int> FloorTiles => _floorTiles;
    public IReadOnlyCollection<Vector2> WallTiles => _wallTiles;
    public IReadOnlyCollection<Vector2Int> CorridorTiles => _corridorTiles;

    /// <summary>
    /// 청크 기반으로 계산된 실제 벽 조각들의 위치/회전 데이터.
    /// RoomContentSpawner가 벽 데코레이션 배치 시 레이캐스트 대신 이 데이터를 직접 재사용한다.
    /// </summary>
    public IReadOnlyList<WallData> WallDataList => _wallDataList;

    public void Initialize(DungeonData dungeon)
    {
        if (dungeon == null)
        {
            Debug.LogError("[DungeonSpawner] 전달된 DungeonData가 null입니다.");
            return;
        }

        _floorPrefab = dungeon.FloorPrefab;
        _wallPrefab = dungeon.WallPrefab;
        _ceilingPrefab = dungeon.CeilingPrefab;
        _doorPrefab = dungeon.DoorPrefab;
        _doorWallPrefab = dungeon.DoorWallPrefab;

        if (_doorWallPrefab == null)
        {
            Debug.LogWarning("[DungeonSpawner] DungeonData에 DoorWallPrefab이 지정되지 않았습니다.");
        }

        _meshBuilder = new DungeonMeshBuilder();
    }

    public void BuildDungeon(List<RoomNode> rooms)
    {
        if (_meshBuilder == null)
        {
            Debug.LogError("[DungeonSpawner] DungeonMeshBuilder가 초기화되지 않았습니다.");
            return;
        }

        Debug.Log($"[DungeonSpawner] 던전 빌드 시작 : {rooms.Count}개 방");

        ClearEnvironment();

        _floorTiles.Clear();
        _wallTiles.Clear();
        _wallDataList.Clear();
        _doorWallDataList.Clear();
        _doorDataList.Clear();
        _corridorTiles.Clear();

        int step = Mathf.RoundToInt(_wallSize);
        if (step < 1)
        {
            step = 6;
        }

        HashSet<Vector2Int> floorChunks = new HashSet<Vector2Int>();
        GenerateRoomChunks(rooms, floorChunks, step);
        GenerateCorridorChunks(rooms, floorChunks, step);

        _floorTiles.UnionWith(ExpandChunksToFloorTiles(floorChunks, step));
        _wallDataList.AddRange(CalculateWallDataFromChunks(floorChunks, step));

        Debug.Log($"[DungeonSpawner] 타일 수 계산 완료 - 바닥: {_floorTiles.Count}, 벽: {_wallDataList.Count}, 문틀벽: {_doorWallDataList.Count}, 문: {_doorDataList.Count}");

        _meshBuilder.BuildFloorMesh(_floorPrefab, _floorTiles, _tileSize, _environmentContainer);
        _meshBuilder.BuildWallMesh(_wallPrefab, _wallDataList, _tileSize, _wallHeight, _environmentContainer);

        if (_doorWallPrefab != null && _doorWallDataList.Count > 0)
        {
            _meshBuilder.BuildWallMesh(_doorWallPrefab, _doorWallDataList, _tileSize, _wallHeight, _environmentContainer);
        }

        _meshBuilder.BuildCeilingMesh(_ceilingPrefab, _floorTiles, _tileSize, _ceilingHeight, _environmentContainer);
        SpawnDoors();

        CreateRoomControllers(rooms);
        BuildDungeonNavMesh();

        Debug.Log($"[DungeonSpawner] 던전 빌드 완료 - Floor:{_floorTiles.Count}, Wall:{_wallDataList.Count}, Door:{_doorDataList.Count}");
    }

    private void ClearEnvironment()
    {
        if (_environmentContainer == null)
        {
            return;
        }

        foreach (Transform child in _environmentContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void GenerateRoomChunks(List<RoomNode> rooms, HashSet<Vector2Int> floorChunks, int step)
    {
        foreach (RoomNode room in rooms)
        {
            int xMin = Mathf.FloorToInt((float)room.Bounds.xMin / step);
            int xMax = Mathf.CeilToInt((float)room.Bounds.xMax / step);
            int yMin = Mathf.FloorToInt((float)room.Bounds.yMin / step);
            int yMax = Mathf.CeilToInt((float)room.Bounds.yMax / step);

            if (xMax <= xMin)
            {
                xMax = xMin + 1;
            }
            if (yMax <= yMin)
            {
                yMax = yMin + 1;
            }

            for (int x = xMin; x < xMax; x++)
            {
                for (int y = yMin; y < yMax; y++)
                {
                    floorChunks.Add(new Vector2Int(x, y));
                }
            }
        }
    }

    private void GenerateCorridorChunks(List<RoomNode> rooms, HashSet<Vector2Int> floorChunks, int step)
    {
        HashSet<RoomNode> visited = new();

        foreach (RoomNode room in rooms)
        {
            visited.Add(room);

            foreach (RoomNode connectedRoom in room.ConnectedRooms)
            {
                if (visited.Contains(connectedRoom))
                {
                    continue;
                }

                BuildCorridorChunks(room, connectedRoom, floorChunks, step);
            }
        }
    }

    private void BuildCorridorChunks(RoomNode startRoom, RoomNode endRoom, HashSet<Vector2Int> floorChunks, int step)
    {
        Vector2Int startChunk = GetAlignedConnectionPoint(endRoom.Center, startRoom.Bounds, step);
        Vector2Int endChunk = GetAlignedConnectionPoint(startRoom.Center, endRoom.Bounds, step);

        List<Vector2Int> corridorPath = new List<Vector2Int>();

        int x = startChunk.x;
        int y = startChunk.y;

        corridorPath.Add(new Vector2Int(x, y));
        floorChunks.Add(new Vector2Int(x, y));
        _corridorTiles.Add(new Vector2Int(x, y));

        while (x != endChunk.x)
        {
            if (endChunk.x > x)
            {
                x++;
            }
            else
            {
                x--;
            }

            corridorPath.Add(new Vector2Int(x, y));
            floorChunks.Add(new Vector2Int(x, y));
            _corridorTiles.Add(new Vector2Int(x, y));
        }

        while (y != endChunk.y)
        {
            if (endChunk.y > y)
            {
                y++;
            }
            else
            {
                y--;
            }

            corridorPath.Add(new Vector2Int(x, y));
            floorChunks.Add(new Vector2Int(x, y));
            _corridorTiles.Add(new Vector2Int(x, y));
        }

        CalculateCorridorDoors(corridorPath, startRoom.Bounds, endRoom.Bounds, step);
    }

    private void CalculateCorridorDoors(List<Vector2Int> corridorPath, RectInt startBounds, RectInt endBounds, int step)
    {
        if (corridorPath == null || corridorPath.Count < 2)
        {
            return;
        }

        float edgeNear = -0.5f;
        float edgeFar = step - 0.5f;
        float center = (step - 1) * 0.5f;

        RoomChunkBounds startChunkBounds = GetRoomChunkBounds(startBounds, step);
        RoomChunkBounds endChunkBounds = GetRoomChunkBounds(endBounds, step);

        bool startDoorPlaced = false;

        for (int i = 0; i < corridorPath.Count - 1; i++)
        {
            Vector2Int current = corridorPath[i];
            Vector2Int next = corridorPath[i + 1];

            bool currentInside = startChunkBounds.Contains(current);
            bool nextInside = startChunkBounds.Contains(next);

            if (currentInside && !nextInside)
            {
                Vector2Int dir = next - current;
                AddDoorAtBoundary(current, dir, edgeNear, edgeFar, center, step);
                startDoorPlaced = true;
                break;
            }
        }

        if (!startDoorPlaced)
        {
            Vector2Int startChunk = corridorPath[0];
            Vector2Int nextChunk = corridorPath[1];
            Vector2Int startDir = nextChunk - startChunk;
            AddDoorAtBoundary(startChunk, startDir, edgeNear, edgeFar, center, step);
        }

        bool endDoorPlaced = false;

        for (int i = corridorPath.Count - 1; i > 0; i--)
        {
            Vector2Int current = corridorPath[i];
            Vector2Int prev = corridorPath[i - 1];

            bool currentInside = endChunkBounds.Contains(current);
            bool prevInside = endChunkBounds.Contains(prev);

            if (currentInside && !prevInside)
            {
                Vector2Int dir = prev - current;
                AddDoorAtBoundary(current, dir, edgeNear, edgeFar, center, step);
                endDoorPlaced = true;
                break;
            }
        }

        if (!endDoorPlaced)
        {
            Vector2Int endChunk = corridorPath[corridorPath.Count - 1];
            Vector2Int prevChunk = corridorPath[corridorPath.Count - 2];
            Vector2Int endDir = prevChunk - endChunk;
            AddDoorAtBoundary(endChunk, endDir, edgeNear, edgeFar, center, step);
        }
    }

    private RoomChunkBounds GetRoomChunkBounds(RectInt bounds, int step)
    {
        int left = Mathf.FloorToInt((float)bounds.xMin / step);
        int right = Mathf.CeilToInt((float)bounds.xMax / step) - 1;
        int bottom = Mathf.FloorToInt((float)bounds.yMin / step);
        int top = Mathf.CeilToInt((float)bounds.yMax / step) - 1;

        return new RoomChunkBounds(left, right, bottom, top);
    }

    private void AddDoorAtBoundary(Vector2Int chunk, Vector2Int dir, float edgeNear, float edgeFar, float center, int step)
    {
        Vector2 chunkWorldPos = new Vector2(chunk.x * step, chunk.y * step);
        Vector2 doorPos = Vector2.zero;
        float rotY = 0f;

        if (dir == new Vector2Int(0, 1))
        {
            doorPos = chunkWorldPos + new Vector2(center, edgeFar);
            rotY = 0f;
        }
        else if (dir == new Vector2Int(0, -1))
        {
            doorPos = chunkWorldPos + new Vector2(center, edgeNear);
            rotY = 180f;
        }
        else if (dir == new Vector2Int(-1, 0))
        {
            doorPos = chunkWorldPos + new Vector2(edgeNear, center);
            rotY = 270f;
        }
        else if (dir == new Vector2Int(1, 0))
        {
            doorPos = chunkWorldPos + new Vector2(edgeFar, center);
            rotY = 90f;
        }

        _doorDataList.Add(new DoorData(doorPos, Quaternion.Euler(0f, rotY, 0f)));

        float wallRotY = (rotY + 180f) % 360f;
        _doorWallDataList.Add(new WallData(doorPos, Quaternion.Euler(0f, wallRotY, 0f)));
    }

    private Vector2Int GetAlignedConnectionPoint(Vector2Int fromCenter, RectInt targetBounds, int step)
    {
        int left = Mathf.FloorToInt((float)targetBounds.xMin / step);
        int right = Mathf.CeilToInt((float)targetBounds.xMax / step) - 1;

        int bottom = Mathf.FloorToInt((float)targetBounds.yMin / step);
        int top = Mathf.CeilToInt((float)targetBounds.yMax / step) - 1;

        int targetChunkX = Mathf.RoundToInt((float)fromCenter.x / step);
        int targetChunkY = Mathf.RoundToInt((float)fromCenter.y / step);

        if (targetChunkX < left)
        {
            return new Vector2Int(left, Mathf.Clamp(targetChunkY, bottom, top));
        }

        if (targetChunkX > right)
        {
            return new Vector2Int(right, Mathf.Clamp(targetChunkY, bottom, top));
        }

        if (targetChunkY < bottom)
        {
            return new Vector2Int(Mathf.Clamp(targetChunkX, left, right), bottom);
        }

        if (targetChunkY > top)
        {
            return new Vector2Int(Mathf.Clamp(targetChunkX, left, right), top);
        }

        int distLeft = Mathf.Abs(targetChunkX - left);
        int distRight = Mathf.Abs(right - targetChunkX);
        int distBottom = Mathf.Abs(targetChunkY - bottom);
        int distTop = Mathf.Abs(top - targetChunkY);

        int min = Mathf.Min(distLeft, distRight, distBottom, distTop);

        if (min == distLeft)
        {
            return new Vector2Int(left, targetChunkY);
        }

        if (min == distRight)
        {
            return new Vector2Int(right, targetChunkY);
        }

        if (min == distBottom)
        {
            return new Vector2Int(targetChunkX, bottom);
        }

        return new Vector2Int(targetChunkX, top);
    }

    private HashSet<Vector2Int> ExpandChunksToFloorTiles(HashSet<Vector2Int> floorChunks, int step)
    {
        HashSet<Vector2Int> floorPositions = new();

        foreach (var chunk in floorChunks)
        {
            int worldX = chunk.x * step;
            int worldY = chunk.y * step;

            for (int dx = 0; dx < step; dx++)
            {
                for (int dy = 0; dy < step; dy++)
                {
                    floorPositions.Add(new Vector2Int(worldX + dx, worldY + dy));
                }
            }
        }

        return floorPositions;
    }

    private List<WallData> CalculateWallDataFromChunks(HashSet<Vector2Int> floorChunks, int step)
    {
        Dictionary<Vector2, Quaternion> wallDict = new();

        float edgeNear = -0.5f;
        float edgeFar = step - 0.5f;
        float center = (step - 1) * 0.5f;

        ChunkWallRule[] rules = new ChunkWallRule[]
        {
            new ChunkWallRule(new Vector2Int(0, 1), new Vector2(center, edgeFar), 180f),
            new ChunkWallRule(new Vector2Int(0, -1), new Vector2(center, edgeNear), 0f),
            new ChunkWallRule(new Vector2Int(-1, 0), new Vector2(edgeNear, center), 90f),
            new ChunkWallRule(new Vector2Int(1, 0), new Vector2(edgeFar, center), 270f)
        };

        foreach (var chunk in floorChunks)
        {
            Vector2 chunkWorldPos = new Vector2(chunk.x * step, chunk.y * step);

            foreach (var rule in rules)
            {
                Vector2Int neighborChunk = chunk + rule.ChunkOffset;

                if (!floorChunks.Contains(neighborChunk))
                {
                    Vector2 exactWallPos = chunkWorldPos + rule.WorldPosOffset;

                    if (!wallDict.ContainsKey(exactWallPos))
                    {
                        wallDict[exactWallPos] = Quaternion.Euler(0f, rule.RotationY, 0f);
                    }
                }
            }
        }

        _wallTiles.Clear();
        List<WallData> wallDataList = new();

        foreach (var kvp in wallDict)
        {
            _wallTiles.Add(kvp.Key);
            wallDataList.Add(new WallData(kvp.Key, kvp.Value));
        }

        Debug.Log($"[DungeonSpawner] 청크 기반 벽 데이터 계산 완료: 총 {wallDataList.Count}개 생성");
        return wallDataList;
    }

    private void SpawnDoors()
    {
        if (_doorPrefab == null)
        {
            Debug.LogWarning("[DungeonSpawner] DungeonData에 지정된 _doorPrefab이 null입니다.");
            return;
        }

        foreach (DoorData doorData in _doorDataList)
        {
            Vector3 spawnPosition = new Vector3(
                doorData.Position.x * _tileSize,
                0f,
                doorData.Position.y * _tileSize);

            Vector3 correctedPosition = spawnPosition + doorData.Rotation * _doorPivotOffset;

            Instantiate(_doorPrefab, correctedPosition, doorData.Rotation, _environmentContainer);
        }

        Debug.Log($"[DungeonSpawner] 통로 입구/출구 문 프리팹 생성 완료: 총 {_doorDataList.Count}개 생성");
    }

    private void CreateRoomControllers(List<RoomNode> rooms)
    {
        _roomControllers.Clear();

        foreach (RoomNode room in rooms)
        {
            GameObject roomObject = new GameObject($"Room_{room.Type}");

            roomObject.transform.position = new Vector3(
                room.Center.x * _tileSize,
                0f,
                room.Center.y * _tileSize);

            roomObject.transform.SetParent(_environmentContainer);

            RoomController controller = roomObject.AddComponent<RoomController>();
            controller.Initialize(room, _tileSize);

            room.RoomControllerInstance = controller;
            _roomControllers.Add(room, controller);
        }
    }

    private void BuildDungeonNavMesh()
    {
        StartCoroutine(BuildNavMeshRoutine());
    }

    private IEnumerator BuildNavMeshRoutine()
    {
        yield return null;

        if (_navMeshSurface == null)
        {
            _navMeshSurface = GetComponent<NavMeshSurface>();
        }

        if (_navMeshSurface != null)
        {
            _navMeshSurface.RemoveData();
            _navMeshSurface.BuildNavMesh();

            Debug.Log("[DungeonSpawner] NavMesh 생성 완료");
        }
    }
}