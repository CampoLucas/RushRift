Shader "Custom/URP/TileRotUnlit_ColorVar"
{
    Properties
    {
        _BaseMap   ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        _EmissionMap   ("Emission", 2D) = "black" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0)

        [Toggle] _AlphaClip ("Alpha Clipping", Float) = 0
        _Cutoff ("Cutoff", Range(0,1)) = 0.5

        _MaxRotation  ("Max Rotation (deg)", Range(0,180)) = 0
        [Toggle] _QuarterTurns ("Use Quarter Turns (0/90/180/270)", Float) = 1
        _Seed ("Random Seed", Float) = 1234.0

        [Header(Color Variation)]
        [Toggle] _EnableColorVariation ("Enable Per-Tile Color Variation", Float) = 1
        _HueShiftDegrees ("Hue Shift Max (±deg)", Range(0,180)) = 20
        _SaturationVariance ("Saturation Variance (±)", Range(0,1)) = 0.15
        _ValueVariance ("Value/Brightness Variance (±)", Range(0,1)) = 0.15
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

                float  _MaxRotation;
                float  _QuarterTurns;
                float  _Seed;

                float  _EnableColorVariation;
                float  _HueShiftDegrees;
                float  _SaturationVariance;
                float  _ValueVariance;
            CBUFFER_END

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

            float Hash12(float2 p)
            {
                p += _Seed;
                float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float2 Rotate2(float2 v, float s, float c)
            {
                return float2(v.x * c - v.y * s, v.x * s + v.y * c);
            }

            float2 ComputeTileRotatedUV(float2 uv0)
            {
                float2 uvTiled = uv0 * _BaseMap_ST.xy + _BaseMap_ST.zw;
                float2 tileId  = floor(uvTiled);
                float2 local   = frac(uvTiled);

                float r = Hash12(tileId);

                float angleDeg;
                if (_QuarterTurns > 0.5)
                {
                    int q = (int)floor(r * 4.0 + 0.5);
                    angleDeg = (q & 3) * 90.0;
                }
                else
                {
                    angleDeg = (r * 2.0 - 1.0) * _MaxRotation;
                }

                float a = radians(angleDeg);
                float2 localRot = Rotate2(local - 0.5, sin(a), cos(a)) + 0.5;

                return tileId + localRot;
            }

            float3 RGBtoHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0/3.0, 2.0/3.0, -1.0);
                float4 p = (c.g < c.b) ? float4(c.bg, K.wz) : float4(c.gb, K.xy);
                float4 q = (c.r < p.x) ? float4(p.xyw, c.r) : float4(c.r, p.yzx);
                float d = q.x - min(q.w, q.y);
                float e = 1e-10;
                float h = abs(q.z + (q.w - q.y) / (6.0 * d + e));
                float s = d / (q.x + e);
                float v = q.x;
                return float3(h, s, v);
            }

            float3 HSVtoRGB(float3 c)
            {
                float3 rgb = saturate(abs(frac(c.x + float3(0,2.0/3.0,1.0/3.0)) * 6.0 - 3.0) - 1.0);
                return lerp(float3(1,1,1), rgb, c.y) * c.z;
            }

            float3 ApplyPerTileColorVariation(float3 rgb, float2 tileId)
            {
                if (_EnableColorVariation < 0.5) return rgb;

                float r1 = Hash12(tileId + float2(17.123, 45.789));
                float r2 = Hash12(tileId + float2(91.337, 12.404));
                float r3 = Hash12(tileId + float2(7.777, 88.888));

                float hueShift = (_HueShiftDegrees / 360.0) * (r1 * 2.0 - 1.0);
                float satMul   = 1.0 + (r2 * 2.0 - 1.0) * _SaturationVariance;
                float valMul   = 1.0 + (r3 * 2.0 - 1.0) * _ValueVariance;

                float3 hsv = RGBtoHSV(rgb);
                hsv.x = frac(hsv.x + hueShift);
                hsv.y = saturate(hsv.y * satMul);
                hsv.z = saturate(hsv.z * valMul);
                return HSVtoRGB(hsv);
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
                float2 uvRot = ComputeTileRotatedUV(i.uv);

                float4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvRot);
                float3 baseColor  = baseSample.rgb * _BaseColor.rgb;
                float  alpha      = baseSample.a * _BaseColor.a;

                if (_AlphaClip > 0.5 && alpha < _Cutoff) discard;

                float2 tileId = floor(uvRot);
                baseColor = ApplyPerTileColorVariation(baseColor, tileId);

                float3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uvRot).rgb * _EmissionColor.rgb;

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
                float  _MaxRotation;
                float  _QuarterTurns;
                float  _Seed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            float Hash12(float2 p)
            {
                p += _Seed;
                float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float2 Rotate2(float2 v, float s, float c) { return float2(v.x * c - v.y * s, v.x * s + v.y * c); }

            float2 ComputeTileRotatedUV(float2 uv0)
            {
                float2 uvTiled = uv0 * _BaseMap_ST.xy + _BaseMap_ST.zw;
                float2 tileId  = floor(uvTiled);
                float2 local   = frac(uvTiled);
                float r = Hash12(tileId);
                float angleDeg = (_QuarterTurns > 0.5) ? ((int)floor(r * 4.0 + 0.5) & 3) * 90.0
                                                       : ((r * 2.0 - 1.0) * _MaxRotation);
                float a = radians(angleDeg);
                float2 localRot = Rotate2(local - 0.5, sin(a), cos(a)) + 0.5;
                return tileId + localRot;
            }

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, 0.0));
                o.uv = v.uv;
                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                if (_AlphaClip > 0.5)
                {
                    float2 uvRot = ComputeTileRotatedUV(i.uv);
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
