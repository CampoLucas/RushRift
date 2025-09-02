Shader "Custom/URP/TileArrayUnlit_EmissiveVar_3Colors_Cyl"
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

        [Toggle] _AlphaClip ("Alpha Clipping", Float) = 0
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

            // Texture2DArray compat
            #ifndef SAMPLE_TEXTURE2D_ARRAY
                #define SAMPLE_TEXTURE2D_ARRAY(tex, samp, uv, slice) (tex).Sample((samp), float3((uv), (slice)))
            #endif
            #ifndef SAMPLE_TEXTURE2D_ARRAY_GRAD
                #define SAMPLE_TEXTURE2D_ARRAY_GRAD(tex, samp, uv, slice, ddx, ddy) (tex).SampleGrad((samp), float3((uv), (slice)), (ddx), (ddy))
            #endif

            // Guard: ensure INV_TWO_PI exists without redefining if Core.hlsl already provided it
            #ifndef INV_TWO_PI
                #define INV_TWO_PI 0.15915494
            #endif

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D_ARRAY(_TexArray); SAMPLER(sampler_TexArray);

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

                float  _AlphaClip;
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
            CBUFFER_END

            float4 _TexArray_TexelSize;

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float  fogCoord   : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            static const int MAX_SLICES = 16;

            float Hash12(float2 p)
            {
                p += _Seed;
                float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float GetWeightByIndex(int i)
            {
                if (i==0) return _Weight0;  if (i==1) return _Weight1;  if (i==2) return _Weight2;  if (i==3) return _Weight3;
                if (i==4) return _Weight4;  if (i==5) return _Weight5;  if (i==6) return _Weight6;  if (i==7) return _Weight7;
                if (i==8) return _Weight8;  if (i==9) return _Weight9;  if (i==10) return _Weight10; if (i==11) return _Weight11;
                if (i==12) return _Weight12; if (i==13) return _Weight13; if (i==14) return _Weight14; return _Weight15;
            }

            int SelectWeightedSlice(float2 tileId)
            {
                int count = (int)clamp(_SliceCount, 1.0, (float)MAX_SLICES);
                float total = 0.0;
                [unroll] for (int i=0;i<MAX_SLICES;i++) { if (i<count) total += max(0.0, GetWeightByIndex(i)); }
                if (total <= 0.0)
                {
                    float r = Hash12(tileId + 37.17);
                    return clamp((int)floor(r * count), 0, count - 1);
                }
                float pick = Hash12(tileId + 137.5) * total;
                float acc = 0.0;
                [unroll] for (int i=0;i<MAX_SLICES;i++)
                {
                    if (i>=count) break;
                    acc += max(0.0, GetWeightByIndex(i));
                    if (pick <= acc) return i;
                }
                return count - 1;
            }

            int SelectRotationQuarter(float2 tileId)
            {
                if (_UseRandomRotationPerTile > 0.5)
                {
                    float r = Hash12(tileId + 91.337);
                    return ((int)floor(r * 4.0)) & 3;
                }
                return ((int)round(_ManualRotationQuarterTurns)) & 3;
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

            float3 SelectEmissionColorPerTile(float2 tileId)
            {
                if (_UseRandomEmissionColor < 0.5) return _EmissionColor.rgb;
                int count = (int)clamp(_EmissionColorCount, 1.0, 3.0);
                float r = Hash12(tileId + float2(311.713, 902.114));
                int idx = clamp((int)floor(r * count), 0, count - 1);
                if (idx == 0) return _EmissionColor0.rgb;
                if (idx == 1) return _EmissionColor1.rgb;
                return _EmissionColor2.rgb;
            }

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.positionOS = v.positionOS.xyz;
                o.uv         = v.uv;
                o.fogCoord   = ComputeFogFactor(pos.positionCS.z);
                return o;
            }

            void ComputeUV_Mesh(in Varyings i, out float2 uvTiled, out float2 ddxBase, out float2 ddyBase)
            {
                uvTiled = i.uv * _Tiling.xy + _Tiling.zw;
                ddxBase = ddx(i.uv) * _Tiling.xy;
                ddyBase = ddy(i.uv) * _Tiling.xy;
            }

            void ComputeUV_Cyl(in Varyings i, out float2 uvTiled, out float2 ddxBase, out float2 ddyBase)
            {
                float3 p  = i.positionOS;
                float3 dx = ddx(i.positionOS);
                float3 dy = ddy(i.positionOS);

                float den   = max(p.x*p.x + p.z*p.z, 1e-8);
                float du_dx =  ( p.z * dx.x - p.x * dx.z) * INV_TWO_PI / den;
                float du_dy =  ( p.z * dy.x - p.x * dy.z) * INV_TWO_PI / den;

                float angle = atan2(p.x, p.z);          // [-pi,pi]
                float u     = 0.5 + angle * INV_TWO_PI; // [0,1)
                float v     = p.y;

                float2 uv   = float2(u, v) + _CylUVOffset.xy;

                float2 scale = float2(_CylTilesAround, _CylTilesPerUnitY);
                uvTiled = uv * scale;

                ddxBase = float2(du_dx, dx.y) * scale;
                ddyBase = float2(du_dy, dy.y) * scale;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 uvTiled, ddxBase, ddyBase;
                if (_ProjectionMode < 0.5)
                    ComputeUV_Mesh(i, uvTiled, ddxBase, ddyBase);
                else
                    ComputeUV_Cyl(i, uvTiled, ddxBase, ddyBase);

                float2 tileId  = floor(uvTiled);
                float2 local   = frac(uvTiled);

                int slice = SelectWeightedSlice(tileId);
                int q     = SelectRotationQuarter(tileId);

                if (_RandomMirrorU > 0.5)
                    if (Hash12(tileId + 101.3) > 0.5) { local.x = 1.0 - local.x; ddxBase.x = -ddxBase.x; ddyBase.x = -ddyBase.x; }
                if (_RandomMirrorV > 0.5)
                    if (Hash12(tileId + 202.6) > 0.5) { local.y = 1.0 - local.y; ddxBase.y = -ddxBase.y; ddyBase.y = -ddyBase.y; }

                float2 localRot = RotateLocalQuarter(local, q);

                float2 insetUV  = _TileInsetTexels * _TexArray_TexelSize.xy;
                float2 localIn  = InsetLocalUV(localRot, insetUV);
                float2 uvRot    = tileId + localIn;

                float2 ddxRot, ddyRot; RotateGradientsQuarter(ddxBase, ddyBase, q, ddxRot, ddyRot);

                float4 s = SAMPLE_TEXTURE2D_ARRAY_GRAD(_TexArray, sampler_TexArray, uvRot, slice, ddxRot, ddyRot);
                float3 baseRGB = s.rgb;
                float3 col = baseRGB * _BaseColor.rgb;
                float  a   = s.a * _BaseColor.a;

                if (_AlphaClip > 0.5 && a < _Cutoff) discard;

                float2 macroUV = i.positionWS.xz * _MacroST.xy + _MacroST.zw;
                col *= lerp(1.0, lerp(0.9, 1.1, MacroNoise(macroUV)), _MacroStrength);

                float phase = Hash12(tileId + float2(912.42, 314.159)) * 6.2831853;
                float pulse = lerp(_EmissiveMin, _EmissiveMax, 0.5 + 0.5 * sin(_Time.y * _EmissivePulseSpeed + phase));

                float3 emiColor = SelectEmissionColorPerTile(tileId) * _EmissionIntensity * pulse;

                float emiMask = 1.0;
                if (_UseEmissionLumaMask > 0.5)
                {
                    float luma = dot(baseRGB, float3(0.2126, 0.7152, 0.0722));
                    float l0 = _EmiMaskThreshold - _EmiMaskFeather;
                    float l1 = _EmiMaskThreshold + _EmiMaskFeather;
                    float maskL = smoothstep(l0, l1, luma);

                    float mx = max(max(baseRGB.r, baseRGB.g), baseRGB.b);
                    float mn = min(min(baseRGB.r, baseRGB.g), baseRGB.b);
                    float sat = (mx - mn) / max(mx, 1e-5);
                    float maskN = step(sat, _EmiNeutralTolerance);

                    emiMask = maskL * maskN;
                }

                col += emiColor * emiMask;

                col = MixFog(col, i.fogCoord);
                return float4(col, a);
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

            #ifndef SAMPLE_TEXTURE2D_ARRAY
                #define SAMPLE_TEXTURE2D_ARRAY(tex, samp, uv, slice) (tex).Sample((samp), float3((uv), (slice)))
            #endif

            // Same guard here too (safe if Core already defines it)
            #ifndef INV_TWO_PI
                #define INV_TWO_PI 0.15915494
            #endif

            // URP14 shim: Shadows.hlsl expects LerpWhiteTo from Color.hlsl
            #ifndef LerpWhiteTo
                inline half3 LerpWhiteTo(half3 b, half t) { return lerp(half3(1,1,1), b, t); }
            #endif
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D_ARRAY(_TexArray); SAMPLER(sampler_TexArray);

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

                float  _AlphaClip;
                float  _Cutoff;
            CBUFFER_END

            float4 _TexArray_TexelSize;

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 positionCS:SV_POSITION; float3 positionOS:TEXCOORD0; float2 uv:TEXCOORD1; };

            static const int MAX_SLICES = 16;

            float Hash12(float2 p)
            {
                p += _Seed;
                float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float GetWeightByIndex(int i)
            {
                if (i==0) return _Weight0;  if (i==1) return _Weight1;  if (i==2) return _Weight2;  if (i==3) return _Weight3;
                if (i==4) return _Weight4;  if (i==5) return _Weight5;  if (i==6) return _Weight6;  if (i==7) return _Weight7;
                if (i==8) return _Weight8;  if (i==9) return _Weight9;  if (i==10) return _Weight10; if (i==11) return _Weight11;
                if (i==12) return _Weight12; if (i==13) return _Weight13; if (i==14) return _Weight14; return _Weight15;
            }

            int SelectWeightedSlice(float2 tileId)
            {
                int count = (int)clamp(_SliceCount, 1.0, (float)MAX_SLICES);
                float total = 0.0;
                [unroll] for (int i=0;i<MAX_SLICES;i++) { if (i<count) total += max(0.0, GetWeightByIndex(i)); }
                if (total <= 0.0)
                {
                    float r = Hash12(tileId + 37.17);
                    return clamp((int)floor(r * count), 0, count - 1);
                }
                float pick = Hash12(tileId + 137.5) * total;
                float acc = 0.0;
                [unroll] for (int i=0;i<MAX_SLICES;i++)
                {
                    if (i>=count) break;
                    acc += max(0.0, GetWeightByIndex(i));
                    if (pick <= acc) return i;
                }
                return count - 1;
            }

            int SelectRotationQuarter(float2 tileId)
            {
                if (_UseRandomRotationPerTile > 0.5)
                {
                    float r = Hash12(tileId + 91.337);
                    return ((int)floor(r * 4.0)) & 3;
                }
                return ((int)round(_ManualRotationQuarterTurns)) & 3;
            }

            float2 RotateLocalQuarter(float2 local, int q)
            {
                q &= 3;
                if (q==0) return local;
                if (q==1) return float2(1.0 - local.y, local.x);
                if (q==2) return 1.0 - local;
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
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, TransformObjectToWorldNormal(v.normalOS), 0.0));
                o.positionOS = v.positionOS.xyz;
                o.uv = v.uv;
                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                if (_AlphaClip < 0.5) return 0;

                float2 uvTiled;
                if (_ProjectionMode < 0.5)
                {
                    uvTiled = i.uv * _Tiling.xy + _Tiling.zw;
                }
                else
                {
                    float3 p = i.positionOS;
                    float angle = atan2(p.x, p.z);
                    float u = 0.5 + angle * INV_TWO_PI;
                    float v = p.y;
                    uvTiled = (float2(u, v) + _CylUVOffset.xy) * float2(_CylTilesAround, _CylTilesPerUnitY);
                }

                float2 tileId  = floor(uvTiled);
                float2 local   = frac(uvTiled);

                if (_RandomMirrorU > 0.5) if (Hash12(tileId + 101.3) > 0.5) local.x = 1.0 - local.x;
                if (_RandomMirrorV > 0.5) if (Hash12(tileId + 202.6) > 0.5) local.y = 1.0 - local.y;

                int slice = SelectWeightedSlice(tileId);
                int q     = SelectRotationQuarter(tileId);
                float2 localRot = RotateLocalQuarter(local, q);

                float2 insetUV = _TileInsetTexels * _TexArray_TexelSize.xy;
                float2 uvRot   = tileId + InsetLocalUV(localRot, insetUV);

                float a = SAMPLE_TEXTURE2D_ARRAY(_TexArray, sampler_TexArray, uvRot, slice).a * _BaseColor.a;
                if (a < _Cutoff) discard;
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
