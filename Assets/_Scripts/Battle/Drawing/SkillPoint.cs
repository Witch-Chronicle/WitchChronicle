using UnityEngine;

public struct SkillPoint
{
    public Vector2 pos;
    public int strokeId;

    public SkillPoint(Vector2 pos, int strokeId)
    {
        this.pos = pos;
        this.strokeId = strokeId;
    }
}