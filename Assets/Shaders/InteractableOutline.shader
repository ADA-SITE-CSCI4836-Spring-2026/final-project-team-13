Shader "Custom/InteractableOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0.35, 0, 1)
        _OutlineWidth ("Outline Width", Float) = 0.04
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Cull Front
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            fixed4 _OutlineColor;
            float _OutlineWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                v.vertex.xyz += normalize(v.normal) * _OutlineWidth;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
}
