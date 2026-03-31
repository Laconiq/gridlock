Shader "Custom/NeonProjectile"
{
    Properties
    {
        _Color0 ("Element Color 0", Color) = (0, 1, 1, 1)
        _Color1 ("Element Color 1", Color) = (0, 1, 1, 1)
        _Color2 ("Element Color 2", Color) = (0, 1, 1, 1)
        _Color3 ("Element Color 3", Color) = (0, 1, 1, 1)
        _ColorCount ("Active Colors", Range(1, 4)) = 1
        _EmissionIntensity ("Emission Intensity", Float) = 5.0
        _RimPower ("Rim Power", Float) = 1.5
        _RimWidth ("Rim Width", Range(0.1, 1.0)) = 0.85
        _RimIntensity ("Rim Intensity Multiplier", Float) = 1.5
        _PulseSpeed ("Pulse Speed", Float) = 0.0
        _PulseMin ("Pulse Min", Range(0, 1)) = 0.7
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+100" "RenderPipeline"="UniversalPipeline" }
        ZWrite On
        Cull Back

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color0, _Color1, _Color2, _Color3;
                half _ColorCount;
                half _EmissionIntensity;
                half _RimPower;
                half _RimWidth;
                half _RimIntensity;
                half _PulseSpeed;
                half _PulseMin;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(posWS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.viewDirWS = GetWorldSpaceNormalizeViewDir(posWS);
                o.positionOS = input.positionOS.xyz;
                return o;
            }

            half3 BlendElementColors(half t)
            {
                int count = (int)_ColorCount;
                if (count <= 1) return _Color0.rgb;

                half4 colors[4] = { _Color0, _Color1, _Color2, _Color3 };
                float scaled = t * count;
                int idx = (int)scaled % count;
                int next = (idx + 1) % count;
                return lerp(colors[idx].rgb, colors[next].rgb, frac(scaled));
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);
                float NdotV = saturate(dot(normalWS, viewDir));

                // Rim only — center stays black, rim glows with smooth fade inward
                float rim = 1.0 - NdotV;
                float rimMask = smoothstep(1.0 - _RimWidth, 1.0, rim);
                rimMask = pow(rimMask, _RimPower);

                float colorT = dot(normalize(input.positionOS), float3(0.5, 0.7, 0.3)) * 0.5 + 0.5;
                colorT += _Time.y * 0.12;
                half3 elemColor = BlendElementColors(colorT);

                float pulse = 1.0;
                if (_PulseSpeed > 0.001)
                    pulse = lerp(_PulseMin, 1.0, sin(_Time.y * _PulseSpeed) * 0.5 + 0.5);

                half3 col = elemColor * rimMask * _EmissionIntensity * _RimIntensity * pulse;
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
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
