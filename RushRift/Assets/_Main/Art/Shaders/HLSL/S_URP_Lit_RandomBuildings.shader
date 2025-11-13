Shader "Custom/URP/TileArrayLit_EmissiveVar_3Colors_Cyl"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        _TexArray ("Tile Variants (Texture2DArray)", 2DArray) = "" {}
        _SliceCount ("Slice Count (1-16)", Range(1,16)) = 8

        _Weight0 ("Weight 0", Range(0,1)) = 1
        _Weight1 ("Weight 1", Range(0,1)) = 1
        _Weight2 ("Weight 2", Range(0,1)) = 1
        _Weight3 ("Weight 3", Range(0,1)) = 1
        _Weight4 ("Weight 4", Range(0,1)) = 1
        _Weight5 ("Weight 5", Range(0,1)) = 1
        _Weight6 ("Weight 6", Range(0,1)) = 1
        _Weight7 ("Weight 7", Range(0,1)) = 1
        _Weight8 ("Weight 8", Range(0,1)) = 0
        _Weight9 ("Weight 9", Range(0,1)) = 0
        _Weight10("Weight 10", Range(0,1)) = 0
        _Weight11("Weight 11", Range(0,1)) = 0
        _Weight12("Weight 12", Range(0,1)) = 0
        _Weight13("Weight 13", Range(0,1)) = 0
        _Weight14("Weight 14", Range(0,1)) = 0
        _Weight15("Weight 15", Range(0,1)) = 0

        [Enum(MeshUV,0,Cylindrical_ObjectY,1)]
        _ProjectionMode ("Projection Mode", Float) = 0

        [Header(Mesh UV Tiling)]
        _Tiling ("Mesh UV: xy=scale zw=offset", Vector) = (1,1,0,0)

        [Header(Cylindrical (Object Y))]
        _CylTilesAround   ("Tiles Around (per revolution)", Float) = 8
        _CylTilesPerUnitY ("Tiles Per Unit (height)", Float) = 1
        _CylUVOffset      ("Cyl UV Offset (u,v)", Vector) = (0,0,0,0)

        [Toggle] _UseRandomRotationPerTile ("Random Rotation Per Tile (Quarter Turns)", Float) = 1
        [Enum(Rot_0,0,Rot_90,1,Rot_180,2,Rot_270,3)]
        _ManualRotationQuarterTurns ("Manual Rotation (0/90/180/270)", Float) = 0

        [Toggle] _RandomMirrorU ("Random Mirror U", Float) = 1
        [Toggle] _RandomMirrorV ("Random Mirror V", Float) = 1

        _TileInsetTexels ("Tile Inset (texels)", Range(0,2)) = 0.5
        _Seed ("Random Seed", Float) = 1234

        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clipping", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5

        [Header(Emission)]
        [HDR] _EmissionColor ("Emission Color (fallback)", Color) = (1,1,1,0)
        _EmissionIntensity ("Emission Intensity", Range(0,20)) = 1
        _EmissivePulseSpeed ("Emissive Pulse Speed", Range(0,10)) = 1
        _EmissiveMin ("Emissive Pulse Min", Range(0,2)) = 0.2
        _EmissiveMax ("Emissive Pulse Max", Range(0,5)) = 1.0

        [Toggle] _UseRandomEmissionColor ("Random Emission Color Per Tile", Float) = 1
        _EmissionColorCount ("Emission Color Count (1-3)", Range(1,3)) = 3
        [HDR] _EmissionColor0 ("Emission Color 0", Color) = (1,1,1,0)
        [HDR] _EmissionColor1 ("Emission Color 1", Color) = (1,0.6,0.2,0)
        [HDR] _EmissionColor2 ("Emission Color 2", Color) = (0.4,0.8,1.0,0)

        [Header(Emission Mask)]
        [Toggle] _UseEmissionLumaMask ("Mask Emission by Luminance/Neutral", Float) = 1
        _EmiMaskThreshold ("Luma Threshold (grey→white)", Range(0,1)) = 0.35
        _EmiMaskFeather   ("Luma Feather", Range(0,0.5)) = 0.08
        _EmiNeutralTolerance ("Neutral Tolerance (sat ≤)", Range(0,1)) = 0.15

        [Header(Macro Noise)]
        _MacroST ("Macro UV (xy scale, zw offset)", Vector) = (0.05,0.05,0,0)
        _MacroStrength ("Macro Strength", Range(0,1)) = 0.35

        [Header(Lit)]
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Occlusion ("Occlusion", Range(0,1)) = 1.0
    }

    HLSLINCLUDE
        #ifndef INV_TWO_PI
            #define INV_TWO_PI 0.15915494
        #endif
        #ifndef SAMPLE_TEXTURE2D_ARRAY
            #define SAMPLE_TEXTURE2D_ARRAY(tex, samp, uv, slice) (tex).Sample((samp), float3((uv), (slice)))
        #endif
        #ifndef SAMPLE_TEXTURE2D_ARRAY_GRAD
            #define SAMPLE_TEXTURE2D_ARRAY_GRAD(tex, samp, uv, slice, ddx, ddy) (tex).SampleGrad((samp), float3((uv), (slice)), (ddx), (ddy))
        #endif

        static const int MAX_SLICES = 16;

        // No globals inside helpers: pass everything needed as parameters.

        float Hash12(float2 p, float seed)
        {
            p += seed;
            float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
            p3 += dot(p3, p3.yzx + 33.33);
            return frac((p3.x + p3.y) * p3.z);
        }

        int SelectWeightedSlice(float2 tileId, float sliceCount,
                                float w0,float w1,float w2,float w3,float w4,float w5,float w6,float w7,
                                float w8,float w9,float w10,float w11,float w12,float w13,float w14,float w15,
                                float seed)
        {
            int count = (int)clamp(sliceCount, 1.0, (float)MAX_SLICES);
            float weights[MAX_SLICES] = { w0,w1,w2,w3,w4,w5,w6,w7,w8,w9,w10,w11,w12,w13,w14,w15 };
            float total = 0.0;
            [unroll] for (int i=0;i<MAX_SLICES;i++) { if (i<count) total += max(0.0, weights[i]); }
            if (total <= 0.0)
            {
                float r = Hash12(tileId + 37.17, seed);
                return clamp((int)floor(r * count), 0, count - 1);
            }
            float pick = Hash12(tileId + 137.5, seed) * total;
            float acc = 0.0;
            [unroll] for (int i=0;i<MAX_SLICES;i++)
            {
                if (i>=count) break;
                acc += max(0.0, weights[i]);
                if (pick <= acc) return i;
            }
            return count - 1;
        }

        int SelectRotationQuarter(float2 tileId, float useRandom, float manualQuarter, float seed)
        {
            if (useRandom > 0.5)
            {
                float r = Hash12(tileId + 91.337, seed);
                return ((int)floor(r * 4.0)) & 3;
            }
            return ((int)round(manualQuarter)) & 3;
        }

        float2 RotateLocalQuarter(float2 local, int q)
        {
            q &= 3;
            if (q==0) return local;
            if (q==1) return float2(1.0 - local.y, local.x);
            if (q==2) return 1.0 - local;
            return float2(local.y, 1.0 - local.x);
        }

        void RotateGradientsQuarter(float2 du, float2 dv, int q, out float2 duOut, out float2 dvOut)
        {
            q &= 3;
            if (q==0) { duOut=du;   dvOut=dv;   return; }
            if (q==1) { duOut=-dv;  dvOut=du;   return; }
            if (q==2) { duOut=-du;  dvOut=-dv;  return; }
            duOut=dv; dvOut=-du;
        }

        float2 InsetLocalUV(float2 localRot, float2 insetUV)
        {
            float2 lo = insetUV;
            float2 hi = 1.0 - insetUV;
            return saturate(lo + localRot * (hi - lo));
        }

        float MacroNoise(float2 uv)
        {
            return frac(sin(dot(uv, float2(12.9898,78.233))) * 43758.5453);
        }

        float3 SelectEmissionColorPerTile(float2 tileId,
                                          float useRandom, float colorCount,
                                          float3 c0, float3 c1, float3 c2, float3 fallback,
                                          float seed)
        {
            if (useRandom < 0.5) return fallback;
            int count = (int)clamp(colorCount, 1.0, 3.0);
            float r = Hash12(tileId + float2(311.713, 902.114), seed);
            int idx = clamp((int)floor(r * count), 0, count - 1);
            if (idx == 0) return c0;
            if (idx == 1) return c1;
            return c2;
        }

        // Small helpers to compute tiled UVs (mesh vs cylindrical)
        void ComputeUV_Mesh(float2 uvIn, float4 Tiling, out float2 uvTiled, out float2 ddxBase, out float2 ddyBase)
        {
            uvTiled = uvIn * Tiling.xy + Tiling.zw;
            ddxBase = ddx(uvIn) * Tiling.xy;
            ddyBase = ddy(uvIn) * Tiling.xy;
        }

        void ComputeUV_Cyl(float3 posOS, float2 tilesAround_perUnitY, float2 uvOffset,
                           out float2 uvTiled, out float2 ddxBase, out float2 ddyBase)
        {
            float3 p  = posOS;
            float3 dx = ddx(posOS);
            float3 dy = ddy(posOS);

            float den   = max(p.x*p.x + p.z*p.z, 1e-8);
            float du_dx =  ( p.z * dx.x - p.x * dx.z) * INV_TWO_PI / den;
            float du_dy =  ( p.z * dy.x - p.x * dy.z) * INV_TWO_PI / den;

            float angle = atan2(p.x, p.z);
            float u     = 0.5 + angle * INV_TWO_PI;
            float v     = p.y;

            float2 uv   = float2(u, v) + uvOffset;

            float2 scale = tilesAround_perUnitY;
            uvTiled = uv * scale;

            ddxBase = float2(du_dx, dx.y) * scale;
            ddyBase = float2(du_dy, dy.y) * scale;
        }
    ENDHLSL

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

            TEXTURE2D_ARRAY(_TexArray); SAMPLER(sampler_TexArray);
            float4 _TexArray_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;

                float  _ProjectionMode;
                float4 _Tiling;
                float  _CylTilesAround;
                float  _CylTilesPerUnitY;
                float4 _CylUVOffset;

                float  _TileInsetTexels;
                float  _SliceCount;

                float  _Weight0,_Weight1,_Weight2,_Weight3,_Weight4,_Weight5,_Weight6,_Weight7;
                float  _Weight8,_Weight9,_Weight10,_Weight11,_Weight12,_Weight13,_Weight14,_Weight15;

                float  _UseRandomRotationPerTile;
                float  _ManualRotationQuarterTurns;
                float  _RandomMirrorU;
                float  _RandomMirrorV;

                float  _Seed;

                float  _Cutoff;

                float4 _EmissionColor;
                float  _EmissionIntensity;
                float  _EmissivePulseSpeed;
                float  _EmissiveMin;
                float  _EmissiveMax;

                float  _UseRandomEmissionColor;
                float  _EmissionColorCount;
                float4 _EmissionColor0;
                float4 _EmissionColor1;
                float4 _EmissionColor2;

                float  _UseEmissionLumaMask;
                float  _EmiMaskThreshold;
                float  _EmiMaskFeather;
                float  _EmiNeutralTolerance;

                float4 _MacroST;
                float  _MacroStrength;

                float  _Metallic;
                float  _Smoothness;
                float  _Occlusion;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                float4 shadowCoord: TEXCOORD4;
                float  fogCoord   : TEXCOORD5;
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
                VertexNormalInputs   nor = GetVertexNormalInputs(v.normalOS);

                o.positionCS  = pos.positionCS;
                o.positionWS  = pos.positionWS;
                o.normalWS    = NormalizeNormalPerVertex(nor.normalWS);
                o.positionOS  = v.positionOS.xyz;
                o.uv          = v.uv;
                o.shadowCoord = GetShadowCoord(pos);
                o.fogCoord    = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            void SampleTile(float2 uvIn, float3 posOS, float3 posWS, out float3 baseRGB, out float alpha, out float2 tileIdOut)
            {
                float2 uvTiled, ddxBase, ddyBase;
                if (_ProjectionMode < 0.5)
                    ComputeUV_Mesh(uvIn, _Tiling, uvTiled, ddxBase, ddyBase);
                else
                    ComputeUV_Cyl(posOS, float2(_CylTilesAround, _CylTilesPerUnitY), _CylUVOffset.xy, uvTiled, ddxBase, ddyBase);

                float2 tileId  = floor(uvTiled);
                float2 local   = frac(uvTiled);

                if (_RandomMirrorU > 0.5)
                    if (Hash12(tileId + 101.3, _Seed) > 0.5) { local.x = 1.0 - local.x; ddxBase.x = -ddxBase.x; ddyBase.x = -ddyBase.x; }
                if (_RandomMirrorV > 0.5)
                    if (Hash12(tileId + 202.6, _Seed) > 0.5) { local.y = 1.0 - local.y; ddxBase.y = -ddxBase.y; ddyBase.y = -ddyBase.y; }

                int q     = SelectRotationQuarter(tileId, _UseRandomRotationPerTile, _ManualRotationQuarterTurns, _Seed);
                int slice = SelectWeightedSlice(tileId, _SliceCount,
                                                _Weight0,_Weight1,_Weight2,_Weight3,_Weight4,_Weight5,_Weight6,_Weight7,
                                                _Weight8,_Weight9,_Weight10,_Weight11,_Weight12,_Weight13,_Weight14,_Weight15,
                                                _Seed);

                float2 localRot = RotateLocalQuarter(local, q);
                float2 ddxRot, ddyRot; RotateGradientsQuarter(ddxBase, ddyBase, q, ddxRot, ddyRot);

                float2 insetUV  = _TileInsetTexels * _TexArray_TexelSize.xy;
                float2 localIn  = InsetLocalUV(localRot, insetUV);
                float2 uvRot    = tileId + localIn;

                float4 s = SAMPLE_TEXTURE2D_ARRAY_GRAD(_TexArray, sampler_TexArray, uvRot, slice, ddxRot, ddyRot);

                baseRGB = s.rgb * _BaseColor.rgb;
                alpha   = s.a   * _BaseColor.a;

                float2 macroUV = posWS.xz * _MacroST.xy + _MacroST.zw;
                baseRGB *= lerp(1.0, lerp(0.9, 1.1, MacroNoise(macroUV)), _MacroStrength);

                tileIdOut = tileId;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 baseRGB; float alpha; float2 tileId;
                SampleTile(i.uv, i.positionOS, i.positionWS, baseRGB, alpha, tileId);

                #ifdef _ALPHATEST_ON
                    clip(alpha - _Cutoff);
                #endif

                float phase = Hash12(tileId + float2(912.42, 314.159), _Seed) * 6.2831853;
                float pulse = lerp(_EmissiveMin, _EmissiveMax, 0.5 + 0.5 * sin(_Time.y * _EmissivePulseSpeed + phase));
                float3 emiColor = SelectEmissionColorPerTile(tileId, _UseRandomEmissionColor, _EmissionColorCount,
                                                             _EmissionColor0.rgb, _EmissionColor1.rgb, _EmissionColor2.rgb, _EmissionColor.rgb,
                                                             _Seed) * _EmissionIntensity * pulse;

                float emiMask = 1.0;
                if (_UseEmissionLumaMask > 0.5)
                {
                    float3 c = baseRGB;
                    float luma = dot(c, float3(0.2126, 0.7152, 0.0722));
                    float l0 = _EmiMaskThreshold - _EmiMaskFeather;
                    float l1 = _EmiMaskThreshold + _EmiMaskFeather;
                    float maskL = smoothstep(l0, l1, luma);

                    float mx = max(max(c.r, c.g), c.b);
                    float mn = min(min(c.r, c.g), c.b);
                    float sat = (mx - mn) / max(mx, 1e-5);
                    float maskN = step(sat, _EmiNeutralTolerance);

                    emiMask = maskL * maskN;
                }

                SurfaceData surfaceData;
                surfaceData.albedo      = baseRGB;
                surfaceData.metallic    = _Metallic;
                surfaceData.specular    = 0;
                surfaceData.smoothness  = _Smoothness;
                surfaceData.normalTS    = half3(0,0,1);
                surfaceData.emission    = emiColor * emiMask;
                surfaceData.occlusion   = _Occlusion;
                surfaceData.alpha       = alpha;
                surfaceData.clearCoatMask       = 0;
                surfaceData.clearCoatSmoothness = 0;

                InputData inputData = (InputData)0;
                inputData.positionWS   = i.positionWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                inputData.normalWS     = NormalizeNormalPerPixel(i.normalWS);
                inputData.shadowCoord  = i.shadowCoord;
                inputData.fogCoord     = i.fogCoord;
                inputData.bakedGI      = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, i.fogCoord);
                return color;
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
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // URP shim
            #ifndef LerpWhiteTo
                inline half3 LerpWhiteTo(half3 b, half t) { return lerp(half3(1,1,1), b, t); }
            #endif
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D_ARRAY(_TexArray); SAMPLER(sampler_TexArray);
            float4 _TexArray_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;

                float  _ProjectionMode;
                float4 _Tiling;
                float  _CylTilesAround;
                float  _CylTilesPerUnitY;
                float4 _CylUVOffset;

                float  _TileInsetTexels;
                float  _SliceCount;

                float  _Weight0,_Weight1,_Weight2,_Weight3,_Weight4,_Weight5,_Weight6,_Weight7;
                float  _Weight8,_Weight9,_Weight10,_Weight11,_Weight12,_Weight13,_Weight14,_Weight15;

                float  _UseRandomRotationPerTile;
                float  _ManualRotationQuarterTurns;
                float  _RandomMirrorU;
                float  _RandomMirrorV;

                float  _Seed;

                float  _Cutoff;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 positionCS:SV_POSITION; float3 positionOS:TEXCOORD0; float2 uv:TEXCOORD1; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, 0.0));
                o.positionOS = v.positionOS.xyz;
                o.uv = v.uv;
                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                    float2 uvTiled;
                    if (_ProjectionMode < 0.5)
                        ComputeUV_Mesh(i.uv, _Tiling, uvTiled, /*ddx*/uvTiled, /*ddy*/uvTiled); // ddx/ddy not needed here
                    else {
                        float2 ddxBase, ddyBase;
                        ComputeUV_Cyl(i.positionOS, float2(_CylTilesAround, _CylTilesPerUnitY), _CylUVOffset.xy, uvTiled, ddxBase, ddyBase);
                    }

                    float2 tileId  = floor(uvTiled);
                    float2 local   = frac(uvTiled);

                    if (_RandomMirrorU > 0.5) if (Hash12(tileId + 101.3, _Seed) > 0.5) local.x = 1.0 - local.x;
                    if (_RandomMirrorV > 0.5) if (Hash12(tileId + 202.6, _Seed) > 0.5) local.y = 1.0 - local.y;

                    int slice = SelectWeightedSlice(tileId, _SliceCount,
                                                    _Weight0,_Weight1,_Weight2,_Weight3,_Weight4,_Weight5,_Weight6,_Weight7,
                                                    _Weight8,_Weight9,_Weight10,_Weight11,_Weight12,_Weight13,_Weight14,_Weight15,
                                                    _Seed);
                    int q     = SelectRotationQuarter(tileId, _UseRandomRotationPerTile, _ManualRotationQuarterTurns, _Seed);
                    float2 localRot = RotateLocalQuarter(local, q);

                    float2 insetUV = _TileInsetTexels * _TexArray_TexelSize.xy;
                    float2 uvRot   = tileId + InsetLocalUV(localRot, insetUV);

                    float a = SAMPLE_TEXTURE2D_ARRAY(_TexArray, sampler_TexArray, uvRot, slice).a * _BaseColor.a;
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

            TEXTURE2D_ARRAY(_TexArray); SAMPLER(sampler_TexArray);
            float4 _TexArray_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;

                float  _ProjectionMode;
                float4 _Tiling;
                float  _CylTilesAround;
                float  _CylTilesPerUnitY;
                float4 _CylUVOffset;

                float  _TileInsetTexels;
                float  _SliceCount;

                float  _Weight0,_Weight1,_Weight2,_Weight3,_Weight4,_Weight5,_Weight6,_Weight7;
                float  _Weight8,_Weight9,_Weight10,_Weight11,_Weight12,_Weight13,_Weight14,_Weight15;

                float  _UseRandomRotationPerTile;
                float  _ManualRotationQuarterTurns;
                float  _RandomMirrorU;
                float  _RandomMirrorV;

                float  _Seed;

                float  _Cutoff;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 positionOS:TEXCOORD1; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.positionOS = v.positionOS.xyz;
                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                    float2 uvTiled;
                    if (_ProjectionMode < 0.5)
                        ComputeUV_Mesh(i.uv, _Tiling, uvTiled, /*ddx*/uvTiled, /*ddy*/uvTiled);
                    else {
                        float2 ddxBase, ddyBase;
                        ComputeUV_Cyl(i.positionOS, float2(_CylTilesAround, _CylTilesPerUnitY), _CylUVOffset.xy, uvTiled, ddxBase, ddyBase);
                    }

                    float2 tileId  = floor(uvTiled);
                    float2 local   = frac(uvTiled);

                    if (_RandomMirrorU > 0.5) if (Hash12(tileId + 101.3, _Seed) > 0.5) local.x = 1.0 - local.x;
                    if (_RandomMirrorV > 0.5) if (Hash12(tileId + 202.6, _Seed) > 0.5) local.y = 1.0 - local.y;

                    int slice = SelectWeightedSlice(tileId, _SliceCount,
                                                    _Weight0,_Weight1,_Weight2,_Weight3,_Weight4,_Weight5,_Weight6,_Weight7,
                                                    _Weight8,_Weight9,_Weight10,_Weight11,_Weight12,_Weight13,_Weight14,_Weight15,
                                                    _Seed);
                    int q     = SelectRotationQuarter(tileId, _UseRandomRotationPerTile, _ManualRotationQuarterTurns, _Seed);
                    float2 localRot = RotateLocalQuarter(local, q);

                    float2 insetUV = _TileInsetTexels * _TexArray_TexelSize.xy;
                    float2 uvRot   = tileId + InsetLocalUV(localRot, insetUV);

                    float a = SAMPLE_TEXTURE2D_ARRAY(_TexArray, sampler_TexArray, uvRot, slice).a * _BaseColor.a;
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

            TEXTURE2D_ARRAY(_TexArray); SAMPLER(sampler_TexArray);
            float4 _TexArray_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;

                float  _ProjectionMode;
                float4 _Tiling;
                float  _CylTilesAround;
                float  _CylTilesPerUnitY;
                float4 _CylUVOffset;

                float  _TileInsetTexels;
                float  _SliceCount;

                float  _Weight0,_Weight1,_Weight2,_Weight3,_Weight4,_Weight5,_Weight6,_Weight7;
                float  _Weight8,_Weight9,_Weight10,_Weight11,_Weight12,_Weight13,_Weight14,_Weight15;

                float  _UseRandomRotationPerTile;
                float  _ManualRotationQuarterTurns;
                float  _RandomMirrorU;
                float  _RandomMirrorV;

                float  _Seed;

                float  _Cutoff;

                float4 _EmissionColor;
                float  _EmissionIntensity;
                float  _EmissivePulseSpeed;
                float  _EmissiveMin;
                float  _EmissiveMax;

                float  _UseRandomEmissionColor;
                float  _EmissionColorCount;
                float4 _EmissionColor0;
                float4 _EmissionColor1;
                float4 _EmissionColor2;

                float  _UseEmissionLumaMask;
                float  _EmiMaskThreshold;
                float  _EmiMaskFeather;
                float  _EmiNeutralTolerance;

                float4 _MacroST;
                float  _MacroStrength;

                float  _Metallic;
                float  _Smoothness;
                float  _Occlusion;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 positionOS:TEXCOORD1; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.positionOS = v.positionOS.xyz;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float2 uvTiled;
                if (_ProjectionMode < 0.5)
                    ComputeUV_Mesh(i.uv, _Tiling, uvTiled, /*ddx*/uvTiled, /*ddy*/uvTiled);
                else {
                    float2 ddxBase, ddyBase;
                    ComputeUV_Cyl(i.positionOS, float2(_CylTilesAround, _CylTilesPerUnitY), _CylUVOffset.xy, uvTiled, ddxBase, ddyBase);
                }

                float2 tileId  = floor(uvTiled);
                float2 local   = frac(uvTiled);

                if (_RandomMirrorU > 0.5) if (Hash12(tileId + 101.3, _Seed) > 0.5) local.x = 1.0 - local.x;
                if (_RandomMirrorV > 0.5) if (Hash12(tileId + 202.6, _Seed) > 0.5) local.y = 1.0 - local.y;

                int slice = SelectWeightedSlice(tileId, _SliceCount,
                                                _Weight0,_Weight1,_Weight2,_Weight3,_Weight4,_Weight5,_Weight6,_Weight7,
                                                _Weight8,_Weight9,_Weight10,_Weight11,_Weight12,_Weight13,_Weight14,_Weight15,
                                                _Seed);
                int q     = SelectRotationQuarter(tileId, _UseRandomRotationPerTile, _ManualRotationQuarterTurns, _Seed);
                float2 localRot = RotateLocalQuarter(local, q);

                float2 insetUV = _TileInsetTexels * _TexArray_TexelSize.xy;
                float2 uvRot   = tileId + InsetLocalUV(localRot, insetUV);

                float4 s = SAMPLE_TEXTURE2D_ARRAY(_TexArray, sampler_TexArray, uvRot, slice);
                float3 albedo = s.rgb * _BaseColor.rgb;
                float  alpha  = s.a   * _BaseColor.a;

                #ifdef _ALPHATEST_ON
                    clip(alpha - _Cutoff);
                #endif

                float3 emiCol = SelectEmissionColorPerTile(tileId, _UseRandomEmissionColor, _EmissionColorCount,
                                                           _EmissionColor0.rgb, _EmissionColor1.rgb, _EmissionColor2.rgb, _EmissionColor.rgb,
                                                           _Seed) * _EmissionIntensity * _EmissiveMax;

                if (_UseEmissionLumaMask > 0.5)
                {
                    float luma = dot(albedo, float3(0.2126, 0.7152, 0.0722));
                    float l0 = _EmiMaskThreshold - _EmiMaskFeather;
                    float l1 = _EmiMaskThreshold + _EmiMaskFeather;
                    float maskL = smoothstep(l0, l1, luma);

                    float mx = max(max(albedo.r, albedo.g), albedo.b);
                    float mn = min(min(albedo.r, albedo.g), albedo.b);
                    float sat = (mx - mn) / max(mx, 1e-5);
                    float maskN = step(sat, _EmiNeutralTolerance);

                    emiCol *= (maskL * maskN);
                }

                MetaInput meta;
                meta.Albedo = albedo;
                meta.Emission = emiCol;
                return UnityMetaFragment(meta);
            }
            ENDHLSL
        }
    }

    FallBack Off
}