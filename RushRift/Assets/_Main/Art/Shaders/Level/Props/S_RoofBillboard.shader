Shader "S_RoofBillboard"
{
    Properties
    {
        _BaseTex ("Base Color (RGB) Alpha (A)", 2D) = "white" {}
        _Color ("Base Tint", Color) = (1,1,1,1)
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Smoothness ("Smoothness", Range(0,1)) = 0.6

        _NormalTex ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0,2)) = 1.0

        _EmissTex ("Emission Texture", 2D) = "white" {}
        _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _EmissionStrength ("Emission Strength", Range(0,20)) = 5.0

        _ScrollSpeed ("UV Scroll (X,Y)", Vector) = (0.1, 0.0, 0, 0)
        _GlitchStrength ("Glitch Strength", Range(0,0.05)) = 0.015
        _ScanlineDensity ("Scanline Density", Range(0,2000)) = 1200
        _FlickerSpeed ("Flicker Speed", Range(0,20)) = 8
        _FlickerAmount ("Flicker Amount", Range(0,1)) = 0.2

        _Alpha ("Alpha", Range(0,1)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalRenderPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        LOD 300
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 tangentOS  : TANGENT;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 tangentWS   : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float3 posWS       : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseTex);   SAMPLER(sampler_BaseTex);
            TEXTURE2D(_NormalTex); SAMPLER(sampler_NormalTex);
            TEXTURE2D(_EmissTex);  SAMPLER(sampler_EmissTex);

            float4 _BaseTex_ST;
            float4 _Color;
            float  _Metallic;
            float  _Smoothness;

            float  _NormalStrength;

            float4 _EmissionColor;
            float  _EmissionStrength;

            float4 _ScrollSpeed;
            float  _GlitchStrength;
            float  _ScanlineDensity;
            float  _FlickerSpeed;
            float  _FlickerAmount;

            float  _Alpha;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.posWS       = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS    = normalize(TransformObjectToWorldNormal(IN.normalOS));

                float3 T = normalize(TransformObjectToWorldDir(IN.tangentOS.xyz));
                float3 B = cross(OUT.normalWS, T) * IN.tangentOS.w;
                OUT.tangentWS   = T;
                OUT.bitangentWS = B;

                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseTex);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float t = _Time.y;

                // UV animadas
                float2 uv = IN.uv + _ScrollSpeed.xy * t;

                // glitch horizontal leve
                float g = sin(uv.y * 120.0 + t * 25.0) * 0.5 + 0.5;
                uv.x += (g - 0.5) * _GlitchStrength;

                // Base color
                float4 baseSample = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, uv) * _Color;
                half3 baseColor = baseSample.rgb;

                // Normal en TS -> WS
                float3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, uv), _NormalStrength);
                float3x3 TBN = float3x3(normalize(IN.tangentWS), normalize(IN.bitangentWS), normalize(IN.normalWS));
                float3 normalWS = normalize(mul(nTS, TBN));

                // Emission (scanlines + flicker)
                float scan = 1.0 + sin(uv.y * _ScanlineDensity + t * 2.0) * 0.05;
                float flicker = 1.0 + (sin(t * _FlickerSpeed) * 0.5 + 0.5) * _FlickerAmount;
                float3 emissTex = SAMPLE_TEXTURE2D(_EmissTex, sampler_EmissTex, uv).rgb;
                float3 emission = emissTex * _EmissionColor.rgb * _EmissionStrength * scan * flicker;

                // --- IMPORTANTES: inicializar en cero ---
                SurfaceData surf = (SurfaceData)0;
                surf.albedo     = baseColor;
                surf.metallic   = _Metallic;
                surf.specular   = float3(0.0, 0.0, 0.0);   // workflow metálico
                surf.smoothness = _Smoothness;
                surf.normalTS   = nTS;
                surf.occlusion  = 1.0;
                surf.emission   = emission;
                surf.alpha      = baseSample.a * _Alpha;

                InputData inp = (InputData)0;
                inp.positionWS        = IN.posWS;
                inp.normalWS          = normalWS;
                inp.viewDirectionWS   = GetWorldSpaceViewDir(IN.posWS);
                inp.shadowCoord       = TransformWorldToShadowCoord(IN.posWS);
                inp.fogCoord          = 0;
                inp.vertexLighting    = 0;
                inp.bakedGI           = SampleSH(normalWS);

                half4 col = UniversalFragmentPBR(inp, surf);
                col.rgb = MixFog(col.rgb, inp.fogCoord);
                return col;
            }
            ENDHLSL
        }
    }
    FallBack Off
}


