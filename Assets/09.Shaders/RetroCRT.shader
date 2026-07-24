Shader "Minsung/UI/RetroCRT"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _CRTTint ("CRT Tint", Color) = (0.35, 0.55, 0.75, 0.08)
        _ScanlineIntensity ("Scanline Intensity", Range(0,1)) = 0.18
        _ScanlineCount ("Scanline Count", Float) = 540
        _Vignette ("Vignette", Range(0,1)) = 0.22
        _Noise ("Noise", Range(0,1)) = 0.035
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                float4 _CRTTint;
                float _ScanlineIntensity, _ScanlineCount, _Vignette, _Noise;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            half4 frag(Varyings input) : SV_Target
            {
                float2 centered = input.uv * 2.0 - 1.0;
                float edge = saturate(dot(centered, centered));
                half scan = sin(input.uv.y * _ScanlineCount * 6.2831853) * 0.5 + 0.5;
                half noise = hash21(input.uv * (_ScanlineCount * 0.03) + _Time.y) - 0.5;
                half alpha = _CRTTint.a * saturate(1.0 + edge * _Vignette + scan * _ScanlineIntensity + noise * _Noise);
                return half4(_CRTTint.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
