// 화면 균열 조각용 UI 셰이더 - RenderTexture를 그리되 중심에서 바깥으로 번지는 방사형 그레이스케일 와이프를 적용한다
// _WipeRadius를 0에서 키우면 흑백이 잉크처럼 퍼진다(가장자리 노이즈로 '물드는' 느낌)
Shader "Minsung/ScreenTearGrayWipe"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GrayAmount ("Gray Amount", Range(0,1)) = 0
        _WipeRadius ("Wipe Radius", Float) = 0
        _WipeFeather ("Wipe Feather", Float) = 0.12
        _EdgeNoise ("Edge Noise", Float) = 0.06
        _CenterUV ("Center UV (xy=center, z=aspect)", Vector) = (0.5,0.5,1.777,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float4 color : COLOR;
                float2 uv    : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _GrayAmount;
            float _WipeRadius;
            float _WipeFeather;
            float _EdgeNoise;
            float4 _CenterUV;

            // 저렴한 해시 노이즈 - 흑백 경계를 울퉁불퉁하게(잉크 번짐)
            float Hash(float2 p)
            {
                return frac(sin(dot(floor(p * 34.0), float2(12.9898, 78.233))) * 43758.5453);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;

                // 종횡비 보정한 중심 거리 - 원형으로 번지게
                float2 duv = i.uv - _CenterUV.xy;
                duv.x *= _CenterUV.z;
                float d = length(duv);
                d += (Hash(i.uv) - 0.5) * _EdgeNoise;

                float wipe = saturate((_WipeRadius - d) / max(_WipeFeather, 1e-4));
                float gray = dot(c.rgb, float3(0.299, 0.587, 0.114));
                c.rgb = lerp(c.rgb, float3(gray, gray, gray), wipe * _GrayAmount);
                return c;
            }
            ENDCG
        }
    }
}
