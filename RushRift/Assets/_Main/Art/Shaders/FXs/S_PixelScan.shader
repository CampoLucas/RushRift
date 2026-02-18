// Made with Amplify Shader Editor v1.9.8.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "S_PixelScan"
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

        _Texture0("Texture 0", 2D) = "white" {}
        _Float0("Float 0", Float) = 0
        _Float1("Float 1", Float) = 0
        _Float2("Float 2", Float) = 0
        _Float3("Float 2", Float) = 0
        _TextureSample0("Texture Sample 0", 2D) = "white" {}
        _Block("Block", Float) = 15
        _Vector0("Vector 0", Vector) = (0,0,0,0)
        _Vector1("Vector 0", Vector) = (0,0,0,0)
        _Vector3("Vector 0", Vector) = (0,0,0,0)
        _Float4("Float 4", Float) = 0
        _Vector4("Vector 4", Vector) = (0,0,0,0)
        _Vector5("Vector 4", Vector) = (0,0,0,0)
        _Float5("Float 5", Float) = 0
        _Float7("Float 7", Float) = 0
        _Color0("Color 0", Color) = (0.6729559,0,0.3251358,0)

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

            uniform sampler2D _TextureSample0;
            uniform float _Float7;
            uniform float4 _Color0;
            uniform float2 _Vector1;
            uniform float _Float0;
            uniform float _Float1;
            uniform float3 _Vector5;
            uniform float3 _Vector4;
            uniform float _Block;
            uniform float _Float2;
            uniform float _Float3;
            uniform float2 _Vector3;
            uniform sampler2D _Texture0;
            uniform float _Float4;
            uniform float2 _Vector0;
            uniform float _Float5;
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

                float2 texCoord6_g268 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float mulTime177 = _Time.y * 0.25;
                float2 temp_cast_0 = (mulTime177).xx;
                float2 temp_output_4_0_g268 = ( ( texCoord6_g268 * float2( 1,1 ) ) + temp_cast_0 );
                float4 tex2DNode77 = tex2D( _TextureSample0, temp_output_4_0_g268 );
                float temp_output_13_0_g265 = ( _Float0 * IN.color.a );
                float3 ase_positionWS = IN.ase_texcoord3.xyz;
                float3 temp_cast_1 = (_Block).xxx;
                float3 blocks8_g262 = abs( temp_cast_1 );
                float3 mosaicUV15_g262 = ( floor( ( ( ( ase_positionWS * _Vector5 ) + _Vector4 ) * blocks8_g262 ) ) / blocks8_g262 );
                float3 result17_g262 = ( mosaicUV15_g262 + ( ( float3( 1,0,0 ) / blocks8_g262 ) * 0.5 ) );
                float3 in_float3230_g263 = result17_g262;
                float3 in232_g263 = in_float3230_g263;
                float3 temp_cast_2 = (1.0).xxx;
                float3 temp_cast_3 = (_Time.y).xxx;
                float3 coords_mapped_float3177_g263 = (in_float3230_g263*temp_cast_2 + temp_cast_3);
                float3 coords26_g264 = coords_mapped_float3177_g263;
                float noise_scale205_g263 = _Float2;
                float scale25_g264 = noise_scale205_g263;
                float simplePerlin2D2_g264 = snoise( (coords26_g264).xy*scale25_g264 );
                simplePerlin2D2_g264 = simplePerlin2D2_g264*0.5 + 0.5;
                float simplePerlin2D5_g264 = snoise( (coords26_g264).yz*scale25_g264 );
                simplePerlin2D5_g264 = simplePerlin2D5_g264*0.5 + 0.5;
                float simplePerlin2D8_g264 = snoise( (coords26_g264).xz*scale25_g264 );
                simplePerlin2D8_g264 = simplePerlin2D8_g264*0.5 + 0.5;
                float3 appendResult23_g264 = (float3(simplePerlin2D2_g264 , simplePerlin2D5_g264 , simplePerlin2D8_g264));
                float3 temp_output_248_0_g263 = appendResult23_g264;
                float3 temp_cast_4 = (1.0).xxx;
                float3 noise118_g263 = saturate( ( ( saturate( temp_output_248_0_g263 ) * 2.0 ) - temp_cast_4 ) );
                float3 distortion_map27_g263 = noise118_g263;
                float3 temp_cast_5 = (-1.0).xxx;
                float temp_output_6_0_g263 = _Float3;
                float3 temp_cast_6 = (temp_output_6_0_g263).xxx;
                float3 distortedUV37_g263 = ( in232_g263 + saturate( (( distortion_map27_g263 * temp_output_6_0_g263 ) + (distortion_map27_g263 - temp_cast_5) * (temp_cast_6 - ( distortion_map27_g263 * temp_output_6_0_g263 )) / (float3( 1,1,1 ) - temp_cast_5)) ) );
                float3 ase_objectPosition = UNITY_MATRIX_M._m03_m13_m23;
                float dist19_g265 = distance( distortedUV37_g263 , ase_objectPosition );
                float smoothstepResult22_g265 = smoothstep( temp_output_13_0_g265 , ( temp_output_13_0_g265 + ( 1.0 - _Float1 ) ) , dist19_g265);
                float mask29_g265 = ( 1.0 - smoothstepResult22_g265 );
                float revealMaskRaw72 = ( 1.0 - saturate( mask29_g265 ) );
                float smoothstepResult121 = smoothstep( _Vector1.x , _Vector1.y , ( 1.0 - revealMaskRaw72 ));
                float smoothstepResult144 = smoothstep( _Vector3.x , _Vector3.y , revealMaskRaw72);
                float2 texCoord12_g266 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float2 uv181_g266 = texCoord12_g266;
                float4 in_float4231_g266 = float4( uv181_g266, 0.0 , 0.0 );
                float4 in232_g266 = in_float4231_g266;
                float2 texCoord124_g266 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float4 tex2DNode96_g266 = tex2D( _Texture0, texCoord124_g266 );
                float height_map116_g266 = tex2DNode96_g266.r;
                float distortion_map27_g266 = height_map116_g266;
                float temp_output_6_0_g266 = 0.05;
                float4 distortedUV37_g266 = ( in232_g266 + saturate( (( distortion_map27_g266 * temp_output_6_0_g266 ) + (distortion_map27_g266 - -1.0) * (temp_output_6_0_g266 - ( distortion_map27_g266 * temp_output_6_0_g266 )) / (1.0 - -1.0)) ) );
                float lerpResult114 = lerp( smoothstepResult121 , smoothstepResult144 , distortedUV37_g266.x);
                float blockCircleMask126 = lerpResult114;
                float4 lerpResult139 = lerp( float4( 0.4666667,0,0.2769394,0 ) , ( _Color0 * tex2DNode77.r ) , blockCircleMask126);
                float4 lerpResult133 = lerp( ( tex2DNode77.r * IN.color * _Float7 ) , lerpResult139 , step( revealMaskRaw72 , _Float4 ));
                float4 temp_output_2_0_g269 = lerpResult133;
                float smoothstepResult119 = smoothstep( _Vector0.x , _Vector0.y , revealMaskRaw72);
                float revealMask124 = smoothstepResult119;
                float lerpResult192 = lerp( 0.25 , 1.0 , tex2DNode77.r);
                float4 appendResult184 = (float4((temp_output_2_0_g269).rgb , saturate( ( revealMask124 * _Float5 * lerpResult192 ) )));
                

                half4 color = appendResult184;

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
Node;AmplifyShaderEditor.CommentaryNode;129;-3088,-1568;Inherit;False;2971.051;1236;Scan;4;110;127;172;208;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;110;-3040,-1504;Inherit;False;2871.051;681.7978;Comment;11;124;119;120;72;107;71;109;171;189;13;297;Reveal;0.3522012,0.06091525,0.06091525,1;0;0
Node;AmplifyShaderEditor.Vector3Node;173;-3248,-752;Inherit;False;Property;_Vector5;Vector 4;14;0;Create;True;0;0;0;False;0;False;0,0,0;1,1.5,1;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldPosInputsNode;66;-3376,-1328;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.CommentaryNode;109;-2720,-1424;Inherit;False;302;283.6666;Mosaic;1;108;;0.5911949,0.03904109,0.1782698,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;208;-3072,-960;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector3Node;172;-2896,-736;Inherit;False;Property;_Vector4;Vector 4;13;0;Create;True;0;0;0;False;0;False;0,0,0;0,-2.25,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.CommentaryNode;107;-1584,-1408;Inherit;False;516;402.6667;Sphere Mask;3;12;55;190;;0.8113208,0.300061,0.1301174,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;171;-2640,-880;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;108;-2672,-1264;Inherit;False;Property;_Block;Block;8;0;Create;True;0;0;0;False;0;False;15;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;71;-2368,-1456;Inherit;False;765.238;599.0781;Disturbance Mask;5;19;18;167;302;337;;1,0.7985466,0.2672954,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;18;-2224,-1216;Inherit;False;Property;_Float2;Float 2;4;0;Create;True;0;0;0;False;0;False;0;0.85;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;167;-2224,-1136;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;19;-2192,-1056;Inherit;False;Property;_Float3;Float 2;6;0;Create;True;0;0;0;False;0;False;0;1.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;346;-2672,-1376;Inherit;True;Mosaic;-1;;262;753f4181acbeb85409aa76f16d4efdcf;3,28,2,31,2,6,1;8;1;FLOAT2;0,0;False;36;FLOAT;0;False;37;FLOAT3;0,0,0;False;38;FLOAT4;0,0,0,0;False;2;INT;50;False;3;FLOAT2;50,50;False;33;FLOAT3;50,50,50;False;34;FLOAT4;50,50,50,50;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;12;-1488,-1168;Inherit;False;Property;_Float0;Float 0;2;0;Create;True;0;0;0;False;0;False;0;4.06;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;189;-1488,-1088;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;13;-1520,-896;Inherit;False;Property;_Float1;Float 1;3;0;Create;True;0;0;0;False;0;False;0;4.27;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ObjectPositionNode;297;-1648,-1152;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.FunctionNode;347;-1904,-1360;Inherit;True;Turbulent Displace;0;;263;1752c056cb77e9b41aa825aec99b7ba3;5,101,1,174,2,227,2,149,2,102,2;17;48;FLOAT3;0,0,0;False;60;FLOAT2;0,0;False;61;FLOAT4;0,0,0,0;False;62;FLOAT;0;False;97;SAMPLER2D;;False;123;FLOAT2;0,0;False;99;SAMPLERSTATE;;False;141;FLOAT2;0,0;False;140;FLOAT;0;False;142;FLOAT3;0,0,0;False;143;FLOAT4;0,0,0,0;False;144;FLOAT2;0,0;False;148;FLOAT;0;False;145;FLOAT3;0,0,0;False;146;FLOAT4;0,0,0,0;False;156;FLOAT;1;False;6;FLOAT;0.05;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;190;-1290.251,-1168.111;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;348;-1520,-1360;Inherit;True;Sphere Mask v2;-1;;265;3349735a98772ad4792a5429a2dc2492;0;4;8;FLOAT3;0,0,0;False;9;FLOAT3;0,0,0;False;13;FLOAT;1.5;False;16;FLOAT;1.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;55;-1248,-1360;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;127;-1376,-784;Inherit;False;1204;466;Block Circle Mask;8;122;121;73;112;114;126;144;145;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;72;-992,-1360;Inherit;False;revealMaskRaw;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;73;-1328,-704;Inherit;False;72;revealMaskRaw;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;145;-1040,-672;Inherit;False;Property;_Vector3;Vector 0;11;0;Create;True;0;0;0;False;0;False;0,0;-0.18,0.88;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;122;-1040,-448;Inherit;False;Property;_Vector1;Vector 0;10;0;Create;True;0;0;0;False;0;False;0,0;0.45,0.05;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.OneMinusNode;112;-1008,-544;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;178;-502.363,164.4946;Inherit;False;Constant;_Float6;Float 6;15;0;Create;True;0;0;0;False;0;False;0.25;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;144;-800,-720;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;121;-800,-576;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;177;-320,144;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;349;-784,-448;Inherit;False;Turbulent Displace;0;;266;1752c056cb77e9b41aa825aec99b7ba3;5,101,0,174,3,227,3,149,3,102,0;17;48;FLOAT3;0,0,0;False;60;FLOAT2;0,0;False;61;FLOAT4;0,0,0,0;False;62;FLOAT;0;False;97;SAMPLER2D;;False;123;FLOAT2;0,0;False;99;SAMPLERSTATE;;False;141;FLOAT2;0,0;False;140;FLOAT;0;False;142;FLOAT3;0,0,0;False;143;FLOAT4;0,0,0,0;False;144;FLOAT2;0,0;False;148;FLOAT;0;False;145;FLOAT3;0,0,0;False;146;FLOAT4;0,0,0,0;False;156;FLOAT;1;False;6;FLOAT;0.05;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp;114;-592,-656;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;120;-992,-1264;Inherit;False;Property;_Vector0;Vector 0;9;0;Create;True;0;0;0;False;0;False;0,0;-0.09,0.93;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.FunctionNode;350;-96,64;Inherit;False;Tilling And Offset;-1;;268;992a7dac1d94f9a47be7a2d63002dd82;0;3;1;FLOAT2;0,0;False;3;FLOAT2;1,1;False;5;FLOAT2;0,0;False;3;FLOAT2;0;FLOAT;7;FLOAT;10
Node;AmplifyShaderEditor.RegisterLocalVarNode;126;-416,-736;Inherit;False;blockCircleMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;191;240,-240;Inherit;False;Property;_Color0;Color 0;17;0;Create;True;0;0;0;False;0;False;0.6729559,0,0.3251358,0;0.9685534,0.3929034,0.4083606,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;77;128,96;Inherit;True;Property;_TextureSample0;Texture Sample 0;7;0;Create;True;0;0;0;False;0;False;-1;None;b6662a9cff1c7d345a378956d0faea28;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SmoothstepOpNode;119;-624,-1360;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;128;560,-128;Inherit;False;126;blockCircleMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;152;800,208;Inherit;False;72;revealMaskRaw;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;186;624,-32;Inherit;False;2;2;0;COLOR;1,1,1,1;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;185;592,432;Inherit;False;Property;_Float7;Float 7;16;0;Create;True;0;0;0;False;0;False;0;1.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;180;544,256;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;153;832,288;Inherit;False;Property;_Float4;Float 4;12;0;Create;True;0;0;0;False;0;False;0;0.64;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;124;-400,-1360;Inherit;False;revealMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;139;880,-128;Inherit;False;3;0;COLOR;0.4666667,0,0.2769394,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StepOpNode;138;1024,192;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;179;560,128;Inherit;False;3;3;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;125;704,656;Inherit;False;124;revealMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;175;736,752;Inherit;False;Property;_Float5;Float 5;15;0;Create;True;0;0;0;False;0;False;0;3.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;192;864,496;Inherit;False;3;0;FLOAT;0.25;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;133;1040,48;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;1,1,1,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;174;1168,560;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;183;1198.643,255.7234;Inherit;False;Alpha Split;-1;;269;07dab7960105b86429ac8eebd729ed6d;0;1;2;COLOR;0,0,0,0;False;2;FLOAT3;0;FLOAT;6
Node;AmplifyShaderEditor.SaturateNode;193;1359.468,686.5437;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;337;-2000,-1072;Inherit;False;Property;_Vector2;Vector 2;18;0;Create;True;0;0;0;False;0;False;0,0,0;-0.72,-0.27,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.DynamicAppendNode;184;1376,512;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;302;-2192,-1296;Inherit;False;Property;_Float8;Float 2;5;0;Create;True;0;0;0;False;0;False;0;0.09;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;182;1552,528;Float;False;True;-1;3;AmplifyShaderEditor.MaterialInspector;0;3;S_PixelScan;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;False;True;3;1;False;;10;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;True;_ColorMask;False;False;False;False;False;False;False;True;True;0;True;_Stencil;255;True;_StencilReadMask;255;True;_StencilWriteMask;0;True;_StencilComp;0;True;_StencilOp;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;0;True;unity_GUIZTestMode;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;208;0;66;0
WireConnection;208;1;173;0
WireConnection;171;0;208;0
WireConnection;171;1;172;0
WireConnection;346;37;171;0
WireConnection;346;33;108;0
WireConnection;347;48;346;0
WireConnection;347;145;167;0
WireConnection;347;156;18;0
WireConnection;347;6;19;0
WireConnection;190;0;12;0
WireConnection;190;1;189;4
WireConnection;348;8;347;0
WireConnection;348;9;297;0
WireConnection;348;13;190;0
WireConnection;348;16;13;0
WireConnection;55;0;348;0
WireConnection;72;0;55;0
WireConnection;112;0;73;0
WireConnection;144;0;73;0
WireConnection;144;1;145;1
WireConnection;144;2;145;2
WireConnection;121;0;112;0
WireConnection;121;1;122;1
WireConnection;121;2;122;2
WireConnection;177;0;178;0
WireConnection;114;0;121;0
WireConnection;114;1;144;0
WireConnection;114;2;349;0
WireConnection;350;5;177;0
WireConnection;126;0;114;0
WireConnection;77;1;350;0
WireConnection;119;0;72;0
WireConnection;119;1;120;1
WireConnection;119;2;120;2
WireConnection;186;0;191;0
WireConnection;186;1;77;1
WireConnection;124;0;119;0
WireConnection;139;1;186;0
WireConnection;139;2;128;0
WireConnection;138;0;152;0
WireConnection;138;1;153;0
WireConnection;179;0;77;1
WireConnection;179;1;180;0
WireConnection;179;2;185;0
WireConnection;192;2;77;1
WireConnection;133;0;179;0
WireConnection;133;1;139;0
WireConnection;133;2;138;0
WireConnection;174;0;125;0
WireConnection;174;1;175;0
WireConnection;174;2;192;0
WireConnection;183;2;133;0
WireConnection;193;0;174;0
WireConnection;184;0;183;0
WireConnection;184;3;193;0
WireConnection;182;0;184;0
ASEEND*/
//CHKSM=349C1C4568E41B1DC3C35F95D8CC6C27AD40A286