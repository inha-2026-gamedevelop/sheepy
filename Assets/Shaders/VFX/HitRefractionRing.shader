Shader "SoundWave/VFX/HitRefractionRing"
{
    Properties
    {
        _DistortionStrength ("Distortion Strength", Range(0, 0.2)) = 0.05
        _NoiseScale ("Noise Scale", Float) = 10
        _FadeRadius ("Edge Fade (Softness)", Range(0.001, 0.5)) = 0.05
        _RingWidth ("Ring Width", Range(0.01, 1)) = 0.15
        _NoiseSpeed ("Noise Scroll Speed", Float) = 1.0
        _Radius ("Ring Radius (0-1, for grow animation)", Range(0, 1)) = 1.0
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
            Name "HitRefractionRing"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // 2D 게임은 대부분의 오브젝트(배경/몬스터)가 Transparent 큐라 _CameraOpaqueTexture가
            // 비어있다. URP 2D Renderer가 제공하는 Camera Sorting Layer Texture를 대신 사용한다.
            // (Renderer2D.asset에서 Use Camera Sorting Layers Texture 활성화 + Bound 레이어 설정 필요)
            TEXTURE2D(_CameraSortingLayerTexture);
            SAMPLER(sampler_CameraSortingLayerTexture);

            CBUFFER_START(UnityPerMaterial)
                float _DistortionStrength;
                float _NoiseScale;
                float _FadeRadius;
                float _RingWidth;
                float _NoiseSpeed;
                float _Radius;
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

            // 간단한 해시 기반 노이즈 (텍스처 샘플링 없이 처리, 텍스처 의존성 제거)
            float2 Hash2D(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = dot(Hash2D(i), f);
                float b = dot(Hash2D(i + float2(1, 0)), f - float2(1, 0));
                float c = dot(Hash2D(i + float2(0, 1)), f - float2(0, 1));
                float d = dot(Hash2D(i + float2(1, 1)), f - float2(1, 1));
                float2 lerped = lerp(float2(a, b), float2(c, d), u.y);
                return lerp(lerped.x, lerped.y, u.x);
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                // UV 중심 기준 -1~1 좌표계로 변환 (원형 마스크용)
                float2 centered = (IN.uv - 0.5) * 2.0;
                float dist = length(centered);

                // 링 마스크: _Radius를 중심으로 RingWidth 두께의 밴드를 만들고 FadeRadius로 경계를 부드럽게 처리
                float outerEdge = saturate(_Radius + _RingWidth * 0.5);
                float innerEdge = saturate(_Radius - _RingWidth * 0.5);
                float outerMask = smoothstep(outerEdge, outerEdge - _FadeRadius, dist);
                float innerMask = smoothstep(innerEdge - _FadeRadius, innerEdge, dist);
                float ringMask = saturate(outerMask * innerMask);

                // 노이즈 기반 왜곡 오프셋 (링 영역에만 강하게 적용)
                float2 noiseUV = IN.uv * _NoiseScale + _Time.y * _NoiseSpeed;
                float n1 = Noise(noiseUV);
                float n2 = Noise(noiseUV + float2(5.2, 1.3));
                float2 distortOffset = float2(n1, n2) * _DistortionStrength * ringMask;

                // 화면 공간 UV에 왜곡 적용 후 Camera Sorting Layer Texture 샘플링
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                screenUV += distortOffset;
                half3 sceneColor = SAMPLE_TEXTURE2D(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture, screenUV).rgb;

                half alpha = ringMask * _Color.a;
                return half4(sceneColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
