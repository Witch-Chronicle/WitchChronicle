using UnityEngine;

[RequireComponent(typeof(DungeonGenerator))]
[RequireComponent(typeof(DungeonSpawner))]
[RequireComponent(typeof(RoomContentSpawner))]
public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance {get; private set;}

    private DungeonGenerator _generator;
    private DungeonSpawner _spawner;
    private RoomContentSpawner _contentSpawner;

    private DungeonAtmosphereController _dungeonAtmosphereController;

    public DungeonData CurrentDungeonData { get; private set; }

    void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        CurrentDungeonData = DungeonSelection.CurrentDungeonData;

        if (CurrentDungeonData == null)
        {
            Debug.LogError("DungeonData가 선택되지 않았습니다.");
            return;
        }

        _generator = GetComponent<DungeonGenerator>();
        _spawner = GetComponent<DungeonSpawner>();
        _contentSpawner = GetComponent<RoomContentSpawner>();
        _dungeonAtmosphereController = GetComponent<DungeonAtmosphereController>();

        _spawner.Initialize(CurrentDungeonData);
        _contentSpawner.Initialize(CurrentDungeonData);

        Generate();
    }

    [ContextMenu("던전 생성")]
    public void Generate()
    {
        Debug.Log("[DungeonManager] 던전 생성 시작");

        var rooms = _generator.GenerateDungeon();

        Debug.Log($"[DungeonManager] 던전 생성 완료 : {rooms.Count}");

        _spawner.BuildDungeon(rooms);

        Debug.Log("[DungeonManager] 던전 스폰 완료");

        _contentSpawner.SetCorridorTiles(_spawner.CorridorTiles);

        _contentSpawner.SpawnContent(rooms, _spawner.GetTileSize);

        Debug.Log("[DungeonManager] 방 콘덴츠 스폰 완료");

        //_dungeonAtmosphereController.ApplyAtmosphere(CurrentDungeonData);

        //Debug.Log("[DungeonManager] 던전 효과 적용 완료");
    }
}