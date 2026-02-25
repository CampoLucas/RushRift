#ifndef TILEARRAY_TILED_SAMPLE_INCLUDED
#define TILEARRAY_TILED_SAMPLE_INCLUDED
#pragma require(2darray)

// Unity/URP will define these for a texture property named _TexArray.
// If ASE doesn’t automatically include it, we declare it extern here.
float4 _TexArray_TexelSize;

// Texture2DArray sampling shims for safety
#ifndef SAMPLE_TEXTURE2D_ARRAY
    #define SAMPLE_TEXTURE2D_ARRAY(tex, samp, uv, slice) (tex).Sample((samp), float3((uv), (slice)))
#endif
#ifndef SAMPLE_TEXTURE2D_ARRAY_GRAD
    #define SAMPLE_TEXTURE2D_ARRAY_GRAD(tex, samp, uv, slice, ddxv, ddyv) (tex).SampleGrad((samp), float3((uv), (slice)), (ddxv), (ddyv))
#endif

inline float3 srgb_to_linear_local(const float3 c)
{
    const float3 lo = c / 12.92;
    const float3 hi = pow(max((c + 0.055) / 1.055, 0.0), 2.4);
    return lerp(hi, lo, step(c, 0.04045));
}

inline float3 linear_to_srgb_local(const float3 c)
{
    const float3 lo = c * 12.92;
    const float3 hi = 1.055 * pow(max(c, 0.0), 1.0 / 2.4) - 0.055;
    return lerp(hi, lo, step(c, 0.0031308));
}

