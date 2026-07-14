Shader "Minsung/SheepyFog"
{
    Properties
    {
        [PerRendererData] _MainTex ("Background Texture", 2D) = "white" {}
        [MainColor] _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Base Tone)]
        _BaseTintColor ("Base Tint (Purple/Blue)", Color) = (0.45, 0.4, 0.7, 1)
        _BaseTintAmount ("Base Tint Amount", Range(0, 1)) = 0.25
        _Desaturation ("Desaturation", Range(0, 1)) = 0.3

        [Header(Fog)]
        _FogColor ("Fog Color", Color) = (0.55, 0.55, 0.85, 1)
        _FogDensity ("Fog Density", Range(0, 1)) = 0.35
        _FogVariance ("Fog Patchiness", Range(0, 1)) = 0.4
        _FogScale ("Fog Scale (world units)", Range(0.01, 2)) = 0.15
        _FogSpeed ("Fog Drift Speed", Vector) = (0.03, 0.015, 0, 0)
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
            Name "SheepyFogUnlit"
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
                half4  _BaseTintColor;
                half   _BaseTintAmount;
                half   _Desaturation;
                half4  _FogColor;
                half   _FogDensity;
                half   _FogVariance;
                half   _FogScale;
                half4  _FogSpeed;
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

            float FogNoise(float2 worldPos, float2 drift)
            {
                float2 p = worldPos * _FogScale + drift;
                float n = ValueNoise(p) * 0.6;
                n += ValueNoise(p * 2.3 - drift * 0.5) * 0.4;
                return n;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half4 tex  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 tint = i.color * _Color;
                half3 baseColor = tex.rgb * tint.rgb;

                half gray = dot(baseColor, half3(0.299, 0.587, 0.114));
                baseColor = lerp(baseColor, gray.xxx, _Desaturation);

                baseColor = lerp(baseColor, _BaseTintColor.rgb, _BaseTintAmount);

                float2 drift = _Time.y * _FogSpeed.xy;
                float noise  = FogNoise(i.positionWS, drift);
                half fogAmount = saturate(_FogDensity + (noise - 0.5) * _FogVariance);

                half3 finalColor = lerp(baseColor, _FogColor.rgb, fogAmount);

                return half4(finalColor, tex.a * tint.a);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
