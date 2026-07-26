using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상자 보상 데이터.
/// 랜덤 보상 목록과 확률을 관리한다.
/// </summary>
[CreateAssetMenu(fileName = "ChestReward_", menuName = "Game/Chest Reward")]
public class ChestRewardData : ScriptableObject
{
    public List<ChestReward> rewards;
}


[System.Serializable]
public class ChestReward
{
    public ItemData item;

    [Range(0, 100)]
    public float chance;
}