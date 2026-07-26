using System;

[Serializable]
public class DialogueChoice
{
    public string text;

    public string next;

    public string startQuest;

    public string completeQuest;

    /// <summary>
    /// 선택지 실행 이벤트
    /// 상점 열기, 퀘스트 수락 등 특수 행동 처리
    /// </summary>
    public string eventID;
}