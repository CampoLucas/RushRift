// Made with Amplify Shader Editor v1.9.8.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Game/FX/Pixel Scan Hologram"
{
    Properties
    {
        //[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        _Float0("Float 0", Float) = 0
        _PixelFXSpeed("Pixel FX Speed", Float) = 0
        _BGAlpha("BG Alpha", Float) = 0
        _LinesSmoothstep("Lines Smoothstep", Vector) = (0,0.53,0,0)
        _BGOffset("BG Offset", Float) = 0
        _LineSpeed("Line Speed", Float) = 0
        _LinesScale("Lines Scale", Float) = 0
        _Blocks("Blocks", Float) = 0
        _PixelFXScale("Pixel FX Scale", Vector) = (1,0.5,1,0)
        _PixelFXOffset("Pixel FX Offset", Vector) = (1,0.5,1,0)
        _MainTex("MainTex", 2D) = "white" {}

    }

    SubShader
    {
		LOD 0

        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

        Stencil
        {
        	Ref [_Stencil]
        	ReadMask [_StencilReadMask]
        	WriteMask [_StencilWriteMask]
        	Comp [_StencilComp]
        	Pass [_StencilOp]
        }


        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        
        Pass
        {
            Name "Default"
        CGPROGRAM
            #define ASE_VERSION 19801

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityShaderVariables.cginc"
            #define ASE_NEEDS_FRAG_COLOR


            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 ase_tangent : TANGENT;
                float3 ase_normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4  mask : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
                float4 ase_texcoord3 : TEXCOORD3;
                float4 ase_texcoord4 : TEXCOORD4;
                float4 ase_texcoord5 : TEXCOORD5;
                float4 ase_texcoord6 : TEXCOORD6;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            uniform float _BGOffset;
            uniform float2 _LinesSmoothstep;
            uniform float _LinesScale;
            uniform float _LineSpeed;
            uniform float _BGAlpha;
            uniform float _Float0;
            uniform float3 _PixelFXScale;
            uniform float3 _PixelFXOffset;
            uniform float _Blocks;
            uniform float _PixelFXSpeed;
            float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
            float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
            float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
            float snoise( float2 v )
            {
            	const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
            	float2 i = floor( v + dot( v, C.yy ) );
            	float2 x0 = v - i + dot( i, C.xx );
            	float2 i1;
            	i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
            	float4 x12 = x0.xyxy + C.xxzz;
            	x12.xy -= i1;
            	i = mod2D289( i );
            	float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
            	float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
            	m = m * m;
            	m = m * m;
            	float3 x = 2.0 * frac( p * C.www ) - 1.0;
            	float3 h = abs( x ) - 0.5;
            	float3 ox = floor( x + 0.5 );
            	float3 a0 = x - ox;
            	m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
            	float3 g;
            	g.x = a0.x * x0.x + h.x * x0.y;
            	g.yz = a0.yz * x12.xz + h.yz * x12.yw;
            	return 130.0 * dot( m, g );
            }
            


            v2f vert(appdata_t v )
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 ase_positionWS = mul( unity_ObjectToWorld, float4( ( v.vertex ).xyz, 1 ) ).xyz;
                OUT.ase_texcoord3.xyz = ase_positionWS;
                float3 ase_tangentWS = UnityObjectToWorldDir( v.ase_tangent );
                OUT.ase_texcoord4.xyz = ase_tangentWS;
                float3 ase_normalWS = UnityObjectToWorldNormal( v.ase_normal );
                OUT.ase_texcoord5.xyz = ase_normalWS;
                float ase_tangentSign = v.ase_tangent.w * ( unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0 );
                float3 ase_bitangentWS = cross( ase_normalWS, ase_tangentWS ) * ase_tangentSign;
                OUT.ase_texcoord6.xyz = ase_bitangentWS;
                
                
                //setting value to unused interpolator channels and avoid initialization warnings
                OUT.ase_texcoord3.w = 0;
                OUT.ase_texcoord4.w = 0;
                OUT.ase_texcoord5.w = 0;
                OUT.ase_texcoord6.w = 0;

                v.vertex.xyz +=  float3( 0, 0, 0 ) ;

                float4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.worldPosition = v.vertex;
                OUT.vertex = vPosition;

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (v.vertex.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
                OUT.texcoord = v.texcoord;
                OUT.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN ) : SV_Target
            {
                //Round up the alpha color coming from the interpolator (to 1.0/256.0 steps)
                //The incoming alpha could have numerical instability, which makes it very sensible to
                //HDR color transparency blend, when it blends with the world's texture.
                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0/alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision)*invAlphaPrecision;

                float3 temp_output_227_0 = (IN.color).rgb;
                float2 temp_cast_0 = (_LinesScale).xx;
                float mulTime240 = _Time.y * _LineSpeed;
                float2 appendResult238 = (float2(0.0 , mulTime240));
                float2 texCoord237 = IN.texcoord.xy * temp_cast_0 + appendResult238;
                float smoothstepResult224 = smoothstep( _LinesSmoothstep.x , _LinesSmoothstep.y , tex2D( _MainTex, texCoord237 ).r);
                float3 lerpResult225 = lerp( ( temp_output_227_0 * _BGOffset ) , temp_output_227_0 , smoothstepResult224);
                float lerpResult231 = lerp( _BGAlpha , 1.0 , smoothstepResult224);
                float4 appendResult228 = (float4(lerpResult225 , lerpResult231));
                float4 color4_g504 = appendResult228;
                float radius34_g504 = ( _Float0 * IN.color.a );
                float temp_output_13_0_g522 = radius34_g504;
                float falloff38_g504 = 1.46;
                float3 ase_positionWS = IN.ase_texcoord3.xyz;
                float3 ase_tangentWS = IN.ase_texcoord4.xyz;
                float3 ase_normalWS = IN.ase_texcoord5.xyz;
                float3 ase_bitangentWS = IN.ase_texcoord6.xyz;
                float3x3 ase_worldToTangent = float3x3( ase_tangentWS, ase_bitangentWS, ase_normalWS );
                float3 worldToTangentPos252 = mul( ase_worldToTangent, ase_positionWS );
                float3 world_pos27_g504 = (worldToTangentPos252*_PixelFXScale + _PixelFXOffset);
                float3 temp_output_15_0_g514 = ( ( world_pos27_g504 * float3( 1,1,1 ) ) + float3( 0,0,0 ) );
                int blocks32_g504 = (int)_Blocks;
                float3 temp_cast_2 = blocks32_g504;
                float3 blocks_amount22_g514 = temp_cast_2;
                float3 blocks8_g517 = abs( blocks_amount22_g514 );
                float3 mosaicUV15_g517 = ( floor( ( temp_output_15_0_g514 * blocks8_g517 ) ) / blocks8_g517 );
                float3 result17_g517 = mosaicUV15_g517;
                float3 in_float3230_g520 = result17_g517;
                float3 in232_g520 = in_float3230_g520;
                float turb_scale33_g504 = 2.8;
                float3 temp_cast_3 = (turb_scale33_g504).xxx;
                float3 turb_scale23_g514 = temp_cast_3;
                float mulTime215 = _Time.y * _PixelFXSpeed;
                float3 temp_cast_4 = (mulTime215).xxx;
                float3 turb_off71_g504 = temp_cast_4;
                float3 turb_offset24_g514 = turb_off71_g504;
                float3 coords_mapped_float3177_g520 = (in_float3230_g520*turb_scale23_g514 + turb_offset24_g514);
                float3 coords26_g521 = coords_mapped_float3177_g520;
                float noise_s26_g514 = 1.0;
                float noise_scale205_g520 = noise_s26_g514;
                float scale25_g521 = noise_scale205_g520;
                float simplePerlin2D2_g521 = snoise( (coords26_g521).xy*scale25_g521 );
                simplePerlin2D2_g521 = simplePerlin2D2_g521*0.5 + 0.5;
                float simplePerlin2D5_g521 = snoise( (coords26_g521).yz*scale25_g521 );
                simplePerlin2D5_g521 = simplePerlin2D5_g521*0.5 + 0.5;
                float simplePerlin2D8_g521 = snoise( (coords26_g521).xz*scale25_g521 );
                simplePerlin2D8_g521 = simplePerlin2D8_g521*0.5 + 0.5;
                float3 appendResult23_g521 = (float3(simplePerlin2D2_g521 , simplePerlin2D5_g521 , simplePerlin2D8_g521));
                float3 temp_output_248_0_g520 = appendResult23_g521;
                float3 temp_cast_5 = (1.0).xxx;
                float3 noise118_g520 = saturate( ( ( saturate( temp_output_248_0_g520 ) * 2.0 ) - temp_cast_5 ) );
                float3 distortion_map27_g520 = noise118_g520;
                float3 temp_cast_6 = (-1.0).xxx;
                float turb_strength35_g504 = 1.0;
                float turb_strenght25_g514 = turb_strength35_g504;
                float temp_output_20_0_g514 = saturate( turb_strenght25_g514 );
                float temp_output_6_0_g520 = temp_output_20_0_g514;
                float3 temp_cast_7 = (temp_output_6_0_g520).xxx;
                float3 distortedUV37_g520 = ( in232_g520 + saturate( (( distortion_map27_g520 * temp_output_6_0_g520 ) + (distortion_map27_g520 - temp_cast_6) * (temp_cast_7 - ( distortion_map27_g520 * temp_output_6_0_g520 )) / (float3( 1,1,1 ) - temp_cast_6)) ) );
                float3 pixel_mapping54_g504 = ( distortedUV37_g520 + ( temp_output_20_0_g514 * -0.5 ) );
                float3 ase_objectPosition = UNITY_MATRIX_M._m03_m13_m23;
                float3 center85_g504 = ase_objectPosition;
                float dist19_g522 = distance( pixel_mapping54_g504 , center85_g504 );
                float smoothstepResult22_g522 = smoothstep( temp_output_13_0_g522 , ( temp_output_13_0_g522 + ( 1.0 - falloff38_g504 ) ) , dist19_g522);
                float mask29_g522 = ( 1.0 - smoothstepResult22_g522 );
                float temp_output_13_0_g505 = ( radius34_g504 + 0.0 );
                float3 temp_output_15_0_g506 = ( ( ( world_pos27_g504 + float3( -0.025,-0.025,-0.025 ) ) * float3( 1,1,1 ) ) + float3( 0.01,0.01,0.01 ) );
                float3 temp_cast_8 = blocks32_g504;
                float3 blocks_amount22_g506 = temp_cast_8;
                float3 blocks8_g509 = abs( blocks_amount22_g506 );
                float3 mosaicUV15_g509 = ( floor( ( temp_output_15_0_g506 * blocks8_g509 ) ) / blocks8_g509 );
                float3 result17_g509 = mosaicUV15_g509;
                float3 in_float3230_g512 = result17_g509;
                float3 in232_g512 = in_float3230_g512;
                float3 temp_cast_9 = (turb_scale33_g504).xxx;
                float3 turb_scale23_g506 = temp_cast_9;
                float3 turb_offset24_g506 = turb_off71_g504;
                float3 coords_mapped_float3177_g512 = (in_float3230_g512*turb_scale23_g506 + turb_offset24_g506);
                float3 coords26_g513 = coords_mapped_float3177_g512;
                float noise_s26_g506 = 1.0;
                float noise_scale205_g512 = noise_s26_g506;
                float scale25_g513 = noise_scale205_g512;
                float simplePerlin2D2_g513 = snoise( (coords26_g513).xy*scale25_g513 );
                simplePerlin2D2_g513 = simplePerlin2D2_g513*0.5 + 0.5;
                float simplePerlin2D5_g513 = snoise( (coords26_g513).yz*scale25_g513 );
                simplePerlin2D5_g513 = simplePerlin2D5_g513*0.5 + 0.5;
                float simplePerlin2D8_g513 = snoise( (coords26_g513).xz*scale25_g513 );
                simplePerlin2D8_g513 = simplePerlin2D8_g513*0.5 + 0.5;
                float3 appendResult23_g513 = (float3(simplePerlin2D2_g513 , simplePerlin2D5_g513 , simplePerlin2D8_g513));
                float3 temp_output_248_0_g512 = appendResult23_g513;
                float3 temp_cast_10 = (1.0).xxx;
                float3 noise118_g512 = saturate( ( ( saturate( temp_output_248_0_g512 ) * 2.0 ) - temp_cast_10 ) );
                float3 distortion_map27_g512 = noise118_g512;
                float3 temp_cast_11 = (-1.0).xxx;
                float turb_strenght25_g506 = turb_strength35_g504;
                float temp_output_20_0_g506 = saturate( turb_strenght25_g506 );
                float temp_output_6_0_g512 = temp_output_20_0_g506;
                float3 temp_cast_12 = (temp_output_6_0_g512).xxx;
                float3 distortedUV37_g512 = ( in232_g512 + saturate( (( distortion_map27_g512 * temp_output_6_0_g512 ) + (distortion_map27_g512 - temp_cast_11) * (temp_cast_12 - ( distortion_map27_g512 * temp_output_6_0_g512 )) / (float3( 1,1,1 ) - temp_cast_11)) ) );
                float dist19_g505 = distance( ( distortedUV37_g512 + ( temp_output_20_0_g506 * -0.5 ) ) , center85_g504 );
                float smoothstepResult22_g505 = smoothstep( temp_output_13_0_g505 , ( temp_output_13_0_g505 + ( 1.0 - ( falloff38_g504 + 0.0 ) ) ) , dist19_g505);
                float mask29_g505 = ( 1.0 - smoothstepResult22_g505 );
                float reveal_mask_offseted55_g504 = saturate( mask29_g505 );
                float reveal_mask60_g504 = ( saturate( mask29_g522 ) - reveal_mask_offseted55_g504 );
                float temp_output_68_0_g504 = saturate( ( ( reveal_mask60_g504 * ( 1.0 - reveal_mask60_g504 ) ) + ( reveal_mask_offseted55_g504 * ( 1.0 - reveal_mask_offseted55_g504 ) ) ) );
                float scan69_g504 = temp_output_68_0_g504;
                float3 temp_cast_13 = (( 1.0 - pow( scan69_g504 , 0.793311 ) )).xxx;
                float lerpResult21_g504 = lerp( 1.0 , 0.0 , reveal_mask_offseted55_g504);
                float lerpResult19_g504 = lerp( 0.0 , lerpResult21_g504 , (color4_g504).w);
                float4 appendResult5_g504 = (float4(( (color4_g504).xyz + step( temp_cast_13 , float3( 0.58,0.41,0.67 ) ) ) , lerpResult19_g504));
                

                half4 color = appendResult5_g504;

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                color.rgb *= color.a;

                return color;
            }
        ENDCG
        }
    }
    CustomEditor "AmplifyShaderEditor.MaterialInspector"
	
	Fallback Off
}
/*ASEBEGIN
Version=19801
Node;AmplifyShaderEditor.RangedFloatNode;241;-769.7312,-1791.264;Inherit;False;Property;_LineSpeed;Line Speed;16;0;Create;True;0;0;0;False;0;False;0;0.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;240;-592,-1792;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;238;-368,-1824;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;242;-368,-1904;Inherit;False;Property;_LinesScale;Lines Scale;17;0;Create;True;0;0;0;False;0;False;0;0.75;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;226;160,-2320;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexturePropertyNode;100;-352,-2096;Inherit;True;Property;_MainTex;MainTex;21;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TextureCoordinatesNode;237;-192,-1904;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;101;80,-2080;Inherit;True;Property;_TextureSample0;Texture Sample 0;10;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ComponentMaskNode;227;384,-2320;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector2Node;232;144,-1872;Inherit;False;Property;_LinesSmoothstep;Lines Smoothstep;14;0;Create;True;0;0;0;False;0;False;0,0.53;0,0.61;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;235;432,-2144;Inherit;False;Property;_BGOffset;BG Offset;15;0;Create;True;0;0;0;False;0;False;0;0.11;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;224;464,-2032;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;234;627.0397,-2168.818;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;230;560,-1888;Inherit;False;Property;_BGAlpha;BG Alpha;13;0;Create;True;0;0;0;False;0;False;0;0.97;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;217;480,-2688;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;220;768,-2496;Inherit;False;Property;_PixelFXScale;Pixel FX Scale;19;0;Create;True;0;0;0;False;0;False;1,0.5,1;1,1.5,1;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;221;768,-2336;Inherit;False;Property;_PixelFXOffset;Pixel FX Offset;20;0;Create;True;0;0;0;False;0;False;1,0.5,1;0,-2,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.LerpOp;231;800,-1952;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;225;848,-2128;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.VertexColorNode;244;1024,-1552;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;243;1024,-1632;Inherit;False;Property;_Float0;Float 0;11;0;Create;True;0;0;0;False;0;False;0;6;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;222;592,-1808;Inherit;False;Property;_PixelFXSpeed;Pixel FX Speed;12;0;Create;True;0;0;0;False;0;False;0;0.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TransformPositionNode;252;688,-2688;Inherit;False;World;Tangent;False;Fast;True;1;0;FLOAT3;0,0,0;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScaleAndOffsetNode;219;1008,-2528;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;1,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DynamicAppendNode;228;1096.483,-2096.461;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;245;1232,-1632;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;246;1216.569,-1952.678;Inherit;False;Property;_Blocks;Blocks;18;0;Create;True;0;0;0;False;0;False;0;7;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;215;784,-1808;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;257;1424,-2080;Inherit;False;Pixel Scan Sphere Mask;0;;504;6f86efe0cf2ef6c4da550677e0a9da94;0;14;78;FLOAT3;0,0,0;False;3;FLOAT4;0,0,0,0;False;14;FLOAT3;0.58,0.41,0.67;False;17;FLOAT;0.793311;False;72;INT;10;False;73;FLOAT;2.8;False;75;FLOAT3;0,0,0;False;74;FLOAT;1;False;86;FLOAT3;0,0,0;False;77;FLOAT;2.58;False;79;FLOAT;1.46;False;80;FLOAT3;-0.025,-0.025,-0.025;False;81;FLOAT;0;False;82;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;62;1968,-1936;Float;False;True;-1;3;AmplifyShaderEditor.MaterialInspector;0;3;Game/FX/Pixel Scan Hologram;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;False;True;3;1;False;;10;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;True;True;True;True;True;True;0;True;_ColorMask;False;False;False;False;False;False;False;True;True;0;True;_Stencil;255;True;_StencilReadMask;255;True;_StencilWriteMask;0;True;_StencilComp;0;True;_StencilOp;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;0;True;unity_GUIZTestMode;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;240;0;241;0
WireConnection;238;1;240;0
WireConnection;237;0;242;0
WireConnection;237;1;238;0
WireConnection;101;0;100;0
WireConnection;101;1;237;0
WireConnection;101;7;100;1
WireConnection;227;0;226;0
WireConnection;224;0;101;1
WireConnection;224;1;232;1
WireConnection;224;2;232;2
WireConnection;234;0;227;0
WireConnection;234;1;235;0
WireConnection;231;0;230;0
WireConnection;231;2;224;0
WireConnection;225;0;234;0
WireConnection;225;1;227;0
WireConnection;225;2;224;0
WireConnection;252;0;217;0
WireConnection;219;0;252;0
WireConnection;219;1;220;0
WireConnection;219;2;221;0
WireConnection;228;0;225;0
WireConnection;228;3;231;0
WireConnection;245;0;243;0
WireConnection;245;1;244;4
WireConnection;215;0;222;0
WireConnection;257;78;219;0
WireConnection;257;3;228;0
WireConnection;257;72;246;0
WireConnection;257;75;215;0
WireConnection;257;77;245;0
WireConnection;62;0;257;0
ASEEND*/
//CHKSM=472AC36BD7E3535B7D4A9F952A61B29C70FF314A