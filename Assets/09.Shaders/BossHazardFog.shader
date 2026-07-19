// 보스 1페이즈 즉사 기믹 - 위험 레이저/안전구역 공용 안개형 연출
// SpriteRenderer.color(알파)로 위험 스윕(진하게)과 안전구역(옅게)의 농도를 자연히 가른다 (Phase3LaserBeam과 동일 관례)
Shader "Minsung/BossHazardFog"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mask Sprite", 2D) = "white" {}
        [MainColor] _Color ("Tint (SpriteRenderer.color 연동)", Color) = (1, 1, 1, 1)

        [Header(Fog)]
        _FogColor ("Fog Color", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Range(0, 2)) = 1.0
        _NoiseScale ("Noise Scale (world units)", Range(0.1, 8)) = 0.45
        _Coverage ("Coverage (안개 밀도)", Range(0, 1)) = 0.55
        _Softness ("Softness (뭉게뭉게 번지는 정도)", Range(0.05, 2)) = 0.9
        _ScrollSpeed ("Scroll Speed (xy)", Vector) = (0.06, 0.03, 0, 0)

        [Header(Shape)]
        _EdgeFade ("Edge Fade (테두리 전체 페이드 폭)", Range(0, 0.5)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "BossHazardFog"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma instancing_options renderinglayer
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                half4  _FogColor;
                half   _Intensity;
                half   _NoiseScale;
                half   _Coverage;
                half   _Softness;
                half4  _ScrollSpeed;
                half   _EdgeFade;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float2 positionWS : TEXCOORD1;
                half4  color      : COLOR;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(v.positionOS);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.uv         = TRANSFORM_TEX(v.uv, _MainTex);
                o.positionWS = positionWS.xy;
                o.color      = v.color;
                return o;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // 3옥타브 + 서로 다른 방향/속도로 흐르는 두 겹 - 뭉게뭉게 겹쳐 흐르는 안개 질감
            float Fbm(float2 p, float2 drift1, float2 drift2)
            {
                // Broad, slowly drifting clouds make the zone read as fog from a distance.
                float n  = ValueNoise(p * 0.55 + drift1 * 0.55) * 0.5;
                // Smaller layers keep the silhouette from looking like a flat transparent block.
                n       += ValueNoise(p * 1.7 - drift2) * 0.3;
                n       += ValueNoise(p * 4.1 + drift1 * 1.7) * 0.2;
                return n;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                half4 tint = i.color * _Color;

                float2 drift1 = _Time.y * _ScrollSpeed.xy;
                float2 drift2 = _Time.y * _ScrollSpeed.xy * half2(-0.8, 0.6);
                float  noise  = Fbm(i.positionWS * _NoiseScale, drift1, drift2);

                // smoothstep 대신 완만한 지수 커브로 - 뭉치기보다 부드럽게 번지는 농도 분포
                half density = saturate((noise - (1.0 - _Coverage)) / max(_Softness, 0.001) + 0.5);
                half fogAmount = density * density * (3.0 - 2.0 * density); // smootherstep

                // 사각형 전체 테두리를 둥글게 접어 뭉게구름처럼 - UV 중심 기준 거리로 비네트
                half2 fromCenter = abs(i.uv - 0.5) * 2.0; // 0=중심, 1=가장자리
                half  edgeDist   = max(fromCenter.x, fromCenter.y);
                half  edge       = smoothstep(1.0, 1.0 - _EdgeFade, edgeDist);

                half3 rgb   = _FogColor.rgb * tint.rgb;
                half  alpha = fogAmount * edge * mask * tint.a * _Intensity;

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
