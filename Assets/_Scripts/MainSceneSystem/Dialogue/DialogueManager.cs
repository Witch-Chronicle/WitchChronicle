using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 대화 흐름 관리
/// JSON 로드, 노드 이동, UI 출력, 선택지 이벤트 처리
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    private readonly DialogueLoader _loader = new();

    private readonly Dictionary<string, DialogueNode> _nodeDictionary = new();

    private DialogueData _currentDialogue;
    private DialogueNode _currentNode;
    private NPCData _currentNPC;


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    /// <summary>
    /// NPC 대화 시작
    /// </summary>
    public void StartDialogue(NPCData npc, string startID)
    {
        if (npc == null)
        {
            return;
        }

        _currentNPC = npc;

        _currentDialogue = _loader.Load(_currentNPC.DialogueJson);

        if (_currentDialogue == null)
        {
            Debug.LogError("Dialogue Data Missing");
            return;
        }

        _nodeDictionary.Clear();

        foreach (DialogueNode node in _currentDialogue.dialogues)
        {
            if (_nodeDictionary.ContainsKey(node.id))
            {
                Debug.LogWarning($"Duplicate Dialogue ID : {node.id}");
                continue;
            }

            _nodeDictionary.Add(node.id, node);
        }

        MoveNode(startID);
    }

    /// <summary>
    /// 대화 노드 이동
    /// 해당 노드의 내용 출력 및 퀘스트 이벤트 처리
    /// </summary>
    private void MoveNode(string id)
    {
        if (_nodeDictionary.TryGetValue(id, out DialogueNode node) == false)
        {
            Debug.LogError($"Dialogue Node Missing : {id}");

            return;
        }


        _currentNode = node;


        if (string.IsNullOrEmpty(_currentNode.startQuest) == false)
        {
            QuestManager.Instance.StartQuest(_currentNode.startQuest);
        }


        if (string.IsNullOrEmpty(_currentNode.completeQuest) == false)
        {
            QuestManager.Instance.RewardQuest(_currentNode.completeQuest);
        }


        DialogueUI.Instance.Show();


        bool isLastNode = string.IsNullOrEmpty(_currentNode.next) &&
        (_currentNode.choices == null || _currentNode.choices.Count == 0);

        DialogueUI.Instance.Refresh(_currentNPC.Portrait, _currentNode.speaker, _currentNode.text, isLastNode);

        DialogueUI.Instance.ClearChoices();

        if (_currentNode.choices != null)
        {
            foreach (DialogueChoice choice in _currentNode.choices)
            {
                DialogueUI.Instance.CreateChoice(choice);
            }
        }
    }


    /// <summary>
    /// 다음 대화 이동
    /// </summary>
    public void NextDialogue()
    {
        if (_currentNode == null)
        {
            return;
        }

        if (_currentNode.choices != null && _currentNode.choices.Count > 0)
        {
            return;
        }

        if (string.IsNullOrEmpty(_currentNode.next))
        {
            EndDialogue();
            return;
        }

        MoveNode(_currentNode.next);
    }


    /// <summary>
    /// 선택지 선택 처리
    /// 선택지 퀘스트, 이벤트 실행 후 다음 노드 이동
    /// </summary>
    public void SelectChoice(DialogueChoice choice)
    {
        if (choice == null)
        {
            return;
        }

        ExecuteChoiceQuest(choice);
        ExecuteChoiceEvent(choice);

        // 이벤트 실행 중(Shop/Enhance 등 외부 UI가 열리며) 대화창이 이미 닫혔다면,
        // 대화 흐름을 더 진행하지 않고 여기서 종료.
        // 단, Show() 시점에 걸었던 EnterUIMode()는 여기서 반드시 짝을 맞춰 풀어줘야 함
        // (안 그러면 CursorLocker의 카운트가 영구히 어긋나서 다른 UI를 다 닫아도 필드 모드로 복귀 못 함).
        if (DialogueUI.Instance != null && DialogueUI.Instance.IsPanelActive == false)
        {
            _currentDialogue = null;
            _currentNode = null;
            _currentNPC = null;
            _nodeDictionary.Clear();

            CursorLocker.Instance?.ExitUIMode();
            return;
        }

        if (string.IsNullOrEmpty(choice.next))
        {
            EndDialogue();
            return;
        }

        MoveNode(choice.next);
    }


    /// <summary>
    /// 선택지 퀘스트 처리
    /// </summary>
    private void ExecuteChoiceQuest(DialogueChoice choice)
    {
        if (string.IsNullOrEmpty(choice.startQuest) == false)
        {
            QuestManager.Instance.StartQuest(choice.startQuest);
        }


        if (string.IsNullOrEmpty(choice.completeQuest) == false)
        {
            QuestManager.Instance.RewardQuest(choice.completeQuest);
        }
    }



    /// <summary>
    /// 선택지 이벤트 처리
    /// </summary>
    private void ExecuteChoiceEvent(DialogueChoice choice)
    {
        if (string.IsNullOrEmpty(choice.eventID))
        {
            return;
        }


        DialogueEventManager.Instance.Execute(choice.eventID);
    }


    /// <summary>
    /// 마지막 노드 확인
    /// </summary>
    private bool IsLastNode()
    {
        return string.IsNullOrEmpty(_currentNode.next);
    }


    /// <summary>
    /// 대화 종료
    /// </summary>
    public void EndDialogue()
    {
        _currentDialogue = null;
        _currentNode = null;
        _currentNPC = null;

        _nodeDictionary.Clear();

        DialogueUI.Instance.Hide();
    }
}