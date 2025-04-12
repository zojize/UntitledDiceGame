Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
        [Toggle] _OutlineEnabled ("Enable Outline", Float) = 0
        _OutlineColor ("Outline Color", Color) = (1,0,0,1)
        _OutlineWidth ("Outline Width", Range(0,0.1)) = 0.01
    }
    
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        
        // Main pass
        Pass
        {
            Cull Off // Render both sides

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Cutoff;
            float _OutlineEnabled;
            float4 _OutlineColor;
            float _OutlineWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i, float facing : VFACE) : SV_Target
            {
                float2 uv = facing > 0 ? i.uv : float2(1 - i.uv.x, i.uv.y);
                if (_OutlineEnabled > 0
                   && (uv.x < _OutlineWidth
                    || uv.x > 1 - _OutlineWidth
                    || uv.y < _OutlineWidth
                    || uv.y > 1 - _OutlineWidth))
                {
                    return _OutlineColor;
                }

                fixed4 col = tex2D(_MainTex, uv);
                clip(col.a - _Cutoff);
                return col;
            }
            ENDCG
        }
    }
}
