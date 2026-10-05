Shader "Custom/BlackToTransparentWithColorAndMask"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _BaseMap ("Base Map (URP)", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _BaseColor ("Base Color (URP)", Color) = (1, 1, 1, 1)
        _DetailTex ("Detail Texture", 2D) = "white" {}
        _DetailTexIntensity ("Detail Texture Intensity", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_DetailTex);
            SAMPLER(sampler_DetailTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _DetailTex_ST;
                float4 _Color;
                float4 _BaseColor;
                float  _DetailTexIntensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                if (length(baseColor.rgb) < 0.1)
                {
                    baseColor.a = 0.0;
                }

                half4 tintColor = _Color * _BaseColor;
                baseColor.rgb *= tintColor.rgb;

                float2 detailUV = input.uv * _DetailTex_ST.xy + _DetailTex_ST.zw;
                half4 detailColor = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, detailUV);

                baseColor.rgb = lerp(baseColor.rgb, detailColor.rgb, _DetailTexIntensity * baseColor.a);

                return baseColor;
            }
            ENDHLSL
        }
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            sampler2D _DetailTex;
            float _DetailTexIntensity;
            float4 _DetailTex_ST;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 baseColor = tex2D(_MainTex, i.uv);

                if (length(baseColor.rgb) < 0.1)
                {
                    baseColor.a = 0.0;
                }

                baseColor.rgb *= _Color.rgb;

                float2 detailUV = i.uv * _DetailTex_ST.xy + _DetailTex_ST.zw;
                half4 detailColor = tex2D(_DetailTex, detailUV);

                baseColor.rgb = lerp(baseColor.rgb, detailColor.rgb, _DetailTexIntensity * baseColor.a);

                return baseColor;
            }
            ENDCG
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
