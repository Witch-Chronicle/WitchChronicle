using System.Collections.Generic;
using UnityEngine;

public enum PlacementType
{
    Floor,
    Wall,
    Corner
}

[CreateAssetMenu(fileName = "DecorationTable", menuName = "Game/Decoration Table")]
public class DecorationTable : ScriptableObject
{
    [System.Serializable]
    public struct DecorationEntry
    {
        public RoomType targetRoomType;
        public PlacementType placement; // 바닥 or 벽 결정
        public List<GameObject> prefabs;
        public int minCount;
        public int maxCount;
    }

    [SerializeField] private List<DecorationEntry> _entries;

    public List<DecorationEntry> Entries
    {
        get { return _entries; }
    }

    /// <summary>
    /// 방 타입에 맞는 모든 데코레이션 엔트리를 반환합니다.
    /// </summary>
    public List<DecorationEntry> GetEntries(RoomType type)
    {
        List<DecorationEntry> results = new List<DecorationEntry>();
        
        foreach (var entry in _entries)
        {
            if (entry.targetRoomType == type || entry.placement == PlacementType.Corner)
            {
                results.Add(entry);
            }
        }
        return results;
    }
}