// 로고 전용 - 외곽 글로우 + 위로 차오르는 물방울/빛 시머
Shader "Minsung/LogoShimmer"
{
    Properties
    {
        [PerRendererData] _MainTex ("Logo Sprite", 2D) = "white" {}
        [MainColor] _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Outer Glow)]
        [HDR] _GlowColor ("Glow Color (HDR)", Color) = (0.55, 0.8, 1.3, 1)
        _GlowRadius ("Glow Radius (uv)", Range(0.001, 0.08)) = 0.02
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 1

        [Header(Rising Shimmer)]
        [HDR] _ShimmerColor ("Shimmer Color (HDR)", Color) = (0.75, 0.95, 1.4, 1)
        _ShimmerSpeed ("Rise Speed", Range(0, 2)) = 0.35
        _ShimmerScaleX ("Column Scale (x)", Range(1, 40)) = 10
        _ShimmerScaleY ("Column Scale (y)", Range(1, 20)) = 4
        _ShimmerSharpness ("Column Sharpness", Range(1, 8)) = 3
        _ShimmerIntensity ("Shimmer Intensity", Range(0, 2)) = 0.8
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
            Name "LogoShimmer"
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
                half4  _GlowColor;
                half   _GlowRadius;
                half   _GlowIntensity;
                half4  _ShimmerColor;
                half   _ShimmerSpeed;
                half   _ShimmerScaleX;
                half   _ShimmerScaleY;
                half   _ShimmerSharpness;
                half   _ShimmerIntensity;
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
                half4  color      : COLOR;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv         = TRANSFORM_TEX(v.uv, _MainTex);
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

            // 8방향 링 샘플로 알파를 바깥으로 번지게 해 값싼 아웃라인 글로우를 만든다
            half SampleGlow(float2 uv, float radius)
            {
                const int TAPS = 8;
                const float2 dirs[TAPS] = {
                    float2( 1,  0), float2(-1,  0), float2( 0,  1), float2( 0, -1),
                    float2( 0.7071,  0.7071), float2(-0.7071,  0.7071),
                    float2( 0.7071, -0.7071), float2(-0.7071, -0.7071)
                };

                half maxAlpha = 0;
                UNITY_UNROLL
                for (int i = 0; i < TAPS; ++i)
                {
                    float2 offset1 = dirs[i] * radius;
                    float2 offset2 = dirs[i] * radius * 0.5;
                    maxAlpha = max(maxAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset1).a);
                    maxAlpha = max(maxAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset2).a);
                }
                return maxAlpha;
            }

            // 세로로 솟아오르는 물방울/빛 기둥 패턴
            half RisingShimmer(float2 uv)
            {
                float drift = _Time.y * _ShimmerSpeed;
                float2 p = float2(uv.x * _ShimmerScaleX, uv.y * _ShimmerScaleY - drift);

                float n = ValueNoise(p) * 0.65 + ValueNoise(p * 1.9 + 11.7) * 0.35;
                float column = pow(saturate(n), _ShimmerSharpness);

                // 살짝 더 느린 2겹째로 두께감 추가
                float2 p2 = float2(uv.x * _ShimmerScaleX * 1.6 + 3.1, uv.y * _ShimmerScaleY * 0.7 - drift * 0.6);
                float n2 = ValueNoise(p2);
                column += pow(saturate(n2), _ShimmerSharpness) * 0.5;

                return saturate(column);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half4 tex  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 tint = i.color * _Color;
                half  mask = tex.a;

                half glowMask = SampleGlow(i.uv, _GlowRadius);
                half glowOuter = saturate(glowMask - mask) * _GlowIntensity;

                half shimmer = RisingShimmer(i.uv) * _ShimmerIntensity;

                half3 baseColor = tex.rgb * tint.rgb;
                baseColor = lerp(baseColor, _ShimmerColor.rgb * tint.rgb, shimmer * mask);

                half3 glowColor = _GlowColor.rgb * tint.rgb * (0.6 + shimmer * 0.8);

                half3 finalColor = baseColor * mask + glowColor * glowOuter;
                half  finalAlpha = saturate(mask + glowOuter) * tint.a;

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
