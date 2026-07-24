using UnityEngine;

public class BossRoomInteraction : RoomInteraction
{
    private BattleEncounter _bossMonsterPrefab;

    public void Setup(BattleEncounter prefab)
    {
        _bossMonsterPrefab = prefab;
    }

    /// <summary>
    /// 부모 클래스 를 override 하여 재정의, 보스 방일 경우, 보스 생성 후 전투(상호작용)
    /// </summary>
    /// <param name="playerTransform">풀레이어 의 위치 정보</param>
    /// <param name="roomCenter">방의 중심, 스폰 위치</param>
    public override void Execute(Vector3 roomCenter)
    {
        if (_bossMonsterPrefab == null) return;

        //전달받은 방의 정중앙 좌표(roomCenter)에 보스 생성
        BattleEncounter boss = Instantiate(_bossMonsterPrefab, roomCenter, Quaternion.identity);
        
        DungeonMonster bossAI = boss.GetComponent<DungeonMonster>();  

        if (bossAI != null)
        {
            //bossAI.OnDeath += ClearBossDungeon;
        }
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

        Debug.Log("Boss Dungeon Clear");
    }
}