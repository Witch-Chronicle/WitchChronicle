#ifndef STOCHASTIC_SAMPLING_INCLUDED
#define STOCHASTIC_SAMPLING_INCLUDED

// 슈도 랜덤 해시
float2 Hash2D(float2 p)
{
    p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
    return frac(sin(p) * 43758.5453);
}

// 스토캐스틱 샘플링 메인 함수 (UnityTexture2D 구조체 사용)
void StochasticSample_float(UnityTexture2D Tex, float2 UV, out float4 Out)
{
    // 그리드 좌표계 산출
    float2 uvSkewed = UV + (UV.x + UV.y) * 0.3660254;
    int2 iUV = floor(uvSkewed);
    float2 fUV = frac(uvSkewed);
    
    float3 w = float3(fUV, 1.0 - fUV.x - fUV.y);
    int2 i0 = iUV + ((w.z < 0.0) ? int2(1, 1) : int2(0, 0));
    int2 i1 = iUV + int2(1, 0);
    int2 i2 = iUV + int2(0, 1);
    
    // 세 지점(3-Tap)에서 무작위 변환 후 샘플링 병합
    float2 h0 = Hash2D(i0);
    float2 h1 = Hash2D(i1);
    float2 h2 = Hash2D(i2);
    
    float rot0 = h0.x * 6.2831853;
    float rot1 = h1.x * 6.2831853;
    float rot2 = h2.x * 6.2831853;
    
    float2x2 r0 = float2x2(cos(rot0), -sin(rot0), sin(rot0), cos(rot0));
    float2x2 r1 = float2x2(cos(rot1), -sin(rot1), sin(rot1), cos(rot1));
    float2x2 r2 = float2x2(cos(rot2), -sin(rot2), sin(rot2), cos(rot2));
    
    // UnityTexture2D 내부의 .tex와 .samplerstate를 이용해 샘플링
    float4 col0 = Tex.tex.SampleLevel(Tex.samplerstate, mul(r0, UV - i0) + h0, 0);
    float4 col1 = Tex.tex.SampleLevel(Tex.samplerstate, mul(r1, UV - i1) + h1, 0);
    float4 col2 = Tex.tex.SampleLevel(Tex.samplerstate, mul(r2, UV - i2) + h2, 0);
    
    float3 weights = (w.z < 0.0) ? float3(1.0 - w.x - w.y, w.y, w.x) : float3(w.z, w.y, w.x);
    
    Out = col0 * weights.x + col1 * weights.y + col2 * weights.z;
}

#endif