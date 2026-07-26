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

        BattleEncounter monster = Instantiate(
            _monsterPrefab,
            roomCenter,
            Quaternion.identity);

        monster.Initialize(_enemyDataList);

        DungeonMonster dungeonMonster = monster.GetComponent<DungeonMonster>();

        if (dungeonMonster != null)
        {
            dungeonMonster.Initialize(_room);
        }
    }
}