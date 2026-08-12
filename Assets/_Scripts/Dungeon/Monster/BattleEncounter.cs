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
    private bool _isBattleTransitionStarted;

    public IReadOnlyList<EnemyBattleData> AssignedEnemies => _assignedEnemies;

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
    public void Initialize(List<EnemyBattleData> enemyGroup)
    {
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

        Vector3 spawnPosition = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);

        GameObject monsterInstance = Instantiate(strongestEnemy.Prefab, spawnPosition, transform.rotation, transform);

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
    /// 적 선공 전투 시작 처리
    /// </summary>
    public void HandleCombatStarted()
    {
        Debug.Log("2. BattleEncounter가 신호를 받음");

        if (_isBattleStarted)
        {
            return;
        }

        _isBattleStarted = true;
        _isPlayerAdvantage = false;
        _isEnemyAdvantage = true;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySfx(SfxType.Encounter);
        }

        StartBattleTransition();

        Debug.Log("[BattleEncounter] 적 선공 전투 시작");
    }

    /// <summary>
    /// 플레이어 선공 전투 예약
    /// </summary>
    /// <returns>예약 성공 여부</returns>
    public bool PreparePlayerAdvantageBattle()
    {
        if (_isBattleStarted)
        {
            return false;
        }

        _isBattleStarted = true;
        _isPlayerAdvantage = true;
        _isEnemyAdvantage = false;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySfx(SfxType.Encounter);
        }

        Debug.Log("[BattleEncounter] 플레이어 선공 예약");

        return true;
    }

    /// <summary>
    /// 예약된 전투 진입 시작
    /// </summary>
    public void StartPreparedBattle()
    {
        if (_isBattleStarted == false ||
            _isBattleTransitionStarted)
        {
            return;
        }

        StartBattleTransition();
    }

    /// <summary>
    /// 전투 진입 연출 시작
    /// </summary>
    private void StartBattleTransition()
    {
        if (_isBattleTransitionStarted)
        {
            return;
        }

        _isBattleTransitionStarted = true;

        Debug.Log("3. 전투 진입 연출 시작");

        if (_assignedEnemies.Count <= 0)
        {
            Debug.LogWarning(
                $"{name}에 조우 적 데이터 없음");

            _isBattleStarted = false;
            return;
        }

        if (BattleEncounterContext.Instance == null)
        {
            Debug.LogError(
                "BattleEncounterContext 없음");

            _isBattleStarted = false;
            return;
        }

        if (BattleEntryTransitionController.Instance == null)
        {
            Debug.LogError(
                "BattleEntryTransitionController 없음");

            _isBattleStarted = false;
            return;
        }

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError(
                "SceneTransitionManager 없음");

            _isBattleStarted = false;
            return;
        }

        string returnSceneName =
            SceneManager.GetActiveScene().name;

        Vector3 returnPosition =
            GetReturnPosition();

        Quaternion returnRotation =
            GetReturnRotation();

        BattleEncounterContext.Instance.SetEncounter(
            this,
            _assignedEnemies,
            returnSceneName,
            returnPosition,
            returnRotation,
            _canEscape,
            _isPlayerAdvantage,
            _isEnemyAdvantage);

        if (CursorLocker.Instance != null)
        {
            CursorLocker.Instance.EnterUIMode();
        }

        BattleEntryTransitionController.Instance.PlayEntry(
            HandleEntryBlackoutReached);
    }

    /// <summary>
    /// 전투 진입 검은 화면 도달 처리
    /// </summary>
    private void HandleEntryBlackoutReached()
    {
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError(
                "SceneTransitionManager 없음");

            _isBattleStarted = false;
            return;
        }

        if (DungeonPartyQueueController.Instance != null)
        {
            DungeonPartyQueueController.Instance
                .HideContent();
        }

        if (Party.Instance != null)
        {
            Party.Instance.gameObject.SetActive(false);
        }

        SceneTransitionManager.Instance
            .LoadSceneAdditiveWithoutTransition(
                _battleSceneName,
                onBeforeLoad: HandleBeforeBattleSceneLoad,
                onLoaded: HandleBattleSceneLoaded);
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

    /// <summary>
    /// 전투 씬 로드 직전 조우 비활성화
    /// </summary>
    private void HandleBeforeBattleSceneLoad()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 전투 씬 로드 완료 처리
    /// </summary>
    private void HandleBattleSceneLoaded()
    {
        Debug.Log(
            $"[BattleEncounter] Battle Scene Load Complete: " +
            $"{_battleSceneName}");

        if (BattleEntryTransitionController.Instance == null)
        {
            return;
        }

        BattleEntryTransitionController.Instance
            .RevealFromBlack();
    }
}