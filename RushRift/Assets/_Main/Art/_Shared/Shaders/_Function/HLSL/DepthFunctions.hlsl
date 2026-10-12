
#ifndef DEPTH_FUNCTIONS_INCLUDED
#define DEPTH_FUNCTIONS_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

inline float get_linear_01depth(const float2 uv)
{
    const float depth = SampleSceneDepth(uv);
    return Linear01Depth(depth, _ZBufferParams);
}

inline float get_eye_depth(const float2 uv)
{
    const float depth = SampleSceneDepth(uv);
    return LinearEyeDepth(depth, _ZBufferParams);
}

inline float3 get_world_pos_from_depth(const float2 uv)
{
    #if !UNITY_REVERSED_Z
    const float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, depth);
    #else
    const float depth = SampleSceneDepth(uv);
    #endif

    return  ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
}

#endif