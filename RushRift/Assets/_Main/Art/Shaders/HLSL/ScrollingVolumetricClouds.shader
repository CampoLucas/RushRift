Shader "Custom/BetterCloudShader"
{
    Properties
    {
        _CloudColor ("Cloud Color", Color) = (1,1,1,1)
        _CloudAlpha ("Cloud Alpha", Range(0,1)) = 1
        _CloudSpeed ("Cloud Speed", Vector) = (0.1, 0.1, 0, 0)
        _CloudScale ("Cloud Scale", Float) = 5
        _DistortionStrength ("Distortion Strength", Float) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _CloudColor;
            float _CloudAlpha;
            float4 _CloudSpeed;
            float _CloudScale;
            float _DistortionStrength;
            float _currentTime;

            // --- Perlin Noise functions ---
            float hash(float2 p)
            {
                p = 50.0 * frac(p * 0.3183099 + float2(0.71, 0.113));
                return frac(p.x * p.y * (p.x + p.y));
            }

            float perlin(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // --- Fractal Brownian Motion (FBM) ---
            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;

                for (int i = 0; i < 5; i++) // 5 octaves
                {
                    value += perlin(p * frequency) * amplitude;
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }

                return value;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Move UVs with time and speed
                float2 movingUV = i.uv * _CloudScale + (_currentTime.xx * _CloudSpeed.xy);

                // Apply distortion
                float distortion = fbm(movingUV * 3.0) * _DistortionStrength;
                movingUV += distortion;

                // Get cloud value from FBM noise
                float cloudValue = fbm(movingUV);

                // Sharpen cloud edges
                float cloudAlpha = smoothstep(0.4, 0.6, cloudValue);

                // Final color with controlled alpha
                float4 finalColor = _CloudColor;
                finalColor.a *= cloudAlpha * _CloudAlpha;

                return finalColor;
            }
            ENDHLSL
        }
    }
}



