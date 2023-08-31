// Upgrade NOTE: replaced '_Projector' with 'unity_Projector'

Shader "Unlit/ProjectorShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            Tags { "RenderType"="Transparent" }
            LOD 100
            ZWrite off
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 texc : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            float4 _MainTex_ST;
            float4x4 unity_Projector;
            float4x4 unity_ProjectorClip;

            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.texc = mul(unity_Projector, v.vertex);
                o.texc.xyzw = o.texc.xyzw / o.texc.w;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texc);
                col = col * step(0, i.texc.w);
                return col;
            }
            ENDCG
        }
    }
}
