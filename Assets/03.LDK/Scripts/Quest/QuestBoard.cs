using UnityEngine;

/// <summary>
/// Quest Board 오브젝트 관리
/// </summary>
public class QuestBoard : MonoBehaviour, ITFInteractable
{
    public string Prompt => "[F] 외뢰 게시판 열기";

    /// <summary>
    /// 퀘스트 게시판 인터렉트
    /// </summary>
    public void Interact(GameObject interactor)
    {
        QuestBoardUI.Instance.Open(this);
    }
}