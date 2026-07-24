using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 퀘스트 체인 데이터
/// </summary>
[CreateAssetMenu(fileName = "QuestChainData", menuName = "Quest/Chain")]
public class QuestChainData : ScriptableObject
{
    public string chainID;


    public List<QuestData> mainQuests;
}