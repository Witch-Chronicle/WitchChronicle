using System.Collections.Generic;
using UnityEngine;

public class BattleRoomInteraction : RoomInteraction
{
    private BattleEncounter _monsterPrefab;
    private  List<EnemyBattleData> _enemyDataList;

    private RoomNode _room;

    public void Setup(BattleEncounter prefab,  List<EnemyBattleData> enemyList, RoomNode room)
    {
        _monsterPrefab = prefab;
        _enemyDataList = enemyList;
        _room = room;
    }

    public override void Execute(Vector3 roomCenter)
    {
        if (_monsterPrefab == null)
        {
            return;
        }

        Vector3 spawnPos = new Vector3(roomCenter.x, -1f, roomCenter.z); // 스폰 위치를 방 중심으로 설정

        BattleEncounter monster = Instantiate(
            _monsterPrefab,
            spawnPos,
            Quaternion.identity);

        monster.Initialize(_enemyDataList);

        DungeonMonster dungeonMonster = monster.GetComponent<DungeonMonster>();

        if (dungeonMonster != null)
        {
            dungeonMonster.Initialize(_room);
        }
    }
}