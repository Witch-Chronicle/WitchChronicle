using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 던전 생성 시점에 각 방의 타입에 맞는 자원을 절차적으로 연산하고 의존성을 조립·주입하는 총괄 클래스.
/// </summary>
public class RoomContentSpawner : MonoBehaviour
{
    [SerializeField] private Transform _contentContainer;
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private FieldPartySpawner _fieldPartySpawner;
    [SerializeField] private float _yOffset = 2f;
    [SerializeField] private float _yOffsetDeco = 2f;
    [SerializeField] private float _yOffsetWallDeco = 2f;

    [Tooltip("벽 데코가 벽면에서 실내 방향으로 얼마나 떨어져 배치될지(타일 단위). " +
             "WallData.Position은 벽의 정확한 경계 좌표이므로, 여기서 room 안쪽으로 살짝 밀어 넣는 용도로만 쓰인다.")]
    [SerializeField] private float _wallInsetOffset = 0.5f;

    [Tooltip("방 경계 판정 시 부동소수점 오차를 흡수하기 위한 여유값. " +
             "DungeonSpawner의 wallSize(step)와 무관하게 0.6 정도면 충분하다.")]
    [SerializeField] private float _wallBoundsMargin = 0.6f;

    private DungeonData _dungeon;
    private RoomContentTable _table;
    private readonly HashSet<Vector2Int> _corridorTiles = new();
    private readonly List<WallData> _wallDataList = new();

    /// <summary>
    /// 외부에서 던전 데이터를 주입받아 초기화한다.
    /// </summary>
    /// <param name="dungeon">던전 데이터</param>
    public void Initialize(DungeonData dungeon)
    {
        _dungeon = dungeon;
        _table = dungeon.RoomContentTable;
    }

    /// <summary>
    /// DungeonSpawner가 생성한 Corridor Tile 정보를 전달받는다.
    /// </summary>
    /// <param name="corridorTiles">통로 타일 목록</param>
    public void SetCorridorTiles(IEnumerable<Vector2Int> corridorTiles)
    {
        _corridorTiles.Clear();

        foreach (Vector2Int tile in corridorTiles)
        {
            _corridorTiles.Add(tile);
        }

        Debug.Log($"[RoomContentSpawner] Corridor Tile 등록 : {_corridorTiles.Count}");
    }

    /// <summary>
    /// DungeonSpawner가 청크 기반으로 계산한 실제 벽 위치/회전 데이터를 전달받는다.
    /// 벽 데코레이션 배치 시 레이캐스트 대신 이 데이터를 직접 사용해 "실제로 벽이 존재하는 자리"에만 배치한다.
    /// </summary>
    /// <param name="wallDataList">DungeonSpawner.WallDataList</param>
    public void SetWallData(IEnumerable<WallData> wallDataList)
    {
        _wallDataList.Clear();

        foreach (WallData wall in wallDataList)
        {
            _wallDataList.Add(wall);
        }

        Debug.Log($"[RoomContentSpawner] Wall Data 등록 : {_wallDataList.Count}");
    }

