// FILE: Assets\_Scripts\Dungeon\Room\RoomInteraction\BossRoomInteraction.cs

using System.Collections.Generic;
using UnityEngine;

public class BossRoomInteraction : RoomInteraction
{
    private BattleEncounter _bossMonsterPrefab;
    private List<EnemyBattleData> _enemyDataList;
    private GameObject _exitPortalPrefab; // 💡 출구 포탈 프리팹 추가
    private RoomNode _room;

    public void Setup(BattleEncounter prefab, List<EnemyBattleData> enemyList, GameObject exitPortalPrefab, RoomNode room)
    {
        _bossMonsterPrefab = prefab;
        _enemyDataList = enemyList;
        _exitPortalPrefab = exitPortalPrefab; // 💡 포탈 프리팹 주입
        _room = room;
    }

    public override void Execute(Vector3 roomCenter)
    {
        if (_bossMonsterPrefab == null) return;

        Vector3 spawnPos = roomCenter; 
        
        BattleEncounter boss = Instantiate(_bossMonsterPrefab, spawnPos, Quaternion.identity);

        if (_enemyDataList != null && _enemyDataList.Count > 0)
        {
            boss.Initialize(_enemyDataList);
        }

        DungeonMonster bossAI = boss.GetComponent<DungeonMonster>();
        if (bossAI != null && _room != null)
        {
            bossAI.Initialize(_room);
        }

        Debug.Log("[BossRoomInteraction] 보스 스폰 및 처치 후 포탈 스포너 등록 완료");
    }
}