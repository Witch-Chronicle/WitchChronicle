using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 방 상호작용을 처리하고 보스 적 데이터와 방 정보를 초기화하는 클래스입니다.
/// </summary>
public class BossRoomInteraction : RoomInteraction
{
    private BattleEncounter _bossMonsterPrefab;
    private List<EnemyBattleData> _enemyDataList;
    private RoomNode _room;

    /// <summary>
    /// 외부에서 보스 프리팹, 적 데이터 목록, 방 정보를 주입받아 초기화합니다.
    /// </summary>
    /// <param name="prefab">보스 몬스터 조우 프리팹</param>
    /// <param name="enemyList">보스 적 데이터 목록</param>
    /// <param name="room">보스가 소속된 방 노드</param>
    public void Setup(BattleEncounter prefab, List<EnemyBattleData> enemyList, RoomNode room)
    {
        _bossMonsterPrefab = prefab;
        _enemyDataList = enemyList;
        _room = room;
    }

    /// <summary>
    /// 부모 클래스를 override 하여 재정의, 보스 방일 경우, 보스 생성 후 데이터 초기화 및 전투 준비
    /// </summary>
    /// <param name="roomCenter">방의 중심, 스폰 위치</param>
    public override void Execute(Vector3 roomCenter)
    {
        if (_bossMonsterPrefab == null)
        {
            return;
        }

        // 전달받은 방의 정중앙 좌표(roomCenter)에 보스 생성
        BattleEncounter boss = Instantiate(_bossMonsterPrefab, roomCenter + new Vector3(0f, -1.5f, 0f), Quaternion.identity);
        
        // 적 데이터 목록 초기화
        if (_enemyDataList != null && _enemyDataList.Count > 0)
        {
            boss.Initialize(_enemyDataList);
        }

        DungeonMonster bossAI = boss.GetComponent<DungeonMonster>();  

        if (bossAI != null)
        {
            if (_room != null)
            {
                bossAI.Initialize(_room);
            }
            
            // bossAI.OnDeath += ClearBossDungeon;
        }

        Debug.Log("[BossRoomInteraction] 보스 방 전투 조우 생성 및 초기화 완료");
    }

    /// <summary>
    /// 보스 처치 시 던전 클리어 처리
    /// </summary>
    private void ClearBossDungeon()
    {
        DungeonController controller = FindObjectOfType<DungeonController>();

        if (controller != null)
        {
            controller.ClearDungeon();
        }

        Debug.Log("[BossRoomInteraction] Boss Dungeon Clear");
    }
}