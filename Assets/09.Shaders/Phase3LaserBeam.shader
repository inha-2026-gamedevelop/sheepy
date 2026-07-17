// 3페이즈 가로지르는 레이저 발사 연출용 에너지빔 쉐이더
// 경고(점멸)/발사 슬롯을 공유하므로 SpriteRenderer.color(alpha)로 강도가 자연히 갈린다
Shader "Minsung/Phase3LaserBeam"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [MainColor] _Color ("Tint (SpriteRenderer.color 연동)", Color) = (1, 1, 1, 1)

        [Header(Core)]
        [HDR] _CoreColor ("Core Color (HDR)", Color) = (2, 2, 2, 1)
        _CoreWidth ("Core Width", Range(0, 1)) = 0.22
        _CoreSoftness ("Core Softness", Range(0.01, 1)) = 0.35

        [Header(Glow)]
        _EdgeFalloff ("Edge Falloff", Range(0.5, 6)) = 2.2
        _Intensity ("Intensity", Range(0, 4)) = 1.6

        [Header(Flow)]
        _FlowSpeed ("Flow Speed (길이 방향 스크롤)", Float) = 6.0
        _FlowScale ("Flow Scale", Float) = 5.0
        _FlowStrength ("Flow Strength", Range(0, 1)) = 0.35

        [Header(Pulse)]
        _PulseSpeed ("Pulse Speed", Float) = 8.0
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.18

        [Header(Tip)]
        _TipFade ("Tip Fade (시작/끝 길이 방향 페이드)", Range(0, 0.5)) = 0.05
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
            Name "Phase3LaserBeam"
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
                half4  _CoreColor;
                half   _CoreWidth;
                half   _CoreSoftness;
                half   _EdgeFalloff;
                half   _Intensity;
                half   _FlowSpeed;
                half   _FlowScale;
                half   _FlowStrength;
                half   _PulseSpeed;
                half   _PulseAmount;
                half   _TipFade;
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

            half4 Frag(Varyings i) : SV_Target
            {
                half mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;

                // 두께 방향(UV.y) - 중심이 밝은 코어, 바깥은 부드러운 글로우로 페이드
                half distFromCenter = abs(i.uv.y - 0.5) * 2.0; // 0 = 중심, 1 = 가장자리
                half core = 1.0 - smoothstep(_CoreWidth, _CoreWidth + _CoreSoftness, distFromCenter);
                half glow = pow(saturate(1.0 - distFromCenter), _EdgeFalloff);

                // 길이 방향(UV.x) - 에너지가 흐르는 듯한 스크롤 패턴
                float flowUV  = i.uv.x * _FlowScale - _Time.y * _FlowSpeed;
                half  flow    = sin(flowUV) * sin(flowUV * 2.3 + 1.7);
                half  flowMul = lerp(1.0, 0.5 + 0.5 * flow, _FlowStrength);

                // 미세한 밝기 펄스
                half pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;

                // 시작/끝 tip 페이드 (판정용 콜라이더는 별개라 시각에만 영향)
                half tip = smoothstep(0.0, _TipFade, i.uv.x) * smoothstep(1.0, 1.0 - _TipFade, i.uv.x);

                half4 tint  = i.color * _Color;
                half3 rgb   = lerp(tint.rgb, _CoreColor.rgb, core) * flowMul * pulse;
                half  alpha = saturate(glow) * flowMul * tip * tint.a * _Intensity * mask;

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
