using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 던전 생성 시점에 각 방의 타입에 맞는 자원을 절차적으로 연산하고 의존성을 조립·주입하는 총괄 클래스.
/// </summary>
public class RoomContentSpawner : MonoBehaviour
{
    [Header("Containers")]
    [SerializeField] private Transform _contentContainer;

    [Header("Player Prefab")]
    [SerializeField] private GameObject _playerPrefab;

    [Header("Field Party")]
    [SerializeField] private FieldPartySpawner _fieldPartySpawner;

    [Header("Spawn Settings")]
    [SerializeField] private float _yOffset = 2f;
    [SerializeField] private float _yOffsetDeco = 2f;
    [SerializeField] private float _yOffsetWallDeco = 2f;
    [SerializeField] private float _wallPadding = 0.5f;
    [SerializeField] private float _cornerPadding = 1.5f; // 코너 데코레이션을 방 안쪽으로 밀어내는 오프셋 값

    [Header("Wall Check Settings")]
    [SerializeField] private LayerMask _wallLayerMask; // 벽 판정을 위한 레이어 (인스펙터에서 설정 필요)
    [SerializeField] private float _wallCheckMaxDistance = 1.5f; // 벽을 찾기 위한 레이 거리

    private DungeonData _dungeon;
    private RoomContentTable _table;
    private readonly HashSet<Vector2Int> _corridorTiles = new();

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
                    bossComp.Setup(_table.bossEncounterPrefab);
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
    /// 방의 물리 구역(Bounds) 데이터 기반으로 통로 및 입구 주변을 제외하고 겹치지 않게 장식용 오브젝트들을 배치한다.
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
                int xMin = room.Bounds.xMin;
                int xMax = room.Bounds.xMax - 1;
                int yMin = room.Bounds.yMin;
                int yMax = room.Bounds.yMax - 1;

                List<WallCandidate> candidates = new List<WallCandidate>();

                // 1. 아래쪽 벽 (yMin)
                for (int x = xMin + 1; x < xMax; x++)
                {
                    Vector2Int gridPos = new Vector2Int(x, yMin);
                    if (IsCorridorPosition(gridPos))
                    {
                        continue;
                    }

                    Vector3 pos = new Vector3(gridPos.x * tileSize, _yOffsetWallDeco, (gridPos.y * tileSize) + _wallPadding);
                    candidates.Add(new WallCandidate { position = pos, rotation = Quaternion.Euler(0f, 0f, 0f) });
                }

                // 2. 위쪽 벽 (yMax)
                for (int x = xMin + 1; x < xMax; x++)
                {
                    Vector2Int gridPos = new Vector2Int(x, yMax);
                    if (IsCorridorPosition(gridPos))
                    {
                        continue;
                    }

                    Vector3 pos = new Vector3(gridPos.x * tileSize, _yOffsetWallDeco, (gridPos.y * tileSize) - _wallPadding);
                    candidates.Add(new WallCandidate { position = pos, rotation = Quaternion.Euler(0f, 180f, 0f) });
                }

                // 3. 왼쪽 벽 (xMin)
                for (int y = yMin + 1; y < yMax; y++)
                {
                    Vector2Int gridPos = new Vector2Int(xMin, y);
                    if (IsCorridorPosition(gridPos))
                    {
                        continue;
                    }

                    Vector3 pos = new Vector3((gridPos.x * tileSize) + _wallPadding, _yOffsetWallDeco, gridPos.y * tileSize);
                    candidates.Add(new WallCandidate { position = pos, rotation = Quaternion.Euler(0f, 90f, 0f) });
                }

                // 4. 오른쪽 벽 (xMax)
                for (int y = yMin + 1; y < yMax; y++)
                {
                    Vector2Int gridPos = new Vector2Int(xMax, y);
                    if (IsCorridorPosition(gridPos))
                    {
                        continue;
                    }

                    Vector3 pos = new Vector3((gridPos.x * tileSize) - _wallPadding, _yOffsetWallDeco, gridPos.y * tileSize);
                    candidates.Add(new WallCandidate { position = pos, rotation = Quaternion.Euler(0f, 270f, 0f) });
                }

                List<WallCandidate> validatedCandidates = new List<WallCandidate>();
                foreach (var candidate in candidates)
                {
                    if (HasActualWall(candidate.position, candidate.rotation))
                    {
                        validatedCandidates.Add(candidate);
                    }
                }

                // 셔플
                for (int i = 0; i < validatedCandidates.Count; i++)
                {
                    int randIdx = Random.Range(i, validatedCandidates.Count);
                    var temp = validatedCandidates[i];
                    validatedCandidates[i] = validatedCandidates[randIdx];
                    validatedCandidates[randIdx] = temp;
                }

