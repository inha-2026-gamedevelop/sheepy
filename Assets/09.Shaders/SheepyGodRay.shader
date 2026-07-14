// 창문/천장 틈으로 쏟아지는 빛줄기 셰이더.

Shader "Minsung/SheepyGodRay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [MainColor] _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Ray)]
        [HDR] _RayColor ("Ray Color (HDR)", Color) = (0.75, 0.85, 1.3, 1)
        _Intensity ("Intensity", Range(0, 3)) = 0.8
        _LengthFade ("Length Fade (아래로 사그라드는 정도)", Range(0.5, 6)) = 2
        _EdgeSoftness ("Edge Softness (좌우 부드러움)", Range(0.05, 1)) = 0.6

        [Header(Shafts)]
        _ShaftScale ("Shaft Density (줄기 개수)", Range(1, 30)) = 8
        _ShaftStrength ("Shaft Strength (줄기 대비)", Range(0, 1)) = 0.5
        _ShaftSpeed ("Shaft Drift Speed", Range(0, 1)) = 0.06
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
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            Name "SheepyGodRay"
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
                half4  _RayColor;
                half   _Intensity;
                half   _LengthFade;
                half   _EdgeSoftness;
                half   _ShaftScale;
                half   _ShaftStrength;
                half   _ShaftSpeed;
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

            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                return frac(p * (p + p));
            }

            float ShaftNoise(float x)
            {
                float i = floor(x);
                float f = frac(x);
                float u = f * f * (3.0 - 2.0 * f);
                return lerp(Hash11(i), Hash11(i + 1.0), u);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;

                half lengthFade = pow(saturate(i.uv.y), _LengthFade);

                half fromCenter = abs(i.uv.x - 0.5) * 2.0;
                half edgeFade = 1.0 - smoothstep(1.0 - _EdgeSoftness, 1.0, fromCenter);

                float drift = _Time.y * _ShaftSpeed;
                half shafts = ShaftNoise(i.uv.x * _ShaftScale + drift)
                            * ShaftNoise(i.uv.x * _ShaftScale * 2.7 - drift * 1.3);
                half shaftMul = lerp(1.0, shafts * 1.6, _ShaftStrength);

                half strength = lengthFade * edgeFade * shaftMul * _Intensity;
                half4 tint = i.color * _Color;

                return half4(_RayColor.rgb * tint.rgb, strength * mask * tint.a * _RayColor.a);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
