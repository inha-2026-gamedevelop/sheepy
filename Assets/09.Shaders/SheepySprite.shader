Shader "Minsung/SheepySprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [MainColor] _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Glow)]
        [HDR] _GlowColor ("Glow Color (HDR)", Color) = (1, 1, 1, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1.2

        [Header(Rim)]
        [HDR] _RimColor ("Rim Color (HDR)", Color) = (0.6, 0.85, 1, 1)
        _RimSize ("Rim Size (px)", Range(0, 8)) = 2
        _RimIntensity ("Rim Intensity", Range(0, 5)) = 1.5
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
            Name "SheepySpriteUnlit"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                half4  _Color;
                half4  _GlowColor;
                half   _GlowIntensity;
                half4  _RimColor;
                half   _RimSize;
                half   _RimIntensity;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR; // SpriteRenderer.color
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

            half4 Frag(Varyings i) : SV_Target
            {
                half4 tex  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 tint = i.color * _Color;
                half  a    = tex.a * tint.a;

                // 실루엣 가장자리 검출 - 주변 4방향 중 가장 투명한 픽셀과의 알파 차이가 곧 윤곽.
                float2 offset = _MainTex_TexelSize.xy * _RimSize;
                half aUp    = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0, offset.y)).a;
                half aDown  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - float2(0, offset.y)).a;
                half aLeft  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - float2(offset.x, 0)).a;
                half aRight = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(offset.x, 0)).a;
                half minNeighbor = min(min(aUp, aDown), min(aLeft, aRight));
                half edge = saturate((tex.a - minNeighbor) * 2.0h);

                half3 baseColor = tex.rgb * tint.rgb * _GlowColor.rgb * _GlowIntensity;
                half3 rimColor  = _RimColor.rgb * _RimIntensity * edge * tex.a;

                return half4(baseColor + rimColor, a);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
