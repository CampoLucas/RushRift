Shader "Custom/URP/AtmosphericFog"
{
    Properties
    {
        [MainTexture] _NoiseTex ("Noise Texture (R channel)", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Float) = 0.02
        _NoiseSpeed ("Noise Speed (XY)", Vector) = (0.5, 0.1, 0, 0)

        [Header(Colors)]
        _FogColorMain ("Fog Color (Dense)", Color) = (0.5, 0.6, 0.7, 1)
        _FogColorSecond ("Fog Color (Thin)", Color) = (0.8, 0.8, 0.8, 1)
        
        [Header(Settings)]
        _FogStart ("Start Distance", Float) = 10.0
        _FogEnd ("End Distance", Float) = 50.0
        
        // UPDATED: Range increased to 10 for super dense fog
        _FogDensity ("Global Density Multiplier", Range(0, 10)) = 2.0
        // NEW: Controls how fast the fog thickens (Higher = thicker sooner)
        _FogPower ("Fog Curvature (Power)", Range(0.5, 5.0)) = 1.0
        
        _HeightFogBase ("Height Fog Base (Y)", Float) = 0.0
        _HeightFogFalloff ("Height Fog Falloff", Range(0.01, 2.0)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "AtmosphericFogPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // Inputs
            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _NoiseSpeed;
                float _NoiseScale;
                float4 _FogColorMain;
                float4 _FogColorSecond;
                float _FogStart;
                float _FogEnd;
                float _FogDensity;
                float _FogPower;     // New variable
                float _HeightFogBase;
                float _HeightFogFalloff;
            CBUFFER_END

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Manual Full Screen Triangle (Fixes "include not found" error)
                float2 uv = float2((input.vertexID << 1) & 2, input.vertexID & 2);
                output.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);

                #if UNITY_UV_STARTS_AT_TOP
                output.uv = uv * float2(1.0, -1.0) + float2(0.0, 1.0);
                #else
                output.uv = uv;
                #endif

                return output;
            }

            float3 ReconstructWorldPosition(float2 uv, float depth)
            {
                float depthValue = depth;
                #if !UNITY_REVERSED_Z
                    depthValue = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
                #endif

                // Fixed float4->float3 conversion error here
                float3 worldPos = ComputeWorldSpacePosition(uv, depthValue, UNITY_MATRIX_I_VP);
                return worldPos;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, input.uv);
                float depth = SampleSceneDepth(input.uv);
                float linearDepth = Linear01Depth(depth, _ZBufferParams);
                
                float3 worldPos = ReconstructWorldPosition(input.uv, depth);

                // --- DISTANCE CALCULATION ---
                float dist = distance(_WorldSpaceCameraPos, worldPos);
                
                // Basic linear 0-1 factor
                float distFactor = saturate((dist - _FogStart) / max(_FogEnd - _FogStart, 0.01));
                
                // NEW: Apply Power Curve (Makes it feel thicker/denser closer to camera)
                distFactor = pow(distFactor, 1.0 / _FogPower);

                // --- HEIGHT CALCULATION ---
                float heightFactor = exp(-(worldPos.y - _HeightFogBase) * _HeightFogFalloff);
                heightFactor = saturate(heightFactor);

                // --- NOISE CALCULATION ---
                float2 noiseUV = worldPos.xz * _NoiseScale + _Time.y * _NoiseSpeed.xy;
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

                // --- COMBINE ---
                // Multiply by _FogDensity (now up to 10.0) to overdrive the opacity
                float combinedDensity = distFactor * heightFactor * _FogDensity;
                
                // Use noise to break it up
                combinedDensity *= lerp(0.5, 1.5, noise); 
                
                // Clamp to 0-1 range for final alpha blend
                combinedDensity = saturate(combinedDensity);

                // --- COLORS ---
                float4 fogTint = lerp(_FogColorSecond, _FogColorMain, noise);

                // Don't fog the skybox
                if (linearDepth > 0.99) 
                {
                    combinedDensity = 0; 
                }

                return lerp(sceneColor, fogTint, combinedDensity);
            }
            ENDHLSL
        }
    }
}