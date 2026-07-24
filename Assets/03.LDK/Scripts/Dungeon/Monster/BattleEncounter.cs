using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 던전 전투 조우 처리
/// </summary>
public class BattleEncounter : MonoBehaviour
{
    [Header("Battle Scene")]
    [SerializeField] private string _battleSceneName = "Battle";

    [Header("Encounter")]
    [SerializeField] private List<EnemyBattleData> _assignedEnemies = new List<EnemyBattleData>();
    [SerializeField] private bool _canEscape = true;
    [SerializeField] private bool _isPlayerAdvantage;
    [SerializeField] private bool _isEnemyAdvantage;

    private readonly List<DungeonMonster> _dungeonMonsters = new List<DungeonMonster>();
    private bool _isBattleStarted;

    public IReadOnlyList<EnemyBattleData> AssignedEnemies => _assignedEnemies;

    private RoomNode _assignedRoom;
    /// <summary>
    /// 몬스터 이벤트 등록
    /// </summary>
    private void OnEnable()
    {
        RegisterMonsterEvents();
    }

    /// <summary>
    /// 몬스터 이벤트 보정
    /// </summary>
    private void Start()
    {
        RegisterMonsterEvents();
    }

    /// <summary>
    /// 몬스터 이벤트 해제
    /// </summary>
    private void OnDisable()
    {
        UnregisterMonsterEvents();
    }

    /// <summary>
    /// 조우 적 데이터 초기화
    /// </summary>
    /// <param name="enemies">조우 적 목록</param>
    public void Initialize( List<EnemyBattleData> enemyGroup, RoomNode roomNode)
    {
        _assignedRoom = roomNode;

        _assignedEnemies.Clear();

        if (enemyGroup != null)
        {
            for (int i = 0; i < enemyGroup.Count; i++)
            {
                EnemyBattleData enemy = enemyGroup[i];

                if (enemy == null)
                {
                    continue;
                }

                _assignedEnemies.Add(enemy);
                
            }
        }
        SpawnRepresentativeMonster();
        RegisterMonsterEvents();

        Debug.Log($"[BattleEncounter] Initialize / Enemy Count: {_assignedEnemies.Count}");
    }

    /// <summary>
    /// 할당된 적들 중 스탯 총합이 가장 높은 적의 프리팹을 필드에 생성한다.
    /// </summary>
    private void SpawnRepresentativeMonster()
    {
        if (_assignedEnemies.Count == 0)
        {
            return;
        }

        EnemyBattleData strongestEnemy = GetStrongestEnemy(_assignedEnemies);

        if (strongestEnemy == null || strongestEnemy.Prefab == null)
        {
            Debug.LogWarning("[BattleEncounter] 생성할 수 있는 가장 강한 적의 프리팹이 존재하지 않습니다.");
            return;
        }

        Vector3 spawnPosition = new Vector3( transform.position.x,  transform.position.y - 1f,  transform.position.z);  

        GameObject monsterInstance = Instantiate(strongestEnemy.Prefab, spawnPosition, transform.rotation, transform);
        
        DungeonMonster dungeonMonster = monsterInstance.GetComponent<DungeonMonster>();
        if (dungeonMonster != null && _assignedRoom != null)
        {
            dungeonMonster.Initialize(_assignedRoom);
        }

        Debug.Log($"[BattleEncounter] 가장 강한 적 [{strongestEnemy.EnemyName}] 프리팹 생성 완료 (스탯 총합 기반)");
    }


    /// <summary>
    /// 적 목록 중 스탯 총합(HP, 공격력, 마력, 방어력, 마법방어력, 속도)이 가장 높은 적 데이터를 반환한다.
    /// </summary>
    /// <param name="enemies">적 데이터 목록</param>
    /// <returns>가장 강한 적 데이터</returns>
    private EnemyBattleData GetStrongestEnemy(List<EnemyBattleData> enemies)
    {
        EnemyBattleData strongest = null;
        float maxTotalStat = float.MinValue;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyBattleData enemy = enemies[i];
            if (enemy == null)
            {
                continue;
            }

            float totalStat = enemy.MaxHp + enemy.AttackPower + enemy.MagicPower + enemy.DefensePower + enemy.MagicDefensePower + enemy.Speed;

            if (totalStat > maxTotalStat)
            {
                maxTotalStat = totalStat;
                strongest = enemy;
            }
        }

