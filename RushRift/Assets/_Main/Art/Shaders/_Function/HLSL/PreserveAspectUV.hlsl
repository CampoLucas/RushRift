#ifndef PRESERVE_ASPECT_UV_INCLUDED
#define PRESERVE_ASPECT_UV_INCLUDED

// Preserves texture aspect ratio while mapping into a target rectangle.
// mode: 0 = Fit, 1 = Fill
// uv: input UV (0-1)
// texelSize: Unity-style _TexelSize (x=1/width, y=1/height, z=width, w=height)
// targetAspect: width/height of the area you're mapping into
// Returns corrected UV in 0-1 (centered)
float2 preserve_aspect_uv_texel(
    const float2 uv,
    float4 texel_size,
    const float target_aspect,
    const int mode)
{
    // Texture aspect = width / height
    const float tex_aspect = texel_size.z / max(texel_size.w, 1.0);

    // Center UV around 0
    const float2 u = uv - 0.5;

    // Ratio between aspects
    float r = tex_aspect / max(target_aspect, 1e-6);


    const float2 scale = lerp(
        (r >= 1.0) ? float2(1.0 / r, 1.0) : float2(1.0, r),
        (r >= 1.0) ? float2(1.0, r) : float2(1.0 / r, 1.0),
        clamp(mode, 0, 1));

    return u * scale + 0.5;
}

float2 preserve_aspect_uv(const float2 uv, const float aspect_ratio)
{
    // Estimate object scale from the ObjectToWorld matrix.
    // Assumes your UVs correspond to the object's local X (U) and local Y (V) axes
    // which is true for a typical quad/plane oriented in XY.
    const float3 x_axis = float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20);
    const float3 y_axis = float3(unity_ObjectToWorld._m01, unity_ObjectToWorld._m11, unity_ObjectToWorld._m21);

    const float sx = max(length(x_axis), 1e-6);
    const float sy = max(length(y_axis), 1e-6);

    const float object_aspect = sx / sy;           // width / height in world units
    const float tex_aspect    = max(aspect_ratio, 1e-6); // width / height

    // Center UVs around 0.5
    const float2 p = uv - 0.5;

    // Scale UVs so the sampled image keeps its aspect ratio inside the object aspect
    float2 scale = float2(1.0, 1.0);

    // If object is wider than the texture aspect, shrink U.
    // Else shrink V. This keeps the image undistorted and centered.
    if (object_aspect > tex_aspect)
    {
        scale.x = tex_aspect / object_aspect;
    }
    else
    {
        scale.y = object_aspect / tex_aspect;
    }

    return p * scale + 0.5;
}

float2 preserve_aspect_uv_from_tex(
    const float2 uv,
    Texture2D tex,
    //SamplerState samp,
    const float target_aspect,
    const int mode)
{
    uint w, h;
    tex.GetDimensions(w, h);

    const float tex_aspect = (float)w / max((float)h, 1.0);

    const float2 u = uv - 0.5;
    float r = tex_aspect / max(target_aspect, 1e-6);

    // float2 scale;
    // if (mode == 0) // FIT
    //     scale = (r >= 1.0) ? float2(1.0 / r, 1.0) : float2(1.0, r);
    // else // FILL
    //     scale = (r >= 1.0) ? float2(1.0, r) : float2(1.0 / r, 1.0);

    const float2 scale = lerp(
        (r >= 1.0) ? float2(1.0 / r, 1.0) : float2(1.0, r),
        (r >= 1.0) ? float2(1.0, r) : float2(1.0 / r, 1.0),
        clamp(mode, 0, 1));
    
    return u * scale + 0.5;
}

float2 get_tex_dimensions(Texture2D tex)
{
    uint w, h;
    tex.GetDimensions(w, h);

    return float2((float)w, (float)h);
}


#endif