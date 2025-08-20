Shader "URP/BeamPillar"
{
    Properties
    {
        _ColorA            ("Color A", Color) = (0.2,0.6,1,1)
        _ColorB            ("Color B", Color) = (1,0.4,0.1,1)
        _ColorSpeed        ("Color Shift Speed", Range(0,5)) = 1
        _ColorByHeight     ("Color Shift By Height", Range(0,6)) = 1

        _Emission          ("Beam Emission", Range(0,50)) = 8

        _NoiseScale        ("Noise Scale (U,V)", Vector) = (2,6,0,0)
        _NoiseSpeed        ("Noise Scroll Speed", Range(0,10)) = 1.5
        _NoiseAmount       ("Noise Amount", Range(0,2)) = 0.6

        _TopFadeStart      ("Top Fade Start (V)", Range(0,1)) = 0.6
        _BottomFadeEnd     ("Bottom Fade End (V)", Range(0,1)) = 0.08

        _ParticleColumns   ("Particle Columns (around U)", Range(1,64)) = 14
        _ParticleCount     ("Particles Per Column (1-4)", Range(1,4)) = 3
        _ParticleSpeed     ("Particle Speed", Range(0,10)) = 1.2
        _ParticleSize      ("Particle Size (vert)", Range(0.001,0.15)) = 0.035
        _ParticleWidth     ("Particle Width (horiz)", Range(0.02,0.6)) = 0.18
        _ParticleIntensity ("Particle Emission", Range(0,50)) = 12

        _FresnelPower      ("Edge Fresnel Power", Range(0,8)) = 3
        _FresnelIntensity  ("Edge Fresnel Emission", Range(0,10)) = 1.6

        // ---- cap removal (side-only rendering) ----
        _CapMin            ("Cap Mask Start (abs Ny)", Range(0,1)) = 0.35
        _CapMax            ("Cap Mask End (abs Ny)", Range(0,1)) = 0.75
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Blend One One      // additive
            ZWrite Off
            Cull Off           // view inside & outside

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;  // U=around, V=height
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float3 normalOS    : TEXCOORD3; // pass OS normal for cap mask
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA, _ColorB;
                float  _ColorSpeed, _ColorByHeight;
                float  _Emission;
                float2 _NoiseScale;
                float  _NoiseSpeed, _NoiseAmount;
                float  _TopFadeStart, _BottomFadeEnd;
                float  _ParticleColumns, _ParticleCount, _ParticleSpeed;
                float  _ParticleSize, _ParticleWidth, _ParticleIntensity;
                float  _FresnelPower, _FresnelIntensity;
                float  _CapMin, _CapMax;
            CBUFFER_END

            // ----- noise helpers -----
            float hash11(float p){ p = frac(p*0.1031); p*=p+33.33; p*=p+p; return frac(p); }
            float hash21(float2 p){ float3 q=frac(float3(p.xyx)*0.1031); q+=dot(q,q.yzx+33.33); return frac((q.x+q.y)*q.z); }
            float noise2d(float2 p){
                float2 i=floor(p), f=frac(p);
                float a=hash21(i);
                float b=hash21(i+float2(1,0));
                float c=hash21(i+float2(0,1));
                float d=hash21(i+float2(1,1));
                float2 u=f*f*(3.0-2.0*f);
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionWS  = TransformObjectToWorld(v.positionOS.xyz);
                o.positionHCS = TransformWorldToHClip(o.positionWS);
                o.normalWS    = TransformObjectToWorldNormal(v.normalOS);
                o.normalOS    = v.normalOS;
                o.uv          = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 uv = float2(frac(i.uv.x), saturate(i.uv.y));
                float v   = uv.y;

                // ---- color shift
                float t = 0.5 + 0.5 * sin(_ColorSpeed * _Time.y + v * _ColorByHeight * 3.14159);
                float3 baseCol = lerp(_ColorA.rgb, _ColorB.rgb, t);

                // ---- scrolling noise
                float2 nUV = float2(uv.x * _NoiseScale.x, uv.y * _NoiseScale.y + _Time.y * _NoiseSpeed);
                float n = noise2d(nUV);
                baseCol *= lerp(1.0 - _NoiseAmount, 1.0 + _NoiseAmount, n);

                // ---- fresnel
                float3 N = normalize(i.normalWS);
                float3 Vdir = normalize(GetWorldSpaceViewDir(i.positionWS));
                float fres = pow(1.0 - saturate(abs(dot(N, Vdir))), _FresnelPower);

                // ---- ascending particles
                float columns = max(1.0, _ParticleColumns);
                float uScaled = uv.x * columns;
                float colIdx  = floor(uScaled);
                float uInCol  = frac(uScaled);
                float xDist   = abs(uInCol - 0.5) * 2.0;
                float xMask   = saturate(1.0 - smoothstep(0.0, _ParticleWidth, xDist));
                float particles = 0.0;
                [unroll]
                for (int k = 0; k < 4; k++)
                {
                    float enabled = step(k, _ParticleCount - 0.001);
                    float seed    = colIdx + k * 7.123;
                    float y       = frac(hash11(seed) + _Time.y * _ParticleSpeed);
                    float yMask   = smoothstep(_ParticleSize, 0.0, abs(v - y));
                    particles += enabled * xMask * yMask;
                }

                // ---- top/bottom fades
                float topFade    = 1.0 - smoothstep(_TopFadeStart, 1.0, v);
                float bottomFade = smoothstep(0.0, _BottomFadeEnd, v);
                float fade = saturate(topFade * bottomFade);

                // ---- CAP MASK: hide top/bottom faces (object-space)
                // abs(Ny)=1 on caps, ~0 on side
                float absNy = abs(i.normalOS.y);
                float sideMask = 1.0 - smoothstep(_CapMin, _CapMax, absNy); // 1 on side, 0 on caps

                // ---- emission
                float3 col = 0;
                col += baseCol * _Emission;
                col += baseCol * (fres * _FresnelIntensity);
                col += baseCol * (particles * _ParticleIntensity);

                col *= fade * sideMask;

                return half4(col, fade * sideMask); // alpha kept for chaining
            }
            ENDHLSL
        }
    }
}
