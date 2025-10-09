// Made with Amplify Shader Editor v1.9.8.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "S_PopUp"
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

        _Color2Intensity("Color2Intensity", Range( 0 , 1)) = 0
        _Frequency("Frequency", Float) = 0
        _Amplitude("Amplitude", Float) = 0
        _Width("Width", Range( 0 , 1)) = 0
        _NoiseScale("NoiseScale", Float) = 0
        _NoiseSpeed("NoiseSpeed", Float) = 0
        _NoiseMinAlpha("NoiseMinAlpha", Range( 0 , 1)) = 0.3804348
        _NoiseMaxAlpha("NoiseMaxAlpha", Range( 0 , 1)) = 1
        _BorderWidthMax("BorderWidthMax", Float) = 0
        _BorderWidthMin("BorderWidthMin", Float) = 0
        _BorderWidth("BorderWidth", Float) = 0
        _LineSpeed("LineSpeed", Float) = 0
        _BackgroundColor("BackgroundColor", Color) = (0,0,0,0)
        _AlphaOffset("AlphaOffset", Float) = 0

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

            uniform float4 _BackgroundColor;
            uniform float _Color2Intensity;
            uniform float _BorderWidthMax;
            uniform float _BorderWidthMin;
            uniform float _BorderWidth;
            uniform float _Frequency;
            uniform float _LineSpeed;
            uniform float _Amplitude;
            uniform float _Width;
            uniform float _NoiseSpeed;
            uniform float _NoiseScale;
            uniform float _NoiseMinAlpha;
            uniform float _NoiseMaxAlpha;
            uniform float _AlphaOffset;
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

                float4 Color5 = IN.color;
                float4 SecondaryColor6 = ( Color5 * _Color2Intensity );
                float2 texCoord41 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float2 Coords42 = texCoord41;
                float2 temp_output_6_0_g16 = ( 1.0 - ( saturate( abs( ( ( Coords42 * float2( 2,2 ) ) - float2( 1,1 ) ) ) ) - float2( 1,1 ) ) );
                float2 temp_cast_1 = (_BorderWidthMax).xx;
                float2 temp_cast_2 = (_BorderWidthMax).xx;
                float2 temp_cast_3 = (_BorderWidthMin).xx;
                float2 smoothstepResult14_g16 = smoothstep( temp_cast_2 , temp_cast_3 , temp_output_6_0_g16);
                float2 lerpResult17_g16 = lerp( step( temp_output_6_0_g16 , temp_cast_1 ) , smoothstepResult14_g16 , (float)1);
                float2 break8_g16 = lerpResult17_g16;
                float2 temp_output_6_0_g15 = ( 1.0 - ( saturate( abs( ( ( Coords42 * float2( 2,2 ) ) - float2( 1,1 ) ) ) ) - float2( 1,1 ) ) );
                float2 temp_cast_5 = (_BorderWidth).xx;
                float2 smoothstepResult14_g15 = smoothstep( float2( 2,2 ) , float2( 2,2 ) , temp_output_6_0_g15);
                float2 lerpResult17_g15 = lerp( step( temp_output_6_0_g15 , temp_cast_5 ) , smoothstepResult14_g15 , (float)0);
                float2 break8_g15 = lerpResult17_g15;
                float temp_output_144_0 = saturate( ( break8_g15.x + break8_g15.y ) );
                float lerpResult145 = lerp( saturate( ( break8_g16.x + break8_g16.y ) ) , temp_output_144_0 , temp_output_144_0);
                float BorderMask109 = lerpResult145;
                float4 lerpResult118 = lerp( SecondaryColor6 , Color5 , BorderMask109);
                float4 break117 = lerpResult118;
                float2 break20_g17 = Coords42;
                float lerpResult11_g17 = lerp( break20_g17.x , break20_g17.y , 1.0);
                float temp_output_3_0_g17 = ( frac( ( ( lerpResult11_g17 * _Frequency ) + ( _LineSpeed * _Time.y ) ) ) * _Amplitude );
                float temp_output_15_0_g17 = _Width;
                float lerpResult13_g17 = lerp( temp_output_3_0_g17 , step( temp_output_3_0_g17 , temp_output_15_0_g17 ) , ceil( temp_output_15_0_g17 ));
                float3 ase_positionWS = IN.ase_texcoord3.xyz;
                float2 temp_cast_8 = (( _Time.y * _NoiseSpeed )).xx;
                float simplePerlin2D39 = snoise( ( ( ase_positionWS.xy * float2( 1,1 ) ) + temp_cast_8 )*_NoiseScale );
                simplePerlin2D39 = simplePerlin2D39*0.5 + 0.5;
                float lerpResult38 = lerp( 0.0 , lerpResult13_g17 , (_NoiseMinAlpha + (simplePerlin2D39 - 0.0) * (_NoiseMaxAlpha - _NoiseMinAlpha) / (1.0 - 0.0)));
                float BackgroundMask111 = lerpResult38;
                float lerpResult112 = lerp( BackgroundMask111 , break117.a , BorderMask109);
                float4 appendResult20 = (float4(break117.r , break117.g , break117.b , lerpResult112));
                float4 lerpResult132 = lerp( _BackgroundColor , appendResult20 , lerpResult112);
                float4 break134 = lerpResult132;
                float4 appendResult131 = (float4(break134.x , break134.y , break134.z , saturate( ( break134.w + _AlphaOffset ) )));
                

                half4 color = appendResult131;

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
Node;AmplifyShaderEditor.CommentaryNode;58;-3152,368;Inherit;False;2254.279;890.6484;Lines Mask;8;110;106;107;38;56;57;111;139;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;59;-1858,-434;Inherit;False;964;419;Cached Vars;10;4;3;2;5;6;41;42;61;62;63;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;56;-3104,864;Inherit;False;1188;371;Noise;10;50;55;45;54;49;46;39;52;53;51;;1,1,1,1;0;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;41;-1792,-384;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleTimeNode;50;-3056,992;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;55;-3056,1072;Inherit;False;Property;_NoiseSpeed;NoiseSpeed;5;0;Create;True;0;0;0;False;0;False;0;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;42;-1536,-384;Inherit;False;Coords;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;103;-1824,0;Inherit;False;1105.324;370.3958;Border;10;141;144;143;105;104;109;145;142;72;99;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;54;-2864,992;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;57;-2816,416;Inherit;False;821;419;Lines;8;47;122;30;26;37;43;123;124;;1,1,1,1;0;0
Node;AmplifyShaderEditor.VertexColorNode;2;-1808,-224;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldPosInputsNode;139;-3136,688;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;99;-1776,48;Inherit;False;42;Coords;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;72;-1792,128;Inherit;False;Property;_BorderWidthMax;BorderWidthMax;8;0;Create;True;0;0;0;False;0;False;0;1.67;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;142;-1792,208;Inherit;False;Property;_BorderWidthMin;BorderWidthMin;9;0;Create;True;0;0;0;False;0;False;0;0.33;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;143;-1728,288;Inherit;False;Property;_BorderWidth;BorderWidth;10;0;Create;True;0;0;0;False;0;False;0;1.03;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;49;-2672,912;Inherit;False;F_TillingAndOffset;-1;;8;992a7dac1d94f9a47be7a2d63002dd82;0;3;1;FLOAT2;0,0;False;3;FLOAT2;1,1;False;5;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;46;-2640,1040;Inherit;False;Property;_NoiseScale;NoiseScale;4;0;Create;True;0;0;0;False;0;False;0;-0.02;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;123;-2752,688;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;124;-2720,608;Inherit;False;Property;_LineSpeed;LineSpeed;11;0;Create;True;0;0;0;False;0;False;0;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;4;-1632,-144;Inherit;False;Property;_Color2Intensity;Color2Intensity;0;0;Create;True;0;0;0;False;0;False;0;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;5;-1568,-224;Inherit;False;Color;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;144;-1472,208;Inherit;False;F_BorderMask;-1;;15;5e46f25ec17629e41a65d4b125b40423;0;5;12;FLOAT2;0,0;False;11;FLOAT2;2,2;False;15;FLOAT2;2,2;False;16;FLOAT2;2,2;False;18;INT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;141;-1520,32;Inherit;False;F_BorderMask;-1;;16;5e46f25ec17629e41a65d4b125b40423;0;5;12;FLOAT2;0,0;False;11;FLOAT2;2,2;False;15;FLOAT2;2,2;False;16;FLOAT2;2,2;False;18;INT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;39;-2416,912;Inherit;False;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;52;-2432,1040;Inherit;False;Property;_NoiseMinAlpha;NoiseMinAlpha;6;0;Create;True;0;0;0;False;0;False;0.3804348;0.3804348;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;53;-2432,1120;Inherit;False;Property;_NoiseMaxAlpha;NoiseMaxAlpha;7;0;Create;True;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;43;-2480,464;Inherit;False;42;Coords;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;37;-2576,752;Inherit;False;Property;_Width;Width;3;0;Create;True;0;0;0;False;0;False;0;0.08073624;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;30;-2448,592;Inherit;False;Property;_Amplitude;Amplitude;2;0;Create;True;0;0;0;False;0;False;0;0.2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;122;-2560,656;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;26;-2448,528;Inherit;False;Property;_Frequency;Frequency;1;0;Create;True;0;0;0;False;0;False;0;60;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3;-1344,-224;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;145;-1264,48;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;51;-2128,912;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;47;-2256,544;Inherit;False;F_FracMask;-1;;17;cebeb8626740bc9488921f98d538d211;0;6;6;FLOAT2;0,0;False;7;FLOAT;100;False;9;FLOAT;1;False;8;FLOAT;0;False;12;FLOAT;1;False;15;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;6;-1136,-224;Inherit;False;SecondaryColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;109;-1120,48;Inherit;False;BorderMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;38;-1888,512;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;119;256,336;Inherit;False;109;BorderMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;116;256,256;Inherit;False;5;Color;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;120;224,176;Inherit;False;6;SecondaryColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;111;-1664,512;Inherit;False;BackgroundMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;118;480,208;Inherit;False;3;0;COLOR;1,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;114;624,400;Inherit;False;111;BackgroundMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;113;656,480;Inherit;False;109;BorderMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;117;624,208;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.LerpOp;112;864,400;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;20;1040,208;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ColorNode;125;976,16;Inherit;False;Property;_BackgroundColor;BackgroundColor;12;0;Create;True;0;0;0;False;0;False;0,0,0,0;0.2452829,0.2452829,0.2452829,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp;132;1232,208;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;136;1392,464;Inherit;False;Property;_AlphaOffset;AlphaOffset;13;0;Create;True;0;0;0;False;0;False;0;0.9;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;134;1392,208;Inherit;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleAddOpNode;137;1600,352;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;138;1824,352;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;45;-2896,912;Inherit;False;42;Coords;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.BreakToComponentsNode;61;-1296,-384;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RegisterLocalVarNode;62;-1184,-384;Inherit;False;CoordsU;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;63;-1184,-304;Inherit;False;CoordsV;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalVertexDataNode;22;-550.9844,-205.2008;Inherit;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PosVertexDataNode;21;-608,0;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;107;-1648,672;Inherit;False;6;SecondaryColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;106;-1408,512;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;110;-1248,512;Inherit;False;Background;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DynamicAppendNode;131;1824,208;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;115;1440,352;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;104;-864,48;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;105;-1088,176;Inherit;False;5;Color;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;17;2304,304;Float;False;True;-1;3;AmplifyShaderEditor.MaterialInspector;0;3;S_PopUp;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;False;True;3;1;False;;10;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;True;_ColorMask;False;False;False;False;False;False;False;True;True;0;True;_Stencil;255;True;_StencilReadMask;255;True;_StencilWriteMask;0;True;_StencilComp;0;True;_StencilOp;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;0;True;unity_GUIZTestMode;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;42;0;41;0
WireConnection;54;0;50;0
WireConnection;54;1;55;0
WireConnection;49;1;139;0
WireConnection;49;5;54;0
WireConnection;5;0;2;0
WireConnection;144;12;99;0
WireConnection;144;11;143;0
WireConnection;141;12;99;0
WireConnection;141;11;72;0
WireConnection;141;15;72;0
WireConnection;141;16;142;0
WireConnection;39;0;49;0
WireConnection;39;1;46;0
WireConnection;122;0;124;0
WireConnection;122;1;123;0
WireConnection;3;0;5;0
WireConnection;3;1;4;0
WireConnection;145;0;141;0
WireConnection;145;1;144;0
WireConnection;145;2;144;0
WireConnection;51;0;39;0
WireConnection;51;3;52;0
WireConnection;51;4;53;0
WireConnection;47;6;43;0
WireConnection;47;7;26;0
WireConnection;47;9;30;0
WireConnection;47;8;122;0
WireConnection;47;15;37;0
WireConnection;6;0;3;0
WireConnection;109;0;145;0
WireConnection;38;1;47;0
WireConnection;38;2;51;0
WireConnection;111;0;38;0
WireConnection;118;0;120;0
WireConnection;118;1;116;0
WireConnection;118;2;119;0
WireConnection;117;0;118;0
WireConnection;112;0;114;0
WireConnection;112;1;117;3
WireConnection;112;2;113;0
WireConnection;20;0;117;0
WireConnection;20;1;117;1
WireConnection;20;2;117;2
WireConnection;20;3;112;0
WireConnection;132;0;125;0
WireConnection;132;1;20;0
WireConnection;132;2;112;0
WireConnection;134;0;132;0
WireConnection;137;0;134;3
WireConnection;137;1;136;0
WireConnection;138;0;137;0
WireConnection;61;0;42;0
WireConnection;62;0;61;0
WireConnection;63;0;61;1
WireConnection;106;0;111;0
WireConnection;106;1;107;0
WireConnection;110;0;106;0
WireConnection;131;0;134;0
WireConnection;131;1;134;1
WireConnection;131;2;134;2
WireConnection;131;3;138;0
WireConnection;115;0;117;3
WireConnection;115;1;134;3
WireConnection;104;0;109;0
WireConnection;104;1;105;0
WireConnection;17;0;131;0
ASEEND*/
//CHKSM=1EE52FF5CF5EE41689C1E9EA219CD8F55A8743C4