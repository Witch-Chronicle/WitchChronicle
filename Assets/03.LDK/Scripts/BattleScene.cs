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
    [SerializeField] [Range(0f, 1f)] private float _decorSpawnChance = 0.1f;

    // 메쉬 변형 캐시 (메모리 낭비 방지 및 재사용)
    private readonly Dictionary<Mesh, Mesh[]> _variationCache = new Dictionary<Mesh, Mesh[]>();

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

        SpawnFloorGrid(dungeonData);
        SpawnWalls(dungeonData);
        SpawnRoof(dungeonData);
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
            container.localScale = Vector3.one;
        }

        return container;
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
        float startX = -(_gridWidth * 0.5f) + 0.5f;
        float startZ = -(_gridDepth * 0.5f) + 0.5f;

        for (int x = 0; x < _gridWidth; x++)
        {
            for (int z = 0; z < _gridDepth; z++)
            {
                GameObject floorInstance = Instantiate(dungeonData.FloorPrefab, floorParent);
                floorInstance.name = $"Floor_{x}_{z}";

                float posX = startX + x;
                float posZ = startZ + z;

                floorInstance.transform.localPosition = new Vector3(posX, -2f, posZ);
                floorInstance.transform.localRotation = Quaternion.identity;
                floorInstance.transform.localScale = Vector3.one;

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
        float startX = -(_gridWidth * 0.5f) + 0.5f;
        float startZ = -(_gridDepth * 0.5f) + 0.5f;

        for (int x = 0; x < _gridWidth; x++)
        {
            for (int z = 0; z < _gridDepth; z++)
            {
                bool isBorder = (x == 0 || x == _gridWidth - 1 || z == 0 || z == _gridDepth - 1);
                if (isBorder == false)
                {
                    continue;
                }

                float posX = startX + x;
                float posZ = startZ + z;

                for (int y = 0; y < _wallHeightLayers; y++)
                {
                    GameObject wallInstance = Instantiate(dungeonData.WallPrefab, wallParent);
                    wallInstance.name = $"Wall_{x}_{y}_{z}";

                    float posY = -2f + 1f + y;

                    wallInstance.transform.localPosition = new Vector3(posX, posY, posZ);
                    wallInstance.transform.localRotation = Quaternion.identity;
                    wallInstance.transform.localScale = Vector3.one;

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
        float startX = -(_gridWidth * 0.5f) + 0.5f;
        float startZ = -(_gridDepth * 0.5f) + 0.5f;

        for (int x = 0; x < _gridWidth; x++)
        {
            for (int z = 0; z < _gridDepth; z++)
            {
                GameObject roofInstance = Instantiate(dungeonData.CeilingPrefab, roofParent);
                roofInstance.name = $"Roof_{x}_{z}";

                float posX = startX + x;
                float posZ = startZ + z;

                roofInstance.transform.localPosition = new Vector3(posX, -2f + _roofHeight, posZ);
                roofInstance.transform.localRotation = Quaternion.identity;
                roofInstance.transform.localScale = Vector3.one;

                ApplyMeshVariation(roofInstance);
            }
        }
    }

    /// <summary>
    /// 내부 데코레이션 랜덤 생성
    /// </summary>
    /// <param name="dungeonData">던전 데이터</param>
    private void SpawnDecorations(DungeonData dungeonData)
    {
        if (dungeonData.DecorPrefabs == null || dungeonData.DecorPrefabs.Length == 0)
        {
            return;
        }

        Transform decorParent = GetOrCreateContainer("Decorations");
        float startX = -(_gridWidth * 0.5f) + 0.5f;
        float startZ = -(_gridDepth * 0.5f) + 0.5f;

        for (int x = 1; x < _gridWidth - 1; x++)
        {
            for (int z = 1; z < _gridDepth - 1; z++)
            {
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

                float posX = startX + x;
                float posZ = startZ + z;

                decorInstance.transform.localPosition = new Vector3(posX, -2f, posZ);
                decorInstance.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                decorInstance.transform.localScale = Vector3.one;
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
        float startX = -(_gridWidth * 0.5f) + 0.5f;
        float startZ = -(_gridDepth * 0.5f) + 0.5f;

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

            float posX = startX + x;
            float posZ = startZ + z;

            decorInstance.transform.localPosition = new Vector3(posX, -2f, posZ);
            decorInstance.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            decorInstance.transform.localScale = Vector3.one;
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