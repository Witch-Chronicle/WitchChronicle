// FILE: Assets\_Scripts\Persistent\BattleEncounterContext.cs

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 진입 정보 및 보스 처치 후 포탈 생성 관리
/// </summary>
public class BattleEncounterContext : MonoBehaviour
{
    public static BattleEncounterContext Instance { get; private set; }

    private BattleEncounter _battleEncounter;

    [Header("Encounter")]
    [SerializeField] private List<EnemyBattleData> _enemyBattleDataList = new List<EnemyBattleData>();
    [SerializeField] private bool _canEscape = true;
    [SerializeField] private bool _isPlayerAdvantage;
    [SerializeField] private bool _isEnemyAdvantage;

    [Header("Return")]
    [SerializeField] private string _returnSceneName;
    [SerializeField] private Vector3 _returnPosition;
    [SerializeField] private Quaternion _returnRotation = Quaternion.identity;

    public IReadOnlyList<EnemyBattleData> EnemyBattleDataList => _enemyBattleDataList;
    public bool CanEscape => _canEscape;
    public bool IsPlayerAdvantage => _isPlayerAdvantage;
    public bool IsEnemyAdvantage => _isEnemyAdvantage;

    public string ReturnSceneName => _returnSceneName;
    public Vector3 ReturnPosition => _returnPosition;
    public Quaternion ReturnRotation => _returnRotation;

    public bool HasEncounter => _enemyBattleDataList.Count > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetEncounter(BattleEncounter encounter,
        IReadOnlyList<EnemyBattleData> enemyBattleDataList,
        string returnSceneName,
        Vector3 returnPosition,
        Quaternion returnRotation,
        bool canEscape = true,
        bool isPlayerAdvantage = false,
        bool isEnemyAdvantage = false)
    {
        _battleEncounter = encounter;
        _enemyBattleDataList.Clear();

        if (enemyBattleDataList != null)
        {
            for (int i = 0; i < enemyBattleDataList.Count; i++)
            {
                if (enemyBattleDataList[i] != null)
                {
                    _enemyBattleDataList.Add(enemyBattleDataList[i]);
                }
            }
        }

        _returnSceneName = returnSceneName;
        _returnPosition = returnPosition;
        _returnRotation = returnRotation;
        _canEscape = canEscape;
        _isPlayerAdvantage = isPlayerAdvantage;
        _isEnemyAdvantage = isEnemyAdvantage;
    }

    public void GetEnemyBattleDataList(List<EnemyBattleData> result)
    {
        if (result == null) return;
        result.Clear();
        for (int i = 0; i < _enemyBattleDataList.Count; i++)
        {
            if (_enemyBattleDataList[i] != null)
            {
                result.Add(_enemyBattleDataList[i]);
            }
        }
    }

    private Vector3 _targetBattlePosition;
    public Vector3 TargetBattlePosition => _targetBattlePosition;

    public void SetBattlePosition(Vector3 position)
    {
        _targetBattlePosition = position;
    }

    public void ClearEncounter()
    {
        _enemyBattleDataList.Clear();
        _returnSceneName = string.Empty;
        _returnPosition = Vector3.zero;
        _returnRotation = Quaternion.identity;
        _canEscape = true;
        _isPlayerAdvantage = false;
        _isEnemyAdvantage = false;
        _battleEncounter = null;
    }

    /// <summary>
    /// 저장된 조우 오브젝트 파괴 및 보스 처치 시 출구 포탈 생성
    /// </summary>
    public void DestroyEncounter()
    {
        if (_battleEncounter == null) return;

        Vector3 spawnPosition = _battleEncounter.transform.position;

        bool wasBossEncounter = HasBossInEncounter();

        if (wasBossEncounter)
        {
            SpawnExitPortal(spawnPosition);
        }

        Destroy(_battleEncounter.gameObject);
        _battleEncounter = null;
    }

    /// <summary>
    /// 이번 전투 조우 목록 중 보스가 포함되어 있었는지 확인
    /// </summary>
    private bool HasBossInEncounter()
    {
        for (int i = 0; i < _enemyBattleDataList.Count; i++)
        {
            if (_enemyBattleDataList[i] != null && _enemyBattleDataList[i].IsBoss)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 보스가 있던 위치에 출구 포탈 스폰
    /// </summary>
    private void SpawnExitPortal(Vector3 position)
    {
        // 1. 보스 오브젝트에 BossPortalSpawner가 붙어있다면 그것을 이용
        BossPortalSpawner spawner = _battleEncounter != null ? _battleEncounter.GetComponent<BossPortalSpawner>() : null;
        if (spawner != null)
        {
            spawner.SpawnPortal();
            return;
        }

        // 2. 컴포넌트가 없더라도 DungeonManager에서 현재 던전의 출구 포탈 프리팹을 직접 찾아 생성!
        GameObject portalPrefab = null;

        if (DungeonManager.Instance != null && DungeonManager.Instance.CurrentDungeonData != null)
        {
            var table = DungeonManager.Instance.CurrentDungeonData.RoomContentTable;
            if (table != null)
            {
                portalPrefab = table.exitPortalPrefab;
            }
        }

        if (portalPrefab != null)
        {
            Vector3 spawnPosition = position;
            spawnPosition.y = 0.5f;

            Instantiate(portalPrefab, spawnPosition, Quaternion.identity);
            Debug.Log($"<color=green>[BattleEncounterContext] 보스 처치 확인 -> 위치({spawnPosition})에 출구 포탈 직접 스폰 완료!</color>");
        }
        else
        {
            Debug.LogWarning("[BattleEncounterContext] 보스는 처치되었으나 exitPortalPrefab을 찾지 못했습니다.");
        }
    }
}