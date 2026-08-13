using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC 머리 위 퀘스트 상태 표시기 (대화 대상 NPC 감지 기능 포함)
/// </summary>
public class NPCQuestIndicator : MonoBehaviour
{
    private enum IndicatorState
    {
        None,
        MainExclamation,
        MainQuestion,
        SubExclamation,
        SubQuestion
    }

    [Header("메인 퀘스트 파티클")]
    [Tooltip("메인 퀘스트 수락 가능 (!)")]
    [SerializeField] private GameObject _mainExclamationPrefab;
    [Tooltip("메인 퀘스트 완료 가능 / 대화 대상 (?)")]
    [SerializeField] private GameObject _mainQuestionPrefab;

    [Header("서브 퀘스트 파티클 (선택)")]
    [Tooltip("서브 퀘스트 수락 가능 (!)")]
    [SerializeField] private GameObject _subExclamationPrefab;
    [Tooltip("서브 퀘스트 완료 가능 / 대화 대상 (?)")]
    [SerializeField] private GameObject _subQuestionPrefab;

    [Header("위치 설정")]
    [Tooltip("머리 위 위치 Transform (없으면 NPC 위치 + Offset)")]
    [SerializeField] private Transform _headTransform;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 2.2f, 0f);

    private NPC _npc;
    private GameObject _mainExclamationInstance;
    private GameObject _mainQuestionInstance;
    private GameObject _subExclamationInstance;
    private GameObject _subQuestionInstance;

    private void Awake()
    {
        _npc = GetComponent<NPC>();
    }

    private void Start()
    {
        InitParticles();
        RefreshIndicator();
    }

    private void OnEnable()
    {
        RefreshIndicator();
    }

    private void Update()
    {
        RefreshIndicator();
    }

    private void InitParticles()
    {
        Vector3 spawnPos = GetHeadPosition();

        _mainExclamationInstance = InstantiateParticle(_mainExclamationPrefab, spawnPos);
        _mainQuestionInstance = InstantiateParticle(_mainQuestionPrefab, spawnPos);
        _subExclamationInstance = InstantiateParticle(_subExclamationPrefab, spawnPos);
        _subQuestionInstance = InstantiateParticle(_subQuestionPrefab, spawnPos);
    }

    private GameObject InstantiateParticle(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return null;
        GameObject instance = Instantiate(prefab, position, Quaternion.identity, transform);
        instance.SetActive(false);
        return instance;
    }

    private Vector3 GetHeadPosition()
    {
        return _headTransform != null ? _headTransform.position : transform.position + _offset;
    }

    /// <summary>
    /// 퀘스트 상태 및 대화 대상 여부에 따른 표시기 갱신
    /// </summary>
    public void RefreshIndicator()
    {
        if (_npc == null || _npc.Data == null)
        {
            SetState(IndicatorState.None);
            return;
        }

        if (QuestManager.Instance == null)
        {
            SetState(IndicatorState.None);
            return;
        }

        // 1. 진행 중인 퀘스트의 대화/영입 대상인 경우 -> 물음표(?)
        List<QuestRuntime> runningList = QuestManager.Instance.GetRunningQuests();
        foreach (QuestRuntime running in runningList)
        {
            if (running.State != QuestState.Running) continue;

            foreach (QuestObjective obj in running.Data.objectives)
            {
                if ((obj.type == QuestObjectiveType.TalkNPC || obj.type == QuestObjectiveType.RecruitNPC) &&
                    obj.targetID == _npc.Data.NpcId)
                {
                    bool isMainQuest = running.Data.type == QuestType.Main;
                    SetState(isMainQuest ? IndicatorState.MainQuestion : IndicatorState.SubQuestion);
                    return;
                }
            }
        }

        if (string.IsNullOrEmpty(_npc.Data.QuestId))
        {
            SetState(IndicatorState.None);
            return;
        }

        // 2. 퀘스트 상태 추적 (무한 루프 방어막 적용)
        string currentQuestId = _npc.Data.QuestId;
        QuestRuntime questRuntime = QuestManager.Instance.GetQuest(currentQuestId);

        int safetyGuard = 0; // 💡 무한 루프 방지용 안전장치

        while (questRuntime != null && questRuntime.State == QuestState.Rewarded)
        {
            safetyGuard++;
            if (safetyGuard > 20) // 20번 이상 순환되면 무한 루프로 판단하고 탈출!
            {
                Debug.LogError($"[NPCQuestIndicator] 무한 순환 감지! {_npc.Data.NpcName}의 퀘스트 NextQuest 설정을 확인하세요.");
                break;
            }

            if (questRuntime.Data != null && questRuntime.Data.nextQuest != null)
            {
                string nextId = questRuntime.Data.nextQuest.id;

                // 자기 자신을 NextQuest로 연결한 경우 즉시 무한 루프 차단
                if (nextId == currentQuestId)
                {
                    Debug.LogError($"[NPCQuestIndicator] {currentQuestId} 퀘스트의 NextQuest가 자기 자신으로 설정되어 있습니다!");
                    break;
                }

                QuestRuntime nextRuntime = QuestManager.Instance.GetQuest(nextId);

                if (nextRuntime != null)
                {
                    questRuntime = nextRuntime;
                    currentQuestId = nextId;
                }
                else
                {
                    questRuntime = null;
                    currentQuestId = nextId;
                    break;
                }
            }
            else
            {
                break;
            }
        }

        QuestData questData = questRuntime != null ? questRuntime.Data : QuestManager.Instance.GetQuestData(currentQuestId);

        if (questData == null)
        {
            SetState(IndicatorState.None);
            return;
        }

        bool isMain = questData.type == QuestType.Main;

        if (questRuntime == null)
        {
            SetState(isMain ? IndicatorState.MainExclamation : IndicatorState.SubExclamation);
            return;
        }

        if (questRuntime.State == QuestState.Completed)
        {
            SetState(isMain ? IndicatorState.MainQuestion : IndicatorState.SubQuestion);
            return;
        }

        SetState(IndicatorState.None);
    }

    private void SetState(IndicatorState state)
    {
        if (_mainExclamationInstance) _mainExclamationInstance.SetActive(state == IndicatorState.MainExclamation || (state == IndicatorState.SubExclamation && _subExclamationInstance == null));
        if (_mainQuestionInstance) _mainQuestionInstance.SetActive(state == IndicatorState.MainQuestion || (state == IndicatorState.SubQuestion && _subQuestionInstance == null));

        if (_subExclamationInstance) _subExclamationInstance.SetActive(state == IndicatorState.SubExclamation);
        if (_subQuestionInstance) _subQuestionInstance.SetActive(state == IndicatorState.SubQuestion);
    }
}