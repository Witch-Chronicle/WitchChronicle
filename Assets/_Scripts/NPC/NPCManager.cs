using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance { get; private set; }

    [SerializeField]
    private NPCDatabase _database;

    private Dictionary<string, NPC> _spawnedNPCs = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _database.Initialize();
    }

    public NPCData GetNPCData(string id)
    {
        return _database.GetNPC(id);
    }

    public void RegisterNPC(NPC npc)
    {
        string id = npc.Data.NpcId;

        if (_spawnedNPCs.ContainsKey(id))
        {
            return;
        }

        _spawnedNPCs.Add(id, npc);
    }

    public NPC GetNPC(string id)
    {
        _spawnedNPCs.TryGetValue(id, out NPC npc);

        return npc;
    }
}