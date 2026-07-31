using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

/// <summary>
/// 생성된 RoomNode 데이터를 기반으로 실제 던전 Mesh와 Runtime Controller를 생성한다.
/// </summary>
public class DungeonSpawner : MonoBehaviour
{
    [Header("Containers")]
    [SerializeField] private Transform _environmentContainer;

    [Header("Dungeon Settings")]
    [SerializeField] private float _wallHeight = 4f;
    [SerializeField] private float _ceilingHeight = 4f;
    [SerializeField] private float _tileSize = 1f;

    [Header("Corridor Settings")]
    [SerializeField] private int _corridorWidth = 4;

    [Header("Navigation")]
    [SerializeField] private NavMeshSurface _navMeshSurface;


    private GameObject _floorPrefab;
    private GameObject _wallPrefab;
    private GameObject _ceilingPrefab;

    private DungeonMeshBuilder _meshBuilder;

    private readonly Dictionary<RoomNode, RoomController> _roomControllers = new();

    private readonly HashSet<Vector2Int> _floorTiles = new();
    private readonly HashSet<Vector2Int> _wallTiles = new();
    private readonly HashSet<Vector2Int> _corridorTiles = new();


    public float GetTileSize => _tileSize;

    public IReadOnlyCollection<Vector2Int> FloorTiles => _floorTiles;

    public IReadOnlyCollection<Vector2Int> WallTiles => _wallTiles;

    public IReadOnlyCollection<Vector2Int> CorridorTiles => _corridorTiles;



    /// <summary>
    /// DungeonData에서 필요한 Prefab 데이터를 초기화한다.
    /// </summary>
    public void Initialize(DungeonData dungeon)
    {
        _floorPrefab = dungeon.FloorPrefab;
        _wallPrefab = dungeon.WallPrefab;
        _ceilingPrefab = dungeon.CeilingPrefab;

        _meshBuilder = new DungeonMeshBuilder();
    }



    /// <summary>
    /// RoomNode 데이터를 실제 Scene 던전으로 생성한다.
    /// </summary>
    public void BuildDungeon(List<RoomNode> rooms)
    {
        Debug.Log($"[DungeonSpawner] 던전 빌드 시작 : {rooms.Count}개 방");

        ClearEnvironment();

        _floorTiles.Clear();
        _wallTiles.Clear();
        _corridorTiles.Clear();


        _floorTiles.UnionWith(GenerateFloorPositions(rooms));

        GenerateCorridors(rooms, _floorTiles);

        _wallTiles.UnionWith(CalculateWallPositions(_floorTiles));

        _meshBuilder.BuildFloorMesh(
            _floorPrefab,
            _floorTiles,
            _tileSize,
            _environmentContainer);

        _meshBuilder.BuildWallMesh(
            _wallPrefab,
            _wallTiles,
            _tileSize,
            _wallHeight,
            _environmentContainer);

        _meshBuilder.BuildCeilingMesh(
            _ceilingPrefab,
            _floorTiles,
            _tileSize,
            _ceilingHeight,
            _environmentContainer);


        CreateRoomControllers(rooms);

        BuildDungeonNavMesh();


        Debug.Log($"[DungeonSpawner] 던전 빌드 완료 - Floor:{_floorTiles.Count}, Wall:{_wallTiles.Count}");
    }



    /// <summary>
    /// 이전에 생성된 던전 오브젝트 제거.
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
    /// Room 영역을 기반으로 Floor Tile 위치 생성.
    /// </summary>
    private HashSet<Vector2Int> GenerateFloorPositions(List<RoomNode> rooms)
    {
        HashSet<Vector2Int> floorPositions = new();


        foreach (RoomNode room in rooms)
        {
            Debug.Log($"[DungeonSpawner] Room Floor 생성 : {room.Type} {room.Bounds}");


            for (int x = room.Bounds.xMin; x < room.Bounds.xMax; x++)
            {
                for (int y = room.Bounds.yMin; y < room.Bounds.yMax; y++)
                {
                    floorPositions.Add(new Vector2Int(x, y));
                }
            }
        }

        return floorPositions;
    }



