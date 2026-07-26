using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Quest 카드 표시 관리
/// </summary>
public class QuestCard : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private TMP_Text _titleText;


    [SerializeField]
    private TMP_Text _typeText;


    private QuestData _quest;


    /// <summary>
    /// 퀘스트 데이터 설정
    /// </summary>
    public void Setup(QuestData quest)
    {
        _quest = quest;

        _titleText.text = quest.title;

        _typeText.text = GetTypeText(quest.type);

    }


    /// <summary>
    /// 카드 클릭
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_quest == null)
        {
            return;
        }


        QuestBoardUI.Instance.SelectQuest(_quest);
    }


    /// <summary>
    /// 타입 표시 조회
    /// </summary>
    private string GetTypeText(QuestType type)
    {
        switch (type)
        {
            case QuestType.Repeatable:
                return "반복 의뢰";


            case QuestType.Dungeon:
                return "던전 의뢰";


            default:
                return "";
        }
    }
}