using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quest Board 관리
/// </summary>
public class QuestBoardManager : MonoBehaviour
{
    public static QuestBoardManager Instance { get; private set; }


    [SerializeField]
    private QuestDatabase _database;


    /// <summary>
    /// 초기화
    /// </summary>
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);

            return;
        }

        Instance = this;
    }


    /// <summary>
    /// 게시판 퀘스트 조회
    /// </summary>
    public List<QuestData> GetBoardQuests()
    {
        List<QuestData> quests = new();


        foreach (QuestData quest in _database.GetAllQuest())
        {
            if (quest.type == QuestType.Repeatable ||
                quest.type == QuestType.Dungeon)
            {
                quests.Add(quest);
            }
        }


        return quests;
    }

    /// <summary>
    /// 뭬스트를 수락할수 있는지 없는지
    /// </summary>
    /// <param name="quest"></param>
    /// <returns></returns>
    public bool CanAccept(QuestData quest)
    {
        QuestRuntime runtime =
            QuestManager.Instance.GetQuest(quest.id);


        if (runtime == null)
        {
            return true;
        }


        if (quest.type == QuestType.Repeatable)
        {
            return runtime.State == QuestState.Rewarded;
        }


        return false;
    }
}