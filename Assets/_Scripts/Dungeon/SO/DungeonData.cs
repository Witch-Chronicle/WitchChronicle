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

    public Sprite DungeonIcon;

    [Header("Description")]
    [TextArea(3, 6)]
    public string Description;

    [Header("Entry Condition")]
    [Tooltip("이 던전에 입장하려면 받아야(Running 이상) 하는 퀘스트. 비워두면 조건 없이 항상 입장 가능.")]
    public QuestData RequiredQuest;

    [Header("Enemy Pool")]
    [Tooltip("이 던전에 등장 가능한 몬스터 풀")]
    public List<EnemyBattleData> EnemyPool;

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

    /// <summary>
    /// 지금 플레이어가 이 던전에 입장 가능한지 여부.
    /// RequiredQuest가 없으면 항상 true. 있으면 그 퀘스트를 받은 상태(Running 이상, GetQuest가 null이 아님)인지로 판단.
    /// * QuestManager는 실제 진행 상태(런타임)를 들고 있는 쪽이라 여기서 조회만 함 - DungeonData 자체는 변하지 않음.
    /// </summary>
    public bool CanEnter(QuestManager questManager)
    {
        if (RequiredQuest == null)
        {
            return true;
        }

        if (questManager == null)
        {
            return false;
        }

        return questManager.GetQuest(RequiredQuest.id) != null;
    }

    /// <summary>
    /// 입장 불가 시 UI에 표시할 안내 문구. (예: "OO 퀘스트 진행 시 개방")
    /// RequiredQuest가 없으면 빈 문자열.
    /// </summary>
    public string GetLockedReasonText()
    {
        if (RequiredQuest == null)
        {
            return string.Empty;
        }

        return $"[{RequiredQuest.title}] 퀘스트 진행 후 입장 가능";
    }
}