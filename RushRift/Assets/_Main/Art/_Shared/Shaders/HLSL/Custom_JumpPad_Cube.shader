Shader "Custom/JumpPadCube_DirectionFix"
{
    Properties
    {
        [Header(Color Pulsing)]
        [HDR] _PulseColor1 ("Pulse Color 1", Color) = (1, 0.4, 0, 1) 
        [HDR] _PulseColor2 ("Pulse Color 2", Color) = (1, 0, 0, 1)   
        _PulseDuration ("Pulse Duration (s)", Range(0.1, 10.0)) = 3.0
        _GeneralAlpha ("Overall Opacity", Range(0, 1)) = 0.8

        [Header(Ring Animation)]
        _Speed ("Ascent Speed", Range(0, 5)) = 1.0
        _Frequency ("Band Density", Range(1, 20)) = 4.0 
        
        [Header(Shape Settings)]
        _Thickness ("Band Sharpness", Range(1, 50)) = 30.0
        _FadeStart ("Fade Start Point (0-1)", Range(0.0, 0.99)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha 
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD8;
                // NEW: Carrying the local object position to the fragment shader
                float3 localPos : TEXCOORD7; 
            };

            float4 _PulseColor1;
            float4 _PulseColor2;
            float _PulseDuration;
            float _GeneralAlpha;
            float _Speed;
            float _Frequency;
            float _Thickness;
            float _FadeStart; 

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                
                // Pass the raw vertex position (Object Space)
                o.localPos = v.vertex.xyz; 
                
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // --- CALCULATION FIX ---
                // Instead of i.uv.y (which relies on texture mapping), we use i.localPos.y.
                // A standard Unity Cube goes from -0.5 (bottom) to 0.5 (top).
                // We add 0.5 to normalize it to a 0.0 to 1.0 range.
                float normalizedHeight = i.localPos.y + 0.5;

                // --- 1. Animation ---
                // Use normalizedHeight instead of i.uv.y
                float scrollingV = normalizedHeight - (_Time.y * _Speed);
                float pattern = sin(scrollingV * _Frequency * 3.14159);
                pattern = abs(pattern);
                pattern = 1.0 - pattern;
                float ringShape = pow(pattern, _Thickness);

                // --- 2. Vertical Fading ---
                // Use normalizedHeight here too, so the fade is always at the physical top
                float topFadeMask = 1.0 - smoothstep(_FadeStart, 1.0, normalizedHeight);

                // --- 3. Cube Face Masking ---
                // Mask out Top and Bottom faces
                float capMask = 1.0 - step(0.9, abs(i.worldNormal.y));
                
                // Combine masks
                float finalAlphaMask = ringShape * topFadeMask * capMask;

                // --- 4. Color Pulsing ---
                float pulse = 0.5 * (sin(_Time.y * 2.0 * 3.14159 / _PulseDuration) + 1.0);
                float4 finalEmissionColor = lerp(_PulseColor1, _PulseColor2, pulse);

                // --- 5. Final Output ---
                float3 finalColor = finalEmissionColor.rgb;
                float alpha = finalAlphaMask * _GeneralAlpha * finalEmissionColor.a;

                UNITY_APPLY_FOG(i.fogCoord, finalColor);

                return fixed4(finalColor, alpha);
            }
            ENDCG
        }
    }
}