                int spawnCount = Mathf.Clamp(Random.Range(entry.minCount, entry.maxCount + 1), 0, validatedCandidates.Count);

                for (int i = 0; i < spawnCount; i++)
                {
                    GameObject selectedPrefab = entry.prefabs[Random.Range(0, entry.prefabs.Count)];
                    Instantiate(selectedPrefab, validatedCandidates[i].position, validatedCandidates[i].rotation, controller.transform);
                }

                Debug.Log($"[RoomContentSpawner] 벽(Wall) 데코레이션 배치 완료: 방 타입 [{room.Type}] / 생성 수: {spawnCount}");
                continue;
            }

            if (entry.placement == PlacementType.Corner)
            {
                List<Vector3> allCorners = GetCornerPositions(room, tileSize);

                foreach (var spawnPos in allCorners)
                {
                    Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    GameObject selectedPrefab = entry.prefabs[Random.Range(0, entry.prefabs.Count)];
                    GameObject instance = Instantiate(selectedPrefab, spawnPos, rotation, controller.transform);

                    Debug.Log($"[RoomContentSpawner] 코너(Corner) 데코레이션 배치 완료: {instance.name}");
                }
                continue;
            }

            // 바닥(Floor) 데코레이션 생성 (통로 및 인접 영역 제외)
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

    /// <summary>
    /// 해당 후보 위치 근처에 실제 벽(콜라이더 등)이 존재하는지 검증한다.
    /// </summary>
    private bool HasActualWall(Vector3 position, Quaternion rotation)
    {
        Vector3 rayOrigin = position + Vector3.up * 0.5f;
        Vector3 rayDirection = rotation * Vector3.back;

        if (_wallLayerMask.value != 0)
        {
            return Physics.Raycast(rayOrigin, rayDirection, _wallCheckMaxDistance, _wallLayerMask);
        }
        else
        {
            return Physics.Raycast(rayOrigin, rayDirection, _wallCheckMaxDistance);
        }
    }

    private struct WallCandidate
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    /// <summary>
    /// 방 내부 바닥 좌표 중 통로 및 인접 영역을 제외한 유효한 바닥 좌표 목록을 반환한다.
    /// </summary>
    /// <param name="room">방 노드</param>
    /// <returns>유효한 바닥 좌표 목록</returns>
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

    /// <summary>
    /// 방의 4개 코너 위치를 계산한다. 통로와 인접한 코너는 차단하며, 방 안쪽 방향으로 오프셋을 적용한다.
    /// </summary>
    /// <param name="room">방 노드</param>
    /// <param name="tileSize">타일 크기</param>
    /// <returns>유효한 코너 위치 목록</returns>
    private List<Vector3> GetCornerPositions(RoomNode room, float tileSize)
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

        List<Vector3> corners = new List<Vector3>();

        foreach (var cornerPos in cornerGridPositions)
        {
            if (IsCorridorPosition(cornerPos))
            {
                Debug.Log($"[RoomContentSpawner] Corridor와 인접하여 Corner 생성 제외 : {cornerPos}");
                continue;
            }

            // 코너 지점에서 방 안쪽 방향으로 레이를 쏴 실제 벽이 존재하는지 검증
            Vector3 cornerWorldPos = new Vector3(cornerPos.x * tileSize, 0f, cornerPos.y * tileSize);
            Vector3 roomCenterWorld = new Vector3(room.Center.x * tileSize, 0f, room.Center.y * tileSize);
            Vector3 dirToCenter = (roomCenterWorld - cornerWorldPos).normalized;
            Vector3 rayOrigin = cornerWorldPos + Vector3.up * 0.5f;

            bool hasWall = _wallLayerMask.value != 0 
                ? Physics.Raycast(rayOrigin, dirToCenter, _wallCheckMaxDistance, _wallLayerMask)
                : Physics.Raycast(rayOrigin, dirToCenter, _wallCheckMaxDistance);

            if (!hasWall)
            {
                Debug.Log($"[RoomContentSpawner] 코너 위치 {cornerPos}에서 실제 벽 검출 실패로 코너 생성 제외됨.");
                continue;
            }

            // 코너 위치에서 방 안쪽 방향으로 오프셋 적용
            float offsetX = (cornerPos.x == xMin) ? _cornerPadding : -_cornerPadding;
            float offsetZ = (cornerPos.y == yMin) ? _cornerPadding : -_cornerPadding;

            Vector3 adjustedPos = new Vector3(
                (cornerPos.x * tileSize) + offsetX, 
                _yOffsetDeco - 0.5f, 
                (cornerPos.y * tileSize) + offsetZ
            );

            corners.Add(adjustedPos);
            Debug.Log($"[RoomContentSpawner] 안쪽으로 오프셋된 유효 코너 위치 확정: {adjustedPos}");
        }

        return corners;
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