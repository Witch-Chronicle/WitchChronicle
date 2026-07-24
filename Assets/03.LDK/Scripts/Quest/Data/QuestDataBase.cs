using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestDatabase", menuName = "Game/Quest Database")]
public class QuestDatabase : ScriptableObject
{
    [SerializeField]
    private List<QuestData> _questList = new();

    private Dictionary<string, QuestData> _questDictionary;

    /// <summary>
    /// 초기화
    /// </summary>
    public void Initialize()
    {
        _questDictionary = new Dictionary<string, QuestData>();

        foreach (QuestData quest in _questList)
        {
            if (_questDictionary.ContainsKey(quest.id))
            {
                Debug.LogWarning($"Duplicate Quest ID : {quest.id}");

                continue;
            }

            _questDictionary.Add(quest.id, quest);
        }
    }

    /// <summary>
    /// 퀘스트 조회
    /// </summary>
    public QuestData GetQuest(string id)
    {
        if (_questDictionary == null)
        {
            Initialize();
        }

        _questDictionary.TryGetValue(id, out QuestData quest);

        return quest;
    }

    /// <summary>
    /// 전체 조회
    /// </summary>
    public List<QuestData> GetAllQuest()
    {
        return _questList;
    }
}