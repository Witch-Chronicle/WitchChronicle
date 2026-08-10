// FILE: Assets\_Scripts\Dungeon\DungeonMeshBuilder.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 던전 타일 데이터를 기반으로 Chunk Mesh를 생성하는 클래스.
/// </summary>
public class DungeonMeshBuilder
{
    /// <summary>
    /// 바닥 타일 메쉬들을 하나의 Chunk Mesh로 결합한다.
    /// (90도 무작위 회전 및 UV 오프셋 변형 적용)
    /// </summary>
    public void BuildFloorMesh(GameObject floorPrefab, IEnumerable<Vector2Int> floorPositions, float tileSize, Transform parent)
    {
        if (floorPrefab == null)
        {
            Debug.LogWarning("[DungeonMeshBuilder] 바닥 프리팹이 없습니다.");
            return;
        }

        MeshFilter sourceMeshFilter = floorPrefab.GetComponentInChildren<MeshFilter>();
        MeshRenderer sourceRenderer = floorPrefab.GetComponentInChildren<MeshRenderer>();

        if (sourceMeshFilter == null || sourceRenderer == null)
        {
            Debug.LogWarning("[DungeonMeshBuilder] 바닥 프리팹(또는 자식)에 MeshFilter 또는 MeshRenderer가 없습니다.");
            return;
        }

        Mesh originalMesh = sourceMeshFilter.sharedMesh;

        // 💡 UV 오프셋이 무작위로 다르게 적용된 변형 메쉬 4개 미리 준비 (성능 최적화)
        Mesh[] meshVariations = CreateMeshVariations(originalMesh, 4);

        List<CombineInstance> combines = new List<CombineInstance>();

        foreach (var pos in floorPositions)
        {
            // 💡 1. 90도 단위 무작위 Y축 회전 (0°, 90°, 180°, 270°) -> 바둑판 패턴 제거
            int randomAngle = Random.Range(0, 4) * 90;
            Quaternion randomRotation = Quaternion.Euler(0f, randomAngle, 0f);

            // 💡 2. 준비된 변형 메쉬 중 하나를 무작위 선택 -> 무늬 오프셋 차별화
            Mesh selectedMesh = meshVariations[Random.Range(0, meshVariations.Length)];

            combines.Add(CreateCombineInstance(
                selectedMesh,
                pos, // Vector2Int -> Vector2 암시적 변환
                tileSize,
                0f,
                randomRotation, // 무작위 회전 전달
                floorPrefab.transform.localScale,
                false
            ));
        }

        BuildChunkMesh("Floor_Mesh", combines, sourceRenderer.sharedMaterial, parent, true, LayerMask.NameToLayer("Default"));
    }

    /// <summary>
    /// 💡 원본 메쉬를 기반으로 UV 오프셋이 다르게 적용된 변형 메쉬들을 미리 생성합니다.
    /// </summary>
    private Mesh[] CreateMeshVariations(Mesh originalMesh, int count)
    {
        Mesh[] variations = new Mesh[count];

        for (int i = 0; i < count; i++)
        {
            if (i == 0)
            {
                variations[i] = originalMesh; // 첫 번째는 원본 그대로 사용
                continue;
            }

            Mesh varMesh = Object.Instantiate(originalMesh);
            Vector2[] uvs = varMesh.uv;

            // 무작위 UV 오프셋 (텍스처 위치를 랜덤으로 이동)
            float offsetX = Random.Range(0f, 1f);
            float offsetY = Random.Range(0f, 1f);

            for (int j = 0; j < uvs.Length; j++)
            {
                uvs[j].x += offsetX;
                uvs[j].y += offsetY;
            }

            varMesh.uv = uvs;
            variations[i] = varMesh;
        }

        return variations;
    }

    /// <summary>
    /// 벽 타일 메쉬들을 하나의 Chunk Mesh로 결합한다.
    /// 벽 프리팹의 피벗은 중앙(center pivot)이며,
    /// wall.Position은 이미 (바닥 타일의 center-pivot 배치를 고려한) 정확한 경계 좌표(소수 가능)로 전달된다.
    /// </summary>
    public void BuildWallMesh(GameObject wallPrefab, List<WallData> wallDataList, float tileSize, float height, Transform parent)
    {
        if (wallPrefab == null)
        {
            Debug.LogWarning("[DungeonMeshBuilder] 벽 프리팹이 없습니다.");
            return;
        }

        MeshFilter sourceMeshFilter = wallPrefab.GetComponentInChildren<MeshFilter>();
        MeshRenderer sourceRenderer = wallPrefab.GetComponentInChildren<MeshRenderer>();

        if (sourceMeshFilter == null || sourceRenderer == null)
        {
            Debug.LogWarning("[DungeonMeshBuilder] 벽 프리팹(또는 자식)에 MeshFilter 또는 MeshRenderer가 없습니다.");
            return;
        }

        float meshLocalHeight = sourceMeshFilter.sharedMesh.bounds.size.y;
        if (meshLocalHeight <= 0f)
        {
            meshLocalHeight = 1f;
        }

        float unitHeight = meshLocalHeight * wallPrefab.transform.localScale.y;
        if (unitHeight <= 0f)
        {
            unitHeight = 1f;
        }

        const float epsilon = 0.001f;

        List<CombineInstance> combines = new List<CombineInstance>();

        foreach (var wall in wallDataList)
        {
            for (float y = 0f; y < height - epsilon; y += unitHeight)
            {
                float centerY = y + (unitHeight * 0.5f);

                combines.Add(CreateCombineInstance(
                    sourceMeshFilter.sharedMesh,
                    wall.Position,
                    tileSize,
                    centerY,
                    wall.Rotation,
                    wallPrefab.transform.localScale,
                    true
                ));
            }
        }

        BuildChunkMesh("Wall_Mesh", combines, sourceRenderer.sharedMaterial, parent, true, LayerMask.NameToLayer("Default"));
    }

