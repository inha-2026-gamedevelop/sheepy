Shader "Custom/WavyGimmick"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _WaveSpeed ("Wave Speed", Float) = 3.0
        _WaveFreq ("Wave Frequency", Float) = 8.0
        _WaveAmp ("Wave Amplitude", Float) = 0.03
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };
            
            fixed4 _Color;
            float _WaveSpeed;
            float _WaveFreq;
            float _WaveAmp;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            sampler2D _MainTex;

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                
                // 시간 (속도 조절)
                float time = _Time.y * _WaveSpeed; 
                
                // 가로(X축) 좌표에 사인파를 더해 '세로 선'이 꾸불꾸불하게 흔들리도록 변경 (세로 웨이브)
                float edgeWave = sin(uv.y * (_WaveFreq * 1.5) + time) * _WaveAmp;
                float x = uv.x + edgeWave;
                
                // 왜곡된 좌표가 0~1을 벗어나면 투명 처리
                if (x < 0.0 || x > 1.0)
                    return fixed4(0, 0, 0, 0);
                
                // 내부 일렁임: 세로로 뻗은 선(X축 변동)이 흔들리는 형태의 패턴
                float pattern = sin(x * 40.0 + time * 3.0) 
                              * cos(uv.y * 2.0 - time);
                              
                fixed4 c = IN.color;
                
                // 레이저(알파 1.0)일 경우 너무 불투명하므로 전체 투명도를 대폭 강제 삭감
                // IN.color.a가 1.0이어도 최대 0.4 수준이 되도록 억제
                float baseAlpha = min(c.a, 0.4); 
                
                c.rgb += pattern * 0.2; 
                c.a = baseAlpha * (0.6 + 0.4 * pattern);
                
                // 가장자리 부드럽게 처리
                float edgeAlpha = smoothstep(0.0, 0.05, x) * smoothstep(1.0, 0.95, x);
                c.a *= edgeAlpha;

                return c;
            }
            ENDCG
        }
    }
}
