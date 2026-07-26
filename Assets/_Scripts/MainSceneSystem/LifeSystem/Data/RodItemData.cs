using UnityEngine;

[CreateAssetMenu(fileName = "NewRod", menuName = "WitchChronicle/Rod Item Data")]
public class RodItemData : ItemData
{
    [Header("낚싯대 등급")]
    [Tooltip("1: 나뭇가지, 2: 철제, 3: 마법")]
    [Range(1, 3)]
    public int rodRank = 1;

    [Tooltip("이 낚싯대로 잡을 수 있는 최고 등급")]
    public FishGrade maxCatchableGrade = FishGrade.Common;
}