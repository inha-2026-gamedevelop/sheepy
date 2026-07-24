// 4페이즈 진입 연출 - 보스 뒤에서 피어오르는 보라색 오라 파티클 쉐이더
// 파티클 기본 원형 스프라이트를 마스크로 쓰고, 노이즈로 반경을 흔들어 몽글몽글 뭉게구름처럼 피어오르는 윤곽을 만든다
Shader "Minsung/Phase4Aura"
{
    Properties
    {
        [PerRendererData] _MainTex ("Particle Sprite", 2D) = "white" {}
        [MainColor] _Color ("Tint (ParticleSystem 색 연동)", Color) = (1, 1, 1, 1)

        [Header(AuraColors)]
        [HDR] _ColorA ("Color A (Core)", Color) = (0.85, 0.55, 1.0, 1)
        [HDR] _ColorB ("Color B (Mid)", Color) = (0.55, 0.22, 0.85, 1)
        [HDR] _ColorC ("Color C (Edge)", Color) = (0.30, 0.05, 0.55, 1)

        // 몽글몽글 피어오르는 뭉게구름 형태 제어
        [Header(Billow)]
        _NoiseScale ("Noise Scale", Range(1, 20)) = 5.0
        _NoiseSpeed ("Noise Rise Speed", Range(0, 4)) = 0.5
        _Softness ("Edge Softness", Range(0.05, 1)) = 0.6

        [Header(Glow)]
        _Intensity ("Glow Intensity", Range(0, 6)) = 2.2
        _PulseSpeed ("Pulse Speed", Range(0, 8)) = 1.4
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.12
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
        Blend SrcAlpha One

        Pass
        {
            Name "Phase4Aura"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _ColorA;
                half4 _ColorB;
                half4 _ColorC;
                half  _NoiseScale;
                half  _NoiseSpeed;
                half  _Softness;
                half  _Intensity;
                half  _PulseSpeed;
                half  _PulseAmount;
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
                o.uv         = v.uv;
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

            // 서로 다른 속도로 위로 흘려보내는 두 옥타브 - 뭉게뭉게 피어오르는 질감(Billow)
            float Billow(float2 p, float time)
            {
                float2 rise1 = float2(0.0, time * _NoiseSpeed);
                float2 rise2 = float2(0.0, time * _NoiseSpeed * 1.7);
                float n  = ValueNoise(p + rise1) * 0.6;
                n       += ValueNoise((p * 2.3) - rise2) * 0.4;
                return n;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                half4 tint = i.color * _Color;

                float billow = Billow(i.uv * _NoiseScale, _Time.y);

                // 중심 -> 가장자리 반경(원형) - 노이즈로 흔들어 완전한 원이 아닌 뭉실뭉실한 윤곽으로
                float radial = saturate(length(i.uv - float2(0.5, 0.5)) * 2.0);
                float billowedRadial = saturate(radial + ((billow - 0.5) * _Softness));
                half shapeMask = 1.0 - smoothstep(1.0 - _Softness, 1.0, billowedRadial);

                // 3색을 노이즈/반경 기준으로 부드럽게 혼합 (중심=A, 중간=B, 가장자리=C 경향)
                half3 colorAB  = lerp(_ColorA.rgb, _ColorB.rgb, saturate(billow * 1.4));
                half3 colorMix = lerp(colorAB, _ColorC.rgb, saturate((radial * 0.9) + ((billow - 0.5) * 0.4)));

                half pulse = 1.0 + (sin(_Time.y * _PulseSpeed) * _PulseAmount);

                half3 rgb   = colorMix * tint.rgb * _Intensity * pulse;
                half  alpha = mask * shapeMask * tint.a * pulse;

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
