using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Quest_", menuName = "Game/Quest")]
public class QuestData : ScriptableObject
{
    [Header("Basic")]

    public string id;

    public string title;

    [TextArea(3, 6)]
    public string description;

    public QuestType type;

    [Header("Objective")]

    public List<QuestObjective> objectives = new();

    [Header("Reward")]

    public QuestReward reward;

    [Header("Next Quest")]

    public QuestData nextQuest;
}