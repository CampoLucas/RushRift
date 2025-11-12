Shader "Unlit/NeonFire_Unlit"


{
    Properties
    {
        _NoiseA ("Noise (tileable)", 2D) = "gray" {}
        _BaseCol ("Base Color", Color) = (1.0,0.55,0.1,1)   // color del líquido
        _HotCol  ("Hot Color",  Color) = (1.0,0.95,0.8,1)
        _Emission("Emission", Range(0,12)) = 6.0

        _Speed    ("Flow Speed", Float) = 1.2
        _Cut      ("Cut", Range(0,1)) = 0.35
        _Soft     ("Soft", Range(0,1)) = 0.25
        _RadialSoft ("Radial Soft", Float) = 0.55   // 0=sin círculo, 0.5~0.7 para tapa
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        ZWrite On
        // OPAQUE: no hay Blend

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            TEXTURE2D(_NoiseA); SAMPLER(sampler_NoiseA); float4 _NoiseA_ST;
            float4 _BaseCol, _HotCol; float _Emission;
            float _Speed, _Cut, _Soft, _RadialSoft;

            float2 ST(float2 uv, float4 st){ return uv*st.xy+st.zw; }

            v2f vert(appdata v){ v2f o; o.pos=TransformObjectToHClip(v.vertex.xyz); o.uv=v.uv; return o; }

            half4 frag(v2f i):SV_Target
            {
                // máscara radial (círculo de la tapa)
                float2 p = (i.uv - 0.5)*2.0;
                float r = length(p);
                float radial = 1.0 - smoothstep(_RadialSoft, 1.0, r);

                // patrón de líquido
                float t = _Time.y * _Speed;
                float2 uv = ST(i.uv + float2(0.0, t*0.15), _NoiseA_ST);
                float n  = SAMPLE_TEXTURE2D(_NoiseA, sampler_NoiseA, uv*2.0).r;

                // “islas” de líquido
                float mask = smoothstep(_Cut, _Cut+_Soft, n) * radial;

                // color + emisión (OPAQUE: Alpha no importa)
                float3 col = lerp(_BaseCol.rgb, _HotCol.rgb, pow(n,3.0));
                col *= (1.0 + _Emission);   // Bloom hará el glow

                // fuera del líquido: pintamos negro (o nada) pero como es OPAQUE ya tapa
                return half4(col*mask, 1.0);
            }
            ENDHLSL
        }
    }
}