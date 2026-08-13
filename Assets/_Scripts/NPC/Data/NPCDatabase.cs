using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NPCDatabase", menuName = "Game/NPC Database")]
public class NPCDatabase : ScriptableObject
{
    [SerializeField]
    private List<NPCData> _npcList = new();

    private Dictionary<string, NPCData> _npcDictionary;

    public void Initialize()
    {
        _npcDictionary = new Dictionary<string, NPCData>();

        foreach (NPCData npc in _npcList)
        {
            if (_npcDictionary.ContainsKey(npc.NpcId))
            {
                Debug.LogWarning($"Duplicate NPC ID : {npc.NpcId}");
                continue;
            }
            
            {
                Debug.LogWarning($"Duplicate NPC ID : {npc.NpcId}");
                continue;
            }

            _npcDictionary.Add(npc.NpcId, npc);
        }
    }

    public NPCData GetNPC(string id)
    {
        if (_npcDictionary == null)
        {
            Initialize();
        }

        _npcDictionary.TryGetValue(id, out NPCData npc);

        return npc;
    }

    public List<NPCData> GetAllNPC()
    {
        return _npcList;
    }
}