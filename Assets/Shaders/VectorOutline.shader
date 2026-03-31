Shader "Custom/VectorOutline"
{
    Properties
    {
        _Color ("Color", Color) = (0, 1, 1, 1)
        _EmissionIntensity ("Emission Intensity", Float) = 3.0
        _EdgeWidth ("Edge Width Pixels", Float) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        ZWrite On
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 bary : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 bary : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _EmissionIntensity;
                half _EdgeWidth;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.bary = input.bary;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 bary = input.bary;
                float3 fw = fwidth(bary);
                float3 edge3 = smoothstep(fw * _EdgeWidth, fw * (_EdgeWidth + 1.0), bary);
                float edge = min(edge3.x, min(edge3.y, edge3.z));
                float outline = 1.0 - edge;

                half3 col = _Color.rgb * _EmissionIntensity * outline;
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

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings ShadowVert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }

            half4 ShadowFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
