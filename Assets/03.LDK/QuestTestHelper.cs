using UnityEngine;

/// <summary>
/// 디버그/테스트용 퀘스트 강제 완료 컨트롤러
/// </summary>
public class QuestTestHelper : MonoBehaviour
{
    [Header("테스트 단축키")]
    [Tooltip("현재 메인 스토리 퀘스트의 목표만 완료")]
    [SerializeField] private KeyCode _completeObjectiveKey = KeyCode.F1;

    [Tooltip("현재 메인 스토리 퀘스트를 보상까지 즉시 완료")]
    [SerializeField] private KeyCode _completeAndRewardKey = KeyCode.F2;

    [Header("특정 퀘스트 직접 테스트")]
    [Tooltip("수동으로 완료할 퀘스트 ID")]
    [SerializeField] private string _targetQuestId = "main_000";

    private void Update()
    {
        if (Input.GetKeyDown(_completeObjectiveKey))
        {
            bool success = CompleteCurrentMainObjectiveOnly();

            Debug.Log(
                success
                    ? "<color=green>[QuestTestHelper] F1 퀘스트 목표 완료 성공</color>"
                    : "<color=red>[QuestTestHelper] F1 퀘스트 목표 완료 실패</color>"
            );
        }

        if (Input.GetKeyDown(_completeAndRewardKey))
        {
            bool success = CompleteCurrentMainWithReward();

            Debug.Log(
                success
                    ? "<color=green>[QuestTestHelper] F2 퀘스트 보상 완료 성공</color>"
                    : "<color=red>[QuestTestHelper] F2 퀘스트 보상 완료 실패</color>"
            );
        }
    }

    /// <summary>
    /// 현재 메인 퀘스트 목표만 완료합니다.
    /// </summary>
    [ContextMenu("1. [F1] 현재 메인 퀘스트 목표 완료")]
    public bool CompleteCurrentMainObjectiveOnly()
    {
        if (QuestChainManager.Instance == null)
        {
            Debug.LogError(
                "[QuestTestHelper] QuestChainManager.Instance가 없습니다.",
                this
            );

            return false;
        }

        QuestData currentMain =
            QuestChainManager.Instance.GetCurrentQuest();

        if (currentMain == null)
        {
            Debug.LogWarning(
                "[QuestTestHelper] QuestChainManager의 현재 메인 퀘스트가 null입니다.",
                this
            );

            return false;
        }

        Debug.Log(
            $"[QuestTestHelper] F1 완료 대상: " +
            $"ID={currentMain.id}, Name={currentMain.title}",
            this
        );

        return ForceCompleteObjective(currentMain.id);
    }

    /// <summary>
    /// 현재 메인 퀘스트를 보상까지 완료합니다.
    /// </summary>
    [ContextMenu("2. [F2] 현재 메인 퀘스트 보상까지 완료")]
    public bool CompleteCurrentMainWithReward()
    {
        if (QuestChainManager.Instance == null)
        {
            Debug.LogError(
                "[QuestTestHelper] QuestChainManager.Instance가 없습니다.",
                this
            );

            return false;
        }

        QuestData currentMain =
            QuestChainManager.Instance.GetCurrentQuest();

        if (currentMain == null)
        {
            Debug.LogWarning(
                "[QuestTestHelper] 현재 메인 퀘스트가 없습니다.",
                this
            );

            return false;
        }

        return ForceCompleteQuestAndReward(currentMain.id);
    }

    [ContextMenu("3. Target Quest ID 목표 완료")]
    public void CompleteTargetObjective()
    {
        ForceCompleteObjective(_targetQuestId);
    }

    /// <summary>
    /// 지정한 퀘스트의 모든 목표를 채우고 Completed로 변경합니다.
    /// 시작하지 않았다면 먼저 시작합니다.
    /// </summary>
    public bool ForceCompleteObjective(string questID)
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogError(
                "[QuestTestHelper] QuestManager.Instance가 없습니다.",
                this
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(questID))
        {
            Debug.LogError(
                "[QuestTestHelper] 완료할 Quest ID가 비어 있습니다.",
                this
            );

            return false;
        }

        QuestRuntime runtime =
            QuestManager.Instance.GetQuest(questID);

        if (runtime == null)
        {
            Debug.Log(
                $"[QuestTestHelper] 퀘스트가 시작되지 않아 시작합니다. " +
                $"ID={questID}",
                this
            );

            QuestManager.Instance.StartQuest(questID);

            runtime = QuestManager.Instance.GetQuest(questID);
        }

        if (runtime == null)
        {
            Debug.LogError(
                $"[QuestTestHelper] 퀘스트를 시작하지 못했습니다. " +
                $"Database에 ID가 존재하는지 확인하세요. ID={questID}",
                this
            );

            return false;
        }

        Debug.Log(
            $"[QuestTestHelper] 변경 전 상태: " +
            $"ID={questID}, State={runtime.State}",
            this
        );

        if (runtime.Data == null)
        {
            Debug.LogError(
                $"[QuestTestHelper] QuestRuntime.Data가 null입니다. ID={questID}",
                this
            );

            return false;
        }

        if (runtime.Progress == null)
        {
            Debug.LogError(
                $"[QuestTestHelper] QuestRuntime.Progress가 null입니다. ID={questID}",
                this
            );

            return false;
        }

        if (runtime.Data.objectives == null)
        {
            Debug.LogError(
                $"[QuestTestHelper] QuestData.objectives가 null입니다. ID={questID}",
                this
            );

            return false;
        }

        if (runtime.Progress.Count != runtime.Data.objectives.Count)
        {
            Debug.LogError(
                $"[QuestTestHelper] 목표와 진행도 개수가 다릅니다. " +
                $"Objectives={runtime.Data.objectives.Count}, " +
                $"Progress={runtime.Progress.Count}, ID={questID}",
                this
            );

            return false;
        }

        for (int i = 0; i < runtime.Data.objectives.Count; i++)
        {
            QuestObjective objective = runtime.Data.objectives[i];

            runtime.Progress[i] = objective.requiredCount;

            Debug.Log(
                $"[QuestTestHelper] 목표 완료: " +
                $"Index={i}, Target={objective.targetID}, " +
                $"Progress={runtime.Progress[i]}/{objective.requiredCount}",
                this
            );
        }

        runtime.State = QuestState.Completed;

        QuestListUI.Instance?.Refresh();

        Debug.Log(
            $"<color=yellow>[QuestTestHelper] 퀘스트 목표 완료: " +
            $"ID={questID}, Name={runtime.Data.title}, " +
            $"State={runtime.State}</color>",
            this
        );

        return runtime.State == QuestState.Completed;
    }

    /// <summary>
    /// 퀘스트 목표 완료 후 보상까지 지급합니다.
    /// </summary>
    public bool ForceCompleteQuestAndReward(string questID)
    {
        bool completed = ForceCompleteObjective(questID);

        if (!completed)
        {
            return false;
        }

        QuestManager.Instance.RewardQuest(questID);

        QuestRuntime runtime =
            QuestManager.Instance.GetQuest(questID);

        bool rewarded =
            runtime != null &&
            runtime.State == QuestState.Rewarded;

        Debug.Log(
            rewarded
                ? $"<color=green>[QuestTestHelper] 보상 완료: {questID}</color>"
                : $"<color=red>[QuestTestHelper] 보상 처리 실패: {questID}</color>",
            this
        );

        return rewarded;
    }
}