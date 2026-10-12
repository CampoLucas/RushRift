#ifndef PARALLAX_SHARP_INCLUDED
#define PARALLAX_SHARP_INCLUDED

// Force LOD 0 sampling so the black/white edge stays sharp (no mip blur).
inline float SampleHeightLOD0(sampler2D heightTex, float2 uv)
{
    return tex2Dlod(heightTex, float4(uv, 0, 0)).r;
}

inline float SampleHeightChannelLOD0(sampler2D heightTex, float2 uv, float4 channelSelect)
{
    float4 c = tex2Dlod(heightTex, float4(uv, 0, 0)); // LOD 0
    // channelSelect should be one-hot: (1,0,0,0) etc.
    return dot(c, channelSelect);
}

// Sharp parallax using a binary height (thresholded).
// Inputs:
// - uv: base UVs
// - viewDirTS: view direction in TANGENT space (normalize it before passing if possible)
// - heightTex: heightmap sampler2D (use R channel)
// - heightScale: how deep the parallax goes (try 0.02 to 0.08)
// - steps: raymarch steps (8 to 32)
// - threshold: 0..1 cutoff for "solid" (0.5 typical)
// - invert: 0 = normal, 1 = inverted (inverse extrusion)
inline float2 ParallaxSharpUV(
    float2 uv,
    float3 viewDirTS,
    sampler2D heightTex,
    float4 channelSelect,
    float heightScale,
    float steps,
    float threshold,
    float invert,
    out float wallMask,
    out float4 wallColor
){
    viewDirTS = normalize(viewDirTS);

    // Avoid exploding offsets when looking nearly parallel to the surface.
    float vz = max(abs(viewDirTS.z), 1e-4);
    float2 parallaxDir = (viewDirTS.xy / vz);

    // For inverted extrusion, just flip the direction.
    // (This is the “go inward” vs “go outward” feel.)
    float inv = saturate(invert);
    parallaxDir *= lerp(1.0, -1.0, inv);

    // Total UV shift across full depth range.
    float2 totalOffset = -parallaxDir * heightScale;

    // Raymarch from top layer to bottom.
    float nSteps = max(1.0, steps);
    float stepSize = 1.0 / nSteps;

    // Binary “solid” test at base UV
    float baseH = SampleHeightChannelLOD0(heightTex, uv, channelSelect);
    float baseSolid = step(threshold, baseH);
    
    float currLayer = 0.0;
    float2 currUV = uv;

    float hitSolid = baseSolid;
    float hitLayer = 0.0;

    // March until we enter the solid.
    [loop]
    for (int i = 0; i < 128; i++)
    {
        if (i >= (int)nSteps) break;

        currLayer += stepSize;
        currUV += totalOffset * stepSize;

        float h = SampleHeightChannelLOD0(heightTex, currUV, channelSelect);
        hitSolid = step(threshold, h);
        
        if (currLayer >= hitSolid)
        {
            hitLayer = currLayer;
            break;
        }
    }

    // 1-step refinement (optional but helps stability at the edge)
    // We step back and do a small linear search.
    float2 prevUV = currUV - totalOffset * stepSize;
    float prevLayer = currLayer - stepSize;
    
    float prevSolid = step(threshold, SampleHeightChannelLOD0(heightTex, prevUV, channelSelect));
    float currSolid = hitSolid;

    // If both are same, refinement won't change much, but it's cheap.
    float denom = max((currLayer - currSolid) - (prevLayer - prevSolid), 1e-4);
    float w = (prevLayer - prevSolid) / denom; // 0..1
    float2 refinedUV = lerp(prevUV, currUV, saturate(w));

    // “Wall mask”: you see walls when the base pixel is empty but the ray hits solid.
    // For inverted, swap interpretation.
    // normal: empty->solid means cavity wall
    // inverted: solid->empty means reverse cavity wall
    float wallMaskNormal  = (1.0 - baseSolid) * hitSolid;
    float wallMaskInvert  = baseSolid * (1.0 - hitSolid);
    wallMask = lerp(wallMaskNormal, wallMaskInvert, inv);

    wallColor = float4(refinedUV, saturate(hitLayer), saturate(wallMask));
    
    return refinedUV;
}

// Returns float4: (parallaxUV.x, parallaxUV.y, hitDepth01, wallMask)
inline float4 ParallaxSharpUV_Walls(
    float2 uv,
    float3 viewDirTS,              // tangent space view dir
    sampler2D heightTex,
    float4 channelSelect,          // one-hot: (1,0,0,0)=R etc
    float heightScale,             // visual depth
    float steps,                   // 8..32
    float threshold,               // 0.5 usually
    float invert                   // 0 normal, 1 inverted
){
    viewDirTS = normalize(viewDirTS);

    float vz = max(abs(viewDirTS.z), 1e-4);
    float2 parallaxDir = (viewDirTS.xy / vz);

    // Invert just flips which side "caves in"
    float inv = saturate(invert);
    parallaxDir *= lerp(1.0, -1.0, inv);

    float2 totalOffset = -parallaxDir * heightScale;

    float nSteps = max(1.0, steps);
    float stepSize = 1.0 / nSteps;

    // Binary “solid” test at base UV
    float baseH = SampleHeightChannelLOD0(heightTex, uv, channelSelect);
    float baseSolid = step(threshold, baseH);

    float currLayer = 0.0;
    float2 currUV = uv;

    float hitSolid = baseSolid;
    float hitLayer = 0.0;

    // March until we enter solid (binary height)
    [loop]
    for (int i = 0; i < 128; i++)
    {
        if (i >= (int)nSteps) break;

        currLayer += stepSize;
        currUV += totalOffset * stepSize;

        float h = SampleHeightChannelLOD0(heightTex, currUV, channelSelect);
        hitSolid = step(threshold, h);

        if (currLayer >= hitSolid)
        {
            hitLayer = currLayer;
            break;
        }
    }

    // Refinement between last two samples (helps stability a bit)
    float2 prevUV = currUV - totalOffset * stepSize;
    float prevLayer = currLayer - stepSize;

    float prevSolid = step(threshold, SampleHeightChannelLOD0(heightTex, prevUV, channelSelect));
    float currSolid = hitSolid;

    float denom = max((currLayer - currSolid) - (prevLayer - prevSolid), 1e-4);
    float w = (prevLayer - prevSolid) / denom;
    float2 refinedUV = lerp(prevUV, currUV, saturate(w));

    // “Wall mask”: you see walls when the base pixel is empty but the ray hits solid.
    // For inverted, swap interpretation.
    // normal: empty->solid means cavity wall
    // inverted: solid->empty means reverse cavity wall
    float wallMaskNormal  = (1.0 - baseSolid) * hitSolid;
    float wallMaskInvert  = baseSolid * (1.0 - hitSolid);
    float wallMask = lerp(wallMaskNormal, wallMaskInvert, inv);

    return float4(refinedUV, saturate(hitLayer), saturate(wallMask));
}

#endif