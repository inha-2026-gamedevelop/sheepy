Shader "SoundWave/VFX/ContinuousRefractionRipple"
{
    Properties
    {
        _DistortionStrength ("Distortion Strength", Range(0, 1.0)) = 0.05
        _WaveFrequency ("Wave Frequency", Float) = 8.0
        _WaveSpeed ("Wave Speed", Float) = 3.0
        _Radius ("Mask Radius", Range(0, 1)) = 1.0
        _EdgeFade ("Edge Fade", Range(0.001, 0.5)) = 0.2
        _Color ("Tint / Fade Alpha", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "ContinuousRefractionRipple"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_CameraSortingLayerTexture);
            SAMPLER(sampler_CameraSortingLayerTexture);

            CBUFFER_START(UnityPerMaterial)
                float _DistortionStrength;
                float _WaveFrequency;
                float _WaveSpeed;
                float _Radius;
                float _EdgeFade;
                float4 _Color;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 screenPos   : TEXCOORD1;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 centered = (IN.uv - 0.5) * 2.0;
                float dist = length(centered);

                // 반경 마스크 (점진적으로 페이드 아웃)
                float mask = 1.0 - smoothstep(_Radius - _EdgeFade, _Radius, dist);
                // 중심부 마스크 (중심에서 물결이 시작되는 것처럼 자연스럽게 페이드인)
                mask *= smoothstep(0.0, 0.1, dist);

                // 파동 (사인파)
                // 거리에 따라 파동을 주고, 시간에 따라 퍼져나가게 (- _Time.y)
                float wave = sin((dist * _WaveFrequency - _Time.y * _WaveSpeed) * 6.2831853);

                // 방향 벡터에 파동 곱하기 (0으로 나누기 방지)
                float2 dir = dist > 0.0001 ? (centered / dist) : float2(0, 0);
                float2 distortOffset = dir * wave * _DistortionStrength * mask;

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                screenUV += distortOffset;
                half3 sceneColor = SAMPLE_TEXTURE2D(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture, screenUV).rgb;

                half alpha = mask * _Color.a;
                return half4(sceneColor, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
