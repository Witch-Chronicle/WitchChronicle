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

        List<CombineInstance> combines = new List<CombineInstance>();

        foreach (var pos in floorPositions)
        {
            combines.Add(CreateCombineInstance(
                sourceMeshFilter.sharedMesh,
                pos, // Vector2Int -> Vector2 암시적 변환
                tileSize,
                0f,
                Quaternion.identity,
                floorPrefab.transform.localScale,
                false
            ));
        }

        BuildChunkMesh("Floor_Mesh", combines, sourceRenderer.sharedMaterial, parent, true, LayerMask.NameToLayer("Default"));
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

        // 벽 한 조각의 실제 월드 높이를 계산한다.
        // Transform.localScale.y 만으로는 부족하다 — 메쉬 자체가 이미 특정 높이로
        // 모델링되어 있고 Scale은 1로 두는 경우가 흔하기 때문에,
        // "메쉬 로컬 바운드 높이 × Scale"을 실제 단위 높이로 사용한다.
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

        // ===== 임시 디버그 로그 (원인 파악 후 삭제할 것) =====
        Debug.Log(
            $"[WALL DEBUG] mesh.bounds.center={sourceMeshFilter.sharedMesh.bounds.center}, " +
            $"mesh.bounds.size={sourceMeshFilter.sharedMesh.bounds.size}, " +
            $"wallPrefab.localScale={wallPrefab.transform.localScale}, " +
            $"wallPrefab.localPosition={wallPrefab.transform.localPosition}, " +
            $"unitHeight={unitHeight}"
        );
        // ===================================================

        List<CombineInstance> combines = new List<CombineInstance>();

        foreach (var wall in wallDataList)
        {
            for (float y = 0f; y < height - epsilon; y += unitHeight)
            {
                // 피벗이 중앙인 프리팹이므로, 각 단의 "바닥" 기준 y가 아니라
                // "중심" 기준 y로 보정해서 배치한다.
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
                pos, // Vector2Int -> Vector2 암시적 변환
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
    /// gridPos는 소수(.5) 좌표도 허용한다 (벽이 타일 경계 중앙에 배치되어야 하는 경우 사용).
    ///
    /// 프리팹의 피벗이 메쉬의 시각적 중심과 정확히 일치하지 않을 수 있으므로,
    /// sourceMesh.bounds.center를 읽어 "실제 보이는 중심"이 targetCenter(원하는 좌표)에
    /// 오도록 배치 위치를 역산해서 보정한다. 피벗이 완벽히 중앙이면 보정량은 0이 되고,
    /// 조금이라도 어긋나 있으면 그 오차만큼 자동으로 상쇄된다.
    /// </summary>
    private CombineInstance CreateCombineInstance(Mesh sourceMesh, Vector2 gridPos, float tileSize, float yOffset, Quaternion rotation, Vector3 localScale, bool isWall)
    {
        Vector3 targetCenter = new Vector3(gridPos.x * tileSize, yOffset, gridPos.y * tileSize);

        // 메쉬 로컬 공간에서의 바운드 중심 (피벗 기준 오프셋)
        Vector3 scaledPivotOffset = Vector3.Scale(localScale, sourceMesh.bounds.center);
        Vector3 rotatedPivotOffset = rotation * scaledPivotOffset;

        // 실제 렌더링되는 중심이 targetCenter에 오도록 피벗 위치를 역산
        Vector3 worldPos = targetCenter - rotatedPivotOffset;

        Vector3 adjustedScale = localScale * 1.005f;

        Matrix4x4 matrix = Matrix4x4.TRS(worldPos, rotation, adjustedScale);

        CombineInstance ci = new CombineInstance();
        ci.mesh = sourceMesh;
        ci.transform = matrix;
        return ci;
    }

    /// <summary>
    /// CombineInstance 리스트를 하나의 청크 메쉬로 병합한다. (UInt32 인덱스 형식 적용)
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

        // 65,535개 이상의 버텍스를 허용하도록 UInt32 인덱스 포맷 설정
        combinedMesh.indexFormat = IndexFormat.UInt32;

        // List를 배열로 변환 (.ToArray() 추가)
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