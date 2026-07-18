Shader "Minsung/OrbGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [MainColor] _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Glow)]
        [HDR] _GlowColor ("Glow Color", Color) = (0.15, 0.65, 1, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 8)) = 2.5
        _PulseSpeed ("Pulse Speed", Range(0, 8)) = 2
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.15
        _RimStart ("Halo Core Radius", Range(0, 1)) = 0.38
        _HaloOpacity ("Halo Opacity", Range(0, 1)) = 0.72
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "OrbGlow"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _GlowColor;
                half _GlowIntensity;
                half _PulseSpeed;
                half _PulseAmount;
                half _RimStart;
                half _HaloOpacity;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4 color       : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half4 color       : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv         = input.uv;
                output.color      = input.color;
                return output;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half4 textureColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half inputAlpha = i.color.a * _Color.a;
                half coreAlpha = textureColor.a * inputAlpha;
                half3 coreColor = i.color.rgb * _Color.rgb;

                float radial = saturate(length(i.uv - float2(0.5, 0.5)) * 2.0);
                half pulse = 1.0 + (sin(_Time.y * _PulseSpeed) * _PulseAmount);
                half haloMask = 1.0 - smoothstep(_RimStart, 1.0, radial);
                half haloAlpha = haloMask * _HaloOpacity * pulse * inputAlpha;
                half alpha = max(coreAlpha, haloAlpha);
                half3 glowColor = _GlowColor.rgb * _GlowIntensity * pulse;
                half3 color = lerp(glowColor, coreColor, saturate(coreAlpha));

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
