using UnityEngine;

public class NPC : MonoBehaviour, ITFInteractable
{
    [SerializeField]
    private NPCData _npcData;

    private void Start()
    {
        if (NPCManager.Instance != null)
        {
            NPCManager.Instance.RegisterNPC(this);
        }

        CheckAlreadyRecruited();
    }

    public NPCData Data => _npcData;

    // NPCData의 NpcName 프로퍼티 활용
    public string Prompt => _npcData != null 
        ? $"[F] {_npcData.NpcName} (과)와 대화하기" 
        : string.Empty;

    public void Interact(GameObject interactor)
    {
        if (_npcData == null)
        {
            Debug.LogWarning($"[{gameObject.name}] NPCData가 할당되지 않았습니다.");
            return;
        }

        string dialogueID = GetDialogueID();

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(_npcData, dialogueID);
        }
    }

    /// <summary>
    /// 이미 영입된 캐릭터인지 확인 후 필드에서 제거
    /// </summary>
    public void CheckAlreadyRecruited()
    {
        if (_npcData == null) return;

        if (PersistentCharacterManager.Instance != null)
        {
            // NPCData의 NpcId로 영입 상태 조회
            if (PersistentCharacterManager.Instance.TryGetCharacter(_npcData.NpcId, out var character))
            {
                if (character != null && character.IsRecruited)
                {
                    Debug.Log($"[{_npcData.NpcName}] 이미 영입된 캐릭터이므로 필드에서 비활성화합니다.");
                    Destroy(gameObject); // 또는 Destroy(gameObject);
                }
            }
        }
    }

    /// <summary>
    /// 현재 NPC 및 퀘스트 상태, NextQuest 체인을 반영한 대화 ID 반환
    /// </summary>
    private string GetDialogueID()
    {
        // 1. NPCData나 QuestId가 없으면 기본 대화 ID 반환
        if (_npcData == null || string.IsNullOrEmpty(_npcData.QuestId))
        {
            return _npcData != null ? _npcData.DefaultDialogueId : string.Empty;
        }

        if (QuestManager.Instance == null)
        {
            return _npcData.DefaultDialogueId;
        }

        // 2. 기본 QuestId(예: "main_003")로 런타임 퀘스트 조회 시작
        string currentQuestId = _npcData.QuestId;
        QuestRuntime quest = QuestManager.Instance.GetQuest(currentQuestId);

        if (quest == null)
        {
            return _npcData.DefaultDialogueId;
        }

        // 3. 현재 퀘스트가 이미 완료(Rewarded)되었다면, NextQuest(main_004)로 자동 추적!
        while (quest != null && quest.State == QuestState.Rewarded)
        {
            QuestData nextQuestData = GetNextQuestData(quest);

            if (nextQuestData != null)
            {
                QuestRuntime nextQuest = QuestManager.Instance.GetQuest(nextQuestData.id);

                if (nextQuest != null)
                {
                    // NextQuest가 런타임에 진행 중이거나 수락 가능한 상태면 퀘스트 교체
                    quest = nextQuest;
                    currentQuestId = nextQuestData.id;
                }
                else
                {
                    // NextQuest가 아직 런타임 등록 전(수락 전)이면 "main_004_default" 형태로 반환하여 다음 퀘스트 시작 대화 실행
                    currentQuestId = nextQuestData.id;
                    return $"{currentQuestId}_{_npcData.DefaultDialogueId}";
                }
            }
            else
            {
                // 더 이상 다음 퀘스트가 없는 최종 완료 상태
                break;
            }
        }

        // 4. 결정된 퀘스트의 상태에 따른 Suffix 선택
        string suffix = _npcData.DefaultDialogueId;

        switch (quest.State)
        {
            case QuestState.Running:
                suffix = _npcData.RunningDialogueId;
                break;

            case QuestState.Completed:
                suffix = _npcData.CompleteDialogueId;
                break;

            case QuestState.Rewarded:
                suffix = _npcData.FinishedDialogueId;
                break;

            default:
                suffix = _npcData.DefaultDialogueId;
                break;
        }

        // 5. 이미 "main_003_running"처럼 완제 키로 저장되어 있다면 그대로 반환, "running" 같은 Suffix면 조합
        if (suffix.StartsWith(currentQuestId))
        {
            return suffix;
        }

        return $"{currentQuestId}_{suffix}";
    }

    /// <summary>
    /// QuestRuntime에서 NextQuest 참조를 안전하게 가져옵니다.
    /// </summary>
    private QuestData GetNextQuestData(QuestRuntime quest)
    {
        if (quest == null) return null;

        // QuestRuntime 내부에 연결된 QuestData의 NextQuest 탐색
        if (quest.Data != null)
        {
            return quest.Data.nextQuest;
        }

        return null;
    }
}