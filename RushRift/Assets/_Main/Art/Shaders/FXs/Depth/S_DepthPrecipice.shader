Shader "Custom/URP/PrecipiceDepthFixed"
{
    Properties
    {
        [Header(Mode)]
        _ModeGradient2 ("Use 2 Color Gradient", Range(0,1)) = 0
        _ModeGradientTex ("Use Gradient Texture", Range(0,1)) = 0
        
        [Header(Single Color)]
        _SolidColor ("Sky Fade Color", Color) = (0.75,0.85,1,1)

        [Header(2 Color Gradient)]
        _ColorA ("Gradient Color A", Color) = (0.75,0.85,1,1)
        _ColorB ("Gradient Color B", Color) = (0.4,0.6,0.9,1)
        
        [Header(Gradient Texture)]
        _GradColor0 ("Gradient Color 0", Color) = (0.75,0.85,1,1)
        _GradColor1 ("Gradient Color 1", Color) = (0.5,0.7,0.95,1)
        _GradColor2 ("Gradient Color 2", Color) = (0.2,0.4,0.8,1)

        _GradPos1 ("Gradient Position 1", Range(0,1)) = 0.5
        _GradPos2 ("Gradient Position 2", Range(0,1)) = 1.0
        
        [Header(Height)]
        _Start ("Fade Start", Float) = 40
        _End ("Fade End", Float) = 200
        _YOffset ("Reference Height", Float) = 0
        
        [Header(Fake Depth)]
        _StepCount ("Step Count", Float) = 6
        _StepStrength ("Step Strength", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "PrecipiceDepthFixed"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _SolidColor;

                float4 _ColorA;
                float4 _ColorB;

                float4 _GradColor0;
                float4 _GradColor1;
                float4 _GradColor2;

                float _GradPos1;
                float _GradPos2;

                float _ModeGradient2;
                float _ModeGradientTex;

                float _Start;
                float _End;
                float _YOffset;

                float _StepCount;
                float _StepStrength;
            CBUFFER_END

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float2 uv = float2((input.vertexID << 1) & 2, input.vertexID & 2);
                output.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);

                #if UNITY_UV_STARTS_AT_TOP
                    output.uv = uv * float2(1.0, -1.0) + float2(0.0, 1.0);
                #else
                    output.uv = uv;
                #endif

                return output;
            }

            float3 ReconstructWorldPos(float2 uv, float depth)
            {
                #if !UNITY_REVERSED_Z
                    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
                #endif

                return ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
            }

            float4 SampleGradient(float t)
            {
                float4 col;

                if (t < _GradPos1)
                {
                    const float f = t / max(_GradPos1, 0.0001);
                    col = lerp(_GradColor0, _GradColor1, f);
                }
                else
                {
                    const float f = (t - _GradPos1) / max(_GradPos2 - _GradPos1, 0.0001);
                    col = lerp(_GradColor1, _GradColor2, f);
                }

                return col;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                const float4 scene_color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, input.uv);

                const float depth = SampleSceneDepth(input.uv);
                float3 world_pos = ReconstructWorldPos(input.uv, depth);

                const float drop = _YOffset - world_pos.y;                
                const float fade = saturate((drop - _Start) / max(_End - _Start, 0.001));

                float steppedFade = floor(fade * _StepCount) / max(_StepCount - 1.0, 1.0);
                float gradientFade = lerp(fade, steppedFade, _StepStrength);

                // choose between modes
                const float4 final_color = lerp(
                    lerp(_SolidColor,// Solid Color
                        lerp(_ColorA, _ColorB, gradientFade), _ModeGradient2),// Gradient 2 Colors
                    SampleGradient(gradientFade), _ModeGradientTex);//Gradient 3 Colors
                
                return lerp(scene_color, final_color, fade);
            }

            ENDHLSL
        }
    }
}