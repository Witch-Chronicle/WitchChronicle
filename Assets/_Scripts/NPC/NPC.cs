using UnityEngine;

public class NPC : MonoBehaviour, ITFInteractable
{
    [SerializeField]
    private NPCData _npcData;

    private void Start()
    {
        NPCManager.Instance.RegisterNPC(this);
    }

    public NPCData Data => _npcData;

    public string Prompt => $"[F] {Data.npcName} (과)와 대화하기";

    public void Interact(GameObject interactor)
    {
        string dialogueID = GetDialogueID();

        DialogueManager.Instance.StartDialogue(_npcData, dialogueID);
    }

    /// <summary>
    /// 현재 NPC 상태에 맞는 대화 ID 반환
    /// </summary>
    private string GetDialogueID()
    {
        if (_npcData.quest == null)
        {
            return _npcData.defaultDialogueID;
        }


        QuestRuntime quest =
            QuestManager.Instance.GetQuest(_npcData.quest.id);


        if (quest == null)
        {
            return _npcData.defaultDialogueID;
        }


        switch (quest.State)
        {
            case QuestState.Running:
                return _npcData.runningDialogueID;


            case QuestState.Completed:
                return _npcData.completeDialogueID;


            case QuestState.Rewarded:
                return _npcData.finishedDialogueID;
        }


        return _npcData.defaultDialogueID;
    }
}