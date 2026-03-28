Shader "Custom/CyberGrid"
{
    Properties
    {
        _GridColor ("Grid Color", Color) = (0, 1, 1, 1)
        _GridSize ("Grid Size", Float) = 2.0
        _LineWidth ("Line Width", Float) = 0.02
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-10" "RenderPipeline"="UniversalPipeline" }
        ZWrite On
        Cull Off

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _GridColor;
                half _GridSize;
                half _LineWidth;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 posWS : TEXCOORD0; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.posWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.posWS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.posWS.xz / _GridSize;
                float2 wrapped = abs(frac(uv) - 0.5);
                float2 duvdx = abs(ddx(uv));
                float2 duvdy = abs(ddy(uv));
                float2 duv = max(duvdx, duvdy);
                float2 draw = smoothstep(duv * 0.5, duv * 1.5, wrapped);
                float grid = 1.0 - min(draw.x, draw.y);
                half3 col = _GridColor.rgb * grid * 0.12;
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}