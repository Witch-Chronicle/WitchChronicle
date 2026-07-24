using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 진입 정보 유지
/// </summary>
public class BattleEncounterContext : MonoBehaviour
{
    public static BattleEncounterContext Instance { get; private set; }

    // 추가 승리 시 파괴 할 몬스터 게임오브젝트
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

    /// <summary>
    /// 싱글톤 등록
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 싱글톤 해제
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 전투 진입 정보 설정
    /// </summary>
    /// <param name="encounter">적</param>
    /// <param name="enemyBattleDataList">적 데이터 목록</param>
    /// <param name="returnSceneName">복귀 씬 이름</param>
    /// <param name="returnPosition">복귀 위치</param>
    /// <param name="returnRotation">복귀 회전</param>
    /// <param name="canEscape">도망 가능 여부</param>
    /// <param name="isPlayerAdvantage">플레이어 선공 여부</param>
    /// <param name="isEnemyAdvantage">적 선공 여부</param>
    public void SetEncounter(BattleEncounter encounter,
        IReadOnlyList<EnemyBattleData> enemyBattleDataList,
        string returnSceneName,
        Vector3 returnPosition,
        Quaternion returnRotation,
        bool canEscape = true,
        bool isPlayerAdvantage = false,
        bool isEnemyAdvantage = false)
    {
        // 추가
        _battleEncounter = encounter;

        _enemyBattleDataList.Clear();

        if (enemyBattleDataList != null)
        {
            for (int i = 0; i < enemyBattleDataList.Count; i++)
            {
                EnemyBattleData enemyBattleData = enemyBattleDataList[i];

                if (enemyBattleData == null)
                {
                    continue;
                }

                _enemyBattleDataList.Add(enemyBattleData);
            }
        }

        _returnSceneName = returnSceneName;
        _returnPosition = returnPosition;
        _returnRotation = returnRotation;
        _canEscape = canEscape;
        _isPlayerAdvantage = isPlayerAdvantage;
        _isEnemyAdvantage = isEnemyAdvantage;
    }

    /// <summary>
    /// 적 데이터 목록 복사
    /// </summary>
    /// <param name="result">복사 대상 목록</param>
    public void GetEnemyBattleDataList(List<EnemyBattleData> result)
    {
        if (result == null)
        {
            return;
        }

        result.Clear();

        for (int i = 0; i < _enemyBattleDataList.Count; i++)
        {
            EnemyBattleData enemyBattleData = _enemyBattleDataList[i];

            if (enemyBattleData == null)
            {
                continue;
            }

            result.Add(enemyBattleData);
        }
    }

    // 추가 배틀 씬 방 중심점으로 
    private Vector3 _targetBattlePosition;
    public Vector3 TargetBattlePosition => _targetBattlePosition;

    public void SetBattlePosition(Vector3 position)
    {
        _targetBattlePosition = position;
    }

    /// <summary>
    /// 전투 진입 정보 초기화
    /// </summary>
    public void ClearEncounter()
    {
        _enemyBattleDataList.Clear();
        _returnSceneName = string.Empty;
        _returnPosition = Vector3.zero;
        _returnRotation = Quaternion.identity;
        _canEscape = true;
        _isPlayerAdvantage = false;
        _isEnemyAdvantage = false;
    }

    // 추가, 몬스터 파괴
    public void DestroyEncounter()
    {
        if (_battleEncounter != null)
        {
            _battleEncounter = null;
        }
    }
}