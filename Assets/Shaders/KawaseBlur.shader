Shader "Hidden/Gridlock/DualKawaseBlur"
{
    Properties
    {
        _MainTex ("", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        ENDHLSL

        Pass
        {
            Name "DualKawaseDown"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDown

            half4 FragDown(Varyings i) : SV_Target
            {
                float4 o = _BlitTexture_TexelSize.xyxy * float4(-1, -1, 1, 1);
                half4 c = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord) * 4.0;
                c += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord + o.xy);
                c += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord + o.xw);
                c += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord + o.zy);
                c += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord + o.zw);
                return c * 0.125;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DualKawaseUp"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragUp

            half4 FragUp(Varyings i) : SV_Target
            {
                float4 o = _BlitTexture_TexelSize.xyxy * float4(-1, -1, 1, 1);
                half4 c = 0;
                c += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord + float2(o.x, 0));
                c += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord + float2(o.z, 0));
                c += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord + float2(0, o.y));
                c += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord + float2(0, o.w));
                c += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord + o.xy * 0.5) * 2.0;
                c += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord + o.xw * 0.5) * 2.0;
                c += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord + o.zy * 0.5) * 2.0;
                c += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord + o.zw * 0.5) * 2.0;
                return c / 12.0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Darken"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDarken

            half _Darken;

            half4 FragDarken(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord);
                c.rgb *= (1.0 - _Darken);
                return c;
            }
            ENDHLSL
        }
    }
}
