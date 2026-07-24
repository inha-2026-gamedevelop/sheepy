// 보스 1페이즈 즉사 기믹 - 위험 레이저/안전구역 공용 물결형 연출 (PSP XMB 스타일)
// SpriteRenderer.color(알파)로 위험 스윕(진하게)과 안전구역(옅게)의 농도를 자연히 가른다 (Phase3LaserBeam과 동일 관례)
// 물결은 UV 대각선을 따라 흐르는 "한 줄기" 소프트 하이라이트 하나로 표현한다 (줄무늬/얼룩 없음)
Shader "Minsung/BossHazardFog"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mask Sprite", 2D) = "white" {}
        [MainColor] _Color ("Tint (SpriteRenderer.color 연동)", Color) = (1, 1, 1, 1)

        [Header(Fog)]
        _FogColor ("Fog Color", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Range(0, 2)) = 1.0
        _Coverage ("Coverage (하이라이트 없을 때의 최소 농도)", Range(0, 1)) = 0.55

        [Header(Shape)]
        _EdgeFade ("Edge Fade (테두리 전체 페이드 폭)", Range(0, 0.5)) = 0.3

        [Header(Safe_Zone_Map2)]
        _SafeBaseColor ("Safe Base (Deep Navy)", Color) = (0.055, 0.09, 0.16, 1)
        _SafeTintAmount ("Safe Color Tint", Range(0, 1)) = 0.42
        _SafeEdgeWidth ("Safe Border Width", Range(0.005, 0.2)) = 0.045
        _SafeModeThreshold ("Safe Alpha Threshold", Range(0, 1)) = 0.7

        [Header(Wave_PSP)]
        _WaveWidth ("Wave Band Sharpness (클수록 좁고 또렷)", Range(0.5, 8)) = 2.2
        _WaveSpeed ("Wave Flow Speed", Range(0, 3)) = 0.8
        _WaveStrength ("Wave Highlight Brightness", Range(0, 1)) = 0.55
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
                half   _Coverage;
                half   _EdgeFade;
                half4  _SafeBaseColor;
                half   _SafeTintAmount;
                half   _SafeEdgeWidth;
                half   _SafeModeThreshold;
                half   _WaveWidth;
                half   _WaveSpeed;
                half   _WaveStrength;
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
                half4 tint = i.color * _Color;

                // 사각형 테두리 비네트 - UV 중심 기준 거리
                half2 fromCenter = abs(i.uv - 0.5) * 2.0;
                half  edgeDist   = max(fromCenter.x, fromCenter.y);
                half  edge       = smoothstep(1.0, 1.0 - _EdgeFade, edgeDist);

                // PSP XMB 배경처럼 대각선을 따라 흐르는 소프트 하이라이트 "한 줄기"만 사용한다.
                // 여러 겹 사인파를 섞지 않고 가우시안 밴드 하나만 이동시켜 줄무늬/얼룩 없이 매끈하게 흐른다.
                half  diag   = i.uv.x + i.uv.y; // 0~2
                half  travel = frac(_Time.y * _WaveSpeed * 0.15) * 2.6 - 0.3; // -0.3~2.3 사이를 슬라이드, 화면 밖에서 순환
                half  d      = (diag - travel) * _WaveWidth;
                half  sheen  = saturate(exp(-(d * d)));

                half3 shadowTone    = tint.rgb * 0.55;
                half3 highlightTone = lerp(tint.rgb, half3(1, 1, 1), _WaveStrength);
                half3 rgb   = lerp(shadowTone, highlightTone, sheen) * _FogColor.rgb;
                // 위험 스윕 농도: 하이라이트가 없을 때도 Coverage만큼은 항상 보이고, 하이라이트 부분만 밝아진다 (반투명 유지)
                half  alpha = lerp(_Coverage, 1.0, sheen) * edge * mask * tint.a * _Intensity;

                // 안전구역은 낮은 SpriteRenderer 알파로 구분한다. 딥 네이비 장막 위에 같은 하이라이트 줄기로
                // 은은한 색 결을 흘려보낸다.
                half safeMode = 1.0 - step(_SafeModeThreshold, tint.a);
                half safeColorStrength = lerp(_SafeTintAmount * 0.6, _SafeTintAmount, sheen);
                half3 safeRgb = lerp(_SafeBaseColor.rgb, tint.rgb, safeColorStrength);
                safeRgb += tint.rgb * sheen * 0.18;

                half2 edgeUv     = min(i.uv, 1.0 - i.uv);
                half  nearestEdge = min(edgeUv.x, edgeUv.y);
                half  border      = 1.0 - smoothstep(0.0, _SafeEdgeWidth, nearestEdge);
                safeRgb += tint.rgb * border * 0.22;

                half safeAlpha = mask * tint.a * _Intensity * (0.32 + (sheen * 0.24) + (border * 0.24));

                rgb   = lerp(rgb, safeRgb, safeMode);
                alpha = lerp(alpha, safeAlpha, safeMode);

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
