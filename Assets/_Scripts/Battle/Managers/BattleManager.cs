using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 Actor 생성, 배치, BattleUnit 전달 관리
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleCycleController _battleCycleController;
    [SerializeField] private BattleIntroDirector _battleIntroDirector;

    [Header("Persistent Party")]
    [SerializeField] private bool _usePersistentParty = true;
    [SerializeField] private bool _fallbackToInspectorParty = true;

    [Header("Actor Prefabs")]
    [SerializeField] private BattleActor[] _playerActorPrefabs;
    [SerializeField] private BattleActor[] _enemyActorPrefabs;

    [Header("Formation")]
    [SerializeField] private Transform _playerFormationRoot;
    [SerializeField] private Transform _enemyFormationRoot;
    [SerializeField] private Vector3 _playerFallbackOrigin = new Vector3(-3f, 0f, 0f);
    [SerializeField] private Vector3 _enemyFallbackOrigin = new Vector3(3f, 0f, 0f);
    [Tooltip("아군 진형 좌우 간격")]
    [SerializeField] private float _playerFormationSpacing = 1.5f;
    [Tooltip("적 진형 좌우 간격 (한 줄 안에서의 간격)")]
    [SerializeField] private float _enemyFormationSpacing = 1.5f;
    [Tooltip("한 줄에 배치할 최대 인원 (예: 3이면 3마리씩 끊어서 다음 줄로)")]
    [SerializeField] private int _maxPerRow = 3;
    [Tooltip("줄과 줄 사이 앞뒤 간격 (상대 진영에서 멀어지는 방향으로)")]
    [SerializeField] private float _rowSpacing = 1.3f;
    [Tooltip("뒷줄을 앞줄 유닛 사이 틈으로 살짝 어긋나게 배치할 비율 (0.5 = 반 칸)")]
    [SerializeField] private float _rowSideStagger = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool _startBattleOnStart = true;
    [SerializeField] private bool _debugLogBattleUnits = true;
    [SerializeField] private bool _clearActorsOnBattleEnd = false;

    [Header("Encounter Enemy")]
    [SerializeField] private bool _useEncounterEnemies = true;
    [SerializeField] private bool _fallbackToInspectorEnemies = true;
    [SerializeField] private BattleActor _defaultEnemyActorPrefab;

    private readonly List<EnemyBattleData> _encounterEnemies = new List<EnemyBattleData>();

    private readonly List<BattleActor> _spawnedActors = new List<BattleActor>();
    private readonly List<BattleUnit> _activeBattleUnits = new List<BattleUnit>();
    private readonly List<PersistentCharacterUnit> _persistentPartyMembers = new List<PersistentCharacterUnit>();
    private readonly Dictionary<BattleUnit, BattleActor> _actorByBattleUnit = new Dictionary<BattleUnit, BattleActor>();

    public IReadOnlyList<BattleActor> SpawnedActors => _spawnedActors;
    public IReadOnlyList<BattleUnit> ActiveBattleUnits => _activeBattleUnits;

    /// <summary>
    /// 참조 자동 연결
    /// </summary>
    private void Awake()
    {
        if (_battleCycleController == null)
        {
            _battleCycleController = GetComponent<BattleCycleController>();
        }

        if (_battleIntroDirector == null)
        {
            _battleIntroDirector = FindFirstObjectByType<BattleIntroDirector>();
        }
    }

    /// <summary>
    /// 전투 종료 이벤트 구독
    /// </summary>
    private void OnEnable()
    {
        if (_battleCycleController != null)
        {
            _battleCycleController.OnBattleEnded += HandleBattleEnded;
        }
    }

    /// <summary>
    /// 전투 종료 이벤트 구독 해제
    /// </summary>
    private void OnDisable()
    {
        if (_battleCycleController != null)
        {
            _battleCycleController.OnBattleEnded -= HandleBattleEnded;
        }
    }

    /// <summary>
    /// 씬 시작 전투 실행
    /// </summary>
    private void Start()
    {
        if (_startBattleOnStart)
        {
            StartTestBattle();
        }
    }

    /// <summary>
    /// 전투 시작
    /// </summary>
    public void StartTestBattle()
    {
        Debug.Log("[BattleManager] StartTestBattle 호출");

        if (CanStartBattle() == false)
        {
            Debug.LogWarning("[BattleManager] CanStartBattle 실패");
            return;
        }

        ClearBattleRuntime();

        bool spawnedPersistentParty = TrySpawnPersistentPartyActors();

        if (spawnedPersistentParty == false && _fallbackToInspectorParty)
        {
            SpawnTeamActors(
                BattleTeamType.Player,
                _playerActorPrefabs,
                _playerFormationRoot);
        }

        Debug.Log("[BattleManager] 적 생성 단계 진입");

        bool spawnedEncounterEnemies = TrySpawnEncounterEnemyActors();

        Debug.Log($"[BattleManager] 조우 적 생성 결과: {spawnedEncounterEnemies}");

        if (spawnedEncounterEnemies == false && _fallbackToInspectorEnemies)
        {
            Debug.Log("[BattleManager] 인스펙터 적 fallback 사용");

            SpawnTeamActors(
                BattleTeamType.Enemy,
                _enemyActorPrefabs,
                _enemyFormationRoot);
        }

        if (HasAliveTeam(BattleTeamType.Player) == false)
        {
            Debug.LogError("[BattleManager] 전투 시작 실패. 플레이어 유닛 없음");
            return;
        }

        if (HasAliveTeam(BattleTeamType.Enemy) == false)
        {
            Debug.LogError("[BattleManager] 전투 시작 실패. 적 유닛 없음");
            return;
        }

        LogBattleUnits();

        if (_battleIntroDirector != null &&
            _battleIntroDirector.isActiveAndEnabled)
        {
            _battleIntroDirector.PlayIntro(
                () => _battleCycleController.StartBattle(
                    _activeBattleUnits));

            return;
        }

        _battleCycleController.StartBattle(
            _activeBattleUnits);
    }

    /// <summary>
    /// 전투 시작 가능 여부 확인
    /// </summary>
    /// <returns>시작 가능 여부</returns>
    private bool CanStartBattle()
    {
        if (_battleCycleController == null)
        {
            Debug.LogError("BattleCycleController 연결 없음");
            return false;
        }

        bool hasPersistentParty = HasValidPersistentParty();
        bool hasInspectorParty = _fallbackToInspectorParty && HasValidPrefab(_playerActorPrefabs);

        if (hasPersistentParty == false && hasInspectorParty == false)
        {
            Debug.LogError("플레이어 전투 참가 데이터 없음");
            return false;
        }

        bool hasEncounterEnemies = HasValidEncounterEnemies();
        bool hasInspectorEnemies = _fallbackToInspectorEnemies && HasValidPrefab(_enemyActorPrefabs);

        if (hasEncounterEnemies == false && hasInspectorEnemies == false)
        {
            Debug.LogError("적 전투 참가 데이터 없음");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 유효 유지 파티 존재 여부 확인
    /// </summary>
    /// <returns>유효 유지 파티 존재 여부</returns>
    private bool HasValidPersistentParty()
    {
        if (_usePersistentParty == false)
        {
            return false;
        }

        if (PersistentCharacterManager.Instance == null)
        {
            return false;
        }

        _persistentPartyMembers.Clear();
        PersistentCharacterManager.Instance.GetActivePartyMembers(_persistentPartyMembers);

        return GetValidPersistentPartyCount(_persistentPartyMembers) > 0;
    }

    /// <summary>
    /// 유지 파티 전투 Actor 생성 시도
    /// </summary>
    /// <returns>생성 성공 여부</returns>
    private bool TrySpawnPersistentPartyActors()
    {
        if (_usePersistentParty == false)
        {
            return false;
        }

        if (PersistentCharacterManager.Instance == null)
        {
            Debug.LogWarning("[BattleManager] PersistentCharacterManager 없음. 테스트 플레이어 프리팹 사용");
            return false;
        }

        _persistentPartyMembers.Clear();
        PersistentCharacterManager.Instance.GetActivePartyMembers(_persistentPartyMembers);

        if (GetValidPersistentPartyCount(_persistentPartyMembers) <= 0)
        {
            Debug.LogWarning("[BattleManager] Active Party 없음. 테스트 플레이어 프리팹 사용");
            return false;
        }

        SpawnPersistentPlayerActors(_persistentPartyMembers);
        return HasAliveTeam(BattleTeamType.Player);
    }

    /// <summary>
    /// 유지 파티 기반 플레이어 Actor 생성
    /// </summary>
    /// <param name="partyMembers">현재 파티 목록</param>
    private void SpawnPersistentPlayerActors(IReadOnlyList<PersistentCharacterUnit> partyMembers)
    {
        int validCount = GetValidPersistentPartyCount(partyMembers);
        int teamIndex = 0;

        for (int i = 0; i < partyMembers.Count; i++)
        {
            PersistentCharacterUnit member = partyMembers[i];

            if (member == null)
            {
                continue;
            }

            BattleActor actorPrefab = member.BattleActorPrefab;

            if (actorPrefab == null)
            {
                Debug.LogWarning($"{member.CharacterName}의 BattleActorPrefab 없음");
                continue;
            }

            Vector3 position = GetFormationPosition(
                BattleTeamType.Player,
                teamIndex,
                validCount);

            Quaternion rotation = GetFormationRotation(
                BattleTeamType.Player,
                position);

            BattleActor actor = Instantiate(
                actorPrefab,
                position,
                rotation,
                _playerFormationRoot);

            actor.InitializeFromPersistentCharacter(member);
            actor.SetFormationPose(position, rotation);

            BattleUnit battleUnit = actor.CreateBattleUnit();

            if (battleUnit == null)
            {
                Debug.LogWarning($"{actor.name} BattleUnit 생성 실패");
                Destroy(actor.gameObject);
                teamIndex++;
                continue;
            }

            RegisterBattleActor(actor, battleUnit);
            teamIndex++;
        }
    }

    /// <summary>
    /// 유효 유지 파티 수 반환
    /// </summary>
    /// <param name="partyMembers">현재 파티 목록</param>
    /// <returns>유효 파티 수</returns>
    private int GetValidPersistentPartyCount(IReadOnlyList<PersistentCharacterUnit> partyMembers)
    {
        if (partyMembers == null)
        {
            return 0;
        }

        int count = 0;

        for (int i = 0; i < partyMembers.Count; i++)
        {
            PersistentCharacterUnit member = partyMembers[i];

            if (member == null)
            {
                continue;
            }

            if (member.BattleActorPrefab == null)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    /// <summary>
    /// 특정 팀 Actor 프리팹 생성
    /// </summary>
    /// <param name="teamType">팀 타입</param>
    /// <param name="actorPrefabs">Actor 프리팹 배열</param>
    /// <param name="formationRoot">진형 루트</param>
    private void SpawnTeamActors(
        BattleTeamType teamType,
        BattleActor[] actorPrefabs,
        Transform formationRoot)
    {
        int validCount = GetValidPrefabCount(actorPrefabs);
        int teamIndex = 0;

        for (int i = 0; i < actorPrefabs.Length; i++)
        {
            BattleActor actorPrefab = actorPrefabs[i];

            if (actorPrefab == null)
            {
                continue;
            }

            Vector3 position = GetFormationPosition(teamType, teamIndex, validCount);
            Quaternion rotation = GetFormationRotation(teamType, position);

            BattleActor actor = Instantiate(actorPrefab, position, rotation, formationRoot);
            actor.SetTeamType(teamType);
            actor.SetFormationPose(position, rotation);

            BattleUnit battleUnit = actor.CreateBattleUnit();

            if (battleUnit == null)
            {
                Debug.LogWarning($"{actor.name} BattleUnit 생성 실패");
                Destroy(actor.gameObject);
                teamIndex++;
                continue;
            }

            RegisterBattleActor(actor, battleUnit);
            teamIndex++;
        }
    }

    /// <summary>
    /// BattleActor 등록
    /// </summary>
    /// <param name="actor">등록 Actor</param>
    /// <param name="battleUnit">등록 BattleUnit</param>
    private void RegisterBattleActor(BattleActor actor, BattleUnit battleUnit)
    {
        if (actor == null || battleUnit == null)
        {
            return;
        }

        _spawnedActors.Add(actor);
        _activeBattleUnits.Add(battleUnit);
        _actorByBattleUnit[battleUnit] = actor;
    }

    /// <summary>
    /// 프리팹 배열 유효 여부 확인
    /// </summary>
    /// <param name="actorPrefabs">확인 프리팹 배열</param>
    /// <returns>유효 프리팹 존재 여부</returns>
    private bool HasValidPrefab(BattleActor[] actorPrefabs)
    {
        if (actorPrefabs == null)
        {
            return false;
        }

        for (int i = 0; i < actorPrefabs.Length; i++)
        {
            if (actorPrefabs[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 프리팹 배열 유효 개수 반환
    /// </summary>
    /// <param name="actorPrefabs">확인 프리팹 배열</param>
    /// <returns>유효 프리팹 개수</returns>
    private int GetValidPrefabCount(BattleActor[] actorPrefabs)
    {
        int count = 0;

        if (actorPrefabs == null)
        {
            return count;
        }

        for (int i = 0; i < actorPrefabs.Length; i++)
        {
            if (actorPrefabs[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 전투 런타임 정리
    /// </summary>
    private void ClearBattleRuntime()
    {
        ClearSpawnedActors();

        _spawnedActors.Clear();
        _activeBattleUnits.Clear();
        _persistentPartyMembers.Clear();
        _actorByBattleUnit.Clear();
    }

    /// <summary>
    /// 생성 Actor 제거
    /// </summary>
    private void ClearSpawnedActors()
    {
        for (int i = 0; i < _spawnedActors.Count; i++)
        {
            BattleActor actor = _spawnedActors[i];

            if (actor == null)
            {
                continue;
            }

            Destroy(actor.gameObject);
        }
    }

    /// <summary>
    /// 팀 생존 유닛 존재 여부 확인
    /// </summary>
    /// <param name="teamType">확인 팀</param>
    /// <returns>존재 여부</returns>
    private bool HasAliveTeam(BattleTeamType teamType)
    {
        for (int i = 0; i < _activeBattleUnits.Count; i++)
        {
            BattleUnit unit = _activeBattleUnits[i];

            if (unit == null)
            {
                continue;
            }

            if (unit.TeamType != teamType)
            {
                continue;
            }

            if (unit.IsAlive)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 진형 위치 계산
    /// </summary>
    /// <param name="teamType">팀 타입</param>
    /// <param name="index">팀 내 인덱스</param>
    /// <param name="count">팀 유닛 수</param>
    /// <returns>계산 위치</returns>
    /// <summary>
    /// 진형 위치 계산.
    /// Player는 기존 방식대로 한 줄로 나란히 배치.
    /// Enemy는 한 줄에 최대 _maxPerRow명씩 배치하고, 넘어가면 다음 줄로(최대 2줄 가정).
    /// 뒷줄은 상대 진영에서 멀어지는 방향(depthDir)으로 _rowSpacing만큼 물러나고,
    /// 가로 위치(sideDir)도 앞줄 틈 사이로 보이도록 반 칸(_rowSideStagger) 어긋나게 배치됨.
    /// </summary>
    /// <param name="teamType">팀 타입</param>
    /// <param name="index">팀 내 인덱스</param>
    /// <param name="count">팀 유닛 수</param>
    /// <returns>계산 위치</returns>
    private Vector3 GetFormationPosition(BattleTeamType teamType, int index, int count)
    {
        Vector3 origin = GetFormationOrigin(teamType);

        if (teamType == BattleTeamType.Player)
        {
            float centerOffset = index - (count - 1) * 0.5f;
            return origin + new Vector3(0f, 0f, centerOffset * _playerFormationSpacing);
        }

        int maxPerRow = Mathf.Max(1, _maxPerRow);

        int row = index / maxPerRow;
        int indexInRow = index % maxPerRow;

        int countInRow = Mathf.Clamp(count - row * maxPerRow, 1, maxPerRow);
        int frontRowCount = Mathf.Min(count, maxPerRow);

        Vector3 opponentOrigin = GetFormationOrigin(GetOpposingTeamType(teamType));

        Vector3 depthDir = origin - opponentOrigin;
        depthDir.y = 0f;
        depthDir = depthDir.sqrMagnitude > 0.0001f ? depthDir.normalized : Vector3.forward;

        Vector3 sideDir = Vector3.Cross(Vector3.up, depthDir).normalized;

        float sideOffset = indexInRow - (countInRow - 1) * 0.5f;

        // 앞줄과 인원수 차이가 짝수(0, 2 등)일 때만 완전히 겹치므로, 그때만 스태거로 밀어줌.
        // 차이가 홀수일 땐 자기 인원수 기준 중앙 정렬만으로 이미 앞줄 틈 사이에 정확히 들어감.
        if (row > 0 && (frontRowCount - countInRow) % 2 == 0)
        {
            sideOffset += _rowSideStagger;
        }

        return origin
            + sideDir * (sideOffset * _enemyFormationSpacing)
            + depthDir * (row * _rowSpacing);
    }

    /// <summary>
    /// 상대 팀 타입 반환 (진형 깊이 축 계산용)
    /// </summary>
    private BattleTeamType GetOpposingTeamType(BattleTeamType teamType)
    {
        return teamType == BattleTeamType.Player
            ? BattleTeamType.Enemy
            : BattleTeamType.Player;
    }

    /// <summary>
    /// 진형 기준 위치 반환
    /// </summary>
    /// <param name="teamType">팀 타입</param>
    /// <returns>진형 기준 위치</returns>
    private Vector3 GetFormationOrigin(BattleTeamType teamType)
    {
        if (teamType == BattleTeamType.Player)
        {
            return _playerFormationRoot != null
                ? _playerFormationRoot.position
                : _playerFallbackOrigin;
        }

        return _enemyFormationRoot != null
            ? _enemyFormationRoot.position
            : _enemyFallbackOrigin;
    }

    /// <summary>
    /// 상대 팀 방향 회전 계산
    /// </summary>
    /// <param name="teamType">팀 타입</param>
    /// <param name="position">Actor 위치</param>
    /// <returns>계산 회전</returns>
    private Quaternion GetFormationRotation(BattleTeamType teamType, Vector3 position)
    {
        BattleTeamType opponentTeam = teamType == BattleTeamType.Player
            ? BattleTeamType.Enemy
            : BattleTeamType.Player;

        Vector3 targetPosition = GetFormationOrigin(opponentTeam);
        Vector3 direction = targetPosition - position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Quaternion.identity;
        }

        return Quaternion.LookRotation(direction.normalized);
    }

    /// <summary>
    /// BattleUnit 기준 BattleActor 검색
    /// </summary>
    /// <param name="battleUnit">검색 BattleUnit</param>
    /// <param name="actor">검색 Actor</param>
    /// <returns>검색 성공 여부</returns>
    public bool TryGetActor(BattleUnit battleUnit, out BattleActor actor)
    {
        return _actorByBattleUnit.TryGetValue(battleUnit, out actor);
    }

    /// <summary>
    /// 전투 종료 처리
    /// </summary>
    /// <param name="winner">승리 팀</param>
    private void HandleBattleEnded(BattleTeamType winner)
    {
        bool isVictory = winner == BattleTeamType.Player;

        ApplyPlayerBattleResultsToVitals(isVictory);

        if (isVictory)
        {
            ProcessMonsterKillQuests();
        }

        Debug.Log($"[BattleManager] Battle End / Winner: {winner}");

        if (_clearActorsOnBattleEnd)
        {
            ClearSpawnedActors();
            _spawnedActors.Clear();
            _actorByBattleUnit.Clear();
        }
    }

    /// <summary>
    /// 전투 승리 시 스폰되었던 적들의 퀘스트 처치 수 증가
    /// </summary>
    private void ProcessMonsterKillQuests()
    {
        if (QuestManager.Instance == null)
        {
            return;
        }

        // 스폰된 모든 전투 참가자 중 '적(Enemy)' 팀의 EnemyId를 전달
        for (int i = 0; i < _spawnedActors.Count; i++)
        {
            BattleActor actor = _spawnedActors[i];

            if (actor != null && actor.TeamType == BattleTeamType.Enemy)
            {
                if (actor.EnemyBattleData != null && string.IsNullOrEmpty(actor.EnemyBattleData.EnemyId) == false)
                {
                    QuestManager.Instance.AddProgress(
                        QuestObjectiveType.KillMonster,
                        actor.EnemyBattleData.EnemyId,
                        1
                    );

                    Debug.Log($"[BattleManager] 퀘스트 처치 반영 : {actor.EnemyBattleData.EnemyName} ({actor.EnemyBattleData.EnemyId})");
                }
            }
        }
    }

    // 추가

    /// <summary>
    /// 드롭 테이블 을 가지고 와 아이템 입벤토리에 지급
    /// </summary>
    // private void GiveBattleRewards()
    // {
    //     foreach (EnemyBattleData enemy in _encounterEnemies)
    //     {
    //         if (enemy == null)
    //         {
    //             continue;
    //         }

    //         List<DropResult> drops = DropManager.Instance.RollDrop(enemy.DropTable);

    //         foreach (DropResult drop in drops)
    //         {
    //             Debug.Log($"{drop.item}, {drop.amount} 만큼 휙득");
    //             PlayerInventory.Instance.AddItem(drop.item, drop.amount);
    //         }
    //     }
    // }

    /// <summary>
    /// 플레이어 전투 결과 반영
    /// </summary>
    /// <param name="isVictory">전투 승리 여부 (사망 캐릭터 부활 처리 판단용)</param>
    private void ApplyPlayerBattleResultsToVitals(bool isVictory)
    {
        for (int i = 0; i < _spawnedActors.Count; i++)
        {
            BattleActor actor = _spawnedActors[i];

            if (actor == null || actor.TeamType != BattleTeamType.Player)
            {
                continue;
            }

            actor.ApplyBattleResultToVitals(isVictory);
        }
    }

    /// <summary>
    /// 현재 BattleUnit 목록 로그 출력
    /// </summary>
    private void LogBattleUnits()
    {
        if (_debugLogBattleUnits == false)
        {
            return;
        }

        for (int i = 0; i < _activeBattleUnits.Count; i++)
        {
            BattleUnit unit = _activeBattleUnits[i];

            if (unit == null)
            {
                continue;
            }

            Debug.Log(
                $"[BattleManager] Unit {i} / " +
                $"{unit.TeamType} / {unit.UnitName} / " +
                $"HP: {unit.CurrentHp}/{unit.MaxHp}, " +
                $"MP: {unit.CurrentMp}/{unit.MaxMp}, " +
                $"Speed: {unit.Speed}");
        }
    }

    /// <summary>
    /// 조우 적 Actor 생성 시도
    /// </summary>
    /// <returns>생성 성공 여부</returns>
    private bool TrySpawnEncounterEnemyActors()
    {
        Debug.Log("[BattleManager] TrySpawnEncounterEnemyActors 호출");

        if (_useEncounterEnemies == false)
        {
            Debug.LogWarning("[BattleManager] Use Encounter Enemies 꺼져 있음");
            return false;
        }

        if (BattleEncounterContext.Instance == null)
        {
            Debug.LogWarning("[BattleManager] BattleEncounterContext 없음");
            return false;
        }

        _encounterEnemies.Clear();
        BattleEncounterContext.Instance.GetEnemyBattleDataList(_encounterEnemies);

        Debug.Log($"[BattleManager] Context Enemy Count: {_encounterEnemies.Count}");

        if (GetValidEncounterEnemyCount(_encounterEnemies) <= 0)
        {
            Debug.LogWarning("[BattleManager] 조우 적 데이터 없음");
            return false;
        }

        if (_defaultEnemyActorPrefab == null)
        {
            Debug.LogWarning("[BattleManager] Default Enemy Actor Prefab 없음");
            return false;
        }

        Debug.Log("[BattleManager] 조우 적 생성 시작");

        SpawnEncounterEnemyActors(_encounterEnemies);
        return HasAliveTeam(BattleTeamType.Enemy);
    }

    /// <summary>
    /// 조우 적 Actor 생성
    /// </summary>
    /// <param name="enemyDataList">조우 적 데이터 목록</param>
    private void SpawnEncounterEnemyActors(IReadOnlyList<EnemyBattleData> enemyDataList)
    {
        int validCount = GetValidEncounterEnemyCount(enemyDataList);
        int teamIndex = 0;

        for (int i = 0; i < enemyDataList.Count; i++)
        {
            EnemyBattleData enemyData = enemyDataList[i];

            if (enemyData == null)
            {
                continue;
            }

            Vector3 position = GetFormationPosition(
                BattleTeamType.Enemy,
                teamIndex,
                validCount);

            Quaternion rotation = GetFormationRotation(
                BattleTeamType.Enemy,
                position);

            BattleActor actor = Instantiate(
                _defaultEnemyActorPrefab,
                position,
                rotation,
                _enemyFormationRoot);

            actor.InitializeEnemyData(enemyData);
            actor.SetFormationPose(position, rotation);

            BattleUnit battleUnit = actor.CreateBattleUnit();

            if (battleUnit == null)
            {
                Debug.LogWarning($"{actor.name} BattleUnit 생성 실패");
                Destroy(actor.gameObject);
                teamIndex++;
                continue;
            }

            RegisterBattleActor(actor, battleUnit);
            teamIndex++;
        }
    }

    /// <summary>
    /// 유효 조우 적 수 반환
    /// </summary>
    /// <param name="enemyDataList">조우 적 데이터 목록</param>
    /// <returns>유효 적 수</returns>
    private int GetValidEncounterEnemyCount(IReadOnlyList<EnemyBattleData> enemyDataList)
    {
        if (enemyDataList == null)
        {
            return 0;
        }

        int count = 0;

        for (int i = 0; i < enemyDataList.Count; i++)
        {
            if (enemyDataList[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 유효 조우 적 데이터 존재 여부 확인
    /// </summary>
    /// <returns>유효 조우 적 데이터 존재 여부</returns>
    private bool HasValidEncounterEnemies()
    {
        Debug.Log("[BattleManager] HasValidEncounterEnemies 검사");

        if (_useEncounterEnemies == false)
        {
            Debug.LogWarning("[BattleManager] Use Encounter Enemies 꺼져 있음");
            return false;
        }

        if (_defaultEnemyActorPrefab == null)
        {
            Debug.LogWarning("[BattleManager] Default Enemy Actor Prefab 없음");
            return false;
        }

        if (BattleEncounterContext.Instance == null)
        {
            Debug.LogWarning("[BattleManager] BattleEncounterContext 없음");
            return false;
        }

        _encounterEnemies.Clear();
        BattleEncounterContext.Instance.GetEnemyBattleDataList(_encounterEnemies);

        int validCount = GetValidEncounterEnemyCount(_encounterEnemies);

        Debug.Log($"[BattleManager] Encounter Enemy Count: {_encounterEnemies.Count}, Valid Count: {validCount}");

        return validCount > 0;
    }

    /// <summary>
    /// 현재 스폰된 적 BattleActor 목록 조회 (보상 계산 등에서 사용).
    /// </summary>
    /// <param name="result">복사 대상 목록</param>
    public void GetEnemyActors(List<BattleActor> result)
    {
        if (result == null)
        {
            return;
        }

        result.Clear();

        for (int i = 0; i < _spawnedActors.Count; i++)
        {
            BattleActor actor = _spawnedActors[i];

            if (actor != null && actor.TeamType == BattleTeamType.Enemy)
            {
                result.Add(actor);
            }
        }
    }
}