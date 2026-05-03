Shader "Custom/InteractableOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0.35, 0, 1)
        _OutlineWidth ("Outline Width", Range(0, 10)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        ZWrite Off
        ZTest LEqual
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Stencil
            {
                Ref 1
                Comp NotEqual
                Pass Keep
            }

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);

                float3 normalClip = mul((float3x3)UNITY_MATRIX_VP, mul((float3x3)unity_ObjectToWorld, v.normal));
                float2 outlineDirection = normalize(normalClip.xy);
                o.pos.xy += outlineDirection / _ScreenParams.xy * o.pos.w * _OutlineWidth * 2.0;

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
