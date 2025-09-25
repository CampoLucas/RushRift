Shader "URP/NeonWhiteClouds_Volume"
{
    Properties
    {
        // Sky
        _SkyTopColor      ("Sky Top Color", Color) = (0.52,0.76,1,1)
        _SkyHorizonColor  ("Sky Horizon Color", Color) = (0.35,0.65,1,1)
        _HorizonContrast  ("Horizon Contrast", Range(0.1,5)) = 1.4

        // Cloud toon colors
        _CloudLight       ("Cloud Light", Color) = (1,1,1,1)
        _CloudShadow      ("Cloud Shadow", Color) = (0.73,0.86,1,1)
        _RampSteps        ("Toon Steps", Range(1,5)) = 3
        _RampPower        ("Toon Curve", Range(0.3,3)) = 1.2

        // Shape (same as flat version)
        _Coverage         ("Coverage", Range(0,1)) = 0.55
        _Softness         ("Edge Softness", Range(0.001,0.5)) = 0.15

        // World->noise scale and scroll
        _WorldScale       ("World->Noise Scale", Range(0.0001,0.02)) = 0.001
        _Tiling1          ("Layer1 Tiling (x,y)", Vector) = (0.25, 0.25, 0, 0)
        _Dir1             ("Layer1 Direction (x,y)", Vector) = (1, 0.2, 0, 0)
        _Speed1           ("Layer1 Speed", Range(-2, 2)) = 0.15
        _Tiling2          ("Layer2 Tiling (x,y)", Vector) = (0.5, 0.5, 0, 0)
        _Dir2             ("Layer2 Direction (x,y)", Vector) = (-0.2, 1, 0, 0)
        _Speed2           ("Layer2 Speed", Range(-2, 2)) = 0.07
        _LayerMix         ("Layer2 Influence", Range(0,1)) = 0.35

        // Distortion
        _DistortScale     ("Distort Scale", Range(0.1,5)) = 1.5
        _DistortAmount    ("Distort Amount", Range(0,1)) = 0.15
        _DistortSpeed     ("Distort Speed", Range(0,3)) = 0.6

        // === Faux Volume controls ===
        _Height           ("Vertex Height (world units)", Range(0,30)) = 8
        _HeightPow        ("Height By Density Power", Range(0.2,4)) = 1.5
        _HeightHorizonFade("Height Horizon Fade", Range(0,3)) = 1.0

        _Thickness        ("Parallax Thickness (world)", Range(0,150)) = 60
        _VolumeSteps      ("Parallax Steps (1-8)", Range(1,8)) = 4
        _LightDir         ("Fake Light Dir (WS, normalized)", Vector) = (0.3,0.75,0.6,0)

        // Edge line (optional ink)
        _LineStrength     ("Edge Line Strength", Range(0,1)) = 0.25
        _LineWidth        ("Edge Line Width", Range(0.001,0.08)) = 0.02
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 posWS      : TEXCOORD1;
                float3 upWS       : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _SkyTopColor, _SkyHorizonColor;
                float  _HorizonContrast;

                float4 _CloudLight, _CloudShadow;
                float  _RampSteps, _RampPower;

                float  _Coverage, _Softness;

                float  _WorldScale;
                float2 _Tiling1, _Dir1;  float _Speed1;
                float2 _Tiling2, _Dir2;  float _Speed2;
                float  _LayerMix;

                float  _DistortScale, _DistortAmount, _DistortSpeed;

                float  _Height, _HeightPow, _HeightHorizonFade;

                float  _Thickness, _VolumeSteps;
                float4 _LightDir;

                float  _LineStrength, _LineWidth;
            CBUFFER_END

            // -------- noise helpers ----------
            float hash21(float2 p){
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }
            float noise2d(float2 p){
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1));
                float d = hash21(i + float2(1,1));
                float2 u = f*f*(3.0 - 2.0*f);
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }
            float fbm(float2 p){
                float a = 0.0, w = 0.5;
                [unroll] for(int k=0;k<4;k++){
                    a += w * noise2d(p);
                    p = p*2.0 + 31.7;
                    w *= 0.5;
                }
                return a;
            }

            // Same field used in vert & frag
            float cloudField(float2 worldXZ)
            {
                float2 uvBase = worldXZ * _WorldScale;

                float2 dUV = uvBase * _DistortScale + _Time.y * _DistortSpeed;
                float2 distort = (float2(noise2d(dUV), noise2d(dUV + 23.1)) - 0.5) * _DistortAmount;

                float2 p1 = (uvBase + distort) * _Tiling1 + _Dir1 * _Speed1 * _Time.y;
                float2 p2 = (uvBase + distort * 1.5) * _Tiling2 + _Dir2 * _Speed2 * _Time.y;

                float l1 = fbm(p1);
                float l2 = fbm(p2);
                return lerp(l1, l2, _LayerMix);   // 0..1 raw field
            }

            float toonPosterize(float x, float steps, float powK){
                x = saturate(pow(saturate(x), powK));
                steps = max(1.0, steps);
                return floor(x * steps) / (steps - 0.0001);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;

                // World transform
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 upWS  = normalize(mul((float3x3)unity_ObjectToWorld, float3(0,1,0)));

                // === Vertex heightfield ===
                float field = cloudField(posWS.xz);
                // Convert to "density" like in frag
                float density = smoothstep(_Coverage - _Softness, _Coverage + _Softness, field);
                float h = pow(saturate(density), _HeightPow) * _Height;

                // Reduce height near horizon (v=0 → horizon in my setup)
                float horizonV = saturate(v.uv.y);
                float horizonFade = saturate(pow(horizonV, _HeightHorizonFade));
                h *= horizonFade;

                posWS += upWS * h;

                o.positionCS = TransformWorldToHClip(posWS);
                o.posWS = posWS;
                o.uv = v.uv;
                o.upWS = upWS;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Sky gradient
                float v = saturate(i.uv.y);
                float3 sky = lerp(_SkyHorizonColor.rgb, _SkyTopColor.rgb, pow(v, _HorizonContrast));

                // View & parallax direction on the plane
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.posWS);
                float3 upWS = normalize(i.upWS);
                float3 rightWS = normalize(cross(upWS, float3(0,0,1)));
                // Fallback if degenerate
                rightWS = any(isnan(rightWS)) ? float3(1,0,0) : rightWS;
                float3 forwardWS = normalize(cross(rightWS, upWS));

                // How glancing the view is relative to the plane (0=down, 1=horizon)
                float parallaxStrength = 1.0 - abs(dot(viewDir, upWS));

                // === Cheap volume: sample along view dir projected into plane ===
                int steps = (int)round(clamp(_VolumeSteps, 1, 8));
                float accum = 0.0;
                float weightSum = 0.0;

                for (int s = 0; s < 8; s++)
                {
                    float enabled = step(s, steps - 1);
                    float t = (s + 0.5) / max(1, steps);        // 0..1 mid-sample
                    float distWorld = t * _Thickness * parallaxStrength;

                    // Project view direction into the plane (XZ) for offset
                    float3 dirPlane = normalize(viewDir - dot(viewDir, upWS) * upWS);
                    float2 worldOffset = (dirPlane.xz) * distWorld;

                    float field = cloudField(i.posWS.xz + worldOffset);
                    float density = smoothstep(_Coverage - _Softness, _Coverage + _Softness, field);

                    // Front-to-back accumulation (thin participating media)
                    float w = 1.0 / steps;
                    accum += enabled * density * w;
                    weightSum += enabled * w;
                }

                float densityAvg = (weightSum > 0) ? accum : 0.0;

                // Toon banding
                float band = toonPosterize(densityAvg, _RampSteps, _RampPower);
                float3 cloudCol = lerp(_CloudShadow.rgb, _CloudLight.rgb, band);

                // Fake lighting from noise gradient (heightfield normal)
                float2 d = float2(0.002 / max(_WorldScale, 1e-4), 0); // small world step
                float fC = cloudField(i.posWS.xz);
                float fX = cloudField(i.posWS.xz + float2(d.x, 0));
                float fY = cloudField(i.posWS.xz + float2(0, d.x));
                float3 nApprox = normalize(float3(fC - fX, 0.5, fC - fY)); // slope → normal in (x,y,z)= (x,height,z)
                // Rotate into world: x→right, y→up, z→forward
                float3 normalWS = normalize(nApprox.x * rightWS + nApprox.y * upWS + nApprox.z * forwardWS);

                float3 L = normalize(_LightDir.xyz);
                float lambert = saturate(dot(normalWS, L));
                cloudCol *= (0.65 + 0.35 * lambert); // subtle lighting

                // Edge ink from gradient of field
                float grad = abs(ddx(fC)) + abs(ddy(fC));
                float edge = smoothstep(_LineWidth*2.0, _LineWidth, grad) * densityAvg * (1.0 - densityAvg);
                cloudCol *= (1.0 - edge * _LineStrength);

                // Composite over sky
                float alpha = saturate(densityAvg);
                float3 finalRGB = lerp(sky, cloudCol, alpha);

                return half4(finalRGB, 1);
            }
            ENDHLSL
        }
    }
}