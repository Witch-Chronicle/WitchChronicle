using UnityEngine;

public class NPC : MonoBehaviour, ITFInteractable
{
    [SerializeField]
    private NPCData _npcData;

    [SerializeField]
    private NPCWorldInteractionUI _worldInteractionUI;

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

        // 상호작용한 방향을 바라보게 함
        if (interactor != null &&
            TryGetComponent(out LookAtOnInteract lookAt))
        {
            lookAt.FaceTarget(interactor.transform);
        }

        // 플레이어도 NPC 쪽으로 돌아보게 함
        if (interactor != null &&
            interactor.TryGetComponent(out LookAtOnInteract playerLookAt))
        {
            playerLookAt.FaceTarget(transform);
        }

        // 플레이어와 NPC를 함께 잡는 대화 카메라로 전환
        if (interactor != null &&
            NpcDialogueCamera.Instance != null)
        {
            NpcDialogueCamera.Instance.Focus(
                interactor.transform,
                this
            );

            // GameObject를 끄지 않고 표시만 억제
            SetWorldInteractionUISuppressed(true);
        }

        // 인사 애니메이션 + 인사 대사
        if (TryGetComponent(out NpcGreeting greeting))
        {
            greeting.OnInteracted();
        }

        string dialogueID = GetDialogueID();

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(
                _npcData,
                dialogueID
            );
        }
    }

    /// <summary>
    /// NPC 월드 상호작용 UI를 일시적으로 숨기거나 복구.
    /// GameObject 자체를 끄지 않기 때문에
    /// 현재 거리/Interact 상태가 유지된다.
    /// </summary>
    public void SetWorldInteractionUISuppressed(bool suppressed)
    {
        if (_worldInteractionUI == null)
        {
            return;
        }

        _worldInteractionUI.SetSuppressed(suppressed);
    }

    /// <summary>
    /// 이미 영입된 캐릭터인지 확인 후 필드에서 제거
    /// </summary>
    public void CheckAlreadyRecruited()
    {
        if (_npcData == null)
        {
            return;
        }

        if (PersistentCharacterManager.Instance != null)
        {
            if (PersistentCharacterManager.Instance.TryGetCharacter(
                    _npcData.NpcId,
                    out var character))
            {
                if (character != null &&
                    character.IsRecruited)
                {
                    Debug.Log(
                        $"[{_npcData.NpcName}] 이미 영입된 캐릭터이므로 필드에서 비활성화합니다."
                    );

                    Destroy(gameObject);
                }
            }
        }
    }

    /// <summary>
    /// 현재 NPC 및 퀘스트 상태,
    /// NextQuest 체인을 반영한 대화 ID 반환
    /// </summary>
    private string GetDialogueID()
    {
        // 1. NPCData나 QuestId가 없으면 기본 대화 ID 반환
        if (_npcData == null ||
            string.IsNullOrEmpty(_npcData.QuestId))
        {
            return _npcData != null
                ? _npcData.DefaultDialogueId
                : string.Empty;
        }

        if (QuestManager.Instance == null)
        {
            return _npcData.DefaultDialogueId;
        }

        // 2. 기본 QuestId로 런타임 퀘스트 조회 시작
        string currentQuestId = _npcData.QuestId;

        QuestRuntime quest =
            QuestManager.Instance.GetQuest(currentQuestId);

        if (quest == null)
        {
            return _npcData.DefaultDialogueId;
        }

        // 3. 현재 퀘스트가 Rewarded라면 NextQuest 자동 추적
        while (quest != null &&
               quest.State == QuestState.Rewarded)
        {
            QuestData nextQuestData =
                GetNextQuestData(quest);

            if (nextQuestData != null)
            {
                QuestRuntime nextQuest =
                    QuestManager.Instance.GetQuest(
                        nextQuestData.id
                    );

                if (nextQuest != null)
                {
                    quest = nextQuest;
                    currentQuestId = nextQuestData.id;
                }
                else
                {
                    // NextQuest가 아직 런타임 등록 전이면
                    // 다음 퀘스트의 기본 시작 대화 반환
                    currentQuestId = nextQuestData.id;

                    return
                        $"{currentQuestId}_{_npcData.DefaultDialogueId}";
                }
            }
            else
            {
                break;
            }
        }

        // 4. 상태에 따른 suffix
        string suffix =
            _npcData.DefaultDialogueId;

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

        // 5. 이미 완성된 ID라면 그대로 반환
        if (suffix.StartsWith(currentQuestId))
        {
            return suffix;
        }

        return $"{currentQuestId}_{suffix}";
    }

    /// <summary>
    /// QuestRuntime에서 NextQuest 참조를 안전하게 가져온다.
    /// </summary>
    private QuestData GetNextQuestData(QuestRuntime quest)
    {
        if (quest == null)
        {
            return null;
        }

        if (quest.Data != null)
        {
            return quest.Data.nextQuest;
        }

        return null;
    }

    /// <summary>
    /// PlayerInteractor가 이 NPC를 Current로 잡거나 놓을 때 호출.
    /// 실제 표시는 NPCWorldInteractionUI의 InteractRoot가 담당한다.
    /// </summary>
    public void ShowInteractPrompt(bool show)
    {
        if (_worldInteractionUI != null)
        {
            _worldInteractionUI.SetInteractRootVisible(show);
        }
    }
}