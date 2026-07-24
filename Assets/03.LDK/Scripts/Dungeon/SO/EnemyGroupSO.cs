using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyGroup", menuName = "Dungeon/Enemy Group")]
public class EnemyGroupSO : ScriptableObject
{
    [Header("Spawn Settings")]
    [SerializeField] private int _minDepth = 0;
    [SerializeField] private int _maxDepth = 10;
    [SerializeField] private int _weight = 10; // 높을수록 출현 확률이 높음

    [Header("Content")]
    [SerializeField] private List<EnemySpawnEntry> _enemyEntries;

    public int MinDepth => _minDepth;
    public int MaxDepth => _maxDepth;
    public int Weight => _weight;

    [Header("Count Settings")] // 추가: 개수 조절용
    [SerializeField] private int _baseCount = 3;      // 기본 몬스터 수
    [SerializeField] private int _depthDivisor = 3;   // 몇 층마다 1마리씩 늘릴지 (예: 2면 2층당 1마리 증가)

    // Property 추가
    public int BaseCount => _baseCount;
    public int DepthDivisor => _depthDivisor;
    public List<EnemyBattleData> Enemies
    {
        get
        {
            List<EnemyBattleData> list = new List<EnemyBattleData>();

            foreach (var entry in _enemyEntries)
            {
                if (entry.enemyData != null)
                {
                    list.Add(entry.enemyData);
                }
            }
            return list;
        }
    }

    public EnemyBattleData GetRandomEnemy()
    {
        if (_enemyEntries == null || _enemyEntries.Count == 0) return null;

        // 1. 전체 가중치 합계 계산
        int totalWeight = 0;

        for (int i = 0; i < _enemyEntries.Count; i++)
        {
            totalWeight += _enemyEntries[i].weight;
        }

        // 가중치 합이 0인 경우 (모두 0으로 설정된 경우) 예외 처리
        if (totalWeight <= 0) 
        {
            Debug.LogWarning($"[EnemyGroupSO] {name}의 가중치 합이 0입니다. 첫 번째 몬스터를 반환합니다.");
            return _enemyEntries[0].enemyData;
        }

        // 2. 랜덤 값 추출
        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        // 3. 누적 가중치로 몬스터 선택
        for (int i = 0; i < _enemyEntries.Count; i++)
        {
            currentWeight += _enemyEntries[i].weight;
            if (randomValue < currentWeight)
            {
                return _enemyEntries[i].enemyData;
            }
        }

        return _enemyEntries[0].enemyData;
    }
}

[System.Serializable]
public struct EnemySpawnEntry
{
    public EnemyBattleData enemyData;
    [Range(0, 100)] 
    public int weight; // 몬스터별 출현 가중치
}