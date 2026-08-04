using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 디버그/테스트용 퀘스트 강제 완료 컨트롤러
/// </summary>
public class QuestTestHelper : MonoBehaviour
{
    [Header("테스트 단축키")]
    [Tooltip("현재 메인 스토리 퀘스트 '목표 달성만' 처리 (NPC 대화 테스트용)")]
    [SerializeField] private KeyCode _completeObjectiveKey = KeyCode.F1;

    [Tooltip("현재 메인 스토리 퀘스트 '보상까지 즉시 완료'")]
    [SerializeField] private KeyCode _completeAndRewardKey = KeyCode.F2;

    [Header("특정 퀘스트 직접 테스트")]
    [Tooltip("수동으로 완료시킬 퀘스트 ID")]
    [SerializeField] private string _targetQuestId = "main_000";


    private void Update()
    {
        // F1 키: 목표만 달성 (NPC 대화 대기 상태 만들기 - 물음표 파티클 확인용)
        if (Input.GetKeyDown(_completeObjectiveKey))
        {
            CompleteCurrentMainObjectiveOnly();
        }

        // F2 키: 즉시 보상 지급 및 퀘스트 완결
        if (Input.GetKeyDown(_completeAndRewardKey))
        {
            CompleteCurrentMainWithReward();
        }
    }


    /// <summary>
    /// 1. 현재 메인 퀘스트 '목표만 달성' (Completed 상태)
    /// </summary>
    [ContextMenu("1. [F1] 현재 메인 퀘스트 '목표만 달성' (Completed)")]
    public void CompleteCurrentMainObjectiveOnly()
    {
        if (QuestChainManager.Instance == null)
        {
            Debug.LogError("[QuestTestHelper] 씬에 QuestChainManager가 없습니다!");
            return;
        }

        QuestData currentMain = QuestChainManager.Instance.GetCurrentQuest();

        if (currentMain == null)
        {
            Debug.LogWarning("[QuestTestHelper] 현재 진행 대상인 메인 퀘스트가 없습니다.");
            return;
        }

        ForceCompleteObjective(currentMain.id);
    }


    /// <summary>
    /// 2. 현재 메인 퀘스트 '보상까지 완결' (Rewarded)
    /// </summary>
    [ContextMenu("2. [F2] 현재 메인 퀘스트 '보상까지 완결' (Rewarded)")]
    public void CompleteCurrentMainWithReward()
    {
        if (QuestChainManager.Instance == null) return;

        QuestData currentMain = QuestChainManager.Instance.GetCurrentQuest();

        if (currentMain == null) return;

        ForceCompleteQuestAndReward(currentMain.id);
    }


    /// <summary>
    /// 지정한 _targetQuestId 목표만 완료
    /// </summary>
    [ContextMenu("3. Target Quest ID 목표만 완료")]
    public void CompleteTargetObjective()
    {
        ForceCompleteObjective(_targetQuestId);
    }


    /// <summary>
    /// [핵심] 퀘스트 목표치 100% 채우고 Completed 상태 변경 (시작 안 되어 있으면 강제 시작)
    /// </summary>
    public void ForceCompleteObjective(string questID)
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogError("[QuestTestHelper] 씬에 QuestManager가 없습니다!");
            return;
        }

        // 1. 퀘스트가 시작 안 된 상태라면 강제로 먼저 시작시킴!
        QuestRuntime runtime = QuestManager.Instance.GetQuest(questID);

        if (runtime == null)
        {
            Debug.Log($"[QuestTestHelper] 퀘스트[{questID}]가 시작 안 되어 있어 강제 시작합니다.");
            QuestManager.Instance.StartQuest(questID);
            runtime = QuestManager.Instance.GetQuest(questID);
        }

        if (runtime == null)
        {
            Debug.LogError($"[QuestTestHelper] 퀘스트[{questID}]를 Database에서 찾을 수 없습니다! ID를 확인하세요.");
            return;
        }

        // 2. 모든 목표 수치를 최대 요구 수치로 강제 설정
        for (int i = 0; i < runtime.Data.objectives.Count; i++)
        {
            runtime.Progress[i] = runtime.Data.objectives[i].requiredCount;
        }

        // 3. 퀘스트 상태를 Completed로 변경
        runtime.State = QuestState.Completed;

        Debug.Log($"<color=yellow>[QuestTestHelper] 퀘스트[{questID}] 목표 달성 성공! (NPC 대화 가능 상태)</color>");
    }


    /// <summary>
    /// 퀘스트 목표 달성 + 보상 즉시 지급까지 완결
    /// </summary>
    public void ForceCompleteQuestAndReward(string questID)
    {
        ForceCompleteObjective(questID);

        // 보상 지급 및 Rewarded 상태 처리
        QuestManager.Instance.RewardQuest(questID);

        Debug.Log($"<color=green>[QuestTestHelper] 퀘스트[{questID}] 보상 즉시 지급 완료!</color>");
    }
}