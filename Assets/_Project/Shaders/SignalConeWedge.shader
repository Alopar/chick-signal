Shader "Project/SignalConeWedge"
{
    Properties
    {
        [Header(Base)]
        _BaseColor ("Base Color", Color) = (1, 0.86, 0.31, 0.55)
        _WaveColor ("Wave Highlight", Color) = (1, 1, 1, 1)
        [Header(Cone)]
        _HalfAngleRad ("Half Angle (rad)", Range(0.05, 1.57)) = 0.785
        _EdgeSoftRadians ("Edge Softness (rad)", Range(0.001, 0.2)) = 0.04
        [Header(Radial)]
        _RadialEdgeSoft ("Radial Edge Softness", Range(0.01, 0.45)) = 0.12
        _OuterEdgeFeather ("Outer Edge Smear (extra)", Range(0, 0.55)) = 0.28
        _RadialEdgeBleed ("Outer Fade Past Circle", Range(0, 0.2)) = 0.06
        _CenterGlow ("Center Glow", Range(0, 8)) = 2.2
        [Header(Waves)]
        _WaveBands ("Wave Bands", Range(1, 24)) = 9
        _WaveSpeed ("Wave Speed", Float) = 4
        _WaveMix ("Wave Mix", Range(0, 1)) = 0.55
        _WavePeakPower ("Wave Peak Sharpness", Range(1, 10)) = 3.5
        _WaveValleyAlpha ("Wave Valley Alpha", Range(0, 1)) = 0.22
        _RippleContrast ("Ripple Contrast", Range(0.2, 1)) = 0.85
        [Header(Global)]
        _AlphaScale ("Overall Alpha", Range(0.15, 1.5)) = 0.55
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

        // Как в URP 2D Sprite-Unlit-Default: без LightMode → SRPDefaultUnlit (иначе с 2D Renderer не рисуется).
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            Name "SignalCone"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #ifndef TAU
            #define TAU 6.28318530718
            #endif

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _WaveColor;
                float _HalfAngleRad;
                float _EdgeSoftRadians;
                float _RadialEdgeSoft;
                float _OuterEdgeFeather;
                float _RadialEdgeBleed;
                float _CenterGlow;
                float _WaveBands;
                float _WaveSpeed;
                float _WaveMix;
                float _WavePeakPower;
                float _WaveValleyAlpha;
                float _RippleContrast;
                float _AlphaScale;
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
                float2 c = (i.uv - 0.5) * 2.0;
                float r = length(c);
                float fadeStart = 1.0 - _RadialEdgeSoft - _OuterEdgeFeather;
                float fadeEnd = 1.0 + _RadialEdgeBleed;
                float disc = 1.0 - smoothstep(fadeStart, fadeEnd, r);
                if (disc <= 1e-4)
                    discard;

                float ang = atan2(c.y, c.x);
                float absAng = abs(ang);
                float wedge = 1.0 - smoothstep(_HalfAngleRad - _EdgeSoftRadians, _HalfAngleRad + _EdgeSoftRadians, absAng);

                // Фаза по r: кольца движутся от центра к краю (исходящий сигнал). Было (1-r) — волны шли внутрь.
                float waves = sin(r * _WaveBands * TAU - _Time.y * _WaveSpeed);
                float waveShade = waves * 0.5 + 0.5;
                waveShade = lerp(1.0, waveShade, _RippleContrast);
                float wavePeak = pow(saturate(waveShade), _WavePeakPower);

                float core = exp(-r * r * _CenterGlow);

                float3 baseRgb = _BaseColor.rgb * i.color.rgb;
                float3 waveRgb = _WaveColor.rgb * i.color.rgb;
                float3 lit = lerp(baseRgb, waveRgb, wavePeak * _WaveMix);
                lit = lit + baseRgb * core * 0.28;

                float alpha = _BaseColor.a * i.color.a * disc * wedge * _AlphaScale;
                alpha *= lerp(_WaveValleyAlpha, 1.0, wavePeak);

                return float4(lit, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
