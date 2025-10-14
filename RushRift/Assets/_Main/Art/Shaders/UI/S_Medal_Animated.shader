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
        _TexAlphaScale("TexAlphaScale", Range( 0 , 1)) = 1

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
            uniform float _TexAlphaScale;
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
                float2 UVs41_g33 = UVs55;
                float OutlineWidth100 = _OutlineWidth;
                float BorderWidth44_g33 = OutlineWidth100;
                float2 BorderOffset46_g33 = ( BorderWidth44_g33 * _OutlineScale );
                float2 appendResult64_g33 = (float2(BorderOffset46_g33.x , 0.0));
                float2 temp_output_4_0_g37 = ( ( UVs41_g33 * float2( 1,1 ) ) + appendResult64_g33 );
                float2 appendResult62_g33 = (float2(( BorderOffset46_g33.x * -1.0 ) , 0.0));
                float2 temp_output_4_0_g36 = ( ( UVs41_g33 * float2( 1,1 ) ) + appendResult62_g33 );
                float2 appendResult61_g33 = (float2(0.0 , BorderOffset46_g33.y));
                float2 temp_output_4_0_g35 = ( ( UVs41_g33 * float2( 1,1 ) ) + appendResult61_g33 );
                float2 appendResult59_g33 = (float2(0.0 , ( BorderOffset46_g33.y * -1.0 )));
                float2 temp_output_4_0_g34 = ( ( UVs41_g33 * float2( 1,1 ) ) + appendResult59_g33 );
                float BorderMask47_g33 = saturate( ( tex2D( _MainTex, temp_output_4_0_g37 ).a + tex2D( _MainTex, temp_output_4_0_g36 ).a + tex2D( _MainTex, temp_output_4_0_g35 ).a + tex2D( _MainTex, temp_output_4_0_g34 ).a ) );
                float OutlineMask80 = ( BorderMask47_g33 - tex2D( _MainTex, UVs41_g33 ).a );
                float4 VertexColor82 = IN.color;
                float4 lerpResult144 = lerp( _OutlineColor , VertexColor82 , (float)_OutlineUseVertexColor);
                float4 temp_output_2_0_g40 = ( lerpResult144 * _OutilineStrength );
                float4 appendResult120 = (float4(( OutlineMask80 * (temp_output_2_0_g40).rgb ) , ( OutlineMask80 * (temp_output_2_0_g40).a )));
                float4 tex2DNode3 = tex2D( _MainTex, UVs55 );
                float4 temp_output_2_0_g31 = tex2DNode3;
                float temp_output_170_0 = ( (temp_output_2_0_g31).a * _TexAlphaScale );
                float4 appendResult4_g32 = (float4((temp_output_2_0_g31).rgb , temp_output_170_0));
                float4 Image14 = appendResult4_g32;
                float TexMask123 = tex2DNode3.b;
                float temp_output_20_0 = pow( TexMask123 , _ColorMaskPower );
                float smoothstepResult28 = smoothstep( _Min , _Max , temp_output_20_0);
                float4 lerpResult30 = lerp( VertexColor82 , _LightColor , saturate( smoothstepResult28 ));
                float ScaledAlpha189 = temp_output_170_0;
                float4 appendResult4_g42 = (float4(saturate( ( temp_output_20_0 * lerpResult30 ) ).rgb , ScaledAlpha189));
                float4 Main10 = appendResult4_g42;
                float Alpha18 = tex2DNode3.a;
                float PinMask9 = step( pow( TexMask123 , _PinMaskPower ) , _PinMaskStep );
                float4 lerpResult13 = lerp( ( Image14 * VertexColor82 ) , Main10 , ( Alpha18 - ( PinMask9 * Alpha18 ) ));
                float4 break24 = lerpResult13;
                float4 appendResult25 = (float4(break24.x , break24.y , break24.z , break24.w));
                float4 lerpResult139 = lerp( _LinesColor , VertexColor82 , (float)_LinesUseVertexColor);
                float4 temp_output_2_0_g39 = lerpResult139;
                float4 appendResult167 = (float4(( (temp_output_2_0_g39).rgb * _LinesStrength ) , Alpha18));
                float2 texCoord20_g38 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float2 break18_g38 = texCoord20_g38;
                float lerpResult17_g38 = lerp( break18_g38.x , break18_g38.y , _LinesDir);
                float mulTime110 = _Time.y * _LinesSpeed;
                float temp_output_4_0_g38 = sin( ( ( lerpResult17_g38 * _LinesFrequency ) + mulTime110 ) );
                float temp_output_3_0_g38 = ( (0.0 + (temp_output_4_0_g38 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0)) * _LinesAmplitude );
                float temp_output_15_0_g38 = _LinesStep;
                float lerpResult13_g38 = lerp( temp_output_3_0_g38 , step( temp_output_3_0_g38 , temp_output_15_0_g38 ) , ceil( temp_output_15_0_g38 ));
                float lerpResult162 = lerp( 0.0 , saturate( lerpResult13_g38 ) , _UseLines);
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
Node;AmplifyShaderEditor.TexturePropertyNode;135;-2768,-496;Inherit;True;Property;_MainTex;MainTex;17;0;Fetch;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.GetLocalVarNode;56;-2736,-304;Inherit;False;55;UVs;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;3;-2496,-496;Inherit;True;Property;_MainTex;_MainTex;6;0;Fetch;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RegisterLocalVarNode;123;-2128,-320;Inherit;False;TexMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;126;-1296,-544;Inherit;False;2054.2;597.8;Make Color;14;10;23;22;30;186;83;28;29;27;20;125;21;187;188;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;21;-1248,-416;Inherit;False;Property;_ColorMaskPower;ColorMaskPower;1;0;Create;True;0;0;0;False;0;False;1.1;1.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;125;-1216,-496;Inherit;False;123;TexMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;81;-2032,-736;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PowerNode;20;-992,-496;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;27;-1088,-352;Inherit;False;Property;_Min;Min;4;0;Create;True;0;0;0;False;0;False;0.08;0.08;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;29;-1088,-256;Inherit;False;Property;_Max;Max;5;0;Create;True;0;0;0;False;0;False;1.31;1.31;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;127;-656,-848;Inherit;False;980;275;Pins Mask;6;8;7;6;5;9;124;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;82;-1824,-736;Inherit;False;VertexColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SmoothstepOpNode;28;-848,-400;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;8;-544,-688;Inherit;False;Property;_PinMaskPower;PinMaskPower;3;0;Create;True;0;0;0;False;0;False;0.63;0.63;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;124;-608,-800;Inherit;False;123;TexMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;83;-688,-176;Inherit;False;82;VertexColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;31;-720,-96;Inherit;False;Property;_LightColor;LightColor;0;0;Create;True;0;0;0;False;0;False;1,1,1,1;1,1,1,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SaturateNode;186;-602.8366,-358.9953;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;174;-2096,-496;Inherit;False;Alpha Split;-1;;31;07dab7960105b86429ac8eebd729ed6d;0;1;2;COLOR;0,0,0,0;False;2;FLOAT3;0;FLOAT;6
Node;AmplifyShaderEditor.RangedFloatNode;169;-2192,-400;Inherit;False;Property;_TexAlphaScale;TexAlphaScale;23;0;Create;True;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;7;-416,-800;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;6;-240,-688;Inherit;False;Property;_PinMaskStep;PinMaskStep;2;0;Create;True;0;0;0;False;0;False;0.03;0.03;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;30;-400,-304;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;1,1,1,1;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;170;-1856,-400;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;5;-80,-800;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;128;-2560,-1488;Inherit;False;1012;291;Outline Offset;2;97;100;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;22;-304,-496;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;189;-1659.248,-357.4897;Inherit;False;ScaledAlpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;18;-2128,-240;Inherit;False;Alpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;122;-1187,128;Inherit;False;1700.448;659.4576;Apply color;15;11;176;25;116;24;4;13;178;32;16;19;15;84;180;181;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;9;80,-800;Inherit;False;PinMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;97;-2512,-1424;Inherit;False;Property;_OutlineWidth;OutlineWidth;10;0;Create;True;0;0;0;False;0;False;0.56;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;175;-1856,-496;Inherit;False;Alpha Merge;-1;;32;e0d79828992f19c4f90bfc29aa19b7a5;0;2;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SaturateNode;23;-112,-496;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;188;-144,-416;Inherit;False;189;ScaledAlpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;11;-1056,544;Inherit;False;9;PinMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;19;-1056,656;Inherit;False;18;Alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;14;-1664,-496;Inherit;False;Image;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.CommentaryNode;136;-658,-1506;Inherit;False;852;579;Border Mask;5;132;133;134;130;80;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;121;-704,944;Inherit;False;1261.947;922.1182;Apply Lines;19;109;158;157;112;113;110;111;155;139;141;138;115;159;161;162;165;166;168;167;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;119;-880,2048;Inherit;False;1681.834;682.7343;Apply Outline;16;120;99;117;147;118;104;146;86;102;101;144;143;114;142;163;164;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;100;-2256,-1424;Inherit;False;OutlineWidth;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;187;80,-496;Inherit;False;Alpha Merge;-1;;42;e0d79828992f19c4f90bfc29aa19b7a5;0;2;2;FLOAT3;0,0,0;False;3;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;181;-768,416;Inherit;False;18;Alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;180;-800,544;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;15;-912,240;Inherit;False;14;Image;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;84;-912,320;Inherit;False;82;VertexColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;134;-576,-1248;Inherit;False;55;UVs;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;130;-608,-1456;Inherit;True;Property;_MainTex;MainTex;0;0;Fetch;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.Vector2Node;132;-576,-1088;Inherit;False;Property;_OutlineScale;OutlineScale;11;0;Create;True;0;0;0;False;0;False;0.0025,0.05;0.0025,0.05;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;111;-688,1600;Inherit;False;Property;_LinesSpeed;LinesSpeed;16;0;Create;True;0;0;0;False;0;False;30;30;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;142;-544,2320;Inherit;False;82;VertexColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;114;-576,2112;Inherit;False;Property;_OutlineColor;OutlineColor;8;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0.6366434,1,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.IntNode;143;-608,2400;Inherit;False;Property;_OutlineUseVertexColor;OutlineUseVertexColor;21;0;Create;True;0;0;0;False;0;False;1;1;False;0;1;INT;0
Node;AmplifyShaderEditor.ColorNode;115;-608,1008;Inherit;False;Property;_LinesColor;LinesColor;12;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0.6310754,1,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.GetLocalVarNode;138;-576,1200;Inherit;False;82;VertexColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.IntNode;141;-640,1280;Inherit;False;Property;_LinesUseVertexColor;LinesUseVertexColor;22;0;Create;True;0;0;0;False;0;False;1;1;False;0;1;INT;0
Node;AmplifyShaderEditor.GetLocalVarNode;133;-608,-1168;Inherit;False;100;OutlineWidth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;10;288,-496;Inherit;False;Main;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;178;-544,416;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;32;-656,272;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleTimeNode;110;-496,1600;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;113;-496,1440;Inherit;False;Property;_LinesFrequency;LinesFrequency;14;0;Create;True;0;0;0;False;0;False;20;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;112;-496,1520;Inherit;False;Property;_LinesAmplitude;LinesAmplitude;15;0;Create;True;0;0;0;False;0;False;0.17;0.3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;157;-592,1680;Inherit;False;Property;_LinesDir;LinesDir;18;0;Create;True;0;0;0;False;0;False;1;1;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;158;-592,1760;Inherit;False;Property;_LinesStep;LinesStep;19;0;Create;True;0;0;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;144;-288,2112;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;164;-272,2256;Inherit;False;Property;_OutilineStrength;Outiline Strength;9;0;Create;True;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;139;-320,1008;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;172;-288,-1248;Inherit;False;F_OutlineMask;-1;;33;5f27c662ee058f34383daf2a1ac23a72;0;4;2;SAMPLER2D;0;False;79;FLOAT2;0,0;False;77;FLOAT;0.2;False;78;FLOAT2;0.0025,0.05;False;2;FLOAT;0;FLOAT;85
Node;AmplifyShaderEditor.GetLocalVarNode;16;-688,192;Inherit;False;10;Main;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;80;-48,-1248;Inherit;False;OutlineMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;101;48,2464;Inherit;False;100;OutlineWidth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;109;-288,1440;Inherit;False;F_SineMask;-1;;38;1defe6340f89a2142b7c5511d772d51f;0;6;6;FLOAT2;0,0;False;7;FLOAT;100;False;9;FLOAT;1;False;8;FLOAT;0;False;12;FLOAT;1;False;15;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;163;-80,2112;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;166;-368,1344;Inherit;False;Property;_LinesStrength;Lines Strength;13;0;Create;True;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;168;-144,1008;Inherit;False;Alpha Split;-1;;39;07dab7960105b86429ac8eebd729ed6d;0;1;2;COLOR;0,0,0,0;False;2;FLOAT3;0;FLOAT;6
Node;AmplifyShaderEditor.LerpOp;13;-352,224;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;1,1,1,1;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.BreakToComponentsNode;24;-16,192;Inherit;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.AbsOpNode;102;288,2464;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;86;128,2112;Inherit;False;80;OutlineMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;146;80,2224;Inherit;False;Alpha Split;-1;;40;07dab7960105b86429ac8eebd729ed6d;0;1;2;COLOR;0,0,0,0;False;2;FLOAT3;0;FLOAT;6
Node;AmplifyShaderEditor.SaturateNode;155;16,1440;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;161;16,1552;Inherit;False;Property;_UseLines;UseLines;20;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;165;48,1008;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;159;48,1136;Inherit;False;18;Alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.CeilOpNode;104;464,2464;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;118;352,2112;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;147;352,2224;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;117;400,2384;Inherit;False;18;Alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;162;224,1424;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;167;224,1008;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.DynamicAppendNode;25;352,192;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp;99;624,2384;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;120;640,2112;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp;108;640,384;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;1,1,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.WireNode;184;656,32;Inherit;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp;85;976,384;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp;176;176,304;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;177;668.7666,235.308;Inherit;False;Alpha Split;-1;;41;07dab7960105b86429ac8eebd729ed6d;0;1;2;FLOAT4;0,0,0,0;False;2;FLOAT3;0;FLOAT;6
Node;AmplifyShaderEditor.LerpOp;4;-160,48;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;1,0,0,0;False;2;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.BreakToComponentsNode;183;1191.045,605.3304;Inherit;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.BreakToComponentsNode;185;992.3867,-23.92883;Inherit;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.GetLocalVarNode;116;-80,352;Inherit;False;18;Alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;1360,384;Float;False;True;-1;3;AmplifyShaderEditor.MaterialInspector;0;3;S_Medal_Animated;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;False;True;3;1;False;;10;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;True;_ColorMask;False;False;False;False;False;False;False;True;True;0;True;_Stencil;255;True;_StencilReadMask;255;True;_StencilWriteMask;0;True;_StencilComp;0;True;_StencilOp;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;0;True;unity_GUIZTestMode;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;False;0;;0;0;Standard;0;0;1;True;False;;False;0
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
WireConnection;186;0;28;0
WireConnection;174;2;3;0
WireConnection;7;0;124;0
WireConnection;7;1;8;0
WireConnection;30;0;83;0
WireConnection;30;1;31;0
WireConnection;30;2;186;0
WireConnection;170;0;174;6
WireConnection;170;1;169;0
WireConnection;5;0;7;0
WireConnection;5;1;6;0
WireConnection;22;0;20;0
WireConnection;22;1;30;0
WireConnection;189;0;170;0
WireConnection;18;0;3;4
WireConnection;9;0;5;0
WireConnection;175;2;174;0
WireConnection;175;3;170;0
WireConnection;23;0;22;0
WireConnection;14;0;175;0
WireConnection;100;0;97;0
WireConnection;187;2;23;0
WireConnection;187;3;188;0
WireConnection;180;0;11;0
WireConnection;180;1;19;0
WireConnection;10;0;187;0
WireConnection;178;0;181;0
WireConnection;178;1;180;0
WireConnection;32;0;15;0
WireConnection;32;1;84;0
WireConnection;110;0;111;0
WireConnection;144;0;114;0
WireConnection;144;1;142;0
WireConnection;144;2;143;0
WireConnection;139;0;115;0
WireConnection;139;1;138;0
WireConnection;139;2;141;0
WireConnection;172;2;130;0
WireConnection;172;79;134;0
WireConnection;172;77;133;0
WireConnection;172;78;132;0
WireConnection;80;0;172;85
WireConnection;109;7;113;0
WireConnection;109;9;112;0
WireConnection;109;8;110;0
WireConnection;109;12;157;0
WireConnection;109;15;158;0
WireConnection;163;0;144;0
WireConnection;163;1;164;0
WireConnection;168;2;139;0
WireConnection;13;0;32;0
WireConnection;13;1;16;0
WireConnection;13;2;178;0
WireConnection;24;0;13;0
WireConnection;102;0;101;0
WireConnection;146;2;163;0
WireConnection;155;0;109;0
WireConnection;165;0;168;0
WireConnection;165;1;166;0
WireConnection;104;0;102;0
WireConnection;118;0;86;0
WireConnection;118;1;146;0
WireConnection;147;0;86;0
WireConnection;147;1;146;6
WireConnection;162;1;155;0
WireConnection;162;2;161;0
WireConnection;167;0;165;0
WireConnection;167;3;159;0
WireConnection;25;0;24;0
WireConnection;25;1;24;1
WireConnection;25;2;24;2
WireConnection;25;3;24;3
WireConnection;99;1;117;0
WireConnection;99;2;104;0
WireConnection;120;0;118;0
WireConnection;120;3;147;0
WireConnection;108;0;25;0
WireConnection;108;1;167;0
WireConnection;108;2;162;0
WireConnection;184;0;13;0
WireConnection;85;0;120;0
WireConnection;85;1;108;0
WireConnection;85;2;99;0
WireConnection;176;0;24;3
WireConnection;176;1;116;0
WireConnection;176;2;116;0
WireConnection;177;2;4;0
WireConnection;4;1;13;0
WireConnection;183;0;85;0
WireConnection;185;0;184;0
WireConnection;0;0;85;0
ASEEND*/
//CHKSM=37FCC0ACCDEEFF591AB30AD656AFDAC2A031D03F