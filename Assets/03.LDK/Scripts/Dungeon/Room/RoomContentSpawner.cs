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
    [SerializeField] private int _baseMonstersPerRoom = 4;
    [SerializeField] private float _wallPadding = 0.5f;

    private DungeonData _dungeon;
    private RoomContentTable _table;

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

                HashSet<Vector2Int> doorways = GetDoorwayPositions(room);
                List<WallCandidate> candidates = new List<WallCandidate>();

                for (int x = xMin + 1; x < xMax; x++)
                {
                    Vector2Int gridPos = new Vector2Int(x, yMin);
                    if (!doorways.Contains(gridPos))
                    {
                        candidates.Add(new WallCandidate {
                            position = new Vector3(gridPos.x * tileSize, _yOffsetWallDeco, (gridPos.y * tileSize) + _wallPadding),
                            rotation = Quaternion.Euler(0f, 0f, 0f)
                        });
                    }
                }

                for (int x = xMin + 1; x < xMax; x++)
                {
                    Vector2Int gridPos = new Vector2Int(x, yMax);
                    if (!doorways.Contains(gridPos))
                    {
                        candidates.Add(new WallCandidate {
                            position = new Vector3(gridPos.x * tileSize, _yOffsetWallDeco, (gridPos.y * tileSize) - _wallPadding),
                            rotation = Quaternion.Euler(0f, 180f, 0f)
                        });
                    }
                }

                for (int y = yMin + 1; y < yMax; y++)
                {
                    Vector2Int gridPos = new Vector2Int(xMin, y);
                    if (!doorways.Contains(gridPos))
                    {
                        candidates.Add(new WallCandidate {
                            position = new Vector3((gridPos.x * tileSize) + _wallPadding, _yOffsetWallDeco, gridPos.y * tileSize),
                            rotation = Quaternion.Euler(0f, 90f, 0f)
                        });
                    }
                }

                for (int y = yMin + 1; y < yMax; y++)
                {
                    Vector2Int gridPos = new Vector2Int(xMax, y);
                    if (!doorways.Contains(gridPos))
                    {
                        candidates.Add(new WallCandidate {
                            position = new Vector3((gridPos.x * tileSize) - _wallPadding, _yOffsetWallDeco, gridPos.y * tileSize),
                            rotation = Quaternion.Euler(0f, 270f, 0f)
                        });
                    }
                }

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

            // 바닥(Floor) 데코레이션 생성 (통로 및 입구 인접 영역 제외)
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
    /// 연결된 방(ConnectedRooms)과의 상대적인 위치 및 경계(Bounds)를 기반으로 출입구 위치를 계산한다.
    /// </summary>
    /// <param name="room">방 노드</param>
    /// <returns>출입구 좌표 목록</returns>
    private HashSet<Vector2Int> GetDoorwayPositions(RoomNode room)
    {
        HashSet<Vector2Int> doorways = new HashSet<Vector2Int>();
        if (room.ConnectedRooms == null)
        {
            return doorways;
        }

        int xMin = room.Bounds.xMin;
        int xMax = room.Bounds.xMax - 1;
        int yMin = room.Bounds.yMin;
        int yMax = room.Bounds.yMax - 1;

        foreach (var other in room.ConnectedRooms)
        {
            if (room.Bounds.xMax == other.Bounds.xMin || room.Bounds.xMin == other.Bounds.xMax ||
                room.Bounds.yMax == other.Bounds.yMin || room.Bounds.yMin == other.Bounds.yMax)
            {
                int overlapXMin = Mathf.Max(room.Bounds.xMin, other.Bounds.xMin);
                int overlapXMax = Mathf.Min(room.Bounds.xMax, other.Bounds.xMax);
                int overlapYMin = Mathf.Max(room.Bounds.yMin, other.Bounds.yMin);
                int overlapYMax = Mathf.Min(room.Bounds.yMax, other.Bounds.yMax);

                if (room.Bounds.xMax == other.Bounds.xMin)
                {
                    for (int y = overlapYMin; y < overlapYMax; y++)
                    {
                        doorways.Add(new Vector2Int(xMax, y));
                    }
                }
                else if (room.Bounds.xMin == other.Bounds.xMax)
                {
                    for (int y = overlapYMin; y < overlapYMax; y++)
                    {
                        doorways.Add(new Vector2Int(xMin, y));
                    }
                }
                else if (room.Bounds.yMax == other.Bounds.yMin)
                {
                    for (int x = overlapXMin; x < overlapXMax; x++)
                    {
                        doorways.Add(new Vector2Int(x, yMax));
                    }
                }
                else if (room.Bounds.yMin == other.Bounds.yMax)
                {
                    for (int x = overlapXMin; x < overlapXMax; x++)
                    {
                        doorways.Add(new Vector2Int(x, yMin));
                    }
                }
            }
        }

        return doorways;
    }

    /// <summary>
    /// 방 내부 바닥 좌표 중 출입구 및 통로 인접 영역을 제외한 유효한 바닥 좌표 목록을 반환한다.
    /// </summary>
    /// <param name="room">방 노드</param>
    /// <returns>유효한 바닥 좌표 목록</returns>
    private List<Vector2Int> GetValidFloorPositions(RoomNode room)
    {
        List<Vector2Int> validPositions = new List<Vector2Int>();
        HashSet<Vector2Int> doorways = GetDoorwayPositions(room);

        for (int x = room.Bounds.xMin; x < room.Bounds.xMax; x++)
        {
            for (int y = room.Bounds.yMin; y < room.Bounds.yMax; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                if (doorways.Contains(pos))
                {
                    continue;
                }

                bool isNearDoorway = false;
                foreach (var doorway in doorways)
                {
                    if (Mathf.Abs(pos.x - doorway.x) + Mathf.Abs(pos.y - doorway.y) <= 1)
                    {
                        isNearDoorway = true;
                        break;
                    }
                }

                if (isNearDoorway)
                {
                    continue;
                }

                validPositions.Add(pos);
            }
        }

        return validPositions;
    }

    /// <summary>
    /// 방의 4개 코너 위치를 계산한다. 단, 통로(출입구) 및 인근 영역에 위치하여 통로를 막는 코너는 제외한다.
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

        HashSet<Vector2Int> doorways = GetDoorwayPositions(room);
        List<Vector3> corners = new List<Vector3>();

        foreach (var cornerPos in cornerGridPositions)
        {
            bool isBlocked = doorways.Contains(cornerPos);

            if (isBlocked == false)
            {
                foreach (var doorway in doorways)
                {
                    // 통로 너비(약 4타일) 및 진입 공간 확보를 위해 코너와 출입구 간의 거리가 3타일 이내인 경우 간섭하는 것으로 판단
                    if (Mathf.Abs(cornerPos.x - doorway.x) + Mathf.Abs(cornerPos.y - doorway.y) <= 3)
                    {
                        isBlocked = true;
                        break;
                    }
                }
            }

            if (isBlocked == false)
            {
                corners.Add(new Vector3(cornerPos.x * tileSize, _yOffsetDeco, cornerPos.y * tileSize));
            }
            else
            {
                Debug.Log($"[RoomContentSpawner] 코너 데코레이션 생성 제외 (통로 간섭 지역): 코너 좌표 ({cornerPos.x}, {cornerPos.y})");
            }
        }

        return corners;
    }

    /// <summary>
    /// 플레이어 오브젝트를 생성한다.
    /// </summary>
    /// <param name="position">생성 위치</param>
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

    /// <summary>
    /// 무작위 적 전투 목록을 생성한다.
    /// </summary>
    /// <param name="group">적 그룹 데이터</param>
    /// <param name="roomDepth">방 깊이(층)</param>
    /// <returns>적 전투 데이터 목록</returns>
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
            Debug.LogWarning($"[RoomContentSpawner] {group.name} 그룹 내부에 등록된 몬스터 엔트리가 없습니다.");
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

        Debug.Log($"[RoomContentSpawner] 전투방 생성 완료 / 그룹: {group.name} / 최종 배치 몬스터 수: {selectedEnemies.Count} (깊이 보정 포함)");
        return selectedEnemies;
    }

    /// <summary>
    /// 방 깊이에 맞는 무작위 적 그룹을 반환한다.
    /// </summary>
    /// <param name="roomDepth">방 깊이(층)</param>
    /// <returns>적 그룹 데이터</returns>
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
            Debug.LogWarning($"[RoomContentSpawner] {roomDepth}층에 맞는 적 그룹이 없습니다. 기본 그룹을 반환합니다.");
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