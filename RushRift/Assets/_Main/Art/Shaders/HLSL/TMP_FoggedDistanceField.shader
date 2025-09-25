Shader "Custom/URP/TMP_FoggedDistanceField"
{
    Properties
    {
        _MainTex       ("SDF Atlas", 2D) = "white" {}
        [HDR]_FaceColor    ("Face Color", Color) = (1,1,1,1)
        [HDR]_OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth  ("Outline Width", Range(0,1)) = 0.0
        _FaceSoftness  ("Face Softness", Range(0,0.1)) = 0.02
        _OutlineSoftness("Outline Softness", Range(0,0.1)) = 0.02
        _FaceDilate    ("Face Dilate (-1..1)", Range(-1,1)) = 0.0
        [Toggle]_UseVertexColor ("Use Vertex Color", Float) = 1

        [Header(Render)]
        [Enum(Off,0,Front,1,Back,2)] _Cull ("Cull", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }

        Pass
        {
            Name "URP_Fogged_SDF"
            Tags { "LightMode"="UniversalForward" }

            Cull [_Cull]
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" // MixFog

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _FaceColor;
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _FaceSoftness;
                float  _OutlineSoftness;
                float  _FaceDilate;
                float  _UseVertexColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv0        : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv0        : TEXCOORD0;
                float  fogCoord   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = pos.positionCS;
                o.uv0        = v.uv0;
                o.color      = (_UseVertexColor > 0.5) ? v.color : 1.0;
                o.fogCoord   = ComputeFogFactor(pos.positionCS.z);
                return o;
            }

            // Median of RGB helps in case the atlas packs distance in RGB channels (some fonts)
            float Median3(float3 x) { return x.x + x.y + x.z - min(x.x, min(x.y, x.z)) - max(x.x, max(x.y, x.z)); }

            half4 frag (Varyings i) : SV_Target
            {
                float4 s = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv0);

                // Use A if present, otherwise median RGB
                float sd = (s.a > 0.0001) ? s.a : Median3(s.rgb);

                // Center threshold around 0.5 and allow dilate to push it
                float tCenter = saturate(0.5 + 0.5 * _FaceDilate);

                // Screen-space AA from derivatives
                float aa = max(fwidth(sd), 1e-4);

                // Face / outline coverage
                float faceSoft = _FaceSoftness + aa;
                float outlineSoft = _OutlineSoftness + aa;

                // Face alpha
                float faceAlpha = smoothstep(tCenter - faceSoft, tCenter + faceSoft, sd);

                // Outline alpha (band just outside the face threshold)
                float ot = saturate(tCenter - _OutlineWidth);
                float outlineBand = smoothstep(ot - outlineSoft, ot + outlineSoft, sd) - faceAlpha;
                outlineBand = saturate(outlineBand);

                // Colors with vertex tint
                float4 faceCol    = _FaceColor    * i.color;
                float4 outlineCol = _OutlineColor * i.color;

                float3 rgb = faceCol.rgb * faceAlpha + outlineCol.rgb * outlineBand;

                // Composite alpha (avoid double-add in overlap)
                float a = faceCol.a * faceAlpha + outlineCol.a * outlineBand * (1.0 - faceAlpha);

                // Apply URP fog
                rgb = MixFog(rgb, i.fogCoord);

                return half4(rgb, a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}