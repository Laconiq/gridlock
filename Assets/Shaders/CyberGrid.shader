Shader "Custom/CyberGrid"
{
    Properties
    {
        _GridColor ("Grid Color", Color) = (0, 1, 1, 1)
        _GridSize ("Grid Size", Float) = 2.0
        _CellMap ("Cell Map", 2D) = "black" {}
        _CellFill ("Cell Fill Strength", Range(0, 1)) = 0.03
        _GridOrigin ("Grid Origin XZ", Vector) = (-24, -14, 0, 0)
        _GridExtent ("Grid Extent XZ", Vector) = (48, 28, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
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
                float4 _GridOrigin;
                float4 _GridExtent;
                half _CellFill;
            CBUFFER_END

            TEXTURE2D(_CellMap);
            SamplerState sampler_point_clamp;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 posWS : TEXCOORD0;
                half4 vertColor : TEXCOORD1;
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.posWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.posWS);
                o.vertColor = i.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Grid lines from deformed world positions
                float2 uv = i.posWS.xz / _GridSize;
                float2 wrapped = abs(frac(uv) - 0.5);
                float2 duvdx = abs(ddx(uv));
                float2 duvdy = abs(ddy(uv));
                float2 duv = max(duvdx, duvdy);
                float2 draw = smoothstep(duv * 0.5, duv * 1.5, wrapped);
                float grid = 1.0 - min(draw.x, draw.y);

                // Vertex color glow intensity
                half glow = saturate(dot(i.vertColor.rgb, half3(0.33, 0.33, 0.33)) * 3.0);

                // Line color: blend from default cyan toward vertex color
                half3 lineColor = lerp(_GridColor.rgb, i.vertColor.rgb, glow * 0.85);
                float lineBrightness = 0.12 + glow * 0.5;

                // Cell map overlay
                half3 fill = half3(0, 0, 0);
                float fillAlpha = 0;
                float2 cellUV = (i.posWS.xz - _GridOrigin.xy) / _GridExtent.xy;

                if (cellUV.x > 0 && cellUV.x < 1 && cellUV.y > 0 && cellUV.y < 1)
                {
                    half4 cell = SAMPLE_TEXTURE2D(_CellMap, sampler_point_clamp, cellUV);
                    if (cell.a > 0.01)
                    {
                        lineColor = lerp(lineColor, cell.rgb, cell.a * (1.0 - glow));
                        float2 cellLocal = frac(uv);
                        float inset = min(min(cellLocal.x, 1.0 - cellLocal.x),
                                          min(cellLocal.y, 1.0 - cellLocal.y));
                        fill = cell.rgb * _CellFill;
                        fillAlpha = _CellFill * smoothstep(0.05, 0.2, inset) * cell.a;
                    }
                }

                // Ripple glow between lines
                half rippleGlow = glow * 0.05;

                half3 col = lineColor * grid * lineBrightness
                          + fill * fillAlpha
                          + i.vertColor.rgb * rippleGlow;

                half alpha = saturate(grid * lineBrightness * 8.0 + fillAlpha + rippleGlow);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
