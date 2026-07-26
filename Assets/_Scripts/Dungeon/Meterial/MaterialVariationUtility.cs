using UnityEngine;

/// <summary>
/// 머티리얼 속성(색상 및 UV 오프셋)을 배칭 유지하며 변경하는 유틸리티.
/// </summary>
public static class MaterialVariationUtility
{
    private static readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();
    
    // 셰이더 속성 ID 캐싱 (성능 최적화)
    private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor"); 
    private static readonly int _colorId = Shader.PropertyToID("_Color");
    private static readonly int _mainTexStId = Shader.PropertyToID("_MainTex_ST");

    /// <summary>
    /// 오브젝트에 랜덤한 밝기와 랜덤한 UV 오프셋을 적용합니다.
    /// </summary>
    public static void ApplyVariation(GameObject target, float brightnessStrength)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        renderer.GetPropertyBlock(_propertyBlock);

        // 1. 밝기 변형
        float randomBrightness = Random.Range(1f - brightnessStrength, 1f + brightnessStrength);
        Color tint = new Color(randomBrightness, randomBrightness, randomBrightness, 1f);

        if (renderer.sharedMaterial.HasProperty(_baseColorId))
        {
            _propertyBlock.SetColor(_baseColorId, tint);
        }
        else if (renderer.sharedMaterial.HasProperty(_colorId))
        {
            _propertyBlock.SetColor(_colorId, tint);
        }

        // 2. UV 오프셋 변형 (랜덤한 0~1 사이의 좌표로 이동)
        // Vector4(tiling.x, tiling.y, offset.x, offset.y)
        float offsetX = Random.Range(0f, 1f);
        float offsetY = Random.Range(0f, 1f);
        _propertyBlock.SetVector(_mainTexStId, new Vector4(1, 1, offsetX, offsetY));

        renderer.SetPropertyBlock(_propertyBlock);

        Debug.Log($"[MaterialVariation] {target.name}에 랜덤 UV 오프셋 적용 (x: {offsetX:F2}, y: {offsetY:F2})");
    }
}