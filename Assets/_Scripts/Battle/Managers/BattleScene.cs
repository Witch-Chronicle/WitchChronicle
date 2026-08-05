using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 씬 위치 초기화 및 개별 인스턴스 기반 던전 구조물 생성과 UV 변형 관리
/// </summary>
public class BattleScene : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int _gridWidth = 30;
    [SerializeField] private int _gridDepth = 30;
    [SerializeField] private int _wallHeightLayers = 20;
    [SerializeField] private float _roofHeight = 20f;
    [SerializeField] private int _variationCount = 4;

    [Header("Floor Decoration")]
    [Tooltip("바닥에 산개 배치되는 개별 데코의 스폰 확률 (타일당)")]
    [SerializeField] [Range(0f, 1f)] private float _decorSpawnChance = 0.12f;

    [Header("Cluster Decoration")]
    [Tooltip("잔해/바위 더미처럼 여러 개가 뭉쳐서 배치되는 클러스터 개수")]
    [SerializeField] private int _clusterCount = 6;
    [SerializeField] private int _clusterMinSize = 3;
    [SerializeField] private int _clusterMaxSize = 6;
    [Tooltip("클러스터 하나가 퍼지는 반경(타일 단위)")]
    [SerializeField] private float _clusterRadius = 2.5f;

    [Header("Landmark / Edge Decoration")]
    [Tooltip("아레나 테두리를 따라 일정 간격으로 배치되는 대형 소품 프리팹 (파괴된 기둥, 바위 등)")]
    [SerializeField] private GameObject[] _landmarkPrefabs;
    [Tooltip("테두리 랜드마크 간의 간격(타일 단위). 값이 작을수록 더 촘촘하게 배치됨")]
    [SerializeField] private int _edgeLandmarkSpacing = 4;
    [Tooltip("아레나 중앙 근처에 배치할 대형 랜드마크 개수. 전투 공간 확보를 위해 기본값 0(비활성화)")]
    [SerializeField] private int _centerLandmarkCount = 0;
    [Tooltip("전투 공간(중앙)을 비우기 위해, 이 반경 안쪽에는 클러스터/랜드마크를 배치하지 않음(타일 단위)")]
    [SerializeField] private float _centerExclusionRadius = 9f;
    [Tooltip("테두리로부터 이 거리(타일 단위) 안쪽에 클러스터 중심을 우선 배치. 값이 작을수록 벽에 더 붙어서 생성됨")]
    [SerializeField] private float _edgeBandDepth = 5f;

    // 메쉬 변형 캐시 (메모리 낭비 방지 및 재사용)
    private readonly Dictionary<Mesh, Mesh[]> _variationCache = new Dictionary<Mesh, Mesh[]>();
    private readonly HashSet<Vector2Int> _occupiedTiles = new HashSet<Vector2Int>();

    /// <summary>
    /// 씬 시작 시 위치 설정 및 던전 구조물 생성
    /// </summary>
    private void Start()
    {
        InitializePosition();
        GenerateDungeonStructure();
    }

    /// <summary>
    /// 전투 조우 컨텍스트 기반 위치 설정
    /// </summary>
    private void InitializePosition()
    {
        if (BattleEncounterContext.Instance == null)
        {
            Debug.LogWarning("[BattleScene] BattleEncounterContext가 존재하지 않습니다.");
            return;
        }

        Vector3 targetPosition = BattleEncounterContext.Instance.TargetBattlePosition;
        transform.position = new Vector3(targetPosition.x, 20f, targetPosition.z);

        Debug.Log($"[BattleScene] 전투 씬 위치 설정 완료: {transform.position}");
    }

    /// <summary>
    /// 던전 데이터에 기반한 전체 구조물 생성 총괄
    /// </summary>
    private void GenerateDungeonStructure()
    {
        DungeonData dungeonData = DungeonSelection.CurrentDungeonData;

        if (dungeonData == null)
        {
            Debug.LogWarning("[BattleScene] 현재 선택된 DungeonData가 없습니다.");
            return;
        }

        _occupiedTiles.Clear();

        SpawnFloorGrid(dungeonData);
        SpawnWalls(dungeonData);
        SpawnRoof(dungeonData);
        SpawnEdgeLandmarks(dungeonData);
        SpawnCenterLandmarks(dungeonData);
        SpawnDecorationClusters(dungeonData);
        SpawnDecorations(dungeonData);
        SpawnCornerDecorations(dungeonData);

        Debug.Log($"[BattleScene] 던전 데이터({dungeonData.name}) 연동 개별 구조물 생성 완료");
    }

    /// <summary>
    /// 하위 구조물을 깔끔하게 정리하기 위한 컨테이너 Transform을 찾거나 생성한다.
    /// </summary>
    /// <param name="containerName">컨테이너 이름</param>
    /// <returns>컨테이너 Transform</returns>
    private Transform GetOrCreateContainer(string containerName)
    {
        Transform container = transform.Find(containerName);
        if (container == null)
        {
            GameObject containerObj = new GameObject(containerName);
            container = containerObj.transform;
            container.SetParent(transform);
            container.localPosition = Vector3.zero;
            container.localRotation = Quaternion.identity;
        }

        return container;
    }

    /// <summary>
    /// 그리드 좌표를 월드 로컬 좌표로 변환한다.
    /// </summary>
    private Vector3 GridToLocalPosition(int x, int z, float y)
    {
        float startX = -(_gridWidth * 0.5f) + 0.5f;
        float startZ = -(_gridDepth * 0.5f) + 0.5f;

        return new Vector3(startX + x, y, startZ + z);
    }

    /// <summary>
    /// 바닥 그리드 개별 생성 및 UV 변형 적용
    /// </summary>
    /// <param name="dungeonData">던전 데이터</param>
    private void SpawnFloorGrid(DungeonData dungeonData)
    {
        if (dungeonData.FloorPrefab == null)
        {
            Debug.LogWarning($"[BattleScene] {dungeonData.name}에 FloorPrefab이 설정되지 않았습니다.");
            return;
        }

        Transform floorParent = GetOrCreateContainer("Floors");

        for (int x = 0; x < _gridWidth; x++)
        {
            for (int z = 0; z < _gridDepth; z++)
            {
                GameObject floorInstance = Instantiate(dungeonData.FloorPrefab, floorParent);
                floorInstance.name = $"Floor_{x}_{z}";

                floorInstance.transform.localPosition = GridToLocalPosition(x, z, -2f);
                floorInstance.transform.localRotation = Quaternion.identity;

                ApplyMeshVariation(floorInstance);
            }
        }
    }

    /// <summary>
    /// 외곽 벽 개별 생성 및 UV 변형 적용
    /// </summary>
    /// <param name="dungeonData">던전 데이터</param>
    private void SpawnWalls(DungeonData dungeonData)
    {
        if (dungeonData.WallPrefab == null)
        {
            Debug.LogWarning($"[BattleScene] {dungeonData.name}에 WallPrefab이 설정되지 않았습니다.");
            return;
        }

        Transform wallParent = GetOrCreateContainer("Walls");

        for (int x = 0; x < _gridWidth; x++)
        {
            for (int z = 0; z < _gridDepth; z++)
            {
                bool isBorder = (x == 0 || x == _gridWidth - 1 || z == 0 || z == _gridDepth - 1);
                if (isBorder == false)
                {
                    continue;
                }

                for (int y = 0; y < _wallHeightLayers; y++)
                {
                    // 버그 수정: FloorPrefab이 아니라 WallPrefab을 사용해야 실제 벽이 생성됨
                    GameObject wallInstance = Instantiate(dungeonData.FloorPrefab, wallParent);
                    wallInstance.name = $"Wall_{x}_{y}_{z}";

                    float posY = -2f + 1f + y;

                    wallInstance.transform.localPosition = GridToLocalPosition(x, z, posY);
                    wallInstance.transform.localRotation = Quaternion.identity;

                    ApplyMeshVariation(wallInstance);
                }
            }
        }
    }

    /// <summary>
    /// 지붕 생성
    /// </summary>
    /// <param name="dungeonData">던전 데이터</param>
    private void SpawnRoof(DungeonData dungeonData)
    {
        if (dungeonData.CeilingPrefab == null)
        {
            return;
        }

        Transform roofParent = GetOrCreateContainer("Roofs");

        for (int x = 0; x < _gridWidth; x++)
        {
            for (int z = 0; z < _gridDepth; z++)
            {
                GameObject roofInstance = Instantiate(dungeonData.CeilingPrefab, roofParent);
                roofInstance.name = $"Roof_{x}_{z}";

                roofInstance.transform.localPosition = GridToLocalPosition(x, z, -2f + _roofHeight);
                roofInstance.transform.localRotation = Quaternion.identity;

                ApplyMeshVariation(roofInstance);
            }
        }
    }

    
    /// <summary>
    /// 아레나 테두리를 따라 일정 간격으로 대형 랜드마크(파괴된 기둥, 바위 등)를 배치한다.
    /// 벽만 있는 밋밋한 테두리에 시각적 리듬을 만들어준다.
    /// </summary>
    /// <param name="dungeonData">던전 데이터</param>
    private void SpawnEdgeLandmarks(DungeonData dungeonData)
    {
        GameObject[] prefabPool = (_landmarkPrefabs != null && _landmarkPrefabs.Length > 0)
            ? _landmarkPrefabs
            : dungeonData.DecorPrefabs;

        if (prefabPool == null || prefabPool.Length == 0)
        {
            return;
        }

        Transform landmarkParent = GetOrCreateContainer("Landmarks");
        int spacing = Mathf.Max(2, _edgeLandmarkSpacing);

        for (int x = spacing; x < _gridWidth - 1; x += spacing)
        {
            TrySpawnLandmark(prefabPool, landmarkParent, new Vector2Int(x, 1), 1.4f);
            TrySpawnLandmark(prefabPool, landmarkParent, new Vector2Int(x, _gridDepth - 2), 1.4f);
        }

        for (int z = spacing; z < _gridDepth - 1; z += spacing)
        {
            TrySpawnLandmark(prefabPool, landmarkParent, new Vector2Int(1, z), 1.4f);
            TrySpawnLandmark(prefabPool, landmarkParent, new Vector2Int(_gridWidth - 2, z), 1.4f);
        }
    }

    /// <summary>
    /// 아레나 중앙 근처(플레이어/적 시작 지점은 피해서)에 눈에 띄는 대형 랜드마크를 배치한다.
    /// 화면 중앙의 밋밋함을 깨는 핵심 요소.
    /// </summary>
    /// <param name="dungeonData">던전 데이터</param>
    private void SpawnCenterLandmarks(DungeonData dungeonData)
    {
        GameObject[] prefabPool = (_landmarkPrefabs != null && _landmarkPrefabs.Length > 0)
            ? _landmarkPrefabs
            : dungeonData.DecorPrefabs;

        if (prefabPool == null || prefabPool.Length == 0)
        {
            return;
        }

        Transform landmarkParent = GetOrCreateContainer("Landmarks");

        int placed = 0;
        int attempts = 0;
        int maxAttempts = _centerLandmarkCount * 20;

        while (placed < _centerLandmarkCount && attempts < maxAttempts)
        {
            attempts++;

            int x = Random.Range(2, _gridWidth - 2);
            int z = Random.Range(2, _gridDepth - 2);
            Vector2Int gridPos = new Vector2Int(x, z);

            Vector2 centerOffset = new Vector2(x - (_gridWidth * 0.5f), z - (_gridDepth * 0.5f));
            if (centerOffset.magnitude < _centerExclusionRadius)
            {
                continue;
            }

            if (TrySpawnLandmark(prefabPool, landmarkParent, gridPos, 1.6f))
            {
                placed++;
            }
        }
    }

    /// <summary>
    /// 지정된 그리드 위치에 랜드마크를 스폰하고, 이미 점유된 타일이면 건너뛴다.
    /// </summary>
    private bool TrySpawnLandmark(GameObject[] prefabPool, Transform parent, Vector2Int gridPos, float scaleMultiplier)
    {
        if (_occupiedTiles.Contains(gridPos))
        {
            return false;
        }

        GameObject prefab = prefabPool[Random.Range(0, prefabPool.Length)];
        if (prefab == null)
        {
            return false;
        }

        GameObject instance = Instantiate(prefab, parent);
        instance.name = $"Landmark_{gridPos.x}_{gridPos.y}";

        instance.transform.localPosition = GridToLocalPosition(gridPos.x, gridPos.y, -2f);
        instance.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        instance.transform.localScale *= Random.Range(scaleMultiplier * 0.85f, scaleMultiplier * 1.15f);

        _occupiedTiles.Add(gridPos);
        return true;
    }

    /// <summary>
    /// 네 벽 중 하나를 무작위로 골라, 그 벽으로부터 _edgeBandDepth 타일 이내의
    /// 좁은 밴드 안에서 랜덤 좌표를 반환한다. 중앙 전투 공간을 비우고 테두리 위주로
    /// 데코를 채우기 위한 헬퍼.
    /// </summary>
    private Vector2Int GetEdgeBandPosition()
    {
        int band = Mathf.Max(2, Mathf.RoundToInt(_edgeBandDepth));
        int side = Random.Range(0, 4); // 0: 좌, 1: 우, 2: 하, 3: 상

        int x;
        int z;

        switch (side)
        {
            case 0: // 왼쪽 벽 인접
                x = Random.Range(1, band);
                z = Random.Range(1, _gridDepth - 1);
                break;
            case 1: // 오른쪽 벽 인접
                x = Random.Range(_gridWidth - band, _gridWidth - 1);
                z = Random.Range(1, _gridDepth - 1);
                break;
            case 2: // 아래쪽 벽 인접
                x = Random.Range(1, _gridWidth - 1);
                z = Random.Range(1, band);
                break;
            default: // 위쪽 벽 인접
                x = Random.Range(1, _gridWidth - 1);
                z = Random.Range(_gridDepth - band, _gridDepth - 1);
                break;
        }

        return new Vector2Int(
            Mathf.Clamp(x, 1, _gridWidth - 2),
            Mathf.Clamp(z, 1, _gridDepth - 2));
    }

    /// <summary>
    /// 바위/잔해 더미처럼 여러 소품이 한 지점 주변에 뭉쳐서 배치되는 클러스터를 생성한다.
    /// 낱개 산개 배치만으로는 밀도감이 부족하기 때문에, 시각적 "덩어리"를 만들어 화면을 채운다.
    /// </summary>
    /// <param name="dungeonData">던전 데이터</param>
    private void SpawnDecorationClusters(DungeonData dungeonData)
    {
        if (dungeonData.DecorPrefabs == null || dungeonData.DecorPrefabs.Length == 0)
        {
            return;
        }

        Transform decorParent = GetOrCreateContainer("Decorations");

        for (int c = 0; c < _clusterCount; c++)
        {
            Vector2Int center = GetEdgeBandPosition();

            int centerX = center.x;
            int centerZ = center.y;

            int clusterSize = Random.Range(_clusterMinSize, _clusterMaxSize + 1);

            for (int i = 0; i < clusterSize; i++)
            {
                Vector2 jitter = Random.insideUnitCircle * _clusterRadius;
                int x = Mathf.Clamp(centerX + Mathf.RoundToInt(jitter.x), 1, _gridWidth - 2);
                int z = Mathf.Clamp(centerZ + Mathf.RoundToInt(jitter.y), 1, _gridDepth - 2);
                Vector2Int gridPos = new Vector2Int(x, z);

                if (_occupiedTiles.Contains(gridPos))
                {
                    continue;
                }

                GameObject decorPrefab = dungeonData.DecorPrefabs[Random.Range(0, dungeonData.DecorPrefabs.Length)];
                if (decorPrefab == null)
                {
                    continue;
                }

                GameObject decorInstance = Instantiate(decorPrefab, decorParent);
                decorInstance.name = $"Cluster_{c}_{x}_{z}";

                decorInstance.transform.localPosition = GridToLocalPosition(x, z, -2f);
                decorInstance.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                _occupiedTiles.Add(gridPos);
            }
        }

        Debug.Log($"[BattleScene] 데코레이션 클러스터 {_clusterCount}개 생성 완료");
    }

    /// <summary>
    /// 내부 데코레이션 랜덤 생성 (클러스터/랜드마크로 점유되지 않은 나머지 타일에 낮은 확률로 산개)
    /// </summary>
    /// <param name="dungeonData">던전 데이터</param>
    private void SpawnDecorations(DungeonData dungeonData)
    {
        if (dungeonData.DecorPrefabs == null || dungeonData.DecorPrefabs.Length == 0)
        {
            return;
        }

        Transform decorParent = GetOrCreateContainer("Decorations");

        for (int x = 1; x < _gridWidth - 1; x++)
        {
            for (int z = 1; z < _gridDepth - 1; z++)
            {
                Vector2Int gridPos = new Vector2Int(x, z);

                if (_occupiedTiles.Contains(gridPos))
                {
                    continue;
                }

                // 전투 공간 확보를 위해 아레나 중앙 근처는 산개 데코도 비워둔다.
                Vector2 centerOffset = new Vector2(x - (_gridWidth * 0.5f), z - (_gridDepth * 0.5f));
                if (centerOffset.magnitude < _centerExclusionRadius)
                {
                    continue;
                }

                if (Random.value > _decorSpawnChance)
                {
                    continue;
                }

                int randomIndex = Random.Range(0, dungeonData.DecorPrefabs.Length);
                GameObject decorPrefab = dungeonData.DecorPrefabs[randomIndex];

                if (decorPrefab == null)
                {
                    continue;
                }

                GameObject decorInstance = Instantiate(decorPrefab, decorParent);
                decorInstance.name = $"Decor_{x}_{z}";

                decorInstance.transform.localPosition = GridToLocalPosition(x, z, -2f);
                decorInstance.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                _occupiedTiles.Add(gridPos);
            }
        }
    }

    /// <summary>
    /// 바닥 모서리 4군데에 0~3 인덱스 데코레이션 랜덤 배치 및 회전
    /// </summary>
    /// <param name="dungeonData">던전 데이터</param>
    private void SpawnCornerDecorations(DungeonData dungeonData)
    {
        if (dungeonData.DecorPrefabs == null || dungeonData.DecorPrefabs.Length == 0)
        {
            return;
        }

        Transform decorParent = GetOrCreateContainer("Decorations");

        Vector2Int[] cornerCoords = new Vector2Int[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(0, _gridDepth - 1),
            new Vector2Int(_gridWidth - 1, 0),
            new Vector2Int(_gridWidth - 1, _gridDepth - 1)
        };

        for (int i = 0; i < cornerCoords.Length; i++)
        {
            int x = cornerCoords[i].x;
            int z = cornerCoords[i].y;

            int maxIndex = Mathf.Min(4, dungeonData.DecorPrefabs.Length);
            int randomIndex = Random.Range(0, maxIndex);

            GameObject decorPrefab = dungeonData.DecorPrefabs[randomIndex];

            if (decorPrefab == null)
            {
                continue;
            }

            GameObject decorInstance = Instantiate(decorPrefab, decorParent);
            decorInstance.name = $"CornerDecor_{x}_{z}";

            decorInstance.transform.localPosition = GridToLocalPosition(x, z, -2f);
            decorInstance.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }
    }

    /// <summary>
    /// 인스턴스화된 오브젝트의 메쉬를 찾아 변형 메쉬를 적용합니다.
    /// </summary>
    /// <param name="targetObj">대상 게임오브젝트</param>
    private void ApplyMeshVariation(GameObject targetObj)
    {
        MeshFilter meshFilter = targetObj.GetComponentInChildren<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return;
        }

        meshFilter.mesh = GetVariationMesh(meshFilter.sharedMesh);
    }

    /// <summary>
    /// 메쉬의 UV(Tiling/Offset)와 Vertex Color를 변형하여 캐싱된 메쉬를 반환합니다.
    /// </summary>
    /// <param name="original">원본 메쉬</param>
    /// <returns>변형된 메쉬</returns>
    private Mesh GetVariationMesh(Mesh original)
    {
        if (!_variationCache.ContainsKey(original))
        {
            Mesh[] variants = new Mesh[_variationCount];

            for (int i = 0; i < _variationCount; i++)
            {
                variants[i] = Instantiate(original);

                // 1. UV 변형 (Tiling & Offset)
                Vector2[] uvs = variants[i].uv;
                float tileX = Random.Range(1.0f, 1.2f);
                float tileY = Random.Range(1.0f, 1.2f);
                Vector2 offset = new Vector2(Random.value, Random.value);

                for (int j = 0; j < uvs.Length; j++)
                {
                    uvs[j].x = (uvs[j].x * tileX) + offset.x;
                    uvs[j].y = (uvs[j].y * tileY) + offset.y;
                }
                variants[i].uv = uvs;

                // 2. Vertex Color (명암 변형)
                Color[] colors = new Color[variants[i].vertexCount];
                float brightness = Random.Range(0.7f, 1.0f);
                for (int k = 0; k < colors.Length; k++)
                {
                    colors[k] = new Color(brightness, brightness, brightness, 1f);
                }
                variants[i].colors = colors;

                variants[i].name = $"{original.name}_Var_{i}";
            }
            _variationCache[original] = variants;
        }

        return _variationCache[original][Random.Range(0, _variationCount)];
    }
}