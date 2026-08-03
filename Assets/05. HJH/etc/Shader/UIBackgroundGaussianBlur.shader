Shader "WitchChronicle/UI Background Gaussian Blur"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _BlurRadius("Blur Radius", Range(0, 5)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Cull Off
        ZWrite Off
        ZTest Always

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        float4 _MainTex_TexelSize;
        float _BlurRadius;

        struct Attributes
        {
            uint vertexID : SV_VertexID;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;

            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.uv = GetFullScreenTriangleTexCoord(input.vertexID);

            return output;
        }

        half4 SampleBlur(float2 uv, float2 direction)
        {
            float2 offset = direction * _MainTex_TexelSize.xy * _BlurRadius;

            half4 color = 0;

            // 9-Tap Gaussian Blur
            color += SAMPLE_TEXTURE2D(
                _MainTex,
                sampler_MainTex,
                uv
            ) * 0.2270270270;

            color += SAMPLE_TEXTURE2D(
                _MainTex,
                sampler_MainTex,
                uv + offset * 1.3846153846
            ) * 0.3162162162;

            color += SAMPLE_TEXTURE2D(
                _MainTex,
                sampler_MainTex,
                uv - offset * 1.3846153846
            ) * 0.3162162162;

            color += SAMPLE_TEXTURE2D(
                _MainTex,
                sampler_MainTex,
                uv + offset * 3.2307692308
            ) * 0.0702702703;

            color += SAMPLE_TEXTURE2D(
                _MainTex,
                sampler_MainTex,
                uv - offset * 3.2307692308
            ) * 0.0702702703;

            return color;
        }

        ENDHLSL

        // Pass 0: Horizontal Blur
        Pass
        {
            Name "Horizontal Blur"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment FragHorizontal

            half4 FragHorizontal(Varyings input) : SV_Target
            {
                return SampleBlur(input.uv, float2(1, 0));
            }

            ENDHLSL
        }

        // Pass 1: Vertical Blur
        Pass
        {
            Name "Vertical Blur"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment FragVertical

            half4 FragVertical(Varyings input) : SV_Target
            {
                return SampleBlur(input.uv, float2(0, 1));
            }

            ENDHLSL
        }
    }
}