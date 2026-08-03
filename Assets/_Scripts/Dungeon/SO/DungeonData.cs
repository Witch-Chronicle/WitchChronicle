using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 던전 생성 및 환경 설정을 관리하는 ScriptableObject 데이터 클래스.
/// </summary>
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
    public GameObject DoorPrefab;
    public GameObject DoorWallPrefab; 

    public GameObject[] DecorPrefabs;

    public GameObject[] WallDecorPrefabs;

    [Header("Dungeon Setting")]
    public Vector2Int MapSize;

    public int MinPadding;

    public int BSPDepth;

    public int MaxConnectionPerRoom;

    public float ExtraConnectionProbability;

    [Header("Room Setting")]
    public int MinRoomCount = 12;
    public int MaxRoomCount = 18;

    public int MinRoomSize;
    public int MaxRoomSize;

    [System.Serializable]
    public struct RoomWeight
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

    public GameObject Fog;

    public DungeonAtmosphereDataSO DungeonAtmosphere;

    [Header("Boss")]
    public bool HasBoss;

    [Header("Exit")]
    public bool HasExit = true;
}