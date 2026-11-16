// THE FOG — muestra la imagen de baja resolución sin suavizar, la reduce a la paleta común
// con tramado ordenado (Bayer 4x4) y busca el color en una tabla 3D aplanada (32x32x32 -> 1024x32).
Shader "TheFog/PixelPaleta"
{
    Properties
    {
        [PerRendererData] _MainTex ("Imagen", 2D) = "white" {}
        _Lut ("Tabla de paleta", 2D) = "white" {}
        _Tramado ("Fuerza del tramado", Range(0, 1)) = 0.35
        _Brillo ("Brillo previo", Range(0.5, 2)) = 1
        _Contraste ("Contraste previo", Range(0.5, 2)) = 1
        _Vineta ("Viñeta", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Opaque" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Cull Off ZWrite Off ZTest Always Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex; float4 _MainTex_TexelSize;
            sampler2D _Lut;
            float _Tramado, _Brillo, _Contraste, _Vineta;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            static const float bayer[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

            float3 Lut(float3 c)
            {
                c = saturate(c) * 31.0;
                float b = floor(c.b + 0.5);
                float2 uv = float2((b * 32.0 + floor(c.r + 0.5) + 0.5) / 1024.0, (floor(c.g + 0.5) + 0.5) / 32.0);
                return tex2D(_Lut, uv).rgb;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 px = floor(i.uv * _MainTex_TexelSize.zw);
                float2 uv = (px + 0.5) * _MainTex_TexelSize.xy;   // centro del píxel: nada de suavizado
                float3 c = tex2D(_MainTex, uv).rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                c = LinearToGammaSpace(c);
                #endif
                c = (c - 0.5) * _Contraste + 0.5;
                c *= _Brillo;
                float2 d = i.uv - 0.5;
                c *= 1.0 - _Vineta * saturate(dot(d, d) * 2.2);
                int idx = (int)(fmod(px.y, 4.0)) * 4 + (int)(fmod(px.x, 4.0));
                c += (bayer[idx] / 16.0 - 0.5) * _Tramado * (32.0 / 255.0);
                // La paleta ya está en gamma y el lienzo de pantalla se escribe tal cual: no convertir.
                return fixed4(Lut(c), 1);
            }
            ENDCG
        }
    }
}
