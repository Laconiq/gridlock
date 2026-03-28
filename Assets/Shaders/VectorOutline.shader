Shader "Custom/VectorOutline"
{
    Properties
    {
        _Color ("Color", Color) = (0, 1, 1, 1)
        _EmissionIntensity ("Emission Intensity", Float) = 3.0
        _FillAlpha ("Fill Alpha", Range(0, 1)) = 0.05
        _EdgeWidth ("Edge Width", Range(0, 0.5)) = 0.08
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 200
        ZWrite On
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _EmissionIntensity;
                half _FillAlpha;
                half _EdgeWidth;
                half _Smoothness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half shadow = mainLight.shadowAttenuation;

                float2 uv = input.uv;
                float edgeX = min(uv.x, 1.0 - uv.x);
                float edgeY = min(uv.y, 1.0 - uv.y);
                float edge = min(edgeX, edgeY);
                float outline = smoothstep(0.0, _EdgeWidth, edge);

                half3 outlineEmission = _Color.rgb * _EmissionIntensity;
                half3 fillCol = _Color.rgb * _FillAlpha;

                half3 baseColor = lerp(outlineEmission, fillCol, outline);
                half3 lit = baseColor * (0.15 + mainLight.color * NdotL * shadow);
                half3 emission = lerp(outlineEmission, half3(0,0,0), outline);

                half3 col = lit + emission;
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output;
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, _LightDirection));
                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
