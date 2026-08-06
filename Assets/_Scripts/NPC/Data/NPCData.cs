using UnityEngine;

/// <summary>
/// NPC 데이터 프로필 및 대화 ID Suffix를 관리하는 ScriptableObject입니다.
/// </summary>
[CreateAssetMenu(fileName = "NewNPCData", menuName = "ScriptableObjects/NPCData")]
public class NPCData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string _npcId;
    [SerializeField] private string _npcName;
    [TextArea(3, 5)]
    [SerializeField] private string _description;

    [SerializeField] private string _prompt;

    [Header("Visual Assets")]
    [SerializeField] private Sprite _portrait;
    [SerializeField] private GameObject _prefab;

    [Header("Dialogue Assets & IDs")]
    [SerializeField] private TextAsset _dialogueJson;
    [SerializeField] private string _defaultDialogueId = "default";
    [SerializeField] private string _startDialogueId = "default";
    [SerializeField] private string _runningDialogueId = "running";
    [SerializeField] private string _completeDialogueId = "complete";
    [SerializeField] private string _finishedDialogueId = "reward";

    [Header("Quest Info")]
    [SerializeField] private string _questId;
    [SerializeField] private string _recruitQuestId;

    [Header("NPC State")]
    [SerializeField] private NPC_State _defaultNpcState = NPC_State.Normal;

    // Basic Info Properties
    public string NpcId => _npcId;
    public string NpcName => _npcName;
    public string Description => _description;
    public string Prompt => _prompt;

    // Visual Assets Properties
    public Sprite Portrait => _portrait;
    public GameObject Prefab => _prefab;

    // Dialogue Data Properties
    public TextAsset DialogueJson => _dialogueJson;
    public string DefaultDialogueId => _defaultDialogueId;
    public string StartDialogueId => _startDialogueId;
    public string RunningDialogueId => _runningDialogueId;
    public string CompleteDialogueId => _completeDialogueId;
    public string FinishedDialogueId => _finishedDialogueId;

    // Quest Info Properties
    public string QuestId => _questId;
    public string RecruitQuestId => _recruitQuestId;

    // State Property
    public NPC_State DefaultNpcState => _defaultNpcState;
}

public enum NPC_State
{
    Normal,
    Recruitable,
    Recruited
}