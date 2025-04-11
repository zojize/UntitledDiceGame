Shader "Custom/OutlineShader"
{
    Properties
    {
        _OutlineWidth ("Outline Width", Range(0,0.1)) = 0.01
        _OutlineColor ("Outline Color", Color) = (1,0,0,1)
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1" }
        
        Pass
        {
            Name "OUTLINE"
            
            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            float _OutlineWidth;
            float4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                
                // Calculate center of the plane
                float4 center = float4(0.5, 0.5, 0, 1);
                
                // Get vertex position relative to center
                float4 vertexPos = v.vertex - center;
                
                // Scale the vertex position slightly
                vertexPos.xyz *= (1.0 + _OutlineWidth);
                
                // Move back to world space
                float4 finalPos = center + vertexPos;
                
                o.vertex = UnityObjectToClipPos(finalPos);
                return o;
            }

            fixed4 frag() : SV_Target 
            { 
                return _OutlineColor;
            }
            ENDCG
        }
    }
}