Shader "Skybox/MatrixWireframe"
{
    Properties
    {
        [Header(Grid)]
        _GridColor ("Grid Color", Color) = (0.0, 0.8, 0.3, 1.0)
        _BackgroundColor ("Background Color", Color) = (0.01, 0.02, 0.01, 1.0)
        _GridSize ("Grid Cell Size", Range(0.005, 0.1)) = 0.03
        _GridThickness ("Grid Line Thickness", Range(0.001, 0.02)) = 0.003
        _GridBrightness ("Grid Brightness", Range(0.0, 1.0)) = 0.15

        [Header(Rain)]
        _RainSpeed ("Rain Speed", Range(0.1, 5.0)) = 1.5
        _RainDensity ("Rain Column Density", Range(5.0, 80.0)) = 40.0
        _RainBrightness ("Rain Brightness", Range(0.0, 2.0)) = 1.0
        _RainTrailLength ("Rain Trail Length", Range(0.05, 0.8)) = 0.35
        _RainCharSize ("Character Size", Range(0.002, 0.02)) = 0.008

        [Header(Glow)]
        _HorizonGlow ("Horizon Glow Intensity", Range(0.0, 1.0)) = 0.3
        _HorizonColor ("Horizon Glow Color", Color) = (0.0, 0.4, 0.15, 1.0)
        _VignetteStrength ("Top/Bottom Fade", Range(0.0, 2.0)) = 0.8

        [Header(Animation)]
        _PulseSpeed ("Pulse Speed", Range(0.0, 3.0)) = 0.5
        _PulseAmount ("Pulse Amount", Range(0.0, 0.5)) = 0.1
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 viewDir : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _GridColor;
                half4 _BackgroundColor;
                half _GridSize;
                half _GridThickness;
                half _GridBrightness;
                half _RainSpeed;
                half _RainDensity;
                half _RainBrightness;
                half _RainTrailLength;
                half _RainCharSize;
                half _HorizonGlow;
                half4 _HorizonColor;
                half _VignetteStrength;
                half _PulseSpeed;
                half _PulseAmount;
            CBUFFER_END

            float hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            static const uint GLYPHS[16] = {
                0x75557u, 0x62227u, 0x71247u, 0x71217u,
                0x11175u, 0x74217u, 0x74257u, 0x71111u,
                0x75257u, 0x71257u, 0x25752u, 0x65656u,
                0x74447u, 0x27572u, 0x74647u, 0x44647u
            };

            float sampleGlyph(float2 uv, uint glyphIndex)
            {
                uint glyph = GLYPHS[glyphIndex & 0xFu];
                int px = clamp((int)(uv.x * 4.0), 0, 3);
                int py = clamp((int)((1.0 - uv.y) * 5.0), 0, 4);
                return (float)((glyph >> (py * 4u + 3u - (uint)px)) & 1u);
            }

            float rainColumn(float2 uv, float columnId, float time)
            {
                float speed = (hash11(columnId * 7.31) * 0.5 + 0.75) * _RainSpeed;
                float offset = hash11(columnId * 13.73) * 100.0;
                float y = frac(uv.y * 0.5 + 0.5);

                float scroll = frac(time * speed * 0.15 + offset);
                float headPos = 1.0 - scroll;
                float dist = y - headPos;

                float trail = smoothstep(_RainTrailLength, 0.0, dist) * step(0.0, dist);
                float head = smoothstep(0.02, 0.0, abs(dist));

                return trail * 0.6 + head * 1.5;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.viewDir = v.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 dir = normalize(i.viewDir);
                float time = _Time.y;

                float2 sphereUV;
                sphereUV.x = atan2(dir.x, dir.z) / (2.0 * PI) + 0.5;
                sphereUV.y = asin(dir.y) / PI + 0.5;

                float pulse = 1.0 + sin(time * _PulseSpeed) * _PulseAmount;

                float2 gridUV = sphereUV / _GridSize;
                float2 gridFrac = abs(frac(gridUV) - 0.5);
                float gridLine = 1.0 - smoothstep(0.0, _GridThickness / _GridSize, min(gridFrac.x, gridFrac.y));
                half3 grid = _GridColor.rgb * gridLine * _GridBrightness * pulse;

                float columnWidth = 1.0 / _RainDensity;
                float columnX = floor(sphereUV.x / columnWidth);
                float localX = frac(sphereUV.x / columnWidth);
                float columnMask = smoothstep(0.0, 0.2, localX) * smoothstep(1.0, 0.8, localX);

                float rainVal = rainColumn(sphereUV, columnX, time)
                             + rainColumn(sphereUV, columnX, time * 1.3 + 50.0) * 0.5;
                rainVal *= columnMask;

                float2 charUV = sphereUV / _RainCharSize;
                float2 charCell = floor(charUV);
                float2 charFrac = frac(charUV);

                float2 glyphUV = (charFrac - 0.15) / 0.7;
                float inBounds = step(0.0, glyphUV.x) * step(glyphUV.x, 1.0)
                               * step(0.0, glyphUV.y) * step(glyphUV.y, 1.0);

                float cellRand = hash21(charCell);
                uint glyphIdx = (uint)(cellRand * 16.0 + floor(time * _RainSpeed * 2.5)) & 0xFu;
                float glyph = sampleGlyph(glyphUV, glyphIdx) * inBounds;

                float rainMasked = rainVal * lerp(0.08, 1.0, glyph);
                half3 rain = _GridColor.rgb * rainMasked * _RainBrightness;

                float horizonFactor = pow(1.0 - abs(dir.y), 3.0);
                half3 horizon = _HorizonColor.rgb * horizonFactor * _HorizonGlow;

                float vertFade = 1.0 - pow(abs(dir.y), _VignetteStrength);

                half3 color = _BackgroundColor.rgb;
                color += grid * vertFade;
                color += rain * vertFade;
                color += horizon;
                color *= sin(sphereUV.y * 800.0) * 0.03 + 0.97;

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
