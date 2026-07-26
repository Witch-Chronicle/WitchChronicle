using System;

[Serializable]
public class QuestReward
{
    public int gold;

    public int exp;

    public ItemData itemID;

    public int itemCount;

    // NPC 영입 보상
    public string recruitNPC;

     // 메인 스토리 진행
    public bool nextStory;
}