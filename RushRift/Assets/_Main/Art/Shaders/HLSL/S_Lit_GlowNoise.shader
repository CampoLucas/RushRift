Shader "Custom/URP/Lit_GlowNoise_ScrollPulse"
{
    Properties
    {
        _BaseColor   ("Base Color", Color) = (1,1,1,1)
        _BaseMap     ("Base Map (optional)", 2D) = "white" {}
        _BaseMapScale("BaseMap Tiling (xy) + Offset (zw)", Vector) = (1,1,0,0)

        _Metallic    ("Metallic", Range(0,1)) = 0
        _Smoothness  ("Smoothness", Range(0,1)) = 0.5
        _Occlusion   ("Occlusion", Range(0,1)) = 1

        [HDR]_GlowColorA("Glow Color A", Color) = (1,0.5,0.1,1)
        [HDR]_GlowColorB("Glow Color B", Color) = (0.1,0.7,1,1)
        _GlowIntensity ("Glow Intensity", Range(0,50)) = 5
        _GlowThreshold ("Glow Threshold", Range(0,1)) = 0.0
        _GlowContrast  ("Glow Contrast", Range(0.1,4)) = 1.5

        // Scroll mask (world-space ramp 0..1 moving along a direction)
        _ScrollDir     ("Scroll Dir (xyz, world)", Vector) = (0,1,0,0)
        _ScrollScale   ("Scroll Scale (units→cycles)", Float) = 1
        _ScrollSpeed   ("Scroll Speed (cycles/sec)", Float) = 0.5
        _ScrollSharp   ("Scroll Sharpness", Range(0.1,8)) = 2

        // Pulse (animated multiplicative)
        _PulseSpeed    ("Pulse Speed (Hz)", Float) = 1
        _PulseMin      ("Pulse Min", Range(0,2)) = 0.2
        _PulseMax      ("Pulse Max", Range(0,5)) = 1.0
        _PulsePhase    ("Pulse Phase (radians)", Float) = 0

        // Noise
        _NoiseScale  ("Noise Scale (xyz)", Vector) = (1,1,1,0)
        _NoiseSpeed  ("Noise Scroll (xyz)", Vector) = (0,0.5,0,0)
        _NoiseOctaves("Noise Octaves (1-6)", Range(1,6)) = 4
        _NoiseAmp    ("Noise Amplitude", Range(0,2)) = 1
        _NoisePers   ("Noise Persistence", Range(0.1,1)) = 0.5
        _NoiseWarp   ("Noise Warp (flow)", Range(0,2)) = 0.25

        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clipping", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back ZWrite On ZTest LEqual Blend One Zero

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMapScale;

                float  _Metallic;
                float  _Smoothness;
                float  _Occlusion;

                float4 _GlowColorA;
                float4 _GlowColorB;
                float  _GlowIntensity;
                float  _GlowThreshold;
                float  _GlowContrast;

                float4 _ScrollDir;   // xyz
                float  _ScrollScale;
                float  _ScrollSpeed;
                float  _ScrollSharp;

                float  _PulseSpeed;
                float  _PulseMin;
                float  _PulseMax;
                float  _PulsePhase;

                float4 _NoiseScale;
                float4 _NoiseSpeed;
                float  _NoiseOctaves;
                float  _NoiseAmp;
                float  _NoisePers;
                float  _NoiseWarp;

                float  _Cutoff;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float4 shadowCoord: TEXCOORD3;
                float  fogCoord   : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // --- Noise helpers ---
            float3 hash3(float3 p){
                p = frac(p*0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.xxx*p.yzz + p.zyx);
            }
            float value3D(float3 p){
                float3 i=floor(p), f=frac(p), u=f*f*(3-2*f);
                float n000=dot(hash3(i+float3(0,0,0)),1);
                float n100=dot(hash3(i+float3(1,0,0)),1);
                float n010=dot(hash3(i+float3(0,1,0)),1);
                float n110=dot(hash3(i+float3(1,1,0)),1);
                float n001=dot(hash3(i+float3(0,0,1)),1);
                float n101=dot(hash3(i+float3(1,0,1)),1);
                float n011=dot(hash3(i+float3(0,1,1)),1);
                float n111=dot(hash3(i+float3(1,1,1)),1);
                float nx00=lerp(n000,n100,u.x), nx10=lerp(n010,n110,u.x), nx01=lerp(n001,n101,u.x), nx11=lerp(n011,n111,u.x);
                float nxy0=lerp(nx00,nx10,u.y), nxy1=lerp(nx01,nx11,u.y);
                return lerp(nxy0,nxy1,u.z);
            }
            float fbm3D(float3 p, int oct, float amp, float pers){
                float s=0,a=amp; float3 x=p;
                [unroll] for(int i=0;i<8;i++){ if(i>=oct)break; s+=value3D(x)*a; x=x*2.02 + hash3(x)*_NoiseWarp; a*=pers; }
                return saturate(s);
            }

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v,o);

                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs   nor = GetVertexNormalInputs(v.normalOS);

                o.positionCS  = pos.positionCS;
                o.positionWS  = pos.positionWS;
                o.normalWS    = NormalizeNormalPerVertex(nor.normalWS);
                o.uv          = v.uv * _BaseMapScale.xy + _BaseMapScale.zw;
                o.shadowCoord = GetShadowCoord(pos);
                o.fogCoord    = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            void SampleAlbedo(float2 uv, out float3 albedo, out float alpha)
            {
                float4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
                albedo = c.rgb;
                alpha  = c.a;
            }

            // 0..1 triangular wave from a 0..1 ramp
            float tri(float x){
                float t = abs(frac(x) * 2.0 - 1.0);
                return 1.0 - t; // peak at middle
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 albedo; float alpha;
                SampleAlbedo(i.uv, albedo, alpha);
                #ifdef _ALPHATEST_ON
                    clip(alpha - _Cutoff);
                #endif

                InputData inputData = (InputData)0;
                inputData.positionWS   = i.positionWS;
                inputData.normalWS     = NormalizeNormalPerPixel(i.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                inputData.shadowCoord  = i.shadowCoord;
                inputData.fogCoord     = i.fogCoord;
                inputData.bakedGI      = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);

                // World noise mask
                int oct = (int)round(_NoiseOctaves);
                float3 npos = i.positionWS * _NoiseScale.xyz + _Time.y * _NoiseSpeed.xyz;
                float nMask = fbm3D(npos, clamp(oct,1,6), _NoiseAmp, _NoisePers);
                nMask = saturate((nMask - _GlowThreshold) * _GlowContrast);

                // Scrolling gradient 0..1 along direction
                float3 dir = normalize(_ScrollDir.xyz + 1e-6);
                float ramp = dot(i.positionWS, dir) * _ScrollScale + _Time.y * _ScrollSpeed;
                float scroll = pow(saturate(tri(ramp)), _ScrollSharp); // sharpened triangular wave

                // Blend between A and B using scroll
                float3 glowBlend = lerp(_GlowColorA.rgb, _GlowColorB.rgb, scroll);

                // Pulse 0..1 then remap to [min,max]
                float s = sin(_Time.y * (6.2831853 * max(0.0001,_PulseSpeed)) + _PulsePhase);
                float pulse01 = 0.5 + 0.5 * s;
                float pulse = lerp(_PulseMin, _PulseMax, pulse01);

                float3 emission = glowBlend * (_GlowIntensity * pulse) * nMask;

                SurfaceData surf;
                surf.albedo      = albedo;
                surf.metallic    = _Metallic;
                surf.specular    = 0;
                surf.smoothness  = _Smoothness;
                surf.normalTS    = half3(0,0,1);
                surf.emission    = emission;
                surf.occlusion   = _Occlusion;
                surf.alpha       = alpha;
                surf.clearCoatMask = 0;
                surf.clearCoatSmoothness = 0;

                half4 col = UniversalFragmentPBR(inputData, surf);
                col.rgb = MixFog(col.rgb, i.fogCoord);
                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull Back ZWrite On ZTest LEqual ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #ifndef LerpWhiteTo
            inline half3 LerpWhiteTo(half3 b, half t){ return lerp(half3(1,1,1), b, t); }
            #endif
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMapScale;
                float  _Cutoff;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 posWS    = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, 0.0));
                o.uv = v.uv * _BaseMapScale.xy + _BaseMapScale.zw;
                return o;
            }

            float4 frag(Varyings i):SV_Target
            {
                #ifdef _ALPHATEST_ON
                    float4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                    clip(c.a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull Back ZWrite On ZTest LEqual ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHATEST_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMapScale;
                float  _Cutoff;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv * _BaseMapScale.xy + _BaseMapScale.zw;
                return o;
            }

            float4 frag(Varyings i):SV_Target
            {
                #ifdef _ALPHATEST_ON
                    float4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                    clip(c.a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags { "LightMode"="Meta" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMapScale;

                float4 _GlowColorA;
                float4 _GlowColorB;
                float  _GlowIntensity;
                float  _GlowThreshold;
                float  _GlowContrast;

                float4 _ScrollDir;
                float  _ScrollScale;
                float  _ScrollSpeed;
                float  _ScrollSharp;

                float4 _NoiseScale;
                float4 _NoiseSpeed;
                float  _NoiseOctaves;
                float  _NoiseAmp;
                float  _NoisePers;
                float  _NoiseWarp;

                float  _Cutoff;
            CBUFFER_END

            float3 hash3(float3 p){ p=frac(p*0.3183099+0.1); p*=17.0; return frac(p.xxx*p.yzz+p.zyx); }
            float value3D(float3 p){
                float3 i=floor(p), f=frac(p), u=f*f*(3-2*f);
                float n000=dot(hash3(i+float3(0,0,0)),1);
                float n100=dot(hash3(i+float3(1,0,0)),1);
                float n010=dot(hash3(i+float3(0,1,0)),1);
                float n110=dot(hash3(i+float3(1,1,0)),1);
                float n001=dot(hash3(i+float3(0,0,1)),1);
                float n101=dot(hash3(i+float3(1,0,1)),1);
                float n011=dot(hash3(i+float3(0,1,1)),1);
                float n111=dot(hash3(i+float3(1,1,1)),1);
                float nx00=lerp(n000,n100,u.x), nx10=lerp(n010,n110,u.x), nx01=lerp(n001,n101,u.x), nx11=lerp(n011,n111,u.x);
                float nxy0=lerp(nx00,nx10,u.y), nxy1=lerp(nx01,nx11,u.y);
                return lerp(nxy0,nxy1,u.z);
            }
            float fbm3D(float3 p, int oct, float amp, float pers){
                float s=0,a=amp; float3 x=p;
                [unroll] for(int i=0;i<8;i++){ if(i>=oct)break; s+=value3D(x)*a; x=x*2.02 + hash3(x)*_NoiseWarp; a*=pers; }
                return saturate(s);
            }
            float tri(float x){ float t=abs(frac(x)*2.0-1.0); return 1.0 - t; }

            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 positionWS:TEXCOORD1; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv * _BaseMapScale.xy + _BaseMapScale.zw;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                return o;
            }

            float4 frag(Varyings i):SV_Target
            {
                float3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;
                float  alpha  = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a   * _BaseColor.a;
                #ifdef _ALPHATEST_ON
                    clip(alpha - _Cutoff);
                #endif

                int oct = 4;
                float3 npos = i.positionWS * _NoiseScale.xyz; // no time in bakes
                float nMask = fbm3D(npos, oct, _NoiseAmp, _NoisePers);
                nMask = saturate((nMask - _GlowThreshold) * _GlowContrast);

                float3 dir = normalize(_ScrollDir.xyz + 1e-6);
                float ramp = dot(i.positionWS, dir) * _ScrollScale;
                float scroll = pow(saturate(tri(ramp)), _ScrollSharp);

                float3 glowBlend = lerp(_GlowColorA.rgb, _GlowColorB.rgb, scroll);
                float3 emission = glowBlend * _GlowIntensity * nMask; // pulse omitted in bake

                MetaInput meta;
                meta.Albedo   = albedo;
                meta.Emission = emission;
                return UnityMetaFragment(meta);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
