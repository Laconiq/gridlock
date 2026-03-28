Shader "Custom/VectorOutline"
{
    Properties
    {
        _Color ("Color", Color) = (0, 1, 1, 1)
        _EmissionIntensity ("Emission Intensity", Float) = 3.0
        _FillAlpha ("Fill Alpha", Range(0, 1)) = 0.05
        _EdgeWidth ("Edge Width", Range(0, 0.5)) = 0.08
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        ZWrite On
        Cull Off

        Pass
        {
            Name "VectorOutline"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _EmissionIntensity;
                half _FillAlpha;
                half _EdgeWidth;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float edgeX = min(uv.x, 1.0 - uv.x);
                float edgeY = min(uv.y, 1.0 - uv.y);
                float edge = min(edgeX, edgeY);
                float outline = smoothstep(0.0, _EdgeWidth, edge);
                
                half3 outlineCol = _Color.rgb * _EmissionIntensity;
                half3 fillCol = _Color.rgb * _FillAlpha;
                half3 col = lerp(outlineCol, fillCol, outline);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}