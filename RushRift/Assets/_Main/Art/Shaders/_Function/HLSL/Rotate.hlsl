#ifndef ROTATE_INCLUDED
#define ROTATE_INCLUDED

// Rotate a 2D vector around a pivot by angle (radians), like UV rotation.
float2 rotate_2d(float2 p, const float angle, const float2 pivot)
{
    const float s = sin(angle);
    const float c = cos(angle);

    p -= pivot;
    const float2 r = float2(p.x * c - p.y * s, p.x * s + p.y * c);
    return r + pivot;
}

float2 rotate_2d(float2 p, const float angle)
{
    return rotate_2d(p, angle, float2(0.0, 0.0));
}

float3 rotate_3d_xy(float3 v, const float angle, const float2 pivot_xy = float2(0.0, 0.0))
{
    v.xy = rotate_2d(v.xy, angle, pivot_xy);
    return v;
}

float3 rotate_3d_xz(float3 v, const float angle, const float2 pivotXZ = float2(0.0, 0.0))
{
    float2 xz = rotate_2d(float2(v.x, v.z), angle, pivotXZ);
    v.x = xz.x;
    v.z = xz.y;
    return v;
}

float3 rotate_3d_yz(float3 v, const float angle, const float2 pivot_yz = float2(0.0, 0.0))
{
    float2 yz = rotate_2d(float2(v.y, v.z), angle, pivot_yz);
    v.y = yz.x;
    v.z = yz.y;
    return v;
}

float4 rotate_4d_xy(float4 v, const float angle, const float2 pivot_xy = float2(0.0, 0.0))
{
    v.xy = rotate_2d(v.xy, angle, pivot_xy);
    return v;
}

#endif