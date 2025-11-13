Shader "Custom/URP/TileArray_Lit_Rot_Brightness_Fix"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        _TexArray ("Tile Variants (Texture2DArray)", 2DArray) = "" {}
        _SliceCount ("Slice Count (1-64)", Range(1,64)) = 8

        [Toggle] _UseRandomSlicePerTile ("Random Slice Per Tile", Float) = 1
        _ManualSliceIndex ("Manual Slice Index", Range(0,63)) = 0

        [Toggle] _UseRandomRotationPerTile ("Random Quarter-Turn Rotation", Float) = 1
        [Enum(Rot_0,0,Rot_90,1,Rot_180,2,Rot_270,3)]
        _ManualRotationQuarterTurns ("Manual Rotation (0/90/180/270)", Float) = 0

        _BrightnessVariance ("Brightness Variance (±)", Range(0,1)) = 0.0

        _Tiling ("Tile Grid (xy scale, zw offset)", Vector) = (1,1,0,0)
        _TileInsetTexels ("Tile Inset (texels)", Range(0,2)) = 0.5
        _Seed ("Random Seed", Float) = 1234

        [Header(Render)]
        [Toggle] _ApplyFog ("Apply Scene Fog", Float) = 0
        [Enum(None,0,ForceSRGBToLinear,1,ForceLinearToSRGB,2)]
        _ColorSpaceComp ("Color Space Compensation", Float) = 0

        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clipping", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5

        [Header(Lit)]
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Occlusion ("Occlusion", Range(0,1)) = 1.0
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual
            Blend One Zero

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            // Lighting variants
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            // Texture2DArray sampling shims
            #ifndef SAMPLE_TEXTURE2D_ARRAY
                #define SAMPLE_TEXTURE2D_ARRAY(tex, samp, uv, slice) (tex).Sample((samp), float3((uv), (slice)))
            #endif
            #ifndef SAMPLE_TEXTURE2D_ARRAY_GRAD
                #define SAMPLE_TEXTURE2D_ARRAY_GRAD(tex, samp, uv, slice, ddx, ddy) (tex).SampleGrad((samp), float3((uv), (slice)), (ddx), (ddy))
            #endif

            TEXTURE2D_ARRAY(_TexArray);      SAMPLER(sampler_TexArray);
            float4 _TexArray_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;

                float  _SliceCount;
                float  _UseRandomSlicePerTile;
                float  _ManualSliceIndex;

                float  _UseRandomRotationPerTile;
                float  _ManualRotationQuarterTurns;

                float  _BrightnessVariance;

                float4 _Tiling;
                float  _TileInsetTexels;
                float  _Seed;

                float  _ApplyFog;
                float  _ColorSpaceComp;

                float  _Cutoff;

                float  _Metallic;
                float  _Smoothness;
                float  _Occlusion;
                float4 _EmissionColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float4 shadowCoord: TEXCOORD3;
                float  fogCoord   : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Color space helpers (local)
            float3 SRGBToLinear_Local(float3 c)
            {
                float3 lo = c / 12.92;
                float3 hi = pow(max((c + 0.055) / 1.055, 0.0), 2.4);
                return lerp(hi, lo, step(c, 0.04045));
            }
            float3 LinearToSRGB_Local(float3 c)
            {
                float3 lo = c * 12.92;
                float3 hi = 1.055 * pow(max(c, 0.0), 1.0/2.4) - 0.055;
                return lerp(hi, lo, step(c, 0.0031308));
            }

            // Hash / tiling utils
            float Hash12(float2 p)
            {
                p += _Seed;
                float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }
            int PickSliceIndex(float2 tileId)
            {
                int count = (int)clamp(_SliceCount, 1.0, 64.0);
                if (_UseRandomSlicePerTile > 0.5)
                {
                    float r = Hash12(tileId + 37.17);
                    return clamp((int)floor(r * count), 0, count - 1);
                }
                return clamp((int)round(_ManualSliceIndex), 0, count - 1);
            }
            int PickRotationQuarter(float2 tileId)
            {
                if (_UseRandomRotationPerTile > 0.5)
                {
                    float r = Hash12(tileId + 91.337);
                    return ((int)floor(r * 4.0)) & 3;
                }
                return ((int)round(_ManualRotationQuarterTurns)) & 3;
            }
            float2 RotateLocalQuarter(float2 l, int q)
            {
                q &= 3;
                if (q==0) return l;
                if (q==1) return float2(1.0 - l.y, l.x);
                if (q==2) return 1.0 - l;
                return float2(l.y, 1.0 - l.x);
            }
            void RotateGradientsQuarter(float2 du, float2 dv, int q, out float2 duOut, out float2 dvOut)
            {
                q &= 3;
                if (q==0) { duOut=du;   dvOut=dv;   return; }
                if (q==1) { duOut=-dv;  dvOut=du;   return; }
                if (q==2) { duOut=-du;  dvOut=-dv;  return; }
                duOut=dv; dvOut=-du;
            }
            float2 InsetLocalUV(float2 local, float2 inset)
            {
                float2 lo = inset;
                float2 hi = 1.0 - inset;
                return saturate(lo + local * (hi - lo));
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs   nor = GetVertexNormalInputs(v.normalOS);

                o.positionCS  = pos.positionCS;
                o.positionWS  = pos.positionWS;
                o.normalWS    = NormalizeNormalPerVertex(nor.normalWS);
                o.uv          = v.uv;
                o.shadowCoord = GetShadowCoord(pos);
                o.fogCoord    = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            // Sample tiled albedo/alpha (with rotation, inset, gradients)
            void SampleTiledSlice(float2 baseUV, out float3 albedo, out float alpha, out float2 uvTiled, out float2 tileId)
            {
                uvTiled = baseUV * _Tiling.xy + _Tiling.zw;
                tileId  = floor(uvTiled);
                float2 local = frac(uvTiled);

                int slice = PickSliceIndex(tileId);
                int q     = PickRotationQuarter(tileId);

                float2 ddxBase = ddx(baseUV) * _Tiling.xy;
                float2 ddyBase = ddy(baseUV) * _Tiling.xy;

                float2 localRot = RotateLocalQuarter(local, q);
                float2 ddxRot, ddyRot; RotateGradientsQuarter(ddxBase, ddyBase, q, ddxRot, ddyRot);

                float2 insetUV = _TileInsetTexels * _TexArray_TexelSize.xy;
                float2 localIn = InsetLocalUV(localRot, insetUV);
                float2 uvSlice = tileId + localIn;

                float4 s = SAMPLE_TEXTURE2D_ARRAY_GRAD(_TexArray, sampler_TexArray, uvSlice, slice, ddxRot, ddyRot);
                float3 col = s.rgb * _BaseColor.rgb;
                float   a  = s.a   * _BaseColor.a;

                // Color-space compensation (optional)
                if (_ColorSpaceComp > 0.5 && _ColorSpaceComp < 1.5)      col = SRGBToLinear_Local(col);
                else if (_ColorSpaceComp >= 1.5)                          col = LinearToSRGB_Local(col);

                // Per-tile brightness variance
                if (_BrightnessVariance > 1e-4)
                {
                    float r = Hash12(tileId + float2(221.7, 19.3)) * 2.0 - 1.0;
                    col *= max(0.0, 1.0 + r * _BrightnessVariance);
                }

                albedo = col;
                alpha  = a;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // Sample base color/alpha from the tile array
                float3 albedo; float alpha; float2 uvTiled; float2 tileId;
                SampleTiledSlice(i.uv, albedo, alpha, uvTiled, tileId);

                #ifdef _ALPHATEST_ON
                    clip(alpha - _Cutoff);
                #endif

                // Build SurfaceData (PBR metallic workflow)
                SurfaceData surfaceData;
                surfaceData.albedo      = albedo;
                surfaceData.metallic    = _Metallic;
                surfaceData.specular    = 0;         // unused in metallic workflow
                surfaceData.smoothness  = _Smoothness;
                surfaceData.normalTS    = half3(0,0,1);
                surfaceData.emission    = _EmissionColor.rgb;
                surfaceData.occlusion   = _Occlusion;
                surfaceData.alpha       = alpha;
                surfaceData.clearCoatMask       = 0;
                surfaceData.clearCoatSmoothness = 0;

                // Build InputData
                InputData inputData = (InputData)0;
                inputData.positionWS   = i.positionWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                inputData.normalWS     = NormalizeNormalPerPixel(i.normalWS);
                inputData.shadowCoord  = i.shadowCoord;
                inputData.fogCoord     = i.fogCoord;
                inputData.bakedGI      = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                inputData.shadowMask   = SAMPLE_SHADOWMASK(i.lightmapUV);

                // Evaluate lighting
                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                if (_ApplyFog > 0.5) color.rgb = MixFog(color.rgb, i.fogCoord);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #ifndef SAMPLE_TEXTURE2D_ARRAY
                #define SAMPLE_TEXTURE2D_ARRAY(tex, samp, uv, slice) (tex).Sample((samp), float3((uv), (slice)))
            #endif
            // URP14 shim: Shadows.hlsl expects LerpWhiteTo from Color.hlsl
            #ifndef LerpWhiteTo
            inline half3 LerpWhiteTo(half3 b, half t) { return lerp(half3(1,1,1), b, t); }
            #endif
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D_ARRAY(_TexArray); SAMPLER(sampler_TexArray);
            float4 _TexArray_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;

                float  _SliceCount;
                float  _UseRandomSlicePerTile;
                float  _ManualSliceIndex;

                float  _UseRandomRotationPerTile;
                float  _ManualRotationQuarterTurns;

                float4 _Tiling;
                float  _TileInsetTexels;
                float  _Seed;

                float  _Cutoff;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 posWS:TEXCOORD1; float3 normalWS:TEXCOORD2; };

            float Hash12(float2 p)
            {
                p += _Seed;
                float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }
            int PickSliceIndex(float2 tileId)
            {
                int count = (int)clamp(_SliceCount, 1.0, 64.0);
                if (_UseRandomSlicePerTile > 0.5)
                {
                    float r = Hash12(tileId + 37.17);
                    return clamp((int)floor(r * count), 0, count - 1);
                }
                return clamp((int)round(_ManualSliceIndex), 0, count - 1);
            }
            int PickRotationQuarter(float2 tileId)
            {
                if (_UseRandomRotationPerTile > 0.5)
                {
                    float r = Hash12(tileId + 91.337);
                    return ((int)floor(r * 4.0)) & 3;
                }
                return ((int)round(_ManualRotationQuarterTurns)) & 3;
            }
            float2 RotateLocalQuarter(float2 l, int q)
            {
                q &= 3;
                if (q==0) return l;
                if (q==1) return float2(1.0 - l.y, l.x);
                if (q==2) return 1.0 - l;
                return float2(l.y, 1.0 - l.x);
            }
            float2 InsetLocalUV(float2 local, float2 inset)
            {
                float2 lo = inset;
                float2 hi = 1.0 - inset;
                return saturate(lo + local * (hi - lo));
            }

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 posWS    = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionCS    = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, 0.0));
                o.uv = v.uv;
                o.posWS = posWS;
                o.normalWS = normalWS;
                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                    float2 uvTiled = i.uv * _Tiling.xy + _Tiling.zw;
                    float2 tileId  = floor(uvTiled);
                    float2 local   = frac(uvTiled);

                    int slice = PickSliceIndex(tileId);
                    int q     = PickRotationQuarter(tileId);

                    float2 localRot = RotateLocalQuarter(local, q);
                    float2 insetUV  = _TileInsetTexels * _TexArray_TexelSize.xy;
                    float2 uvSlice  = tileId + InsetLocalUV(localRot, insetUV);

                    float a = SAMPLE_TEXTURE2D_ARRAY(_TexArray, sampler_TexArray, uvSlice, slice).a * _BaseColor.a;
                    clip(a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #ifndef SAMPLE_TEXTURE2D_ARRAY
                #define SAMPLE_TEXTURE2D_ARRAY(tex, samp, uv, slice) (tex).Sample((samp), float3((uv), (slice)))
            #endif

            TEXTURE2D_ARRAY(_TexArray); SAMPLER(sampler_TexArray);
            float4 _TexArray_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _SliceCount;
                float  _UseRandomSlicePerTile;
                float  _ManualSliceIndex;
                float  _UseRandomRotationPerTile;
                float  _ManualRotationQuarterTurns;
                float4 _Tiling;
                float  _TileInsetTexels;
                float  _Seed;
                float  _Cutoff;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };

            float Hash12(float2 p)
            {
                p += _Seed;
                float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }
            int PickSliceIndex(float2 tileId)
            {
                int count = (int)clamp(_SliceCount, 1.0, 64.0);
                if (_UseRandomSlicePerTile > 0.5)
                {
                    float r = Hash12(tileId + 37.17);
                    return clamp((int)floor(r * count), 0, count - 1);
                }
                return clamp((int)round(_ManualSliceIndex), 0, count - 1);
            }
            int PickRotationQuarter(float2 tileId)
            {
                if (_UseRandomRotationPerTile > 0.5)
                {
                    float r = Hash12(tileId + 91.337);
                    return ((int)floor(r * 4.0)) & 3;
                }
                return ((int)round(_ManualRotationQuarterTurns)) & 3;
            }
            float2 RotateLocalQuarter(float2 l, int q)
            {
                q &= 3;
                if (q==0) return l;
                if (q==1) return float2(1.0 - l.y, l.x);
                if (q==2) return 1.0 - l;
                return float2(l.y, 1.0 - l.x);
            }
            float2 InsetLocalUV(float2 local, float2 inset)
            {
                float2 lo = inset;
                float2 hi = 1.0 - inset;
                return saturate(lo + local * (hi - lo));
            }

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                    float2 uvTiled = i.uv * _Tiling.xy + _Tiling.zw;
                    float2 tileId  = floor(uvTiled);
                    float2 local   = frac(uvTiled);

                    int slice = PickSliceIndex(tileId);
                    int q     = PickRotationQuarter(tileId);

                    float2 localRot = RotateLocalQuarter(local, q);
                    float2 insetUV  = _TileInsetTexels * _TexArray_TexelSize.xy;
                    float2 uvSlice  = tileId + InsetLocalUV(localRot, insetUV);

                    float a = SAMPLE_TEXTURE2D_ARRAY(_TexArray, sampler_TexArray, uvSlice, slice).a * _BaseColor.a;
                    clip(a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags { "LightMode"="Meta" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            #ifndef SAMPLE_TEXTURE2D_ARRAY
                #define SAMPLE_TEXTURE2D_ARRAY(tex, samp, uv, slice) (tex).Sample((samp), float3((uv), (slice)))
            #endif

            TEXTURE2D_ARRAY(_TexArray); SAMPLER(sampler_TexArray);
            float4 _TexArray_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;

                float  _SliceCount;
                float  _UseRandomSlicePerTile;
                float  _ManualSliceIndex;

                float  _UseRandomRotationPerTile;
                float  _ManualRotationQuarterTurns;

                float4 _Tiling;
                float  _TileInsetTexels;
                float  _Seed;

                float  _Cutoff;

                float  _Metallic;
                float  _Smoothness;
                float  _Occlusion;
                float4 _EmissionColor;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };

            float Hash12(float2 p)
            {
                p += _Seed;
                float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }
            int PickSliceIndex(float2 tileId)
            {
                int count = (int)clamp(_SliceCount, 1.0, 64.0);
                if (_UseRandomSlicePerTile > 0.5)
                {
                    float r = Hash12(tileId + 37.17);
                    return clamp((int)floor(r * count), 0, count - 1);
                }
                return clamp((int)round(_ManualSliceIndex), 0, count - 1);
            }
            int PickRotationQuarter(float2 tileId)
            {
                if (_UseRandomRotationPerTile > 0.5)
                {
                    float r = Hash12(tileId + 91.337);
                    return ((int)floor(r * 4.0)) & 3;
                }
                return ((int)round(_ManualRotationQuarterTurns)) & 3;
            }
            float2 RotateLocalQuarter(float2 l, int q)
            {
                q &= 3;
                if (q==0) return l;
                if (q==1) return float2(1.0 - l.y, l.x);
                if (q==2) return 1.0 - l;
                return float2(l.y, 1.0 - l.x);
            }
            float2 InsetLocalUV(float2 local, float2 inset)
            {
                float2 lo = inset;
                float2 hi = 1.0 - inset;
                return saturate(lo + local * (hi - lo));
            }

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float2 uvTiled = i.uv * _Tiling.xy + _Tiling.zw;
                float2 tileId  = floor(uvTiled);
                float2 local   = frac(uvTiled);

                int slice = PickSliceIndex(tileId);
                int q     = PickRotationQuarter(tileId);

                float2 localRot = RotateLocalQuarter(local, q);
                float2 insetUV  = _TileInsetTexels * _TexArray_TexelSize.xy;
                float2 uvSlice  = tileId + InsetLocalUV(localRot, insetUV);

                float4 s = SAMPLE_TEXTURE2D_ARRAY(_TexArray, sampler_TexArray, uvSlice, slice);
                float3 albedo = s.rgb * _BaseColor.rgb;
                float  alpha  = s.a   * _BaseColor.a;

                #ifdef _ALPHATEST_ON
                    clip(alpha - _Cutoff);
                #endif

                MetaInput meta;
                meta.Albedo = albedo;
                meta.Emission = _EmissionColor.rgb;
                return UnityMetaFragment(meta);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
