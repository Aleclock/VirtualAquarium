Shader "Custom/BlackToTransparentWithColorAndMask"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _DetailTex ("Detail Texture", 2D) = "white" {}
        _DetailTexIntensity ("Detail Texture Intensity", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            // Input structure for the vertex shader
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Output structure for the vertex shader
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Uniforms (properties)
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            sampler2D _DetailTex;
            float _DetailTexIntensity;
            float4 _DetailTex_ST;

            // Vertex shader
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // Fragment shader
            half4 frag(v2f i) : SV_Target
            {
                // Sample the base texture
                half4 baseColor = tex2D(_MainTex, i.uv);

                // Set alpha to 0 for near-black pixels
                if (length(baseColor.rgb) < 0.1)
                {
                    baseColor.a = 0.0;
                }

                // Multiply the base color with the user-defined color
                baseColor.rgb *= _Color.rgb;

                // Adjust the UVs for the detail texture
                float2 detailUV = i.uv * _DetailTex_ST.xy + _DetailTex_ST.zw;

                // Sample the detail texture
                half4 detailColor = tex2D(_DetailTex, detailUV);

                // Blend the detail texture with the base texture
                baseColor.rgb = lerp(baseColor.rgb, detailColor.rgb, _DetailTexIntensity * baseColor.a);

                return baseColor;
            }
            ENDCG
        }
    }

    Fallback "Diffuse"
}
