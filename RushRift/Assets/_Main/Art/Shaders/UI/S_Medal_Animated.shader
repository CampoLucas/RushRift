// Made with Amplify Shader Editor v1.9.8.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "S_Medal_Animated"
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

        _MainTex("MainTex", 2D) = "white" {}
        _LightColor("LightColor", Color) = (1,1,1,1)
        _ColorMaskPower("ColorMaskPower", Float) = 1.1
        _PinMaskStep("PinMaskStep", Range( -1 , 1)) = 0.03
        _PinMaskPower("PinMaskPower", Range( -1 , 1)) = 0.63
        _Min("Min", Float) = 0.08
        _Max("Max", Float) = 1.31
        _AnimSpeed("AnimSpeed", Float) = 8
        _Animate("Animate", Float) = 0
        _OutlineColor("OutlineColor", Color) = (0,0,0,0)
        _OutilineStrength("Outiline Strength", Range( 0 , 1)) = 1
        _OutlineWidth("OutlineWidth", Float) = 0.56
        _OutlineScale("OutlineScale", Vector) = (0.0025,0.05,0,0)
        _LinesColor("LinesColor", Color) = (0,0,0,0)
        _LinesStrength("Lines Strength", Range( 0 , 1)) = 1
        _LinesFrequency("LinesFrequency", Float) = 20
        _LinesAmplitude("LinesAmplitude", Float) = 0.17
        _LinesSpeed("LinesSpeed", Float) = 30
        _LinesDir("LinesDir", Range( -1 , 1)) = 1
        _LinesStep("LinesStep", Range( 0 , 1)) = 1
        _UseLines("UseLines", Float) = 1
        _OutlineUseVertexColor("OutlineUseVertexColor", Int) = 1
        _LinesUseVertexColor("LinesUseVertexColor", Int) = 1

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
                
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            uniform float _AnimSpeed;
            uniform float _Animate;
            uniform float _OutlineWidth;
            uniform float2 _OutlineScale;
            uniform float4 _OutlineColor;
            uniform int _OutlineUseVertexColor;
            uniform float _OutilineStrength;
            uniform float _ColorMaskPower;
            uniform float4 _LightColor;
            uniform float _Min;
            uniform float _Max;
            uniform float _PinMaskPower;
            uniform float _PinMaskStep;
            uniform float4 _LinesColor;
            uniform int _LinesUseVertexColor;
            uniform float _LinesStrength;
            uniform float _LinesDir;
            uniform float _LinesFrequency;
            uniform float _LinesSpeed;
            uniform float _LinesAmplitude;
            uniform float _LinesStep;
            uniform float _UseLines;


            v2f vert(appdata_t v )
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                

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

                float2 texCoord46 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float lerpResult50 = lerp( 0.0 , _AnimSpeed , _Animate);
                // *** BEGIN Flipbook UV Animation vars ***
                // Total tiles of Flipbook Texture
                float fbtotaltiles45 = 14.0 * 1;
                // Offsets for cols and rows of Flipbook Texture
                float fbcolsoffset45 = 1.0f / 14.0;
                float fbrowsoffset45 = 1.0f / 1;
                // Speed of animation
                float fbspeed45 = _Time.y * lerpResult50;
                // UV Tiling (col and row offset)
                float2 fbtiling45 = float2(fbcolsoffset45, fbrowsoffset45);
                // UV Offset - calculate current tile linear index, and convert it to (X * coloffset, Y * rowoffset)
                // Calculate current tile linear index
                float fbcurrenttileindex45 = floor( fmod( fbspeed45 + 0.0, fbtotaltiles45) );
                fbcurrenttileindex45 += ( fbcurrenttileindex45 < 0) ? fbtotaltiles45 : 0;
                // Obtain Offset X coordinate from current tile linear index
                float fblinearindextox45 = round ( fmod ( fbcurrenttileindex45, 14.0 ) );
                // Multiply Offset X by coloffset
                float fboffsetx45 = fblinearindextox45 * fbcolsoffset45;
                // Obtain Offset Y coordinate from current tile linear index
                float fblinearindextoy45 = round( fmod( ( fbcurrenttileindex45 - fblinearindextox45 ) / 14.0, 1 ) );
                // Reverse Y to get tiles from Top to Bottom
                fblinearindextoy45 = (int)(1-1) - fblinearindextoy45;
                // Multiply Offset Y by rowoffset
                float fboffsety45 = fblinearindextoy45 * fbrowsoffset45;
                // UV Offset
                float2 fboffset45 = float2(fboffsetx45, fboffsety45);
                // Flipbook UV
                half2 fbuv45 = texCoord46 * fbtiling45 + fboffset45;
                // *** END Flipbook UV Animation vars ***
                int flipbookFrame45 = ( ( int )fbcurrenttileindex45);
                float2 UVs55 = fbuv45;
                float2 UVs41_g12 = UVs55;
                float OutlineWidth100 = _OutlineWidth;
                float BorderWidth44_g12 = OutlineWidth100;
                float2 BorderOffset46_g12 = ( BorderWidth44_g12 * _OutlineScale );
                float2 appendResult64_g12 = (float2(BorderOffset46_g12.x , 0.0));
                float2 temp_output_4_0_g16 = ( ( UVs41_g12 * float2( 1,1 ) ) + appendResult64_g12 );
                float2 appendResult62_g12 = (float2(( BorderOffset46_g12.x * -1.0 ) , 0.0));
                float2 temp_output_4_0_g15 = ( ( UVs41_g12 * float2( 1,1 ) ) + appendResult62_g12 );
                float2 appendResult61_g12 = (float2(0.0 , BorderOffset46_g12.y));
                float2 temp_output_4_0_g14 = ( ( UVs41_g12 * float2( 1,1 ) ) + appendResult61_g12 );
                float2 appendResult59_g12 = (float2(0.0 , ( BorderOffset46_g12.y * -1.0 )));
                float2 temp_output_4_0_g13 = ( ( UVs41_g12 * float2( 1,1 ) ) + appendResult59_g12 );
                float temp_output_75_0_g12 = saturate( ( tex2D( _MainTex, temp_output_4_0_g16 ).a + tex2D( _MainTex, temp_output_4_0_g15 ).a + tex2D( _MainTex, temp_output_4_0_g14 ).a + tex2D( _MainTex, temp_output_4_0_g13 ).a ) );
                float OutlineMask80 = temp_output_75_0_g12;
                float4 VertexColor82 = IN.color;
                float4 lerpResult144 = lerp( _OutlineColor , VertexColor82 , (float)_OutlineUseVertexColor);
                float4 temp_output_2_0_g20 = ( lerpResult144 * _OutilineStrength );
                float4 appendResult120 = (float4(( OutlineMask80 * (temp_output_2_0_g20).rgb ) , ( OutlineMask80 * (temp_output_2_0_g20).a )));
                float4 tex2DNode3 = tex2D( _MainTex, UVs55 );
                float TexMask123 = tex2DNode3.b;
                float temp_output_20_0 = pow( TexMask123 , _ColorMaskPower );
                float smoothstepResult28 = smoothstep( _Min , _Max , temp_output_20_0);
                float4 lerpResult30 = lerp( VertexColor82 , _LightColor , smoothstepResult28);
                float4 Main10 = saturate( ( temp_output_20_0 * lerpResult30 ) );
                float4 Image14 = tex2DNode3;
                float PinMask9 = step( pow( TexMask123 , _PinMaskPower ) , _PinMaskStep );
                float4 lerpResult13 = lerp( Main10 , ( Image14 * VertexColor82 ) , PinMask9);
                float Alpha18 = tex2DNode3.a;
                float4 lerpResult4 = lerp( float4( 0,0,0,0 ) , lerpResult13 , Alpha18);
                float4 break24 = lerpResult4;
                float4 appendResult25 = (float4(break24.r , break24.g , break24.b , Alpha18));
                float4 lerpResult139 = lerp( _LinesColor , VertexColor82 , (float)_LinesUseVertexColor);
                float4 temp_output_2_0_g19 = lerpResult139;
                float4 appendResult167 = (float4(( (temp_output_2_0_g19).rgb * _LinesStrength ) , Alpha18));
                float2 texCoord20_g18 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float2 break18_g18 = texCoord20_g18;
                float lerpResult17_g18 = lerp( break18_g18.x , break18_g18.y , _LinesDir);
                float mulTime110 = _Time.y * _LinesSpeed;
                float temp_output_4_0_g18 = sin( ( ( lerpResult17_g18 * _LinesFrequency ) + mulTime110 ) );
                float temp_output_3_0_g18 = ( (0.0 + (temp_output_4_0_g18 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0)) * _LinesAmplitude );
                float temp_output_15_0_g18 = _LinesStep;
                float lerpResult13_g18 = lerp( temp_output_3_0_g18 , step( temp_output_3_0_g18 , temp_output_15_0_g18 ) , ceil( temp_output_15_0_g18 ));
                float lerpResult162 = lerp( 0.0 , saturate( lerpResult13_g18 ) , _UseLines);
                float4 lerpResult108 = lerp( appendResult25 , appendResult167 , lerpResult162);
                float lerpResult99 = lerp( 1.0 , Alpha18 , ceil( abs( OutlineWidth100 ) ));
                float4 lerpResult85 = lerp( appendResult120 , lerpResult108 , lerpResult99);
                

                half4 color = lerpResult85;

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
Node;AmplifyShaderEditor.RangedFloatNode;48;-2560,-976;Inherit;False;Property;_AnimSpeed;AnimSpeed;6;0;Create;True;0;0;0;False;0;False;8;8;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;51;-2528,-896;Inherit;False;Property;_Animate;Animate;7;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;47;-2368,-864;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;50;-2336,-992;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;46;-2400,-1120;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCFlipBookUVAnimation;45;-2064,-1120;Inherit;False;0;0;7;0;FLOAT2;0,0;False;1;FLOAT;14;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;-1;False;4;FLOAT2;0;FLOAT;1;FLOAT;2;INT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode;55;-1776,-1120;Inherit;False;UVs;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;135;-2480,-496;Inherit;True;Property;_MainTex;MainTex;17;0;Fetch;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.GetLocalVarNode;56;-2448,-304;Inherit;False;55;UVs;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;3;-2208,-496;Inherit;True;Property;_MainTex;_MainTex;6;0;Fetch;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.CommentaryNode;126;-1296,-544;Inherit;False;1620;603;Make Color;12;20;27;29;28;30;22;23;10;21;125;83;31;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;123;-1872,-416;Inherit;False;TexMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;21;-1248,-416;Inherit;False;Property;_ColorMaskPower;ColorMaskPower;1;0;Create;True;0;0;0;False;0;False;1.1;1.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;125;-1216,-496;Inherit;False;123;TexMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;81;-2032,-736;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PowerNode;20;-992,-496;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;27;-992,-352;Inherit;False;Property;_Min;Min;4;0;Create;True;0;0;0;False;0;False;0.08;0.08;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;29;-992,-256;Inherit;False;Property;_Max;Max;5;0;Create;True;0;0;0;False;0;False;1.31;1.31;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;82;-1824,-736;Inherit;False;VertexColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;127;-656,-848;Inherit;False;980;275;Pins Mask;6;8;7;6;5;9;124;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SmoothstepOpNode;28;-768,-384;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;83;-768,-256;Inherit;False;82;VertexColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;31;-800,-176;Inherit;False;Property;_LightColor;LightColor;0;0;Create;True;0;0;0;False;0;False;1,1,1,1;1,1,1,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp;30;-480,-384;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;1,1,1,1;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;8;-544,-688;Inherit;False;Property;_PinMaskPower;PinMaskPower;3;0;Create;True;0;0;0;False;0;False;0.63;0.63;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;124;-608,-800;Inherit;False;123;TexMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;22;-304,-496;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.PowerNode;7;-416,-800;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;6;-240,-688;Inherit;False;Property;_PinMaskStep;PinMaskStep;2;0;Create;True;0;0;0;False;0;False;0.03;0.03;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;128;-2560,-1488;Inherit;False;1012;291;Outline Offset;2;97;100;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;122;-976,128;Inherit;False;1300;339;Apply color;11;13;19;4;24;116;25;15;84;11;16;32;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SaturateNode;23;-112,-496;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StepOpNode;5;-80,-800;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;97;-2512,-1424;Inherit;False;Property;_OutlineWidth;OutlineWidth;10;0;Create;True;0;0;0;False;0;False;0.56;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;14;-1872,-496;Inherit;False;Image;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;15;-912,192;Inherit;False;14;Image;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;84;-912,320;Inherit;False;82;VertexColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;10;80,-496;Inherit;False;Main;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;9;80,-800;Inherit;False;PinMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;136;-658,-1506;Inherit;False;852;579;Border Mask;6;132;133;134;130;131;80;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;100;-2256,-1424;Inherit;False;OutlineWidth;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;121;-745,494;Inherit;False;1261.947;922.1182;Apply Lines;19;109;158;157;112;113;110;111;155;139;141;138;115;159;161;162;165;166;168;167;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;119;-1168.24,1472;Inherit;False;1681.834;682.7343;Apply Outline;16;120;99;117;147;118;104;146;86;102;101;144;143;114;142;163;164;;1,1,1,1;0;0
Node;AmplifyShaderEditor.GetLocalVarNode;11;-688,368;Inherit;False;9;PinMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;16;-688,192;Inherit;False;10;Main;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;32;-656,272;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;133;-576,-1168;Inherit;False;100;OutlineWidth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;134;-576,-1248;Inherit;False;55;UVs;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;130;-608,-1456;Inherit;True;Property;_MainTex;MainTex;0;0;Fetch;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.Vector2Node;132;-576,-1088;Inherit;False;Property;_OutlineScale;OutlineScale;11;0;Create;True;0;0;0;False;0;False;0.0025,0.05;0.0025,0.05;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RegisterLocalVarNode;18;-1872,-336;Inherit;False;Alpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;111;-720,1152;Inherit;False;Property;_LinesSpeed;LinesSpeed;16;0;Create;True;0;0;0;False;0;False;30;30;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;142;-832,1744;Inherit;False;82;VertexColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;114;-864,1536;Inherit;False;Property;_OutlineColor;OutlineColor;8;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0.6366434,1,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.IntNode;143;-896,1824;Inherit;False;Property;_OutlineUseVertexColor;OutlineUseVertexColor;21;0;Create;True;0;0;0;False;0;False;1;1;False;0;1;INT;0
Node;AmplifyShaderEditor.ColorNode;115;-640,560;Inherit;False;Property;_LinesColor;LinesColor;12;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0.6310754,1,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.GetLocalVarNode;138;-608,752;Inherit;False;82;VertexColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.IntNode;141;-672,832;Inherit;False;Property;_LinesUseVertexColor;LinesUseVertexColor;22;0;Create;True;0;0;0;False;0;False;1;1;False;0;1;INT;0
Node;AmplifyShaderEditor.LerpOp;13;-400,192;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;19;-432,336;Inherit;False;18;Alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;131;-288,-1248;Inherit;False;F_OutlineMask;-1;;12;5f27c662ee058f34383daf2a1ac23a72;0;4;2;SAMPLER2D;0;False;79;FLOAT2;0,0;False;77;FLOAT;0.2;False;78;FLOAT2;0.0025,0.05;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;110;-528,1152;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;113;-528,992;Inherit;False;Property;_LinesFrequency;LinesFrequency;14;0;Create;True;0;0;0;False;0;False;20;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;112;-528,1072;Inherit;False;Property;_LinesAmplitude;LinesAmplitude;15;0;Create;True;0;0;0;False;0;False;0.17;0.3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;157;-624,1232;Inherit;False;Property;_LinesDir;LinesDir;18;0;Create;True;0;0;0;False;0;False;1;1;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;158;-624,1312;Inherit;False;Property;_LinesStep;LinesStep;19;0;Create;True;0;0;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;144;-576,1536;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;164;-560,1680;Inherit;False;Property;_OutilineStrength;Outiline Strength;9;0;Create;True;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;139;-352,560;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;4;-208,192;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;1,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;80;-48,-1248;Inherit;False;OutlineMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;101;-240,1888;Inherit;False;100;OutlineWidth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;109;-320,992;Inherit;False;F_SineMask;-1;;18;1defe6340f89a2142b7c5511d772d51f;0;6;6;FLOAT2;0,0;False;7;FLOAT;100;False;9;FLOAT;1;False;8;FLOAT;0;False;12;FLOAT;1;False;15;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;163;-368,1536;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;166;-400,896;Inherit;False;Property;_LinesStrength;Lines Strength;13;0;Create;True;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;168;-176,560;Inherit;False;Alpha Split;-1;;19;07dab7960105b86429ac8eebd729ed6d;0;1;2;COLOR;0,0,0,0;False;2;FLOAT3;0;FLOAT;6
Node;AmplifyShaderEditor.BreakToComponentsNode;24;-16,192;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.GetLocalVarNode;116;-80,336;Inherit;False;18;Alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;102;0,1888;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;86;-160,1536;Inherit;False;80;OutlineMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;146;-208,1648;Inherit;False;Alpha Split;-1;;20;07dab7960105b86429ac8eebd729ed6d;0;1;2;COLOR;0,0,0,0;False;2;FLOAT3;0;FLOAT;6
Node;AmplifyShaderEditor.SaturateNode;155;-16,992;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;161;-16,1104;Inherit;False;Property;_UseLines;UseLines;20;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;165;16,560;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;159;16,688;Inherit;False;18;Alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;25;160,192;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.CeilOpNode;104;176,1888;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;118;64,1536;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;147;64,1648;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;117;112,1808;Inherit;False;18;Alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;162;192,976;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;167;192,560;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp;108;480,352;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;1,1,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp;99;336,1808;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;120;352,1536;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp;85;768,544;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;960,544;Float;False;True;-1;3;AmplifyShaderEditor.MaterialInspector;0;3;S_Medal_Animated;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;False;True;3;1;False;;10;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;True;_ColorMask;False;False;False;False;False;False;False;True;True;0;True;_Stencil;255;True;_StencilReadMask;255;True;_StencilWriteMask;0;True;_StencilComp;0;True;_StencilOp;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;0;True;unity_GUIZTestMode;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;50;1;48;0
WireConnection;50;2;51;0
WireConnection;45;0;46;0
WireConnection;45;3;50;0
WireConnection;45;5;47;0
WireConnection;55;0;45;0
WireConnection;3;0;135;0
WireConnection;3;1;56;0
WireConnection;123;0;3;3
WireConnection;20;0;125;0
WireConnection;20;1;21;0
WireConnection;82;0;81;0
WireConnection;28;0;20;0
WireConnection;28;1;27;0
WireConnection;28;2;29;0
WireConnection;30;0;83;0
WireConnection;30;1;31;0
WireConnection;30;2;28;0
WireConnection;22;0;20;0
WireConnection;22;1;30;0
WireConnection;7;0;124;0
WireConnection;7;1;8;0
WireConnection;23;0;22;0
WireConnection;5;0;7;0
WireConnection;5;1;6;0
WireConnection;14;0;3;0
WireConnection;10;0;23;0
WireConnection;9;0;5;0
WireConnection;100;0;97;0
WireConnection;32;0;15;0
WireConnection;32;1;84;0
WireConnection;18;0;3;4
WireConnection;13;0;16;0
WireConnection;13;1;32;0
WireConnection;13;2;11;0
WireConnection;131;2;130;0
WireConnection;131;79;134;0
WireConnection;131;77;133;0
WireConnection;131;78;132;0
WireConnection;110;0;111;0
WireConnection;144;0;114;0
WireConnection;144;1;142;0
WireConnection;144;2;143;0
WireConnection;139;0;115;0
WireConnection;139;1;138;0
WireConnection;139;2;141;0
WireConnection;4;1;13;0
WireConnection;4;2;19;0
WireConnection;80;0;131;0
WireConnection;109;7;113;0
WireConnection;109;9;112;0
WireConnection;109;8;110;0
WireConnection;109;12;157;0
WireConnection;109;15;158;0
WireConnection;163;0;144;0
WireConnection;163;1;164;0
WireConnection;168;2;139;0
WireConnection;24;0;4;0
WireConnection;102;0;101;0
WireConnection;146;2;163;0
WireConnection;155;0;109;0
WireConnection;165;0;168;0
WireConnection;165;1;166;0
WireConnection;25;0;24;0
WireConnection;25;1;24;1
WireConnection;25;2;24;2
WireConnection;25;3;116;0
WireConnection;104;0;102;0
WireConnection;118;0;86;0
WireConnection;118;1;146;0
WireConnection;147;0;86;0
WireConnection;147;1;146;6
WireConnection;162;1;155;0
WireConnection;162;2;161;0
WireConnection;167;0;165;0
WireConnection;167;3;159;0
WireConnection;108;0;25;0
WireConnection;108;1;167;0
WireConnection;108;2;162;0
WireConnection;99;1;117;0
WireConnection;99;2;104;0
WireConnection;120;0;118;0
WireConnection;120;3;147;0
WireConnection;85;0;120;0
WireConnection;85;1;108;0
WireConnection;85;2;99;0
WireConnection;0;0;85;0
ASEEND*/
//CHKSM=C99F7A66765BAEF242716EFD62303053088A28B2