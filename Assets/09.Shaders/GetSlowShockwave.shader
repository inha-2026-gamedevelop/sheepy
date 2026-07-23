// GetSlow 능력 획득 연출 초입에 재생되는 원형 파동파 쉐이더
// _Progress(0~1)를 스크립트에서 매 프레임 갱신해, 중심에서 바깥으로 퍼지는 동심원 파동이 확산되며 옅어지도록 만든다
Shader "Minsung/GetSlowShockwave"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _Color ("Wave Color", Color) = (0.55, 0.85, 1, 1)
        _RingCount ("Ring Count", Range(1, 12)) = 5
        _RingSharpness ("Ring Sharpness", Range(1, 32)) = 8
        _Progress ("Progress (0=start, 1=end)", Range(0, 1)) = 0
        _CoreGlow ("Core Glow Amount", Range(0, 2)) = 0.6
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
            Name "GetSlowShockwave"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half  _RingCount;
                half  _RingSharpness;
                half  _Progress;
                half  _CoreGlow;
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
                float2 centered = (i.uv - 0.5) * 2.0;
                float dist = length(centered);

                // 진행도(_Progress)만큼 바깥으로 퍼지는 경계 - 그 안쪽에서만 파동이 보인다
                float insideMask = 1.0 - smoothstep(_Progress, _Progress + 0.15, dist);
                float ringPhase  = (dist * _RingCount) - (_Progress * _RingCount * 1.6);
                float ring       = pow(saturate((sin(ringPhase * 3.14159265) * 0.5) + 0.5), _RingSharpness);

                float core = (1.0 - smoothstep(0.0, 0.35, dist)) * _CoreGlow;
                float overallFade = 1.0 - _Progress; // 확산될수록 전체적으로 옅어진다

                half alpha = saturate((ring * insideMask) + core) * overallFade * i.color.a;
                half3 rgb  = _Color.rgb * i.color.rgb;

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
