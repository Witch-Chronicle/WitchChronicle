Shader "WitchChronicle/FullScreenBlur"
{
    Properties
    {
        _BlurStrength("Blur Strength", Range(0, 5)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        Cull Off

        Pass
        {
            Name "FullScreenBlur"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _BlurStrength;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                float2 texelSize =
                    _BlitTexture_TexelSize.xy * _BlurStrength;

                half4 color = 0;

                // 중앙
                color += SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv
                ) * 0.20;

                // 상하좌우
                color += SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv + float2(texelSize.x, 0)
                ) * 0.10;

                color += SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv - float2(texelSize.x, 0)
                ) * 0.10;

                color += SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv + float2(0, texelSize.y)
                ) * 0.10;

                color += SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv - float2(0, texelSize.y)
                ) * 0.10;

                // 대각선
                color += SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv + texelSize
                ) * 0.10;

                color += SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv - texelSize
                ) * 0.10;

                color += SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv + float2(texelSize.x, -texelSize.y)
                ) * 0.10;

                color += SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv + float2(-texelSize.x, texelSize.y)
                ) * 0.10;

                return color;
            }

            ENDHLSL
        }
    }
}