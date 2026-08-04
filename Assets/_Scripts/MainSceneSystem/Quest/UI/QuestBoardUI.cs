using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quest Board UI 관리
/// </summary>
public class QuestBoardUI : MonoBehaviour
{
    public static QuestBoardUI Instance { get; private set; }


    [Header("Panel")]

    [SerializeField]
    private GameObject _panel;


    [Header("Quest List")]

    [SerializeField]
    private Transform _questRoot;


    [SerializeField]
    private QuestCard _questCardPrefab;


    [Header("Detail")]

    [SerializeField]
    private QuestDetailPanel _detailPanel;


    private QuestBoard _currentBoard;

    [SerializeField] private Button _closeButton;


    /// <summary>
    /// 초기화
    /// </summary>
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);

            return;
        }

        Instance = this;


        _panel.SetActive(false);

        _closeButton.onClick.AddListener(Close);
    }


    /// <summary>
    /// 게시판 열기
    /// </summary>
    public void Open(QuestBoard board)
    {
        _currentBoard = board;


        Clear();


        List<QuestData> quests = QuestBoardManager.Instance.GetBoardQuests();


        foreach (QuestData quest in quests)
        {
            CreateCard(quest);
        }


        _panel.SetActive(true);

        CursorLocker.Instance.EnterUIMode();
    }


    /// <summary>
    /// 퀘스트 카드 생성
    /// </summary>
    private void CreateCard(QuestData quest)
    {
        QuestCard card = Instantiate( _questCardPrefab,  _questRoot);


        card.Setup(quest);
    }


    /// <summary>
    /// 퀘스트 선택
    /// </summary>
    public void SelectQuest(QuestData quest)
    {
        _detailPanel.Show(quest);
    }


    /// <summary>
    /// 퀘스트 수락
    /// </summary>
    public void AcceptQuest(QuestData quest)
    {
        QuestManager.Instance.StartQuest(quest.id);

        Debug.Log($"Quest Accept : {quest.title}");

        Close();
    }


    /// <summary>
    /// 목록 제거
    /// </summary>
    private void Clear()
    {
        foreach (Transform child in _questRoot)
        {
            Destroy(child.gameObject);
        }
    }


    /// <summary>
    /// 게시판 닫기
    /// </summary>
    public void Close()
    {
        _panel.SetActive(false);

        _currentBoard = null;

        CursorLocker.Instance.ExitUIMode();
    }
}