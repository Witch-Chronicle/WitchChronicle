using System;
using System.Collections.Generic;

[Serializable]
public class DialogueNode
{
    public string id;

    public string speaker;

    public string text;

    public string next;

    public string startQuest;

    public string completeQuest;

    public List<DialogueChoice> choices;
}