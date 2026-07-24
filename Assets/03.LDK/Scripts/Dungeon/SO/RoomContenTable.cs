using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Dungeon/Room Content Table")]
public class RoomContentTable : ScriptableObject
{
    [Header("Battle")]
    public BattleEncounter battleEncounterPrefab;
    public List<EnemyGroupSO> monsterGroupPool;

    [Serializable]
    public class ChestEntry
    {
        public GameObject prefab;

        public ChestRewardData rewardData;

        [Range(0, 100)] public int Weight;// 인스펙터에서 설정할 가중치
    }

    [Header("Treasure")]
    public List<ChestEntry> chestPrefabs;

    [Header("Shop")]
    public GameObject shopKeeperPrefab;

    [Header("Boss")]
    public BattleEncounter bossEncounterPrefab;

    [Header("Event")]
    public EventRoomTableSO eventRoomTableSO;

    [Header("Exit")]
    public GameObject exitPortalPrefab;
}