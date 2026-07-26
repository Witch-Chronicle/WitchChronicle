using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class DungeonSpawner : MonoBehaviour
{
    [Header("Minimap Settings")]
    [SerializeField] private GameObject _roomMarkerPrefab;
    [SerializeField] private float _markerYOffset = 10f;

    private Dictionary<RoomNode, RoomController> _roomControllers = new Dictionary<RoomNode, RoomController>();

    [Header("Containers")]
    [SerializeField] private Transform _environmentContainer;

    [Header("Dungeon Prefabs")]
    private GameObject _floorPrefab;
    private GameObject _wallPrefab;
    private GameObject _ceilingPrefab;

    [Header("Dungeon Settings")]
    [SerializeField] private float _wallHeight = 4f;
    [SerializeField] private float _ceilingHeight = 4f;
    [SerializeField] private float _tileSize = 1f;

    [Header("Corridor Settings")]
    [SerializeField] private int _corridorWidth = 4;

    [Header("Navigation")]
    [SerializeField] private NavMeshSurface _navMeshSurface;

    public float GetTileSize => _tileSize;

    private DungeonMeshBuilder _meshBuilder;

    private readonly HashSet<Vector2Int> _corridorTiles = new();

    public IReadOnlyCollection<Vector2Int> CorridorTiles => _corridorTiles;

    /// <summary>
    /// DungeonData에서 생성에 필요한 Prefab 데이터를 초기화한다.
    /// </summary>
    /// <param name="dungeon">던전 데이터</param>
    public void Initialize(DungeonData dungeon)
    {
        _floorPrefab = dungeon.FloorPrefab;
        _wallPrefab = dungeon.WallPrefab;
        _ceilingPrefab = dungeon.CeilingPrefab;

        _meshBuilder = new DungeonMeshBuilder();
    }


    /// <summary>
    /// 생성된 Dungeon Room 데이터를 실제 Scene 오브젝트로 생성한다.
    /// </summary>
    /// <param name="rooms">생성된 Room 목록</param>
    public void BuildDungeon(List<RoomNode> rooms)
    {
        Debug.Log($"[DungeonSpawner] 던전 빌드 시작 - 방 개수: {rooms.Count}");

        ClearEnvironment();

        HashSet<Vector2Int> floorPositions = GenerateFloorPositions(rooms);

        _corridorTiles.Clear();

        GenerateCorridors(rooms, floorPositions);

        HashSet<Vector2Int> wallPositions = CalculateWallPositions(floorPositions);

        _meshBuilder.BuildFloorMesh(_floorPrefab, floorPositions, _tileSize, _environmentContainer);
        _meshBuilder.BuildWallMesh(_wallPrefab, wallPositions, _tileSize, _wallHeight, _environmentContainer);
        _meshBuilder.BuildCeilingMesh(_ceilingPrefab, floorPositions, _tileSize, _ceilingHeight, _environmentContainer);

        CreateRoomControllers(rooms);
        CreateMinimapMarkers(rooms);
        BuildDungeonNavMesh();

        Debug.Log($"[DungeonSpawner] 던전 빌드 완료 - 총 바닥: {floorPositions.Count}개, 벽: {wallPositions.Count}개");
    }

    /// <summary>
    /// 기존 생성된 던전 오브젝트를 제거한다.
    /// </summary>
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

    /// <summary>
    /// Room 영역을 기반으로 바닥 좌표를 생성한다.
    /// </summary>
    /// <param name="rooms">Room 목록</param>
    /// <returns>바닥 좌표 목록</returns>
    private HashSet<Vector2Int> GenerateFloorPositions(List<RoomNode> rooms)
    {
        HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();

        foreach (RoomNode room in rooms)
        {
            Debug.Log($"[DungeonSpawner] 방 바닥 생성 - 타입: {room.Type}, 영역: {room.Bounds}");

            for (int x = room.Bounds.xMin; x < room.Bounds.xMax; x++)
            {
                for (int z = room.Bounds.yMin; z < room.Bounds.yMax; z++)
                {
                    floorPositions.Add(new Vector2Int(x, z));
                }
            }
        }

        return floorPositions;
    }

    /// <summary>
    /// Room 연결 정보를 기반으로 통로를 추가한다.
    /// </summary>
    /// <param name="rooms">Room 목록</param>
    /// <param name="floorPositions">바닥 좌표 목록</param>
    private void GenerateCorridors(List<RoomNode> rooms, HashSet<Vector2Int> floorPositions)
    {
        HashSet<RoomNode> visited = new HashSet<RoomNode>();

        foreach (RoomNode room in rooms)
        {
            visited.Add(room);

            foreach (RoomNode connectedRoom in room.ConnectedRooms)
            {
                if (visited.Contains(connectedRoom))
                {
                    continue;
                }

                BuildCorridor(room, connectedRoom, floorPositions);
            }
        }
    }

    /// <summary>
    /// 두 Room을 연결하는 L자 형태 통로를 생성한다. (넓은 폭 4~5 및 코너 끊김 방지 적용)
    /// </summary>
    /// <param name="startRoom">시작 방 노드</param>
    /// <param name="endRoom">목표 방 노드</param>
    /// <param name="floorPositions">바닥 좌표 목록</param>
    private void BuildCorridor(RoomNode startRoom, RoomNode endRoom, HashSet<Vector2Int> floorPositions)
    {
        Vector2Int start = GetSafeWallConnectionPoint(endRoom.Center, startRoom.Bounds);
        Vector2Int end = GetSafeWallConnectionPoint(startRoom.Center, endRoom.Bounds);

        int minX = Mathf.Min(start.x, end.x);
        int maxX = Mathf.Max(start.x, end.x);
        int minY = Mathf.Min(start.y, end.y);
        int maxY = Mathf.Max(start.y, end.y);

        int halfWidth = _corridorWidth / 2;
        int offsetStart = -halfWidth;
        int offsetEnd = offsetStart + _corridorWidth;

        // 수평 구간 전체 채우기
        for (int x = minX; x <= maxX; x++)
        {
            for (int w = offsetStart; w < offsetEnd; w++)
            {
                Vector2Int pos = new Vector2Int(x, start.y + w);

                floorPositions.Add(pos);
                _corridorTiles.Add(pos);
            }
        }

        // 수직 구간 전체 채우기 (코너 교차 영역 포함)
        for (int y = minY; y <= maxY; y++)
        {
            for (int w = offsetStart; w < offsetEnd; w++)
            {
                Vector2Int pos = new Vector2Int(end.x + w, y);

                floorPositions.Add(pos);
                _corridorTiles.Add(pos);
            }
        }
    }

    /// <summary>
    /// 방의 코너를 피해 평평한 벽면에 안전하게 연결되는 통로 입출력 좌표를 계산한다.
    /// </summary>
    /// <param name="fromCenter">상대 방의 중심 위치</param>
    /// <param name="targetBounds">대상 방의 영역</param>
    /// <returns>안전한 벽 연결 좌표</returns>
    private Vector2Int GetSafeWallConnectionPoint(Vector2Int fromCenter, RectInt targetBounds)
    {
        int xMin = targetBounds.xMin + 1;
        int xMax = targetBounds.xMax - 2;
        int yMin = targetBounds.yMin + 1;
        int yMax = targetBounds.yMax - 2;

        int clampedX = Mathf.Clamp(fromCenter.x, xMin, xMax);
        int clampedY = Mathf.Clamp(fromCenter.y, yMin, yMax);

        if (fromCenter.x < targetBounds.xMin)
        {
            return new Vector2Int(targetBounds.xMin, clampedY);
        }
        else if (fromCenter.x >= targetBounds.xMax)
        {
            return new Vector2Int(targetBounds.xMax - 1, clampedY);
        }
        else if (fromCenter.y < targetBounds.yMin)
        {
            return new Vector2Int(clampedX, targetBounds.yMin);
        }
        else
        {
            return new Vector2Int(clampedX, targetBounds.yMax - 1);
        }
    }

    /// <summary>
    /// 바닥 주변 빈 공간을 검사하여 벽 위치를 계산한다.
    /// </summary>
    /// <param name="floorPositions">바닥 좌표 목록</param>
    /// <returns>벽 좌표 목록</returns>
    private HashSet<Vector2Int> CalculateWallPositions(HashSet<Vector2Int> floorPositions)
    {
        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        HashSet<Vector2Int> wallPositions = new HashSet<Vector2Int>();

        foreach (Vector2Int floorPos in floorPositions)
        {
            foreach (Vector2Int direction in directions)
            {
                Vector2Int checkPos = floorPos + direction;

                if (!floorPositions.Contains(checkPos))
                {
                    wallPositions.Add(checkPos);
                }
            }
        }

        return wallPositions;
    }

    /// <summary>
    /// Room Controller를 생성하고 Room 데이터와 연결한다.
    /// </summary>
    /// <param name="rooms">Room 목록</param>
    private void CreateRoomControllers(List<RoomNode> rooms)
    {
        _roomControllers.Clear();

        foreach (RoomNode room in rooms)
        {
            GameObject roomObject = new GameObject($"Room_{room.Type}");

            roomObject.transform.position = new Vector3(
                room.Center.x * _tileSize,
                0f,
                room.Center.y * _tileSize
            );

            roomObject.transform.SetParent(_environmentContainer);

            RoomController controller = roomObject.AddComponent<RoomController>();

            controller.Initialize(room, _tileSize);

            room.RoomControllerInstance = controller;

            _roomControllers.Add(room, controller);
        }
    }

    /// <summary>
    /// 미니맵용 Room Marker를 생성한다.
    /// </summary>
    /// <param name="rooms">Room 목록</param>
    private void CreateMinimapMarkers(List<RoomNode> rooms)
    {
        if (_roomMarkerPrefab == null)
        {
            return;
        }

        foreach (RoomNode room in rooms)
        {
            if (!_roomControllers.TryGetValue(room, out RoomController controller))
            {
                continue;
            }

            Vector3 position = new Vector3(
                room.Center.x * _tileSize,
                _markerYOffset,
                room.Center.y * _tileSize
            );

            GameObject markerObject = Instantiate(
                _roomMarkerPrefab,
                position,
                Quaternion.Euler(90f, 0f, 0f),
                _environmentContainer
            );

            MinimapMarker marker = markerObject.GetComponent<MinimapMarker>();

            if (marker != null)
            {
                marker.SetupDefault(room.Type);
            }

            controller.RegisterMinimapMarker(marker);
        }
    }

    /// <summary>
    ///생성된 던전 기반으로 NavMesh를 생성한다.
    /// </summary>
    private void BuildDungeonNavMesh()
    {
        StartCoroutine(BuildNavMeshRoutine());
    }

    /// <summary>
    /// 다음 프레임 이후 NavMesh를 Bake한다.
    /// </summary>
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
        }
    }
}