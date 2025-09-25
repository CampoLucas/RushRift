Shader "Custom/URP/TileQuarterRotUnlit_Manual"
{
    Properties
    {
        _BaseMap   ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        _EmissionMap   ("Emission", 2D) = "black" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0)

        [Toggle] _AlphaClip ("Alpha Clipping", Float) = 0
        _Cutoff ("Cutoff", Range(0,1)) = 0.5

        [Enum(Rot_0,0,Rot_90,1,Rot_180,2,Rot_270,3)]
        _RotationQuarterTurns ("Rotation (0/90/180/270)", Float) = 0

        _TileInsetTexels ("Tile Inset (texels)", Range(0,2)) = 0.5
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "UnlitForward"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);     SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _EmissionColor;
                float  _AlphaClip;
                float  _Cutoff;
                float  _RotationQuarterTurns;
                float  _TileInsetTexels;
            CBUFFER_END

            // Unity auto-sets this: (1/width, 1/height, width, height)
            float4 _BaseMap_TexelSize;

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float  fogCoord   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float2 RotateLocalQuarter(float2 local, int q)
            {
                q = q & 3;
                if (q == 0) return local;                                   // (u, v)
                if (q == 1) return float2(1.0 - local.y, local.x);          // (1-v, u)
                if (q == 2) return 1.0 - local;                             // (1-u, 1-v)
                return float2(local.y, 1.0 - local.x);                      // (v, 1-u)
            }

            void RotateGradientsQuarter(float2 du, float2 dv, int q, out float2 duOut, out float2 dvOut)
            {
                q = q & 3;
                if (q == 0) { duOut = du;   dvOut = dv;   return; }
                if (q == 1) { duOut = -dv;  dvOut = du;   return; }
                if (q == 2) { duOut = -du;  dvOut = -dv;  return; }
                /* q == 3 */ duOut = dv;   dvOut = -du;
            }

            float2 InsetLocalUV(float2 localRot, float2 insetUV)
            {
                float2 lo = insetUV;
                float2 hi = 1.0 - insetUV;
                return saturate(lo + localRot * (hi - lo));
            }

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = pos.positionCS;
                o.uv         = v.uv;
                o.fogCoord   = ComputeFogFactor(pos.positionCS.z);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                int q = (int)round(_RotationQuarterTurns);

                float2 uvTiled   = i.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                float2 tileId    = floor(uvTiled);
                float2 local     = frac(uvTiled);
                float2 localRot  = RotateLocalQuarter(local, q);

                float2 insetUV   = _TileInsetTexels * _BaseMap_TexelSize.xy;
                float2 localIn   = InsetLocalUV(localRot, insetUV);

                float2 uvRot     = tileId + localIn;

                float2 ddxBase   = ddx(i.uv) * _BaseMap_ST.xy;
                float2 ddyBase   = ddy(i.uv) * _BaseMap_ST.xy;
                float2 ddxRot, ddyRot;
                RotateGradientsQuarter(ddxBase, ddyBase, q, ddxRot, ddyRot);

                float4 baseSample = SAMPLE_TEXTURE2D_GRAD(_BaseMap, sampler_BaseMap, uvRot, ddxRot, ddyRot);
                float3 baseColor  = baseSample.rgb * _BaseColor.rgb;
                float  alpha      = baseSample.a  * _BaseColor.a;

                if (_AlphaClip > 0.5 && alpha < _Cutoff) discard;

                float3 emission   = SAMPLE_TEXTURE2D_GRAD(_EmissionMap, sampler_EmissionMap, uvRot, ddxRot, ddyRot).rgb * _EmissionColor.rgb;

                float3 color = baseColor + emission;
                color = MixFog(color, i.fogCoord);

                return float4(color, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #ifndef LerpWhiteTo
            inline half3 LerpWhiteTo(half3 b, half t) { return lerp(half3(1,1,1), b, t); }
            #endif
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float  _AlphaClip;
                float  _Cutoff;
                float  _RotationQuarterTurns;
                float  _TileInsetTexels;
            CBUFFER_END

            float4 _BaseMap_TexelSize;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            float2 RotateLocalQuarter(float2 local, int q)
            {
                q = q & 3;
                if (q == 0) return local;
                if (q == 1) return float2(1.0 - local.y, local.x);
                if (q == 2) return 1.0 - local;
                return float2(local.y, 1.0 - local.x);
            }

            float2 InsetLocalUV(float2 localRot, float2 insetUV)
            {
                float2 lo = insetUV;
                float2 hi = 1.0 - insetUV;
                return saturate(lo + localRot * (hi - lo));
            }

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 posWS    = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionCS    = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, 0.0));
                o.uv = v.uv;
                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                if (_AlphaClip > 0.5)
                {
                    int q = (int)round(_RotationQuarterTurns);
                    float2 uvTiled  = i.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                    float2 tileId   = floor(uvTiled);
                    float2 local    = frac(uvTiled);
                    float2 localRot = RotateLocalQuarter(local, q);
                    float2 insetUV  = _TileInsetTexels * _BaseMap_TexelSize.xy;
                    float2 localIn  = InsetLocalUV(localRot, insetUV);
                    float2 uvRot    = tileId + localIn;

                    float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvRot).a * _BaseColor.a;
                    if (alpha < _Cutoff) discard;
                }
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
