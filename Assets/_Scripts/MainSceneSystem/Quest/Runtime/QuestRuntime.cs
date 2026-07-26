using System.Collections.Generic;

/// <summary>
/// 퀘스트 런타임 데이터
/// </summary>
public class QuestRuntime
{
    public QuestData Data { get; }

    public QuestState State { get; set; }

    public Dictionary<int, int> Progress { get; }

    /// <summary>
    /// 생성
    /// </summary>
    public QuestRuntime(QuestData data)
    {
        Data = data;

        State = QuestState.Running;

        Progress = new Dictionary<int, int>();

        for (int i = 0; i < data.objectives.Count; i++)
        {
            Progress.Add(i, 0);
        }
    }
}