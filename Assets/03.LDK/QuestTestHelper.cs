using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 디버그/테스트용 퀘스트 강제 완료 컨트롤러
/// - 키보드 숫자 9 키: 현재 진행 중인 일반 퀘스트 목표치 100% 즉시 달성
/// - 영입 퀘스트(main_003, main_007, main_011)는 치트 실행을 강제로 원천 차단!
/// </summary>
public class QuestTestHelper : MonoBehaviour
{
    [Header("테스트 단축키")]
    [Tooltip("메인 퀘스트 목표 완료 전용 키 (기본: 숫자 9키)")]
    [SerializeField] private KeyCode _completeObjectiveKey = KeyCode.Alpha9;

    [Header("특정 퀘스트 직접 테스트")]
    [Tooltip("수동으로 완료할 퀘스트 ID")]
    [SerializeField] private string _targetQuestId = "main_000";

    // 💡 [핵심] 치트/단축키 실행을 원천 차단할 영입 퀘스트 ID 목록
    private static readonly HashSet<string> _recruitmentQuestIds = new HashSet<string>
    {
        "main_003", // 셀레네 영입
        "main_007", // 라이아 영입
        "main_011"  // 페이 영입
    };

    private void Update()
    {
        // 숫자 9 키 감지 (신형 & 구형 Input 모두 지원)
        bool isAlpha9Pressed = false;

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.digit9Key.wasPressedThisFrame)
        {
            isAlpha9Pressed = true;
        }
#endif

        if (Input.GetKeyDown(_completeObjectiveKey))
        {
            isAlpha9Pressed = true;
        }

        // 숫자 9 키를 누르면 일반 퀘스트만 목표 완료 처리!
        if (isAlpha9Pressed)
        {
            bool success = CompleteAllRunningOrMainObjective();

            if (!success)
            {
                Debug.LogWarning("[QuestTestHelper] [9키] 목표 달성 실행 불가 (진행 중인 일반 퀘스트가 없거나 영입 퀘스트 차단됨)");
            }
        }
    }

    /// <summary>
    /// 현재 진행 중인 일반 퀘스트의 목표치를 100%로 채웁니다. (영입 퀘스트는 차단)
    /// </summary>
    [ContextMenu("1. [9키] 진행 중인 퀘스트 목표 완료")]
    public bool CompleteAllRunningOrMainObjective()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogError("[QuestTestHelper] QuestManager.Instance가 없습니다.", this);
            return false;
        }

        List<QuestRuntime> runningList = QuestManager.Instance.GetRunningQuests();
        bool anySuccess = false;

        if (runningList != null && runningList.Count > 0)
        {
            for (int i = runningList.Count - 1; i >= 0; i--)
            {
                if (runningList[i] != null && runningList[i].State == QuestState.Running)
                {
                    bool ok = ForceCompleteObjective(runningList[i].Data.id);
                    if (ok) anySuccess = true;
                }
            }

            if (anySuccess) return true;
        }

        if (QuestChainManager.Instance != null)
        {
            QuestData currentMain = QuestChainManager.Instance.GetCurrentQuest();
            if (currentMain != null)
            {
                return ForceCompleteObjective(currentMain.id);
            }
        }

        return false;
    }

    /// <summary>
    /// 지정한 퀘스트의 목표를 채웁니다.
    /// 💡 [원천 차단] main_003, main_007, main_011 영입 퀘스트는 강제로 차단하고 경고를 출력합니다.
    /// </summary>
    public bool ForceCompleteObjective(string questID)
    {
        if (QuestManager.Instance == null || string.IsNullOrWhiteSpace(questID))
        {
            return false;
        }

        // 💡 [핵심 강제 차단] 영입 퀘스트는 치트 실행을 아예 막아버림!
        if (_recruitmentQuestIds.Contains(questID))
        {
            Debug.LogWarning($"<color=orange>[QuestTestHelper] '{questID}'는 영입 퀘스트입니다. 치트로 완료할 수 없으니 플레이어가 직접 NPC와 대화하여 진행하십시오!</color>", this);
            return false;
        }

        QuestRuntime runtime = QuestManager.Instance.GetQuest(questID);

        if (runtime == null)
        {
            Debug.Log($"[QuestTestHelper] 퀘스트가 시작되지 않아 시작합니다. ID={questID}", this);
            QuestManager.Instance.StartQuest(questID);
            runtime = QuestManager.Instance.GetQuest(questID);
        }

        if (runtime == null || runtime.Data == null || runtime.Progress == null || runtime.Data.objectives == null)
        {
            return false;
        }

        // 목표 진행도를 100% 채움
        for (int i = 0; i < runtime.Data.objectives.Count; i++)
        {
            QuestObjective objective = runtime.Data.objectives[i];
            runtime.Progress[i] = objective.requiredCount;
        }

        // 상태를 Completed(완료 대기)로 변경
        runtime.State = QuestState.Completed;

        QuestListUI.Instance?.Refresh();

        Debug.Log($"<color=yellow>[QuestTestHelper] 퀘스트 목표 달성 성공: ID={questID}, Title={runtime.Data.title}</color>", this);

        return runtime.State == QuestState.Completed;
    }
}