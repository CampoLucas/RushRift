Shader "Custom/VortexShader"
{
    Properties
    {
        [Header(Texture)]
        _MainTex ("Sprite Texture", 2D) = "white" {}

        [Header(Color Gradient)]
        _Color1 ("Primary Color", Color) = (0.2, 0.07, 0.93, 1)
        _Color2 ("Secondary Color", Color) = (0.63, 0, 1, 1)
        _Color3 ("Tertiary Color", Color) = (1, 0.2, 0.5, 1)

        [Header(UV Distortion)]
        _DistortionStrength ("Distortion Strength", Float) = 0.05
        _Speed ("Wave Speed", Float) = 2.0
        _VortexSpeed ("Vortex Spin Speed", Float) = 0.3

        [Header(Pulse Settings)]
        _PulseSpeed ("Pulse Speed", Float) = 2.0
        _PulseIntensity ("Pulse Intensity", Float) = 0.3
        _PulseBase ("Pulse Base Brightness", Float) = 0.7

        [Header(Grain Overlay)]
        _GrainIntensity ("Grain Intensity", Range(0, 1)) = 0.15
        _GrainSize ("Grain Size", Float) = 400.0
        _GrainSpeed ("Grain Speed", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Sprite"
            "CanUseSpriteAtlas"="True"
        }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            fixed4 _Color1;
            fixed4 _Color2;
            fixed4 _Color3;

            float _DistortionStrength;
            float _Speed;

            float _PulseSpeed;
            float _PulseIntensity;
            float _PulseBase;

            float _GrainIntensity;
            float _GrainSize;
            float _GrainSpeed;

            float _VortexSpeed;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD1;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            float rand(float2 co)
            {
                return frac(sin(dot(co, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;

                float2 uv = i.uv;

                // === Radial Vortex Spin ===
                float2 fromCenter = uv - 0.5;
                float angleOffset = t * _VortexSpeed;
                float angle = atan2(fromCenter.y, fromCenter.x) + angleOffset;
                float radius = length(fromCenter);
                uv = float2(cos(angle), sin(angle)) * radius + 0.5;

                // === Swirl Distortion ===
                float2 center = float2(0.5, 0.5);
                float2 delta = uv - center;
                float dist = length(delta);
                float swirlAngle = atan2(delta.y, delta.x) + sin(t * _Speed + dist * 10.0) * 0.5;
                uv = center + dist * float2(cos(swirlAngle), sin(swirlAngle));

                // === Wavy UV Distortion ===
                uv.x += sin(uv.y * 10 + t * _Speed) * _DistortionStrength;
                uv.y += cos(uv.x * 10 + t * _Speed) * _DistortionStrength;

                // === Texture Sampling ===
                fixed4 texColor = tex2D(_MainTex, uv);

                // === 3-Color Gradient ===
                float wave = (sin(t * _Speed + uv.x * 10 + uv.y * 10) + 1.0) * 0.5;
                fixed4 finalColor;

                if (wave < 0.5)
                {
                    float blend = wave / 0.5;
                    finalColor = lerp(_Color1, _Color2, blend);
                }
                else
                {
                    float blend = (wave - 0.5) / 0.5;
                    finalColor = lerp(_Color2, _Color3, blend);
                }

                // === Pulsing Brightness ===
                float pulse = sin(_Time.y * _PulseSpeed) * _PulseIntensity + _PulseBase;
                finalColor.rgb *= pulse;

                // === Procedural Grain ===
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float2 grainUV = floor(screenUV * _GrainSize + _Time.y * _GrainSpeed);
                float noise = rand(grainUV);
                finalColor.rgb += (noise - 0.5) * _GrainIntensity;

                // === Apply Alpha ===
                finalColor *= texColor.a;
                finalColor.a = texColor.a;

                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}