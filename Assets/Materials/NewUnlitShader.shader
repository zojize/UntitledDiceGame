Shader "Custom/OutlineShader"
{
    Properties
    {
        _OutlineWidth ("Outline Width", Range(0,0.1)) = 0.01
        _OutlineColor ("Outline Color", Color) = (1,0,0,1)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent+1" }
        
        Pass
        {
            Name "OUTLINE"
            
            Cull Front
            ZWrite On
            ZTest Less
            Blend SrcAlpha OneMinusSrcAlpha // Enable transparency
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            float _OutlineWidth;
            float4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                
                // Expand the vertices along their normals
                float3 normal = normalize(v.normal);
                float3 outlineOffset = normal * _OutlineWidth;
                float4 pos = v.vertex;
                pos.xyz += outlineOffset;
                
                o.vertex = UnityObjectToClipPos(pos);
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
