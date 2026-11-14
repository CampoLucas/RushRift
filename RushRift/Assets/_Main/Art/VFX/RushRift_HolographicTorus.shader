
Shader "RushRift/HolographicTorus"

{
    Properties
    {
        [HDR] _Color ("Network Color", Color) = (0, 0.65, 0.85, 1)
        _Brightness ("Brightness", Range(0, 5)) = 1.2
        _Opacity ("Opacity", Range(0, 1)) = 0.65

        _GridX ("Network Columns", Range(4, 48)) = 20
        _GridY ("Network Rows", Range(2, 24)) = 8
        _LineWidth ("Line Width", Range(0.002, 0.1)) = 0.012

        _NodeSize ("Node Size", Range(0.01, 0.2)) = 0.055
        _NodeDensity ("Node Density", Range(0, 1)) = 0.3

        _DataSpeed ("Data Speed", Range(0, 3)) = 0.35
        _DataDensity ("Data Density", Range(0, 1)) = 0.15
        _DataIntensity ("Data Intensity", Range(0, 3)) = 1.0

        _BaseVisibility ("Base Visibility", Range(0, 1)) = 0.12
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Brightness;
                float _Opacity;
                float _GridX;
                float _GridY;
                float _LineWidth;
                float _NodeSize;
                float _NodeDensity;
                float _DataSpeed;
                float _DataDensity;
                float _DataIntensity;
                float _BaseVisibility;
            CBUFFER_END

            float hash21(float2 p)
            {
                return frac(
                    sin(dot(p, float2(127.1, 311.7)))
                    * 43758.5453
                );
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS =
                    TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 grid = float2(_GridX, _GridY);
                float2 uv = IN.uv * grid;

                float2 cell = floor(uv);
                float2 localUV = frac(uv);

                // Fine network lines
                float2 lineDistance =
                    min(localUV, 1.0 - localUV);

                float lineX = 1.0 - smoothstep(
                    _LineWidth,
                    _LineWidth + 0.02,
                    lineDistance.x
                );

                float lineY = 1.0 - smoothstep(
                    _LineWidth,
                    _LineWidth + 0.02,
                    lineDistance.y
                );

                float network = max(lineX, lineY);

                // Sparse connection nodes
                float2 nearestNode = floor(uv + 0.5);
                float2 nodeUV = uv - nearestNode;

                float nodeRandom = hash21(nearestNode);

                float nodeMask =
                    step(nodeRandom, _NodeDensity);

                float node = 1.0 - smoothstep(
                    _NodeSize * 0.55,
                    _NodeSize,
                    length(nodeUV)
                );

                node *= nodeMask;

                // Data flowing along selected paths
                float pathRandom = hash21(
                    float2(cell.y, 17.0)
                );

                float activePath =
                    step(pathRandom, _DataDensity);

                float direction = hash21(
                    float2(cell.y, 31.0)
                ) > 0.5 ? 1.0 : -1.0;

                float travel = frac(
                    IN.uv.x * 3.0
                    - _Time.y * _DataSpeed * direction
                    + pathRandom
                );

                float packet = 1.0 - smoothstep(
                    0.0,
                    0.045,
                    travel
                );

                // Keep the packet on a thin horizontal path
                float pathLine = 1.0 - smoothstep(
                    _LineWidth,
                    _LineWidth + 0.035,
                    lineDistance.y
                );

                float dataFlow =
                    packet * activePath * pathLine;

                // Subtle base illumination
                float intensity =
                    _BaseVisibility
                    + network * 0.2
                    + node * 0.55
                    + dataFlow * _DataIntensity;

                float3 finalColor =
                    _Color.rgb * _Brightness * intensity;

                float alpha = saturate(
                    intensity * _Opacity
                );

                return half4(finalColor, alpha);
            }

            ENDHLSL
        }
    }
}

