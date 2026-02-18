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
                
                
                //setting value to unused interpolator channels and avoid initialization warnings
                OUT.ase_texcoord3.w = 0;

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
                float4 color4_g428 = appendResult228;
                float radius34_g428 = ( _Float0 * IN.color.a );
                float temp_output_13_0_g430 = radius34_g428;
                float falloff38_g428 = 1.46;
                float3 ase_positionWS = IN.ase_texcoord3.xyz;
                float3 world_pos27_g428 = (ase_positionWS*_PixelFXScale + _PixelFXOffset);
                float3 temp_output_15_0_g439 = ( ( world_pos27_g428 * float3( 1,1,1 ) ) + float3( 0,0,0 ) );
                int temp_output_72_0_g428 = (int)_Blocks;
                float3 appendResult85_g428 = (float3((float)temp_output_72_0_g428 , (float)temp_output_72_0_g428 , (float)temp_output_72_0_g428));
                float3 blocks32_g428 = appendResult85_g428;
                float3 blocks_amount22_g439 = blocks32_g428;
                float3 blocks8_g446 = abs( blocks_amount22_g439 );
                float3 mosaicUV15_g446 = ( floor( ( temp_output_15_0_g439 * blocks8_g446 ) ) / blocks8_g446 );
                float3 result17_g446 = mosaicUV15_g446;
                float3 in_float3230_g441 = result17_g446;
                float3 in232_g441 = in_float3230_g441;
                float turb_scale33_g428 = 2.8;
                float3 temp_cast_5 = (turb_scale33_g428).xxx;
                float3 turb_scale23_g439 = temp_cast_5;
                float mulTime215 = _Time.y * _PixelFXSpeed;
                float3 temp_cast_6 = (mulTime215).xxx;
                float3 turb_off71_g428 = temp_cast_6;
                float3 turb_offset24_g439 = turb_off71_g428;
                float3 coords_mapped_float3177_g441 = (in_float3230_g441*turb_scale23_g439 + turb_offset24_g439);
                float3 coords26_g442 = coords_mapped_float3177_g441;
                float noise_s26_g439 = 1.0;
                float noise_scale205_g441 = noise_s26_g439;
                float scale25_g442 = noise_scale205_g441;
                float simplePerlin2D2_g442 = snoise( (coords26_g442).xy*scale25_g442 );
                simplePerlin2D2_g442 = simplePerlin2D2_g442*0.5 + 0.5;
                float simplePerlin2D5_g442 = snoise( (coords26_g442).yz*scale25_g442 );
                simplePerlin2D5_g442 = simplePerlin2D5_g442*0.5 + 0.5;
                float simplePerlin2D8_g442 = snoise( (coords26_g442).xz*scale25_g442 );
                simplePerlin2D8_g442 = simplePerlin2D8_g442*0.5 + 0.5;
                float3 appendResult23_g442 = (float3(simplePerlin2D2_g442 , simplePerlin2D5_g442 , simplePerlin2D8_g442));
                float3 temp_output_248_0_g441 = appendResult23_g442;
                float3 temp_cast_7 = (1.0).xxx;
                float3 noise118_g441 = saturate( ( ( saturate( temp_output_248_0_g441 ) * 2.0 ) - temp_cast_7 ) );
                float3 distortion_map27_g441 = noise118_g441;
                float3 temp_cast_8 = (-1.0).xxx;
                float turb_strength35_g428 = 1.0;
                float turb_strenght25_g439 = turb_strength35_g428;
                float temp_output_20_0_g439 = saturate( turb_strenght25_g439 );
                float temp_output_6_0_g441 = temp_output_20_0_g439;
                float3 temp_cast_9 = (temp_output_6_0_g441).xxx;
                float3 distortedUV37_g441 = ( in232_g441 + saturate( (( distortion_map27_g441 * temp_output_6_0_g441 ) + (distortion_map27_g441 - temp_cast_8) * (temp_cast_9 - ( distortion_map27_g441 * temp_output_6_0_g441 )) / (float3( 1,1,1 ) - temp_cast_8)) ) );
                float3 pixel_mapping54_g428 = ( distortedUV37_g441 + ( temp_output_20_0_g439 * -0.5 ) );
                float3 ase_objectPosition = UNITY_MATRIX_M._m03_m13_m23;
                float dist19_g430 = distance( pixel_mapping54_g428 , ase_objectPosition );
                float smoothstepResult22_g430 = smoothstep( temp_output_13_0_g430 , ( temp_output_13_0_g430 + ( 1.0 - falloff38_g428 ) ) , dist19_g430);
                float mask29_g430 = ( 1.0 - smoothstepResult22_g430 );
                float temp_output_13_0_g429 = ( radius34_g428 + 0.0 );
                float3 temp_output_15_0_g431 = ( ( ( world_pos27_g428 + float3( -0.025,-0.025,-0.025 ) ) * float3( 1,1,1 ) ) + float3( 0.01,0.01,0.01 ) );
                float3 blocks_amount22_g431 = blocks32_g428;
                float3 blocks8_g438 = abs( blocks_amount22_g431 );
                float3 mosaicUV15_g438 = ( floor( ( temp_output_15_0_g431 * blocks8_g438 ) ) / blocks8_g438 );
                float3 result17_g438 = mosaicUV15_g438;
                float3 in_float3230_g433 = result17_g438;
                float3 in232_g433 = in_float3230_g433;
                float3 temp_cast_10 = (turb_scale33_g428).xxx;
                float3 turb_scale23_g431 = temp_cast_10;
                float3 turb_offset24_g431 = turb_off71_g428;
                float3 coords_mapped_float3177_g433 = (in_float3230_g433*turb_scale23_g431 + turb_offset24_g431);
                float3 coords26_g434 = coords_mapped_float3177_g433;
                float noise_s26_g431 = 1.0;
                float noise_scale205_g433 = noise_s26_g431;
                float scale25_g434 = noise_scale205_g433;
                float simplePerlin2D2_g434 = snoise( (coords26_g434).xy*scale25_g434 );
                simplePerlin2D2_g434 = simplePerlin2D2_g434*0.5 + 0.5;
                float simplePerlin2D5_g434 = snoise( (coords26_g434).yz*scale25_g434 );
                simplePerlin2D5_g434 = simplePerlin2D5_g434*0.5 + 0.5;
                float simplePerlin2D8_g434 = snoise( (coords26_g434).xz*scale25_g434 );
                simplePerlin2D8_g434 = simplePerlin2D8_g434*0.5 + 0.5;
                float3 appendResult23_g434 = (float3(simplePerlin2D2_g434 , simplePerlin2D5_g434 , simplePerlin2D8_g434));
                float3 temp_output_248_0_g433 = appendResult23_g434;
                float3 temp_cast_11 = (1.0).xxx;
                float3 noise118_g433 = saturate( ( ( saturate( temp_output_248_0_g433 ) * 2.0 ) - temp_cast_11 ) );
                float3 distortion_map27_g433 = noise118_g433;
                float3 temp_cast_12 = (-1.0).xxx;
                float turb_strenght25_g431 = turb_strength35_g428;
                float temp_output_20_0_g431 = saturate( turb_strenght25_g431 );
                float temp_output_6_0_g433 = temp_output_20_0_g431;
                float3 temp_cast_13 = (temp_output_6_0_g433).xxx;
                float3 distortedUV37_g433 = ( in232_g433 + saturate( (( distortion_map27_g433 * temp_output_6_0_g433 ) + (distortion_map27_g433 - temp_cast_12) * (temp_cast_13 - ( distortion_map27_g433 * temp_output_6_0_g433 )) / (float3( 1,1,1 ) - temp_cast_12)) ) );
                float dist19_g429 = distance( ( distortedUV37_g433 + ( temp_output_20_0_g431 * -0.5 ) ) , ase_objectPosition );
                float smoothstepResult22_g429 = smoothstep( temp_output_13_0_g429 , ( temp_output_13_0_g429 + ( 1.0 - ( falloff38_g428 + 0.0 ) ) ) , dist19_g429);
                float mask29_g429 = ( 1.0 - smoothstepResult22_g429 );
                float reveal_mask_offseted55_g428 = saturate( mask29_g429 );
                float reveal_mask60_g428 = ( saturate( mask29_g430 ) - reveal_mask_offseted55_g428 );
                float temp_output_68_0_g428 = saturate( ( ( reveal_mask60_g428 * ( 1.0 - reveal_mask60_g428 ) ) + ( reveal_mask_offseted55_g428 * ( 1.0 - reveal_mask_offseted55_g428 ) ) ) );
                float scan69_g428 = temp_output_68_0_g428;
                float3 temp_cast_14 = (( 1.0 - pow( scan69_g428 , 0.793311 ) )).xxx;
                float lerpResult21_g428 = lerp( 1.0 , 0.0 , reveal_mask_offseted55_g428);
                float lerpResult19_g428 = lerp( 0.0 , lerpResult21_g428 , (color4_g428).w);
                float4 appendResult5_g428 = (float4(( (color4_g428).xyz + step( temp_cast_14 , float3( 0.58,0.41,0.67 ) ) ) , lerpResult19_g428));
                

                half4 color = appendResult5_g428;

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
Node;AmplifyShaderEditor.Vector3Node;220;768,-2496;Inherit;False;Property;_PixelFXScale;Pixel FX Scale;19;0;Create;True;0;0;0;False;0;False;1,0.5,1;1,1.5,1;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;221;768,-2336;Inherit;False;Property;_PixelFXOffset;Pixel FX Offset;20;0;Create;True;0;0;0;False;0;False;1,0.5,1;0,-2,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;222;864,-1776;Inherit;False;Property;_PixelFXSpeed;Pixel FX Speed;12;0;Create;True;0;0;0;False;0;False;0;0.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;231;800,-1952;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;225;848,-2128;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.VertexColorNode;244;1024,-1552;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;243;1024,-1632;Inherit;False;Property;_Float0;Float 0;11;0;Create;True;0;0;0;False;0;False;0;5.74;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;217;768,-2656;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ScaleAndOffsetNode;219;1008,-2528;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;1,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleTimeNode;215;1056,-1776;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;228;1096.483,-2096.461;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;245;1232,-1632;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;246;1216.569,-1952.678;Inherit;False;Property;_Blocks;Blocks;18;0;Create;True;0;0;0;False;0;False;0;7;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ObjectPositionNode;247;992,-2784;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.FunctionNode;250;1424,-2080;Inherit;False;Pixel Scan Sphere Mask;0;;428;6f86efe0cf2ef6c4da550677e0a9da94;0;13;78;FLOAT3;0,0,0;False;3;FLOAT4;0,0,0,0;False;14;FLOAT3;0.58,0.41,0.67;False;17;FLOAT;0.793311;False;72;INT;10;False;73;FLOAT;2.8;False;75;FLOAT3;0,0,0;False;74;FLOAT;1;False;77;FLOAT;2.58;False;79;FLOAT;1.46;False;80;FLOAT3;-0.025,-0.025,-0.025;False;81;FLOAT;0;False;82;FLOAT;0;False;1;FLOAT4;0
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
WireConnection;219;0;217;0
WireConnection;219;1;220;0
WireConnection;219;2;221;0
WireConnection;215;0;222;0
WireConnection;228;0;225;0
WireConnection;228;3;231;0
WireConnection;245;0;243;0
WireConnection;245;1;244;4
WireConnection;250;78;219;0
WireConnection;250;3;228;0
WireConnection;250;72;246;0
WireConnection;250;75;215;0
WireConnection;250;77;245;0
WireConnection;62;0;250;0
ASEEND*/
//CHKSM=B462908E61CBFD81AE979F62F5BD014451490D23