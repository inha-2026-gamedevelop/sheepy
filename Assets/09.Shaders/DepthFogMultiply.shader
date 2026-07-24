// 깊이감용 어두운 틴트 오버레이 (Multiply 블렌드)
// 배경 레이어와 캐릭터 레이어 사이에 깔면 뒤쪽이 뿌옇게 가라앉아 원근감이 생긴다.
Shader "Minsung/DepthFogMultiply"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mask Sprite", 2D) = "white" {}
        [MainColor] _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Depth Fog)]
        _FogColor ("Fog Color (어두운 남색)", Color) = (0.16, 0.20, 0.34, 1)
        _Strength ("Strength (0이면 무효과)", Range(0, 1)) = 0.45
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
        Blend DstColor Zero // 결과 = 화면색 * 출력색

        Pass
        {
            Name "DepthFogMultiply"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // _MainTex_ST는 넣지 않는다 - 2D SRP Batcher가 _ST/_TexelSize 프로퍼티를 지원하지 않아 배칭이 꺼진다
            CBUFFER_START(UnityPerMaterial)
                half4  _Color;
                half4  _FogColor;
                half   _Strength;
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

            half4 Frag(Varyings i) : SV_Target
            {
                // 스프라이트 알파 * SpriteRenderer 컬러 알파를 마스크로 써서 부분 적용도 가능하게
                half mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a * i.color.a * _Color.a;

                // 마스크가 0인 곳은 곱셈 항등원(흰색)이라 화면이 그대로 통과한다
                half3 tint = lerp(half3(1, 1, 1), _FogColor.rgb * _Color.rgb * i.color.rgb, _Strength * mask);
                return half4(tint, 1);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
