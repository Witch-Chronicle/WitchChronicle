using TMPro;
using UnityEngine;

/// <summary>
/// 진행 중인 개별 퀘스트 UI 관리.
/// 퀘스트 제목, 상태, 목표 진행도를 표시한다.
/// </summary>
public class QuestItem : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;

    [SerializeField] private TMP_Text _progressText;

    [SerializeField] private TMP_Text _stateText;

    private QuestRuntime _runtime;


    /// <summary>
    /// 퀘스트 Runtime 연결.
    /// UI 표시 데이터를 초기화한다.
    /// </summary>
    public void Setup(QuestRuntime runtime)
    {
        _runtime = runtime;

        Refresh();
    }


    /// <summary>
    /// 현재 퀘스트 상태와 목표 진행도를 다시 표시한다.
    /// </summary>
    public void Refresh()
    {
        if (_runtime == null)
        {
            return;
        }


        _titleText.text = _runtime.Data.title;


        _stateText.text = GetStateText(_runtime.State);

        string text = "";

        for (int i = 0; i < _runtime.Data.objectives.Count; i++)
        {
            QuestObjective objective = _runtime.Data.objectives[i];


            text += $"{GetObjectiveText(objective)} " + $"{_runtime.Progress[i]}/{objective.requiredCount}\n";
        }


        _progressText.text = text;
    }


    /// <summary>
    /// 퀘스트 상태에 따른 UI 문구 반환.
    /// </summary>
    private string GetStateText(QuestState state)
    {
        switch (state)
        {
            case QuestState.Running:
                return "진행중";


            case QuestState.Completed:
                return "보상 받기";


            case QuestState.Rewarded:
                return "완료";


            default:
                return "";
        }
    }


    /// <summary>
    /// 퀘스트 목표 타입을 사용자 표시 문구로 변환.
    /// </summary>
    private string GetObjectiveText(QuestObjective objective)
    {
        switch (objective.type)
        {
            case QuestObjectiveType.KillMonster:
                return $"{objective.targetName} 처치";


            case QuestObjectiveType.TalkNPC:
                return $"{objective.targetName}와 대화";


            case QuestObjectiveType.CollectItem:
                return $"{objective.targetName} 획득";


            case QuestObjectiveType.ClearDungeon:
                return $"{objective.targetName} 클리어";


            case QuestObjectiveType.RecruitNPC:
                return $"{objective.targetName} 합류";
        }


        return objective.targetName;
    }
}