using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Dungeon_", menuName = "Game/Dungeon")]
public class DungeonData : ScriptableObject
{
    [Header("Basic")]
    public string Id;
    public string DungeonName;

    [Header("Environment")]
    public GameObject FloorPrefab;
    public GameObject WallPrefab;
    public GameObject CeilingPrefab;

    public GameObject[] DecorPrefabs;
    

    [Header("Dungeon Setting")]
    public Vector2Int MapSize;

    public int MinRoomSize;
    public int MaxRoomSize;

    public int MinPadding;

    public int BSPDepth;

    public int MaxConnectionPerRoom;

    public float ExtraConnectionProbability;

    [System.Serializable]
    public struct RoomWeight // Room 의 생성 확률
    {
        public RoomType Type;
        public int Weight;
    }
 
    public List<RoomWeight> RoomWeights;

    [Header("Content")]
    public RoomContentTable RoomContentTable;

    [Header("Decoration")]
    [SerializeField] private DecorationTable _decorationTable;
    public DecorationTable DecorationTable
    {
        get { return _decorationTable; }
    }

    [Header("Presentation")]
    public AudioClip BGM;
    public Material Skybox;
    public Sprite Icon;

    public DungeonAtmosphereDataSO DungeonAtmosphere;

    [Header("Boss")]
    public bool HasBoss;

    [Header("Exit")]
    public bool HasExit = true;
}