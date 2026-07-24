using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quest 상세 정보 관리
/// </summary>
public class QuestDetailPanel : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _titleText;


    [SerializeField]
    private TMP_Text _descriptionText;


    [SerializeField]
    private TMP_Text _objectiveText;


    [SerializeField]
    private TMP_Text _rewardText;


    [SerializeField]
    private Button _acceptButton;


    private QuestData _quest;


    /// <summary>
    /// 초기화
    /// </summary>
    private void Awake()
    {
        _acceptButton.onClick.AddListener(Accept);


        gameObject.SetActive(false);
    }


    /// <summary>
    /// 상세 표시
    /// </summary>
    public void Show(QuestData quest)
    {
        _quest = quest;


        gameObject.SetActive(true);


        _titleText.text =
            quest.title;


        _descriptionText.text =
            quest.description;


        SetObjective(quest);


        _rewardText.text =
            GetRewardText(quest.reward);
    }


    /// <summary>
    /// 목표 표시
    /// </summary>
    private void SetObjective(QuestData quest)
    {
        string text = "목표\n";


        foreach (QuestObjective objective in quest.objectives)
        {
            text +=
                $"{objective.type} : {objective.requiredCount}\n";
        }


        _objectiveText.text = text;
    }


    /// <summary>
    /// 퀘스트 수락
    /// </summary>
    private void Accept()
    {
        if (_quest == null)
        {
            return;
        }


        QuestBoardUI.Instance.AcceptQuest(_quest);


        gameObject.SetActive(false);
    }


    /// <summary>
    /// 보상 표시 조회
    /// </summary>
    private string GetRewardText(QuestReward reward)
    {
        string text = "보상\n";


        if (reward.gold > 0)
        {
            text += $"Gold : {reward.gold}\n";
        }


        if (reward.exp > 0)
        {
            text += $"Exp : {reward.exp}\n";
        }


        if (reward.itemID != null)
        {
            text += $"{reward.itemID.name} x {reward.itemCount}";
        }


        return text;
    }
}