        return strongest;
    }


    /// <summary>
    /// 몬스터 이벤트 등록
    /// </summary>
    private void RegisterMonsterEvents()
    {
        UnregisterMonsterEvents();

        GetComponentsInChildren(true, _dungeonMonsters);

        for (int i = 0; i < _dungeonMonsters.Count; i++)
        {
            DungeonMonster monster = _dungeonMonsters[i];

            if (monster == null)
            {
                continue;
            }

            monster.OnCombatStarted += HandleCombatStarted;
        }
    }

    /// <summary>
    /// 몬스터 이벤트 해제
    /// </summary>
    private void UnregisterMonsterEvents()
    {
        for (int i = 0; i < _dungeonMonsters.Count; i++)
        {
            DungeonMonster monster = _dungeonMonsters[i];

            if (monster == null)
            {
                continue;
            }

            monster.OnCombatStarted -= HandleCombatStarted;
        }

        _dungeonMonsters.Clear();
    }

    /// <summary>
    /// 전투 시작 이벤트 처리
    /// </summary>
    private void HandleCombatStarted()
    {
        Debug.Log("2. BattleEncounter가 신호를 받음");
        if (_isBattleStarted)
        {
            return;
        }

        _isBattleStarted = true;
        StartBattleTransition();

        Debug.Log("[BattleEncounter] HandleCombatStarted 호출");
    }

    /// <summary>
    /// 전투 씬 전환 처리
    /// </summary>
    private void StartBattleTransition()
    {
        Debug.Log("3. 전환 함수 진입 시작");
        
        if (_assignedEnemies.Count <= 0)
        {
            Debug.LogWarning($"{name}에 조우 적 데이터 없음");
            return;
        }

        if (BattleEncounterContext.Instance == null)
        {
            Debug.LogError("BattleEncounterContext 없음");
            return;
        }

        string returnSceneName = SceneManager.GetActiveScene().name;
        Vector3 returnPosition = GetReturnPosition();
        Quaternion returnRotation = GetReturnRotation();
        
        // 추가, 배틀 씬 중심점으로
        Vector3 roomCenter = CalculateRoomCenter();

        
        BattleEncounterContext.Instance.SetEncounter(this,
            _assignedEnemies,
            returnSceneName,
            returnPosition,
            returnRotation,
            _canEscape,
            _isPlayerAdvantage,
            _isEnemyAdvantage);

        // 던전 파티(조작/카메라) 비활성화 — 씬은 유지, 배경으로 남아있음
        if (Party.Instance != null)
        {
            Party.Instance.gameObject.SetActive(false);
        }

        Debug.Log($"[BattleEncounter] Load Battle Scene (Additive): {_battleSceneName}");
        
        // 추가, 배틀 씬 중심점으로
        BattleEncounterContext.Instance.SetBattlePosition(roomCenter);

        DestroyEncounter();

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneAdditive(_battleSceneName);
            CursorLocker.Instance.EnterUIMode();
        }
        else
        {
            SceneManager.LoadScene(_battleSceneName, LoadSceneMode.Additive);
        }
    }

    private Vector3 CalculateRoomCenter()
    {
        if (_assignedRoom != null)
        {
            return new Vector3(_assignedRoom.Bounds.center.x, 0, _assignedRoom.Bounds.center.y);
        }
        return transform.position; // 실패 시 자기 위치
    }

    /// <summary>
    /// 복귀 위치 반환
    /// </summary>
    /// <returns>복귀 위치</returns>
    private Vector3 GetReturnPosition()
    {
        if (Party.Instance != null && Party.Instance.Leader != null)
        {
            return Party.Instance.Leader.transform.position;
        }

        GameObject player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            return player.transform.position;
        }

        return transform.position;
    }

    /// <summary>
    /// 복귀 회전 반환
    /// </summary>
    /// <returns>복귀 회전</returns>
    private Quaternion GetReturnRotation()
    {
        if (Party.Instance != null && Party.Instance.Leader != null)
        {
            return Party.Instance.Leader.transform.rotation;
        }

        GameObject player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            return player.transform.rotation;
        }

        return Quaternion.identity;
    }

    // 추가, 승리시 몬스터 파괴 용도
    private void DestroyEncounter()
    {
        Destroy(gameObject);
    }
}