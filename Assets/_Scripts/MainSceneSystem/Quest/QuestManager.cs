using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [SerializeField]
    private QuestDatabase _database;

    private readonly Dictionary<string, QuestRuntime> _runningQuest = new();

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

        _database.Initialize();
    }

    /// <summary>
    /// 퀘스트 시작
    /// </summary>
    public void StartQuest(string questID)
    {
        // 진행중이면 X
        if (_runningQuest.ContainsKey(questID))
        {
            return;
        }

        QuestData quest = _database.GetQuest(questID);

        if (quest == null)
        {
            Debug.LogError($"Quest Missing : {questID}");

            return;
        }

        QuestRuntime runtime = new QuestRuntime(quest);

        _runningQuest.Add(questID, runtime);

        Debug.Log($"Quest Start : {quest.title}");

        QuestListUI.Instance.Refresh();
    }

    /// <summary>
    /// 퀘스트 조회
    /// </summary>
    public QuestRuntime GetQuest(string questID)
    {
        _runningQuest.TryGetValue(questID, out QuestRuntime runtime);

        return runtime;
    }

    /// <summary>
    /// 진행 중인 퀘스트 목록 반환
    /// 현재 플레이어가 수락한 퀘스트 데이터 조회
    /// </summary>
    public List<QuestRuntime> GetRunningQuests()
    {
        return new List<QuestRuntime>(_runningQuest.Values);
    }

    /// <summary>
    /// 진행 중 여부
    /// </summary>
    public bool IsRunning(string questID)
    {
        return _runningQuest.ContainsKey(questID);
    }

    /// <summary>
    /// 완료 여부
    /// </summary>
    public bool IsCompleted(string questID)
    {
        if (_runningQuest.TryGetValue(questID, out QuestRuntime runtime) == false)
        {
            return false;
        }

        return runtime.State == QuestState.Completed;
    }

    /// <summary>
    /// 진행도 추가
    /// </summary>
    public void AddProgress(QuestObjectiveType type, string targetID, int amount = 1)
    {
        foreach (QuestRuntime runtime in _runningQuest.Values)
        {
            if (runtime.State != QuestState.Running)
            {
                continue;
            }

            for (int i = 0; i < runtime.Data.objectives.Count; i++)
            {
                QuestObjective objective = runtime.Data.objectives[i];

                if (objective.type != type)
                {
                    continue;
                }

                if (objective.targetID != targetID)
                {
                    continue;
                }

                runtime.Progress[i] += amount;

                if (runtime.Progress[i] > objective.requiredCount)
                {
                    runtime.Progress[i] = objective.requiredCount;
                }
            }

            CheckComplete(runtime);
        }

        QuestListUI.Instance.Refresh();
    }

    /// <summary>
    /// 완료 검사
    /// </summary>
    private void CheckComplete(QuestRuntime runtime)
    {
        for (int i = 0; i < runtime.Data.objectives.Count; i++)
        {
            if (runtime.Progress[i] < runtime.Data.objectives[i].requiredCount)
            {
                return;
            }
        }

        runtime.State = QuestState.Completed;

        Debug.Log($"Quest Complete : {runtime.Data.title}");
    }

    /// <summary>
    /// 퀘스트 완료 처리
    /// 완료 상태 변경 및 목록 갱신
    /// </summary>
    public void CompleteQuest(string questID)
    {
        QuestRuntime runtime = GetQuest(questID);


        if (runtime == null)
        {
            return;
        }


        if (runtime.State != QuestState.Completed)
        {
            return;
        }


        RewardQuest(questID);
    }

    // QuestManager.cs 에 추가
    public QuestData GetQuestData(string questID)
    {
        return _database != null ? _database.GetQuest(questID) : null;
    }

    /// <summary>
    /// 보상 지급
    /// </summary>
    public void RewardQuest(string questID)
    {
        QuestRuntime runtime = GetQuest(questID);

        if (runtime == null)
        {
            return;
        }

        // 💡 [수정] 진행 중(Running) 상태라면, 목표 진행도를 채우고 Completed 상태로 전환해 줍니다.
        if (runtime.State == QuestState.Running)
        {
            for (int i = 0; i < runtime.Data.objectives.Count; i++)
            {
                runtime.Progress[i] = runtime.Data.objectives[i].requiredCount;
            }

            runtime.State = QuestState.Completed;
        }

        // 이미 보상을 받은 상태(Rewarded)이거나 시작하지 않은 상태라면 중단
        if (runtime.State != QuestState.Completed)
        {
            return;
        }

        runtime.State = QuestState.Rewarded;

        QuestReward reward = runtime.Data.reward;

        List<string> rewardMessages = new List<string>();

        // 골드 처리
        if (reward.gold > 0)
        {
            // PlayerManager.Instance.AddGold(reward.gold);
            Debug.Log($"Reward Gold : {reward.gold}");

            rewardMessages.Add($"골드 +{reward.gold}");
        }

        // 경험치 처리
        if (reward.exp > 0)
        {
            //PlayerManager.Instance.AddExp(reward.exp);
            rewardMessages.Add($"경험치 +{reward.exp}");

            Debug.Log($"Reward Exp : {reward.exp}");
        }

        // 아이템 처리
        if (reward.item != null && reward.itemCount > 0) // (ItemData 타입 검사로 수정)
        {
            rewardMessages.Add($"아이템 {reward.item.name} x {reward.itemCount}");
            PlayerInventory.Instance.AddItem(reward.item, reward.itemCount);
            Debug.Log($"Reward Item : {reward.item.name} x {reward.itemCount}");
        }

        // NPC 영입 처리
        if (string.IsNullOrEmpty(reward.recruitNPC) == false)
        {
            rewardMessages.Add($"동료 영입 : {reward.recruitNPC}");
            RecruitManager.Instance.Recruit(reward.recruitNPC);
            Debug.Log($"Reward Recruit : {reward.recruitNPC}");
        }
        
        if (reward.nextStory)
        {
            QuestChainManager.Instance.NextQuest();
        }

        // 출력할 보상이 있을 경우만 출력
        if (rewardMessages.Count > 0)
        {
            string message = string.Join("\n", rewardMessages);

            ShowMessageManager.Instance.ShowMessage(message);
        }
    }
    public List<QuestData> GetAvailableQuests()
    {
        // 데이터베이스의 모든 퀘스트 반환
        return _database.GetAllQuest(); 
    }

}