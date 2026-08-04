using System;

[Serializable]
public class QuestObjective
{
    public QuestObjectiveType type;

    public string targetID;

    public string targetName;

    public int requiredCount;
}