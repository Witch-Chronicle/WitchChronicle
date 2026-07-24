using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC 영입 관리
/// </summary>
public class RecruitManager : MonoBehaviour
{
    public static RecruitManager Instance { get; private set; }


    private readonly HashSet<string> _recruitedNPC = new();


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
    }


    /// <summary>
    /// NPC 영입
    /// </summary>
    public void Recruit(string npcID)
    {
        if (_recruitedNPC.Contains(npcID))
        {
            return;
        }


        _recruitedNPC.Add(npcID);


        Debug.Log($"NPC Recruit : {npcID}");
    }


    /// <summary>
    /// 영입 여부 조회
    /// </summary>
    public bool IsRecruited(string npcID)
    {
        return _recruitedNPC.Contains(npcID);
    }
}