    /// <summary>
    /// Room 연결 Graph 기반 Corridor 생성.
    /// </summary>
    private void GenerateCorridors(List<RoomNode> rooms, HashSet<Vector2Int> floorPositions)
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


                BuildCorridor(
                    room,
                    connectedRoom,
                    floorPositions);
            }
        }
    }



    /// <summary>
    /// 두 Room 사이 L자 Corridor 생성.
    /// </summary>
    private void BuildCorridor(RoomNode startRoom, RoomNode endRoom, HashSet<Vector2Int> floorPositions)
    {
        Vector2Int start = GetSafeWallConnectionPoint(endRoom.Center, startRoom.Bounds);

        Vector2Int end = GetSafeWallConnectionPoint(startRoom.Center, endRoom.Bounds);

        int minX = Mathf.Min(start.x, end.x);
        int maxX = Mathf.Max(start.x, end.x);

        int minY = Mathf.Min(start.y, end.y);
        int maxY = Mathf.Max(start.y, end.y);


        int halfWidth = _corridorWidth / 2;


        for (int x = minX; x <= maxX; x++)
        {
            for (int offset = -halfWidth; offset < halfWidth; offset++)
            {
                Vector2Int position = new Vector2Int(x, start.y + offset);

                floorPositions.Add(position);
                _corridorTiles.Add(position);
            }
        }


        for (int y = minY; y <= maxY; y++)
        {
            for (int offset = -halfWidth; offset < halfWidth; offset++)
            {
                Vector2Int position =new Vector2Int(end.x + offset, y);

                floorPositions.Add(position);
                _corridorTiles.Add(position);
            }
        }
    }



    /// <summary>
    /// Room 벽면에 연결 가능한 위치 계산.
    /// </summary>
    private Vector2Int GetSafeWallConnectionPoint(Vector2Int fromCenter, RectInt targetBounds)
    {
        int xMin = targetBounds.xMin + 1;
        int xMax = targetBounds.xMax - 2;

        int yMin = targetBounds.yMin + 1;
        int yMax = targetBounds.yMax - 2;

        int clampX = Mathf.Clamp(fromCenter.x, xMin, xMax);

        int clampY = Mathf.Clamp(fromCenter.y, yMin, yMax);

        if (fromCenter.x < targetBounds.xMin)
        {
            return new Vector2Int(
                targetBounds.xMin,
                clampY);
        }

        if (fromCenter.x >= targetBounds.xMax)
        {
            return new Vector2Int(
                targetBounds.xMax - 1,
                clampY);
        }

        if (fromCenter.y < targetBounds.yMin)
        {
            return new Vector2Int(
                clampX,
                targetBounds.yMin);
        }

        return new Vector2Int(
            clampX,
            targetBounds.yMax - 1);
    }



    /// <summary>
    /// Floor 주변 빈 공간을 찾아 Wall Tile 생성.
    /// </summary>
    private HashSet<Vector2Int> CalculateWallPositions(HashSet<Vector2Int> floorPositions)
    {
        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };


        HashSet<Vector2Int> walls = new();


        foreach (Vector2Int floor in floorPositions)
        {
            foreach (Vector2Int direction in directions)
            {
                Vector2Int check =
                    floor + direction;


                if (!floorPositions.Contains(check))
                {
                    walls.Add(check);
                }
            }
        }

        return walls;
    }



    /// <summary>
    /// RoomNode와 Scene RoomController 연결.
    /// </summary>
    private void CreateRoomControllers(List<RoomNode> rooms)
    {
        _roomControllers.Clear();


        foreach (RoomNode room in rooms)
        {
            GameObject roomObject =
                new GameObject(
                    $"Room_{room.Type}");


            roomObject.transform.position =
                new Vector3(
                    room.Center.x * _tileSize,
                    0f,
                    room.Center.y * _tileSize);


            roomObject.transform.SetParent(
                _environmentContainer);



            RoomController controller =
                roomObject.AddComponent<RoomController>();


            controller.Initialize(
                room,
                _tileSize);


            room.RoomControllerInstance =
                controller;


            _roomControllers.Add(
                room,
                controller);
        }
    }



    /// <summary>
    /// 생성된 Mesh 기반 NavMesh Bake.
    /// </summary>
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