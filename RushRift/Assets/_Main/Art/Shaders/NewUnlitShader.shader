Shader "Skybox/NightProcedural"
{
    Properties
    {
        _ZenithColor    ("Zenith Color", Color) = (0.03,0.06,0.18,1)
        _HorizonColor   ("Horizon Color", Color) = (0.01,0.01,0.04,1)
        _SkyExponent    ("Sky Gradient Power", Range(0.1,8)) = 1.6
        _Exposure       ("Exposure", Range(0.25,3)) = 1

        _Rotation       ("Sky Rotation (deg)", Range(0,360)) = 0

        _StarDensity    ("Star Grid (cells)", Range(200,3000)) = 1200
        _StarSize       ("Star Size", Range(0.05,2)) = 0.6
        _StarIntensity  ("Star Intensity", Range(0,4)) = 1.2
        _TwinkleSpeed   ("Twinkle Speed", Range(0,20)) = 4

        _CloudColor     ("Cloud Color", Color) = (0.55,0.65,0.9,1)
        _CloudOpacity   ("Cloud Opacity", Range(0,1)) = 0.55
        _CloudScale     ("Cloud Scale", Range(0.5,10)) = 2.2
        _CloudSpeed     ("Cloud Speed", Range(0,1)) = 0.05
        _CloudHorizonBoost ("Cloud Horizon Boost", Range(0,5)) = 1.5

        _MoonColor      ("Moon Color", Color) = (0.85,0.9,1,1)
        _MoonSize       ("Moon Size (deg)", Range(1,30)) = 6
        _MoonGlow       ("Moon Glow", Range(0,3)) = 1.0
        _MoonDir        ("Moon Direction (world)", Vector) = (0,0.2,1,0)
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Front
        ZWrite Off
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            #define PI 3.14159265
            #define TAU 6.2831853
            float4 _ZenithColor, _HorizonColor, _CloudColor, _MoonColor;
            float _SkyExponent, _Exposure, _Rotation;

            float _StarDensity, _StarSize, _StarIntensity, _TwinkleSpeed;

            float _CloudOpacity, _CloudScale, _CloudSpeed, _CloudHorizonBoost;

            float _MoonSize, _MoonGlow;
            float4 _MoonDir;

            struct appdata { float3 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            float3 rotateY(float3 v, float angRad)
            {
                float s = sin(angRad), c = cos(angRad);
                return float3(c*v.x + s*v.z, v.y, -s*v.x + c*v.z);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // Direction from skybox center; rotate around Y for simple time-of-day
                float3 d = normalize(mul((float3x3)unity_ObjectToWorld, v.vertex));
                o.dir = rotateY(d, radians(_Rotation));
                return o;
            }

            // ---- hash / noise ---------------------------------------------------------
            float hash21(float2 p)
            {
                p = frac(p*float2(123.34, 456.21));
                p += dot(p, p+45.32);
                return frac(p.x*p.y);
            }

            float noise2(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1));
                float d = hash21(i + float2(1,1));
                float2 u = f*f*(3.0-2.0*f);
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            float fbm(float2 p)
            {
                float v=0.0, a=0.5;
                float2 shift = float2(37.1,17.3);
                [unroll] for (int i=0;i<5;i++)
                {
                    v += a * noise2(p);
                    p = p*2.0 + shift;
                    a *= 0.5;
                }
                return v;
            }

            float2 toSphericalUV(float3 dir) // equirectangular UV from direction
            {
                float2 uv;
                uv.x = atan2(dir.z, dir.x) * (1.0/TAU) + 0.5;
                uv.y = asin(dir.y) * (1.0/PI) + 0.5;
                return uv;
            }

            // Antialiased cell-based star field
            float starCell(float2 uv, float density, float size, float twinklePhase)
            {
                float2 p = uv * density;
                float2 g = floor(p);
                float2 f = frac(p);

                float rnd = hash21(g);                          // random per-cell
                float2 starPos = frac(float2(rnd, rnd*1.2154)); // random position
                float d = length(f - starPos);                  // distance to star center

                float aa = fwidth(d) + 1e-3;                    // anti-alias
                float disc = smoothstep(size+aa, size-aa, d);

                float hasStar = step(0.985, rnd);               // ~1.5% of cells get a star
                float tw = 0.75 + 0.25*sin(twinklePhase + rnd*TAU);
                return disc * hasStar * tw;
            }

            float3 baseSky(float y01, float3 horizon, float3 zenith, float powK)
            {
                return lerp(horizon, zenith, pow(saturate(y01), powK));
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);
                float y01 = dir.y * 0.5 + 0.5;                  // -1..1 → 0..1
                float2 uv = toSphericalUV(dir);

                // --- base sky gradient
                float3 col = baseSky(y01, _HorizonColor.rgb, _ZenithColor.rgb, _SkyExponent);

                // --- stars (two layers for richness)
                float tp = _Time.y * _TwinkleSpeed;
                float s1 = starCell(uv, _StarDensity,     _StarSize*0.020, tp);
                float s2 = starCell(uv*1.7 + 0.123, _StarDensity*0.7, _StarSize*0.016, tp*1.3);
                col += (s1 + s2) * _StarIntensity;

                // --- moon (disc + glow), direction in world space
                float3 md = normalize(_MoonDir.xyz);
                float dDot = dot(dir, md);
                float cosSize = cos(radians(_MoonSize));
                float moonDisc = smoothstep(cosSize, cosSize + 0.002, dDot);
                float moonGlow = smoothstep(cos(radians(_MoonSize*3.0)), 1.0, dDot) * _MoonGlow;
                col += _MoonColor.rgb * (moonGlow * 0.25);      // soft halo
                col = lerp(col, _MoonColor.rgb, moonDisc);      // hard disc

                // --- clouds (fbm 2D on spherical UV)
                float2 wind = float2(0.03, 0.01) * (_Time.y * _CloudSpeed * 100.0);
                float2 cuv = uv * _CloudScale * 8.0 + wind;
                float n = fbm(cuv);                              // 0..1
                float clouds = smoothstep(0.45, 0.75, n);
                float horizonBoost = pow(1.0 - saturate(dir.y), _CloudHorizonBoost);
                clouds *= horizonBoost;
                col = lerp(col, _CloudColor.rgb, clouds * _CloudOpacity);

                return float4(col * _Exposure, 1);
            }
            ENDHLSL
        }
    }
}
