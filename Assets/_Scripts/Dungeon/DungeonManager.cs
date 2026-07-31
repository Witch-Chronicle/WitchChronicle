using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 던전 생성 흐름 및 전반적인 시스템을 총괄하는 매니저 클래스.
/// </summary>
[RequireComponent(typeof(DungeonGenerator))]
[RequireComponent(typeof(DungeonSpawner))]
[RequireComponent(typeof(RoomContentSpawner))]
[RequireComponent(typeof(MinimapRenderer))]
[RequireComponent(typeof(DungeonAtmosphereController))]
public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance { get; private set; }

    private DungeonGenerator _generator;
    private DungeonSpawner _spawner;
    private RoomContentSpawner _contentSpawner;
    private DungeonAtmosphereController _dungeonAtmosphereController;
    private MinimapRenderer _minimapRenderer;
    private MinimapData _minimapData;

    public DungeonData CurrentDungeonData { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
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
            Debug.LogError("[DungeonManager] DungeonData가 선택되지 않았습니다.");
            return;
        }

        _generator = GetComponent<DungeonGenerator>();
        _spawner = GetComponent<DungeonSpawner>();
        _contentSpawner = GetComponent<RoomContentSpawner>();
        _dungeonAtmosphereController = GetComponent<DungeonAtmosphereController>();
        _minimapRenderer = GetComponent<MinimapRenderer>();

        _spawner.Initialize(CurrentDungeonData);
        _contentSpawner.Initialize(CurrentDungeonData);

        Generate();
    }

    private void Update()
    {
        if (CurrentDungeonData == null)
        {
            return;
        }

        DungeonAtmosphereDataSO atmosphere = CurrentDungeonData.DungeonAtmosphere;

        if (atmosphere == null || !atmosphere.UseFog || !atmosphere.AnimateFog)
        {
            return;
        }

        RenderSettings.fogDensity =
            atmosphere.FogDensity +
            Mathf.Sin(
                Time.time *
                atmosphere.FogDensitySpeed) *
            atmosphere.FogDensityAmplitude;
    }

    /// <summary>
    /// 던전을 생성하고 관련 컴포넌트들을 초기화한다.
    /// </summary>
    public void Generate()
    {
        Debug.Log("[DungeonManager] 던전 생성 시작");

        var rooms = _generator.GenerateDungeon();

        Debug.Log($"[DungeonManager] 던전 생성 완료 : {rooms.Count}개의 방");

        _spawner.BuildDungeon(rooms);

        Debug.Log("[DungeonManager] 던전 타일 스폰 완료");

        _contentSpawner.SetCorridorTiles(_spawner.CorridorTiles);
        _contentSpawner.SpawnContent(rooms, _spawner.GetTileSize);

        _minimapData = new MinimapData(_spawner.FloorTiles, _spawner.WallTiles, rooms);

        _minimapRenderer.Render(_minimapData);

        Debug.Log("[DungeonManager] 미니맵 텍스처 생성 완료");

        InitializeMinimapTracking(rooms);

        _dungeonAtmosphereController.ApplyAtmosphere(CurrentDungeonData);

        Debug.Log("[DungeonManager] 던전 분위기 효과 적용 완료");
    }

    /// <summary>
    /// 미니맵 UI 플레이어 추적 및 컨트롤러를 초기화한다.
    /// </summary>
    private void InitializeMinimapTracking(IReadOnlyList<RoomNode> rooms)
    {
        if (_minimapData == null)
        {
            return;
        }

        int minX = _minimapData.MinX;
        int minY = _minimapData.MinY;
        int maxX = _minimapData.MaxX;
        int maxY = _minimapData.MaxY;
        float tileSize = _spawner.GetTileSize;

        GameObject playerObj = GameObject.FindWithTag("Player");
        Transform playerTransform = playerObj != null ? playerObj.transform : null;

        if (playerTransform == null)
        {
            Debug.LogWarning("[DungeonManager] 씬 내에 'Player' 태그를 가진 오브젝트를 찾지 못했습니다.");
        }

        MinimapUIController uiController = FindObjectOfType<MinimapUIController>();
        if (uiController != null && playerTransform != null)
        {
            uiController.Initialize(playerTransform, minX, minY, maxX, maxY, tileSize);
            Debug.Log("[DungeonManager] 미니맵 UI 플레이어 추적 컨트롤러 초기화 완료");
        }

        // 미니맵 플레이어 아이콘 회전 및 위치 추적기 연동 초기화
        if (MinimapIconManager.Instance != null && playerTransform != null)
        {
            MinimapIconManager.Instance.InitializePlayerIcon(playerTransform, tileSize);
            Debug.Log("[DungeonManager] 미니맵 플레이어 아이콘 및 회전 추적 초기화 완료");
        }
    }

    /// <summary>
    /// 플레이어가 방을 발견하거나 상태가 변경되었을 때 미니맵 텍스처를 갱신한다.
    /// </summary>
    public void RefreshMinimap()
    {
        if (_minimapRenderer != null)
        {
            _minimapRenderer.Refresh();
            Debug.Log("[DungeonManager] 미니맵 텍스처 갱신 완료");
        }
    }

    /// <summary>
    /// 현재 미니맵 데이터를 반환한다.
    /// </summary>
    public MinimapData GetMinimapData()
    {
        return _minimapData;
    }
}