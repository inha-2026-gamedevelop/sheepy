Shader "Minsung/OrbTrail"
{
    Properties
    {
        [HDR] _TintColor ("Tint Color", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Range(0, 8)) = 1.5
        _PulseSpeed ("Pulse Speed", Range(0, 16)) = 8
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            Name "OrbTrail"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TintColor;
                half _Intensity;
                half _PulseSpeed;
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

            half4 Frag(Varyings input) : SV_Target
            {
                half pulse = 0.8 + (sin((_Time.y * _PulseSpeed) + (input.uv.x * _PulseSpeed)) * 0.2);
                half4 color = input.color * _TintColor;
                color.rgb *= _Intensity * pulse;

                return color;
            }
            ENDHLSL
        }
    }
}