    /// <summary>
    /// 각 Room type에 맞는 상호작용 전략을 생성하고 데이터 의존성을 주입한다.
    /// </summary>
    /// <param name="rooms">방 목록</param>
    /// <param name="tileSize">타일 크기</param>
    public void SpawnContent(List<RoomNode> rooms, float tileSize)
    {
        if (_contentContainer != null)
        {
            foreach (Transform child in _contentContainer)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (var room in rooms)
        {
            RoomController roomController = room.RoomControllerInstance;

            Debug.Log($"[RoomContentSpawner] 콘텐츠 스폰 체크 시작 : {room.Type}");

            if (roomController == null)
            {
                Debug.LogWarning($"[RoomContentSpawner] RoomNode [{room.Type}] 에 RoomController 인스턴스가 연결되어 있지 않음.");
                continue;
            }

            roomController.Initialize(room, tileSize);

            Vector3 spawnPos = roomController.transform.position;
            spawnPos.y = _yOffset;

            Vector3 newRoomCenter = new Vector3(room.Center.x, 0f, room.Center.y);

            if (_dungeon.Fog != null)
            {
                Instantiate(_dungeon.Fog, newRoomCenter, Quaternion.identity);
            }

            switch (room.Type)
            {
                case RoomType.Start:
                {
                    SpawnPlayer(spawnPos);
                    break;
                }
                case RoomType.Battle:
                {
                    var battleComp = roomController.gameObject.AddComponent<BattleRoomInteraction>();
                    EnemyGroupSO selectedGroup = GetRandomEnemyGroup(room.Depth);
                    List<EnemyBattleData> enemies = GenerateRandomEnemyList(selectedGroup, room.Depth);

                    battleComp.Setup(_table.battleEncounterPrefab, enemies, room);
                    roomController.InjectInteraction(battleComp);
                    roomController.SpawnRoomContent();
                    break;
                }
                case RoomType.Treasure:
                {
                    var treasureComp = roomController.gameObject.AddComponent<TreasureRoomInteraction>();
                    treasureComp.Setup(_table.chestPrefabs, _yOffsetDeco);
                    roomController.InjectInteraction(treasureComp);
                    roomController.SpawnRoomContent();
                    break;
                }
                case RoomType.Shop:
                {
                    var shopComp = roomController.gameObject.AddComponent<ShopRoomInteraction>();
                    shopComp.Setup(_table.shopKeeperPrefab, _yOffsetDeco);
                    roomController.InjectInteraction(shopComp);
                    roomController.SpawnRoomContent();
                    break;
                }
                case RoomType.Boss:
                {
                    var bossComp = roomController.gameObject.AddComponent<BossRoomInteraction>();
                    EnemyGroupSO selectedGroup = GetRandomEnemyGroup(room.Depth);
                    bossComp.Setup(_table.bossEncounterPrefab, _table.bossData, room);
                    roomController.InjectInteraction(bossComp);
                    roomController.SpawnRoomContent();
                    break;
                }
                case RoomType.Event:
                {
                    var eventComp = roomController.gameObject.AddComponent<EventRoomInteraction>();
                    eventComp.Setup(_table.eventRoomTableSO, _yOffsetDeco);
                    roomController.InjectInteraction(eventComp);
                    roomController.SpawnRoomContent();
                    break;
                }
                case RoomType.Exit:
                {
                    var exitComp = roomController.gameObject.AddComponent<ExitRoomInteraction>();
                    exitComp.Setup(_table.exitPortalPrefab, _yOffsetDeco);
                    roomController.InjectInteraction(exitComp);
                    roomController.SpawnRoomContent();
                    break;
                }
            }

            SpawnRoomDecorations(room, roomController, tileSize);
        }
    }

    /// <summary>
    /// 방의 물리 구역(Bounds) 데이터 기반으로 장식용 오브젝트들을 배치한다.
    /// </summary>
    /// <param name="room">방 노드</param>
    /// <param name="controller">방 컨트롤러</param>
    /// <param name="tileSize">타일 크기</param>
    private void SpawnRoomDecorations(RoomNode room, RoomController controller, float tileSize)
    {
        if (_dungeon == null || _dungeon.DecorationTable == null)
        {
            return;
        }

        List<DecorationTable.DecorationEntry> entries = _dungeon.DecorationTable.GetEntries(room.Type);

        foreach (var entry in entries)
        {
            if (entry.prefabs == null || entry.prefabs.Count == 0)
            {
                continue;
            }

            if (entry.placement == PlacementType.Wall)
            {
                List<WallCandidate> candidates = GetWallCandidatesForRoom(room, tileSize);

                for (int i = 0; i < candidates.Count; i++)
                {
                    int randIdx = Random.Range(i, candidates.Count);
                    var temp = candidates[i];
                    candidates[i] = candidates[randIdx];
                    candidates[randIdx] = temp;
                }

                int spawnCount = Mathf.Clamp(Random.Range(entry.minCount, entry.maxCount + 1), 0, candidates.Count);

                for (int i = 0; i < spawnCount; i++)
                {
                    GameObject selectedPrefab = entry.prefabs[Random.Range(0, entry.prefabs.Count)];
                    Instantiate(selectedPrefab, candidates[i].position, candidates[i].rotation, controller.transform);
                }

                Debug.Log($"[RoomContentSpawner] 벽(Wall) 데코레이션 배치 완료: 방 타입 [{room.Type}] / 후보: {candidates.Count} / 생성 수: {spawnCount}");
                continue;
            }

            if (entry.placement == PlacementType.Corner)
            {
                int xMin = room.Bounds.xMin;
                int xMax = room.Bounds.xMax - 1;
                int yMin = room.Bounds.yMin;
                int yMax = room.Bounds.yMax - 1;

                Vector2Int[] cornerGridPositions = new Vector2Int[]
                {
                    new Vector2Int(xMin, yMin),
                    new Vector2Int(xMax, yMin),
                    new Vector2Int(xMin, yMax),
                    new Vector2Int(xMax, yMax)
                };

                foreach (var gridPos in cornerGridPositions)
                {
                    Vector3 spawnPos = new Vector3(gridPos.x * tileSize, _yOffsetDeco, gridPos.y * tileSize);
                    GameObject selectedPrefab = entry.prefabs[Random.Range(0, entry.prefabs.Count)];
                    GameObject instance = Instantiate(selectedPrefab, spawnPos, Quaternion.identity, controller.transform);

                    Debug.Log($"[RoomContentSpawner] 코너(Corner) 데코레이션 배치 완료 (패딩 없음, 겹침 허용): {instance.name} at {spawnPos}");
                }
                continue;
            }

            int floorSpawnCount = Random.Range(entry.minCount, entry.maxCount + 1);
            List<Vector2Int> validFloorPositions = GetValidFloorPositions(room);

            for (int i = 0; i < floorSpawnCount; i++)
            {
                if (validFloorPositions.Count == 0)
                {
                    break;
                }

                int randomIndex = Random.Range(0, validFloorPositions.Count);
                Vector2Int gridPos = validFloorPositions[randomIndex];
                validFloorPositions.RemoveAt(randomIndex);

                Vector3 spawnPos = new Vector3(gridPos.x * tileSize, _yOffsetDeco, gridPos.y * tileSize);
                Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                GameObject selectedPrefab = entry.prefabs[Random.Range(0, entry.prefabs.Count)];
                GameObject instance = Instantiate(selectedPrefab, spawnPos, rotation, controller.transform);

                Debug.Log($"[RoomContentSpawner] 바닥(Floor) 데코레이션 배치 완료: {instance.name}");
            }
        }
    }

    private struct WallCandidate
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    /// <summary>
    /// DungeonSpawner가 실제로 벽을 생성한 좌표(_wallDataList) 중,
    /// 해당 방의 경계 안에 속하는 것만 골라 데코레이션 후보로 변환한다.
    /// 레이캐스트를 쓰지 않으므로 레이어/거리/스케일 설정과 무관하게 항상 정확한 벽 위치를 얻는다.
    /// </summary>
    private List<WallCandidate> GetWallCandidatesForRoom(RoomNode room, float tileSize)
    {
        List<WallCandidate> candidates = new List<WallCandidate>();

        RectInt bounds = room.Bounds;

        float minX = bounds.xMin - _wallBoundsMargin;
        float maxX = (bounds.xMax - 1) + _wallBoundsMargin;
        float minY = bounds.yMin - _wallBoundsMargin;
        float maxY = (bounds.yMax - 1) + _wallBoundsMargin;

        foreach (WallData wall in _wallDataList)
        {
            if (wall.Position.x < minX || wall.Position.x > maxX)
            {
                continue;
            }

            if (wall.Position.y < minY || wall.Position.y > maxY)
            {
                continue;
            }

            // 문(코리도어 연결부) 자리는 별도의 DoorWall로 대체되어 있으므로,
            // 통로 타일과 인접한 벽 조각은 데코 후보에서 제외한다.
            Vector2Int roundedTile = new Vector2Int(Mathf.RoundToInt(wall.Position.x), Mathf.RoundToInt(wall.Position.y));
            if (IsCorridorPosition(roundedTile))
            {
                continue;
            }

            // wall.Rotation은 "벽이 바라보는 바깥쪽 방향"이므로, 그 반대(-forward)가 방 안쪽 방향이다.
            Vector3 inward = (wall.Rotation * Vector3.back) * _wallInsetOffset;

            Vector3 worldPos = new Vector3(
                wall.Position.x * tileSize,
                _yOffsetWallDeco,
                wall.Position.y * tileSize) + inward;

            candidates.Add(new WallCandidate
            {
                position = worldPos,
                rotation = wall.Rotation
            });
        }

        return candidates;
    }

    /// <summary>
    /// 방 내부 바닥 좌표 중 통로 및 인접 영역을 제외한 유효한 바닥 좌표 목록을 반환한다.
    /// </summary>
    private List<Vector2Int> GetValidFloorPositions(RoomNode room)
    {
        List<Vector2Int> validPositions = new List<Vector2Int>();

        for (int x = room.Bounds.xMin; x < room.Bounds.xMax; x++)
        {
            for (int y = room.Bounds.yMin; y < room.Bounds.yMax; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                if (IsCorridorPosition(pos))
                {
                    continue;
                }

                validPositions.Add(pos);
            }
        }

        return validPositions;
    }

    /// <summary>
    /// Corridor 위 또는 Corridor 근처인지 검사한다.
    /// </summary>
    private bool IsCorridorPosition(Vector2Int position)
    {
        if (_corridorTiles.Contains(position))
        {
            return true;
        }

        for (int x = -2; x <= 2; x++)
        {
            for (int y = -2; y <= 2; y++)
            {
                if (_corridorTiles.Contains(position + new Vector2Int(x, y)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void SpawnPlayer(Vector3 position)
    {
        bool isReturningFromBattle = BattleEncounterContext.Instance != null && BattleEncounterContext.Instance.HasEncounter;

        if (isReturningFromBattle)
        {
            return;
        }

        if (_fieldPartySpawner != null)
        {
            _fieldPartySpawner.SpawnParty(position, Quaternion.identity);
            return;
        }

        if (_playerPrefab != null)
        {
            Instantiate(_playerPrefab, position, Quaternion.identity, _contentContainer);
        }
    }

    private List<EnemyBattleData> GenerateRandomEnemyList(EnemyGroupSO group, int roomDepth)
    {
        List<EnemyBattleData> selectedEnemies = new List<EnemyBattleData>();

        if (group == null)
        {
            return selectedEnemies;
        }

        int calculatedMonsterCount = group.BaseCount + (roomDepth / group.DepthDivisor);

        if (group.Enemies.Count == 0)
        {
            return selectedEnemies;
        }

        for (int i = 0; i < calculatedMonsterCount; i++)
        {
            EnemyBattleData randomEnemy = group.GetRandomEnemy();
            if (randomEnemy != null)
            {
                selectedEnemies.Add(randomEnemy);
            }
        }

        return selectedEnemies;
    }

    private EnemyGroupSO GetRandomEnemyGroup(int roomDepth)
    {
        List<EnemyGroupSO> validGroups = new List<EnemyGroupSO>();

        foreach (var group in _table.monsterGroupPool)
        {
            if (roomDepth >= group.MinDepth && roomDepth <= group.MaxDepth)
            {
                validGroups.Add(group);
            }
        }

        if (validGroups.Count == 0)
        {
            return _table.monsterGroupPool.Count > 0 ? _table.monsterGroupPool[0] : null;
        }

        int totalWeight = 0;
        for (int i = 0; i < validGroups.Count; i++)
        {
            totalWeight += validGroups[i].Weight;
        }

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var group in validGroups)
        {
            currentWeight += group.Weight;

            if (randomValue < currentWeight)
            {
                return group;
            }
        }

        return validGroups[validGroups.Count - 1];
    }
}