using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 던전 메쉬 청크 결합 및 변형을 담당하는 클래스.
/// </summary>
public class DungeonMeshBuilder
{
    private const int ChunkSize = 1000;
    private const int VariationCount = 5;

    private readonly Dictionary<Mesh, Mesh[]> _variationCache = new Dictionary<Mesh, Mesh[]>();
    private static readonly int MainTexST = Shader.PropertyToID("_MainTex_ST");

    /// <summary>
    /// 바닥 타일 데이터를 Chunk Mesh로 결합한다.
    /// </summary>
    public GameObject BuildFloorMesh(GameObject floorPrefab, HashSet<Vector2Int> positions, float tileSize, Transform parent)
    {
        if (floorPrefab == null)
        {
            Debug.LogWarning("[DungeonMeshBuilder] 바닥 프리팹이 없습니다.");
            return null;
        }

        MeshFilter sourceMeshFilter = floorPrefab.GetComponent<MeshFilter>();
        MeshRenderer sourceRenderer = floorPrefab.GetComponent<MeshRenderer>();

        if (sourceMeshFilter == null || sourceRenderer == null)
        {
            Debug.LogWarning("[DungeonMeshBuilder] 바닥 프리팹에 MeshFilter 또는 MeshRenderer가 없습니다.");
            return null;
        }

        GameObject root = new GameObject("Dungeon_Floor_Mesh");
        root.transform.SetParent(parent);

        List<Vector2Int> positionList = new List<Vector2Int>(positions);

        for (int startIndex = 0; startIndex < positionList.Count; startIndex += ChunkSize)
        {
            List<CombineInstance> combines = new List<CombineInstance>();
            int endIndex = Mathf.Min(startIndex + ChunkSize, positionList.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                Vector2Int position = positionList[i];
                combines.Add(CreateCombineInstance(
                    sourceMeshFilter.sharedMesh,
                    position,
                    tileSize,
                    0f,
                    floorPrefab.transform.rotation,
                    floorPrefab.transform.localScale,
                    true
                ));
            }

            BuildChunkMesh(
                $"Floor_Chunk_{startIndex / ChunkSize}",
                combines,
                sourceRenderer.sharedMaterial,
                root.transform,
                true,
                LayerMask.NameToLayer("Default")
            );
        }

        return root;
    }

    /// <summary>
    /// 벽 데이터를 Chunk Mesh로 결합한다.
    /// </summary>
    public void BuildWallMesh(GameObject wallPrefab, HashSet<Vector2Int> positions, float tileSize, float height, Transform parent)
    {
        if (wallPrefab == null)
        {
            return;
        }

        MeshFilter sourceMeshFilter = wallPrefab.GetComponent<MeshFilter>();
        MeshRenderer sourceRenderer = wallPrefab.GetComponent<MeshRenderer>();

        if (sourceMeshFilter == null || sourceRenderer == null)
        {
            return;
        }

        List<CombineInstance> combines = new List<CombineInstance>();

        foreach (Vector2Int position in positions)
        {
            for (float y = 0f; y < height; y += wallPrefab.transform.localScale.y)
            {
                combines.Add(CreateCombineInstance(
                    sourceMeshFilter.sharedMesh,
                    position,
                    tileSize,
                    y,
                    wallPrefab.transform.rotation,
                    wallPrefab.transform.localScale,
                    true
                ));
            }
        }

        BuildChunkMesh("Wall_Mesh", combines, sourceRenderer.sharedMaterial, parent, true, LayerMask.NameToLayer("Default"));
    }

    /// <summary>
    /// 천장 데이터를 Chunk Mesh로 결합한다.
    /// </summary>
    public void BuildCeilingMesh(GameObject ceilingPrefab, HashSet<Vector2Int> positions, float tileSize, float height, Transform parent)
    {
        if (ceilingPrefab == null)
        {
            return;
        }

        MeshFilter sourceMeshFilter = ceilingPrefab.GetComponent<MeshFilter>();
        MeshRenderer sourceRenderer = ceilingPrefab.GetComponent<MeshRenderer>();

        if (sourceMeshFilter == null || sourceRenderer == null)
        {
            return;
        }

        List<CombineInstance> combines = new List<CombineInstance>();

        foreach (Vector2Int position in positions)
        {
            combines.Add(CreateCombineInstance(
                sourceMeshFilter.sharedMesh,
                position,
                tileSize,
                height,
                ceilingPrefab.transform.rotation,
                ceilingPrefab.transform.localScale,
                true
            ));
        }

        BuildChunkMesh("Ceiling_Mesh", combines, sourceRenderer.sharedMaterial, parent, false, LayerMask.NameToLayer("Minimap"));
    }

