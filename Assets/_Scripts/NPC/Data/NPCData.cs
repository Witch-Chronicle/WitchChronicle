using UnityEngine;

[CreateAssetMenu(fileName = "NPC_", menuName = "Game/NPC")]
public class NPCData : ScriptableObject
{
    [Header("Basic")]

    public string id;

    public string npcName;

    [TextArea(3, 5)]
    public string description;

    [Header("Visual")]

    public Sprite portrait;

    public GameObject prefab;

    [Header("Dialogue")] // 다양한 대화를 위한, running 일때 다른 반응, complete 일때 다른 반응

    public TextAsset dialogueJson;

    public string defaultDialogueID;

    public string runningDialogueID;

    public string completeDialogueID;

    public string finishedDialogueID;

    public string startDialogueID;

    [Header("Quest")]
    public QuestData quest;

    public QuestData recruitQuest;

    [Header("Default")]

    public NPC_State defaultState;
}

public enum NPC_State
{
    Normal,
    Recruitable,
    Recruited
}