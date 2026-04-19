Shader "Project/SpriteDashEcho"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [Header(Dash FX)]
        _DashIntensity ("Dash Intensity", Range(0, 1)) = 0
        _DashDir ("Dash Dir (UV)", Vector) = (1, 0, 0, 0)
        _EchoSpread ("Echo Spread (UV)", Range(0, 0.85)) = 0.12
        _EchoStepScale ("Echo Step (trail length)", Range(0.08, 0.65)) = 0.28
        _EchoStrength ("Echo Strength", Range(0, 2)) = 0.85
        _EchoTint ("Echo Tint", Color) = (0.55, 0.92, 1, 1)
        _ChromaticAmt ("Chromatic Mix", Range(0, 1)) = 0.35
        _ChromaticSpread ("Chromatic Spread (UV)", Range(0, 0.06)) = 0.014
        _Glow ("Glow Add", Range(0, 1.2)) = 0.18
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            Name "SpriteDashEcho"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _DashDir;
                float _DashIntensity;
                float _EchoSpread;
                float _EchoStepScale;
                float _EchoStrength;
                float4 _EchoTint;
                float _ChromaticAmt;
                float _ChromaticSpread;
                float _Glow;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                float4 baseCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Color * i.color;
                float dash = saturate(_DashIntensity);

                if (dash < 0.001)
                    return baseCol;

                float2 dir = _DashDir.xy;
                float lenDir = length(dir);
                dir = lenDir > 1e-4 ? (dir / lenDir) : float2(1, 0);
                float2 smear = dir * _EchoSpread;

                float3 rgb = baseCol.rgb;
                float a = baseCol.a;

                float3 ghost = float3(0, 0, 0);
                for (int k = 1; k <= 4; k++)
                {
                    float fk = (float)k;
                    float2 uvg = uv - smear * fk * _EchoStepScale;
                    float4 ck = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvg) * _Color * i.color;
                    float wgt = exp(-fk * 0.62) * 0.42;
                    ghost += ck.rgb * ck.a * wgt;
                }
                rgb += ghost * _EchoTint.rgb * dash * _EchoStrength;

                float2 cOff = dir * _ChromaticSpread * dash * _ChromaticAmt;
                float r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + cOff).r;
                float g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).g;
                float b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - cOff).b;
                float3 tint = float3(r, g, b) * _Color.rgb * i.color.rgb;
                rgb = lerp(rgb, tint, dash * _ChromaticAmt * 0.55);

                rgb += dash * _Glow * a;

                return float4(rgb, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
