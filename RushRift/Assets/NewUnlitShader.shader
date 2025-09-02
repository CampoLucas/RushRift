Shader "Custom/URP/VolumetricBoxFog_AdvancedNoise_DepthOccl"
{
    Properties
    {
        [Header(Volume)]
        _BaseDensity ("Base Density", Range(0,5)) = 1.0
        _BottomBoost ("Bottom Boost (power)", Range(0,6)) = 2.5
        _EdgeFade    ("Edge Fade (units in local box)", Range(0,0.25)) = 0.03

        [Header(Layers)]
        _LayerCount    ("Layer Count", Range(0,64)) = 12
        _LayerFeather  ("Layer Feather", Range(0,0.5)) = 0.15
        _LayerStrength ("Layer Strength", Range(0,2)) = 0.6

        [Header(Color Gradient)]
        [HDR]_ColorBottom ("Color Bottom", Color) = (0.35,0.6,0.9,1)
        [HDR]_ColorMid    ("Color Mid",    Color) = (0.6,0.8,0.9,1)
        [HDR]_ColorTop    ("Color Top",    Color) = (0.95,0.95,1.0,1)
        _MidHeight        ("Mid Height (0..1)", Range(0,1)) = 0.5

        [Header(Noise Base)]
        _NoiseStrength ("Noise Strength", Range(0,1)) = 0.35
        _NoiseScale    ("Noise Scale (xyz)", Vector) = (4,6,4,0)

        [Header(Advanced Noise)]
        _NoiseOctaves      ("Noise Octaves (1-6)", Range(1,6)) = 4
        _NoiseLacunarity   ("Noise Lacunarity", Range(1.2,3.0)) = 2.0
        _NoiseGain         ("Noise Gain", Range(0.2,0.9)) = 0.5
        _NoiseVelocity     ("Noise Velocity (obj space xyz)", Vector) = (0.0, 0.2, 0.0, 0)

        _DomainWarpStrength("Domain Warp Strength", Range(0,0.5)) = 0.15
        _DomainWarpScale   ("Domain Warp Scale", Range(0.1,4)) = 1.0

        [Header(Scene Occlusion)]
        [Toggle]_UseSceneDepthOcclusion ("Use Scene Depth Occlusion", Float) = 1
        _DepthSoftness ("Depth Softness (world units)", Range(0.0,1.0)) = 0.1
        _DepthBias     ("Depth Bias (world units)", Range(-0.5,0.5)) = 0.0

        [Header(Integration)]
        _Steps      ("Ray Steps", Range(8,256)) = 96
        _Jitter     ("Jitter", Range(0,1)) = 0.4

        [Header(Alpha)]
        _Opacity    ("Opacity Multiplier", Range(0,2)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Pass
        {
            Name "VolumetricBox"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Camera depth texture (URP) helpers
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float  _BaseDensity;
                float  _BottomBoost;
                float  _EdgeFade;

                float  _LayerCount;
                float  _LayerFeather;
                float  _LayerStrength;

                float4 _ColorBottom;
                float4 _ColorMid;
                float4 _ColorTop;
                float  _MidHeight;

                float  _NoiseStrength;
                float4 _NoiseScale;

                float  _NoiseOctaves;
                float  _NoiseLacunarity;
                float  _NoiseGain;
                float4 _NoiseVelocity;

                float  _DomainWarpStrength;
                float  _DomainWarpScale;

                float  _UseSceneDepthOcclusion;
                float  _DepthSoftness;
                float  _DepthBias;

                float  _Steps;
                float  _Jitter;

                float  _Opacity;
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
                float3 positionOS : TEXCOORD1;
                float2 screenUV   : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            static const int MAX_STEPS = 256;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float Hash31(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float Noise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float n000 = Hash31(i + float3(0,0,0));
                float n100 = Hash31(i + float3(1,0,0));
                float n010 = Hash31(i + float3(0,1,0));
                float n110 = Hash31(i + float3(1,1,0));
                float n001 = Hash31(i + float3(0,0,1));
                float n101 = Hash31(i + float3(1,0,1));
                float n011 = Hash31(i + float3(0,1,1));
                float n111 = Hash31(i + float3(1,1,1));
                float3 u = f*f*(3.0 - 2.0*f);
                float n00 = lerp(n000, n100, u.x);
                float n10 = lerp(n010, n110, u.x);
                float n01 = lerp(n001, n101, u.x);
                float n11 = lerp(n011, n111, u.x);
                float n0  = lerp(n00, n10, u.y);
                float n1  = lerp(n01, n11, u.y);
                return lerp(n0, n1, u.z); // 0..1
            }

            float FBM3D(float3 p, int octaves, float lac, float gain)
            {
                octaves = clamp(octaves, 1, 8);
                float amp = 1.0, sum = 0.0, norm = 0.0;
                [unroll] for (int o = 0; o < 8; ++o)
                {
                    if (o >= octaves) break;
                    sum  += Noise3D(p) * amp;
                    norm += amp;
                    p    *= lac;
                    amp  *= gain;
                }
                return (norm > 1e-6) ? (sum / norm) : 0.0;
            }

            float3 DomainWarpVec(float3 p)
            {
                float x = Noise3D(p + float3(31.7, 0.0, 0.0)) * 2.0 - 1.0;
                float y = Noise3D(p + float3(0.0, 57.3, 0.0)) * 2.0 - 1.0;
                float z = Noise3D(p + float3(0.0, 0.0, 101.1)) * 2.0 - 1.0;
                return float3(x,y,z);
            }

            float3 Gradient3(float h)
            {
                h = saturate(h);
                float mid = saturate(_MidHeight);
                if (h <= mid)
                {
                    float t = (mid > 1e-4) ? saturate(h / mid) : 0.0;
                    return lerp(_ColorBottom.rgb, _ColorMid.rgb, t);
                }
                else
                {
                    float denom = max(1e-4, 1.0 - mid);
                    float t = saturate((h - mid) / denom);
                    return lerp(_ColorMid.rgb, _ColorTop.rgb, t);
                }
            }

            bool RayBoxIntersect(float3 ro, float3 rd, float3 bmin, float3 bmax, out float t0, out float t1)
            {
                float3 inv = 1.0 / rd;
                float3 tA = (bmin - ro) * inv;
                float3 tB = (bmax - ro) * inv;
                float3 tsm = min(tA, tB);
                float3 tsM = max(tA, tB);
                t0 = max(tsm.x, max(tsm.y, tsm.z));
                t1 = min(tsM.x, min(tsM.y, tsM.z));
                return (t1 > max(t0, 0.0));
            }

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.positionOS = v.positionOS.xyz;

                float2 ndc = pos.positionCS.xy / max(pos.positionCS.w, 1e-6);
                o.screenUV = ndc * 0.5 + 0.5;

                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 camWS = GetCameraPositionWS();
                float3 camOS = TransformWorldToObject(camWS);

                float3 posWS = i.positionWS;
                float3 rdWS = normalize(posWS - camWS);
                float3 rdOS = normalize(TransformWorldToObjectDir(rdWS));

                float3 bmin = float3(-0.5, -0.5, -0.5);
                float3 bmax = float3( 0.5,  0.5,  0.5);

                float tEnter, tExit;
                if (!RayBoxIntersect(camOS, rdOS, bmin, bmax, tEnter, tExit))
                    return 0;

                tEnter = max(tEnter, 0.0);
                float segLen = max(0.0, tExit - tEnter);
                if (segLen <= 1e-5) return 0;

                // Scene depth in view space (eye units)
                float sceneEye = 1e9;
                if (_UseSceneDepthOcclusion > 0.5)
                {
                    float rawDepth = SampleSceneDepth(i.screenUV);
                    sceneEye = LinearEyeDepth(rawDepth, _ZBufferParams) + _DepthBias;
                }

                int steps = (int)clamp(_Steps, 8.0, (float)MAX_STEPS);
                float dt = segLen / steps;

                float jitter = (_Jitter > 1e-4) ? (_Jitter * (Hash21(i.screenUV * _ScreenParams.xy) - 0.5)) : 0.0;
                float t = tEnter + dt * jitter;

                float3 accum = 0;
                float transmittance = 1.0;

                for (int s = 0; s < MAX_STEPS; ++s)
                {
                    if (s >= steps) break;
                    if (transmittance < 1e-3) break;

                    float3 pOS = camOS + rdOS * t;

                    float3 dWalls = 0.5 - abs(pOS);
                    float edge = saturate(min(dWalls.x, min(dWalls.y, dWalls.z)));
                    float edgeFade = (_EdgeFade > 1e-6) ? saturate(edge / _EdgeFade) : 1.0;

                    float h = saturate(pOS.y + 0.5);

                    float dens = _BaseDensity * pow(1.0 - h, _BottomBoost);

                    if (_LayerCount > 0.5)
                    {
                        float u = frac(h * _LayerCount);
                        float band = smoothstep(0.0, _LayerFeather, u) * (1.0 - smoothstep(1.0 - _LayerFeather, 1.0, u));
                        dens *= (1.0 + _LayerStrength * band);
                    }

                    if (_NoiseStrength > 1e-3)
                    {
                        float3 pVelOS = _NoiseVelocity.xyz * _Time.y;
                        float3 pBase   = pOS + pVelOS;

                        float3 warp = 0;
                        if (_DomainWarpStrength > 1e-4)
                        {
                            warp = DomainWarpVec(pBase * _DomainWarpScale) * _DomainWarpStrength;
                        }

                        float3 pN = (pBase + warp) * _NoiseScale.xyz;

                        int   octs = (int)round(_NoiseOctaves);
                        float n = FBM3D(pN, octs, _NoiseLacunarity, _NoiseGain); // 0..1

                        float nMul = lerp(1.0, lerp(0.65, 1.35, n), _NoiseStrength);
                        dens *= nMul;
                    }

                    // Depth-based occlusion & smoothing near building edges
                    if (_UseSceneDepthOcclusion > 0.5)
                    {
                        // Convert current sample to eye depth (view space units)
                        float3 pWS = TransformObjectToWorld(pOS);
                        float3 pVS = TransformWorldToView(pWS);
                        float eye = -pVS.z;

                        // Fade out as we approach scene surface within _DepthSoftness
                        float df = saturate((sceneEye - eye) / max(_DepthSoftness, 1e-4));
                        dens *= df;

                        // If we are past the scene surface, stop
                        if (eye > sceneEye) break;
                    }

                    dens *= edgeFade;

                    float3 fogCol = Gradient3(h);

                    float absorb = exp(-dens * dt);
                    float contrib = (1.0 - absorb) * transmittance;

                    accum += fogCol * contrib;
                    transmittance *= absorb;

                    t += dt;
                }

                float alpha = saturate((1.0 - transmittance) * _Opacity);
                return half4(accum, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
