#ifndef CC_GLASS_INCLUDED
#define CC_GLASS_INCLUDED

// --------------------
// Small hash / noise
// --------------------
inline float hash12(float2 p)
{
    // fast, stable hash
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

inline float valueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);

    float a = hash12(i);
    float b = hash12(i + float2(1, 0));
    float c = hash12(i + float2(0, 1));
    float d = hash12(i + float2(1, 1));

    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

inline float fbm(float2 p)
{
    // 4 octaves, cheap
    float v = 0.0;
    float a = 0.5;
    float2 shift = float2(37.2, 19.7);

    [unroll] for (int o = 0; o < 4; o++)
    {
        v += a * valueNoise(p);
        p = p * 2.02 + shift;
        a *= 0.5;
    }
    return v;
}

// --------------------
// Height field + soften
// --------------------
// softness is in "coord units". If In is UV, softness ~ 0..0.02 is typical.
// If In is world position, softness needs to be bigger (depends on world scale).
inline float heightField(float2 c)
{
    // tweak frequency here if you want different glass grain
    return fbm(c * 6.0);
}

inline float softenedHeight(float2 c, float softness)
{
    // 5-tap cross blur (cheap). softness=0 gives sharp height.
    float r = max(softness, 0.0);

    float h0 = heightField(c);
    if (r <= 0.000001) return h0;

    float2 dx = float2(r, 0);
    float2 dy = float2(0, r);

    float h1 = heightField(c + dx);
    float h2 = heightField(c - dx);
    float h3 = heightField(c + dy);
    float h4 = heightField(c - dy);

    return (h0 * 0.4 + (h1 + h2 + h3 + h4) * 0.15);
}

// --------------------
// Main: returns UV offset
// --------------------
// Height: bump strength (how "tall" the glass is).
// Displacement: how much the background refracts (scales the UV offset).
inline float2 CCGlass_Offset_FromCoords(float2 c, float softness, float height, float displacement)
{
    // Compute gradient of softened height field
    float eps = max(softness * 0.5, 0.0005);

    float hL = softenedHeight(c - float2(eps, 0), softness);
    float hR = softenedHeight(c + float2(eps, 0), softness);
    float hD = softenedHeight(c - float2(0, eps), softness);
    float hU = softenedHeight(c + float2(0, eps), softness);

    float2 grad = float2(hR - hL, hU - hD);

    // Height scales the "normal", displacement scales the refraction
    float2 offset = grad * (height * displacement);

    return offset;
}

// --------------------
// Overloads for In
// --------------------
inline float2 CCGlass_Offset_In2(float2 In, float softness, float height, float displacement)
{
    return CCGlass_Offset_FromCoords(In, softness, height, displacement);
}

inline float2 CCGlass_Offset_In3(float3 In, float softness, float height, float displacement)
{
    // Use xy by default (common for world-position driven glass)
    return CCGlass_Offset_FromCoords(In.xy, softness, height, displacement);
}

inline float2 CCGlass_Offset_In4(float4 In, float softness, float height, float displacement)
{
    // Use xy, and use z as a stable seed-ish warp so float4 actually matters
    float2 c = In.xy + In.z * 0.17;
    return CCGlass_Offset_FromCoords(c, softness, height, displacement);
}

#endif