    /// <summary>
    /// 메쉬 결합용 CombineInstance를 생성하며, UV/Vertex Color 변형이 적용된 메쉬를 선택합니다.
    /// </summary>
    private CombineInstance CreateCombineInstance(Mesh mesh, Vector2Int position, float tileSize, float height, Quaternion rotation, Vector3 scale, bool useVariation)
    {
        CombineInstance combine = new CombineInstance();
        combine.mesh = useVariation ? GetVariationMesh(mesh) : mesh;
        combine.transform = Matrix4x4.TRS(
            new Vector3(position.x * tileSize, height, position.y * tileSize),
            rotation,
            scale
        );
        return combine;
    }

    /// <summary>
    /// 메쉬의 UV(Tiling/Offset)와 Vertex Color를 변형하여 캐싱된 메쉬를 반환합니다.
    /// </summary>
    private Mesh GetVariationMesh(Mesh original)
    {
        if (original == null)
        {
            return null;
        }

        if (!_variationCache.ContainsKey(original))
        {
            Mesh[] variants = new Mesh[VariationCount];

            for (int i = 0; i < VariationCount; i++)
            {
                Mesh variant = Object.Instantiate(original);
                variant.name = $"{original.name}_Var_{i}";

                Vector2[] uvs = variant.uv;
                if (uvs != null && uvs.Length > 0)
                {
                    float tileX = Random.Range(1.0f, 1.2f);
                    float tileY = Random.Range(1.0f, 1.2f);
                    Vector2 offset = new Vector2(Random.value, Random.value);

                    for (int j = 0; j < uvs.Length; j++)
                    {
                        uvs[j].x = (uvs[j].x * tileX) + offset.x;
                        uvs[j].y = (uvs[j].y * tileY) + offset.y;
                    }
                    variant.uv = uvs;
                }

                Color[] colors = new Color[variant.vertexCount];
                float brightness = Random.Range(0.7f, 1.0f);
                for (int k = 0; k < colors.Length; k++)
                {
                    colors[k] = new Color(brightness, brightness, brightness, 1f);
                }
                variant.colors = colors;

                variants[i] = variant;
            }
            _variationCache[original] = variants;
        }

        Mesh[] cachedVariants = _variationCache[original];
        return cachedVariants[Random.Range(0, cachedVariants.Length)];
    }

    /// <summary>
    /// 생성된 변형 메쉬 캐시 메모리를 안전하게 해제한다. (던전 재생성 시 호출 권장)
    /// </summary>
    public void ClearCache()
    {
        foreach (var kvp in _variationCache)
        {
            if (kvp.Value != null)
            {
                foreach (Mesh variant in kvp.Value)
                {
                    if (variant != null)
                    {
                        Object.Destroy(variant);
                    }
                }
            }
        }
        _variationCache.Clear();
        Debug.Log("[DungeonMeshBuilder] 메쉬 변형 캐시 메모리를 안전하게 정리했습니다.");
    }

    /// <summary>
    /// Combine 데이터를 하나의 Mesh Object로 생성하고, 개별 텍스처 변형 속성을 적용합니다.
    /// </summary>
    private void BuildChunkMesh(string name, List<CombineInstance> combines, Material material, Transform parent, bool addCollider, int layer)
    {
        if (combines.Count == 0)
        {
            return;
        }

        Mesh mesh = new Mesh { indexFormat = IndexFormat.UInt32, name = name };
        mesh.CombineMeshes(combines.ToArray());
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        GameObject chunkObject = new GameObject(name);
        chunkObject.transform.SetParent(parent);

        MeshFilter meshFilter = chunkObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = material;
        chunkObject.layer = layer;

        if (addCollider)
        {
            MeshCollider meshCollider = chunkObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }

        Debug.Log($"[DungeonMeshBuilder] {name} 생성 완료 / Vertex : {mesh.vertexCount}");
    }
}