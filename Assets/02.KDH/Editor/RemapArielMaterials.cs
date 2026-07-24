using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// FBX(흰 모델)의 머티리얼을 같은 폴더 GLB의 정상 머티리얼(텍스처 연결된 것)로 자동 연결.
/// 이름이 정확히 같지 않아도 "tripo_part_숫자" 토큰이 같으면 매칭.
///
/// 사용법: Project 창에서 FBX 선택 → Tools → KDH → 선택한 FBX 머티리얼을 GLB로 연결
/// (FBX와 원본 GLB가 같은 폴더에 있어야 함)
public static class RemapArielMaterials
{
    [MenuItem("Tools/KDH/선택한 FBX 머티리얼을 GLB로 연결")]
    public static void RemapSelected()
    {
        string fbxPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(fbxPath) || !fbxPath.ToLower().EndsWith(".fbx"))
        {
            Debug.LogError("Project 창에서 FBX 파일을 선택한 뒤 실행하세요.");
            return;
        }

        string dir = Path.GetDirectoryName(fbxPath)?.Replace('\\', '/');
        string glbPath = Directory.GetFiles(dir ?? "", "*.glb").FirstOrDefault()?.Replace('\\', '/');
        if (glbPath == null)
        {
            Debug.LogError($"같은 폴더({dir})에 GLB 파일이 없습니다.");
            return;
        }

        Remap(fbxPath, glbPath);
    }

    private static void Remap(string fbxPath, string glbPath)
    {
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError($"FBX를 찾을 수 없습니다: {fbxPath}");
            return;
        }

        // GLB 머티리얼을 "tripo_part_숫자" 토큰 기준으로 색인 (토큰 없으면 전체 이름)
        var glbByToken = AssetDatabase.LoadAllAssetsAtPath(glbPath)
            .OfType<Material>()
            .GroupBy(m => Token(m.name))
            .ToDictionary(g => g.Key, g => g.First());

        if (glbByToken.Count == 0)
        {
            Debug.LogError($"GLB에서 머티리얼을 찾지 못했습니다: {glbPath}");
            return;
        }

        // FBX의 소스 머티리얼 이름 = 임포트된 내부 머티리얼 이름
        var sourceNames = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<Material>()
            .Select(m => m.name)
            .Distinct()
            .ToList();

        int remapped = 0;
        foreach (string sourceName in sourceNames)
        {
            if (!glbByToken.TryGetValue(Token(sourceName), out var glbMaterial)) continue;
            importer.AddRemap(
                new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceName),
                glbMaterial);
            remapped++;
        }

        // 이름이 달라도 양쪽 다 재질이 1개뿐이면 그냥 서로 연결 (신형 Tripo 단일 재질 모델 대응)
        if (remapped == 0 && sourceNames.Count == 1 && glbByToken.Count == 1)
        {
            importer.AddRemap(
                new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceNames[0]),
                glbByToken.Values.First());
            remapped = 1;
        }

        importer.SaveAndReimport();
        Debug.Log($"[RemapArielMaterials] {Path.GetFileName(fbxPath)}: 소스 {sourceNames.Count}개 중 {remapped}개를 GLB 머티리얼로 연결했습니다.");
    }

    /// "Material_tripo_part_12.001" → "tripo_part_12". 토큰이 없으면 원래 이름 반환.
    private static string Token(string name)
    {
        var match = Regex.Match(name, @"tripo_part_\d+");
        return match.Success ? match.Value : name;
    }
}
