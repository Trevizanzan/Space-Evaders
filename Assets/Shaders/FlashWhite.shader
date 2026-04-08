Shader "Custom/FlashWhite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION;  float2 uv : TEXCOORD0; };

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed4 tex = tex2D(_MainTex, i.uv);
                return fixed4(1, 1, 1, tex.a);
            }
            ENDCG
        }
    }
}