inline float hash12(float2 p, const float seed)
{
    p += seed;
    float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

inline float4 lerp_4(const float a, const float b, const float c, const float d, float t)
{
    t = clamp(t, 0.0, 3.0);
    const float4 ab = lerp(a, b, saturate(t));
    const float4 bc = lerp(b, c, saturate(t - 1.0));
    const float4 cd = lerp(c, d, saturate(t - 2.0));
    return lerp(lerp(ab, bc, step(1.0, t)), cd, step(2.0, t));
}

inline float2 lerp_4(const float2 a, const float2 b, const float2 c, const float2 d, float t)
{
    t = clamp(t, 0.0, 3.0);

    const float2 ab = lerp(a, b, saturate(t));
    const float2 bc = lerp(b, c, saturate(t - 1.0));
    const float2 cd = lerp(c, d, saturate(t - 2.0));

    return lerp(lerp(ab, bc, step(1.0, t)), cd, step(2.0, t));
}

inline int pick_slice_index(const float2 tile_id, const float slice_count, float2 count_range, const int use_random, const float manual_index, const float seed)
{
    const int count = (int)clamp(slice_count, count_range.x, count_range.y);
    
    return lerp(
        clamp((int)round(manual_index), 0, count - 1),
        clamp((int)floor(hash12(tile_id + 37.17, seed) * count), 0, count - 1),
        saturate(use_random));
}

inline int pick_rotation_quarter(const float2 tile_id, const int use_random, const float manual_quarter, const float seed)
{
    const int result = lerp(
        (int)round(manual_quarter),
        (int)floor(hash12(tile_id + 91.337, seed) * 4.0),
        saturate(use_random));
    
    return result & 3;
}

inline float2 rotate_local_quarter(float2 l, int q)
{
    q &= 3;
    float t = (float)q;

    const float2 r0 = l;
    const float2 r1 = float2(1.0 - l.y, l.x);
    const float2 r2 = 1.0 - l;
    const float2 r3 = float2(l.y, 1.0 - l.x);

    return lerp_4(r0, r1, r2, r3, q);
}

inline void rotate_gradients_quarter(const float2 du, const float2 dv, int q, out float2 du_out, out float2 dv_out)
{
    q &= 3;
    const float t = (float)q;

    // duOut candidates for q = 0,1,2,3
    const float2 du0 = du;
    const float2 du1 = -dv;
    const float2 du2 = -du;
    const float2 du3 = dv;

    // dvOut candidates for q = 0,1,2,3
    const float2 dv0 = dv;
    const float2 dv1 = du;
    const float2 dv2 = -dv;
    const float2 dv3 = -du;

    du_out = lerp_4(du0, du1, du2, du3, t);
    dv_out = lerp_4(dv0, dv1, dv2, dv3, t);
}

inline float2 inset_local_uv(const float2 local, const float2 inset)
{
    const float2 lo = inset;
    const float2 hi = 1.0 - inset;
    return saturate(lo + local * (hi - lo));
}

inline float2 inset_uv(const float inset)
{
    return inset * _TexArray_TexelSize.xy;
}

inline void tile_array_compute_uv(
    const float2 base_uv,
    const float slice_count,
    const float use_random_slice,
    const float manual_slice_index,
    const float use_random_rotation,
    const float manual_rotation,
    const float4 tiling,
    const float tile_inset,
    const float seed,
    out float2 uv_slice,
    out int slice,
    out float2 ddx_rot,
    out float2 ddy_rot,
    out float2 tile_id
)
{
    const float2 uv_tiled = base_uv * tiling.xy + tiling.zw;
    tile_id = floor(uv_tiled);
    const float2 local = frac(uv_tiled);

    slice = pick_slice_index(tile_id, slice_count, float2(1.0, 64.0), use_random_slice, manual_slice_index, seed);
    const int q = pick_rotation_quarter(tile_id, use_random_rotation, manual_rotation, seed);

    // base gradients in tiled space
    const float2 ddx_base = ddx(base_uv) * tiling.xy;
    const float2 ddy_base = ddy(base_uv) * tiling.xy;

    const float2 local_rot = rotate_local_quarter(local, q);
    rotate_gradients_quarter(ddx_base, ddy_base, q, ddx_rot, ddy_rot);

    const float2 inset = inset_uv(tile_inset);
    const float2 local_in = inset_local_uv(local_rot, inset);
    uv_slice = tile_id + local_in;
}

inline float3 do_brightness_variance(const float3 col, const float2 tile_id, const float brightness_variance, const float seed)
{
    return lerp(
        col,
        col * max(0.0, 1.0 + (hash12(tile_id + float2(221.7, 19.3), seed) * 2.0 - 1.0) * brightness_variance),
        ceil(brightness_variance));
}

inline float brightness_variance(const float2 tile_id, const float brightness_variance, const float seed)
{
    return lerp(
        1,
        max(0.0, 1.0 + (hash12(tile_id + float2(221.7, 19.3), seed) * 2.0 - 1.0) * brightness_variance),
        ceil(brightness_variance));
}

inline float4 tile_array_sample_color(
    const Texture2DArray tex_array,
    const SamplerState samp,
    const float2 uv_slice,
    const int slice,
    const float2 ddx_rot,
    const float2 ddy_rot,
    const float4 base_color,
    const float2 tile_id,
    const float brightness_variation,
    const float seed,
    const float color_space
)
{
    float4 s = SAMPLE_TEXTURE2D_ARRAY_GRAD(tex_array, samp, uv_slice, slice, ddx_rot, ddy_rot);

    float3 col = s.rgb * base_color.rgb;
    float  a   = s.a   * base_color.a;

    if (color_space > 0.5 && color_space < 1.5) col = srgb_to_linear_local(col);
    else if (color_space >= 1.5) col = linear_to_srgb_local(col);

    col *= brightness_variance(tile_id, brightness_variation, seed);

    return float4(col, a);
}

inline float4 tile_array_tiled_sample(
    const Texture2DArray tex_array,
    const SamplerState samp,
    const float2 base_uv,
    const float4 base_color,
    const float slice_count,
    const float use_random_slice,
    const float manual_slice_index,
    const float use_random_rotation,
    const float manual_rotation,
    const float brightness_variance,
    const float4 tiling,
    const float tile_inset,
    const float seed,
    const float color_space
)
{
    float2 uv_slice, ddx_rot, ddy_rot, tile_id;
    int slice;
    tile_array_compute_uv(base_uv, slice_count, use_random_slice, manual_slice_index, use_random_rotation,
        manual_rotation, tiling, tile_inset, seed, uv_slice, slice, ddx_rot, ddy_rot, tile_id);

    float4 s = SAMPLE_TEXTURE2D_ARRAY_GRAD(tex_array, samp, uv_slice, slice, ddx_rot, ddy_rot);

    float3 col = s.rgb * base_color.rgb;
    float  a   = s.a   * base_color.a;

    if (color_space > 0.5 && color_space < 1.5) col = srgb_to_linear_local(col);
    else if (color_space >= 1.5) col = linear_to_srgb_local(col);

    if (brightness_variance > 1e-4)
    {
        const float r = hash12(tile_id + float2(221.7, 19.3), seed) * 2.0 - 1.0;
        col *= max(0.0, 1.0 + r * brightness_variance);
    }

    float4 color = tile_array_sample_color(tex_array, samp, uv_slice, slice, ddx_rot, ddy_rot, base_color,
        tile_id, brightness_variance, seed, color_space);

    return color;
}

#endif