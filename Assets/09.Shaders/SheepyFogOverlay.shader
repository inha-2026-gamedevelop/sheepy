// 안개
Shader "Minsung/SheepyFogOverlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mask Sprite", 2D) = "white" {}
        [MainColor] _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Fog)]
        _FogColor ("Fog Color", Color) = (0.45, 0.45, 0.75, 1)
        _Intensity ("Intensity", Range(0, 2)) = 0.5
        _NoiseScale ("Noise Scale (world units)", Range(0.01, 2)) = 0.12
        _Coverage ("Coverage (안개 차지 비율)", Range(0, 1)) = 0.55
        _Softness ("Softness (뭉침 경계 부드러움)", Range(0.05, 1)) = 0.45
        _ScrollSpeed ("Scroll Speed (xy)", Vector) = (0.04, 0.012, 0, 0)
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
        Blend OneMinusDstColor One

        Pass
        {
            Name "SheepyFogOverlay"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

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

            float Fbm(float2 p)
            {
                float n = ValueNoise(p) * 0.5;
                n += ValueNoise(p * 2.1 + 17.3) * 0.3;
                n += ValueNoise(p * 4.7 - 8.1) * 0.2;
                return n;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a * i.color.a * _Color.a;

                // 두 겹의 안개가 서로 다른 속도로 교차
                float2 drift1 = _Time.y * _ScrollSpeed.xy;
                float2 drift2 = _Time.y * _ScrollSpeed.xy * float2(-0.6, 0.4);
                float noise = Fbm(i.positionWS * _NoiseScale + drift1) * 0.65
                            + Fbm(i.positionWS * _NoiseScale * 1.8 + drift2) * 0.35;

                half fog = smoothstep(1.0 - _Coverage - _Softness, 1.0 - _Coverage + _Softness, noise);

                half3 color = _FogColor.rgb * _Color.rgb * i.color.rgb * fog * _Intensity * mask;
                return half4(color, 1);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
