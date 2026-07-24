using System.Collections.Generic;
using UnityEngine;


public class TreasureRoomInteraction : RoomInteraction
{
    private IReadOnlyList<RoomContentTable.ChestEntry> _chests;

    private float _spawnRadius = 2f;

    private float _yOffset;


    /// <summary>
    /// 런타임 보물상자 설정
    /// DungeonData 또는 RoomData에서 전달
    /// </summary>
    public void Setup(IReadOnlyList<RoomContentTable.ChestEntry> chestEntries, float yOffset)
    {
        _chests = chestEntries;

        _yOffset = yOffset;

    }

    public override void Execute(Vector3 roomCenter)
    {
        RoomContentTable.ChestEntry chestInfo = GetRandomChest();


        if (chestInfo == null)
        {
            Debug.LogWarning("Treasure Chest Missing");

            return;
        }

            Vector3 position = roomCenter + Random.insideUnitSphere * _spawnRadius;

            position.y = _yOffset;

            Instantiate(chestInfo.prefab, position, Quaternion.identity);
    }



    /// <summary>
    /// 가중치 총합 기반의 안전한 랜덤 상자 추출 로직
    /// </summary>
    private RoomContentTable.ChestEntry GetRandomChest()
    {
        if (_chests == null || _chests.Count == 0)
        {
            return null;
        }

        int totalWeight = 0;
        foreach (var chest in _chests)
        {
            totalWeight += chest.Weight;
        }

        if (totalWeight <= 0)
        {
            return _chests[0];
        }

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var chest in _chests)
        {
            currentWeight += chest.Weight;

            if (randomValue < currentWeight)
            {
                return chest;
            }
        }

        return _chests[_chests.Count - 1];
    }
}