    /// <summary>
    /// 천장 타일 메쉬들을 하나의 Chunk Mesh로 결합한다.
    /// </summary>
    public void BuildCeilingMesh(GameObject ceilingPrefab, IEnumerable<Vector2Int> floorPositions, float tileSize, float height, Transform parent)
    {
        if (ceilingPrefab == null)
        {
            Debug.LogWarning("[DungeonMeshBuilder] 천장 프리팹이 없습니다.");
            return;
        }

        MeshFilter sourceMeshFilter = ceilingPrefab.GetComponentInChildren<MeshFilter>();
        MeshRenderer sourceRenderer = ceilingPrefab.GetComponentInChildren<MeshRenderer>();

        if (sourceMeshFilter == null || sourceRenderer == null)
        {
            Debug.LogWarning("[DungeonMeshBuilder] 천장 프리팹(또는 자식)에 MeshFilter 또는 MeshRenderer가 없습니다.");
            return;
        }

        List<CombineInstance> combines = new List<CombineInstance>();

        foreach (var pos in floorPositions)
        {
            combines.Add(CreateCombineInstance(
                sourceMeshFilter.sharedMesh,
                pos,
                tileSize,
                height,
                Quaternion.identity,
                ceilingPrefab.transform.localScale,
                false
            ));
        }

        BuildChunkMesh("Ceiling_Mesh", combines, sourceRenderer.sharedMaterial, parent, true, LayerMask.NameToLayer("Default"));
    }

    /// <summary>
    /// 개별 메쉬 조합을 위한 CombineInstance를 생성한다.
    /// </summary>
    private CombineInstance CreateCombineInstance(Mesh sourceMesh, Vector2 gridPos, float tileSize, float yOffset, Quaternion rotation, Vector3 localScale, bool isWall)
    {
        Vector3 targetCenter = new Vector3(gridPos.x * tileSize, yOffset, gridPos.y * tileSize);

        Vector3 scaledPivotOffset = Vector3.Scale(localScale, sourceMesh.bounds.center);
        Vector3 rotatedPivotOffset = rotation * scaledPivotOffset;

        Vector3 worldPos = targetCenter - rotatedPivotOffset;

        Vector3 adjustedScale = localScale * 1.005f;

        Matrix4x4 matrix = Matrix4x4.TRS(worldPos, rotation, adjustedScale);

        CombineInstance ci = new CombineInstance();
        ci.mesh = sourceMesh;
        ci.transform = matrix;
        return ci;
    }

    /// <summary>
    /// CombineInstance 리스트를 하나의 청크 메쉬로 병합한다.
    /// </summary>
    private void BuildChunkMesh(string meshName, List<CombineInstance> combines, Material material, Transform parent, bool generateCollider, int layer)
    {
        if (combines.Count == 0)
        {
            return;
        }

        GameObject chunkObj = new GameObject(meshName);
        chunkObj.transform.SetParent(parent);
        chunkObj.transform.localPosition = Vector3.zero;
        chunkObj.transform.localRotation = Quaternion.identity;
        chunkObj.transform.localScale = Vector3.one;
        chunkObj.layer = layer;

        MeshFilter mf = chunkObj.AddComponent<MeshFilter>();
        MeshRenderer mr = chunkObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;

        Mesh combinedMesh = new Mesh();
        combinedMesh.indexFormat = IndexFormat.UInt32;

        combinedMesh.CombineMeshes(combines.ToArray(), true, true);
        mf.sharedMesh = combinedMesh;

        if (generateCollider)
        {
            MeshCollider mc = chunkObj.AddComponent<MeshCollider>();
            mc.sharedMesh = combinedMesh;
        }

        Debug.Log($"[DungeonMeshBuilder] 청크 메쉬 생성 완료: {meshName} (버텍스 수: {combinedMesh.vertexCount})");
    }
}