// Made with Amplify Shader Editor v1.9.9.8
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "S_UpgradeIcons"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        _AtlasSize( "Atlas Size", Vector ) = ( 3, 1, 0, 0 )
        _Index( "Index", Int ) = 0
        _BorderColor1( "BorderColor1", Color ) = ( 1, 0, 0, 1 )
        _BorderColor2( "BorderColor2", Color ) = ( 1, 0, 0, 1 )
        _RChannel( "RChannel", Color ) = ( 1, 1, 1, 1 )
        _GChannel( "GChannel", Color ) = ( 1, 1, 1, 1 )
        _BChannel( "BChannel", Color ) = ( 1, 1, 1, 1 )
        _Float0( "Float 0", Float ) = 0
        _Float1( "Float 0", Float ) = 0
        _Float2( "Float 2", Float ) = 0
        _Float3( "Float 2", Float ) = 0
        _Speed( "Speed", Float ) = 0

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
            #define ASE_VERSION 19908

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityShaderVariables.cginc"
            #define ASE_NEEDS_TEXTURE_COORDINATES0
            #define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0


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

            uniform float4 _RChannel;
            uniform float2 _AtlasSize;
            uniform int _Index;
            uniform float4 _GChannel;
            uniform float4 _BChannel;
            uniform float _Float0;
            uniform float _Speed;
            uniform float _Float1;
            uniform float _Float2;
            uniform float _Float3;
            uniform float4 _BorderColor2;
            uniform float4 _BorderColor1;


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

                float temp_output_4_0_g1 = _AtlasSize.x;
                float temp_output_5_0_g1 = _AtlasSize.y;
                // *** BEGIN Flipbook UV Animation vars ***
                // Total tiles of Flipbook Texture
                float fbtotaltiles246_g1 = min( temp_output_4_0_g1 * temp_output_5_0_g1, ( ( temp_output_4_0_g1 * temp_output_5_0_g1 ) - 0.0 ) + 1 );
                // Offsets for cols and rows of Flipbook Texture
                float fbcolsoffset246_g1 = 1.0f / temp_output_4_0_g1;
                float fbrowsoffset246_g1 = 1.0f / temp_output_5_0_g1;
                // Speed of animation
                float fbspeed246_g1 = _Time.y * 0.0;
                // UV Tiling (col and row offset)
                float2 fbtiling246_g1 = float2(fbcolsoffset246_g1, fbrowsoffset246_g1);
                // UV Offset - calculate current tile linear index, and convert it to (X * coloffset, Y * rowoffset)
                // Calculate current tile linear index
                float fbcurrenttileindex246_g1 = floor( fmod( fbspeed246_g1 + (float)_Index, fbtotaltiles246_g1) );
                fbcurrenttileindex246_g1 += ( fbcurrenttileindex246_g1 < 0) ? fbtotaltiles246_g1 : 0;
                // Obtain Offset X coordinate from current tile linear index
                float fblinearindextox246_g1 = round ( fmod ( fbcurrenttileindex246_g1, temp_output_4_0_g1 ) );
                // Multiply Offset X by coloffset
                float fboffsetx246_g1 = fblinearindextox246_g1 * fbcolsoffset246_g1;
                // Obtain Offset Y coordinate from current tile linear index
                float fblinearindextoy246_g1 = round( fmod( ( fbcurrenttileindex246_g1 - fblinearindextox246_g1 ) / temp_output_4_0_g1, temp_output_5_0_g1 ) );
                // Reverse Y to get tiles from Top to Bottom
                fblinearindextoy246_g1 = (int)(temp_output_5_0_g1-1) - fblinearindextoy246_g1;
                // Multiply Offset Y by rowoffset
                float fboffsety246_g1 = fblinearindextoy246_g1 * fbrowsoffset246_g1;
                // UV Offset
                float2 fboffset246_g1 = float2(fboffsetx246_g1, fboffsety246_g1);
                // Flipbook UV
                float2 fbuv246_g1 = IN.texcoord.xy * fbtiling246_g1 + fboffset246_g1;
                // *** END Flipbook UV Animation vars ***
                int flipbookFrame246_g1 = ( ( int )fbcurrenttileindex246_g1);
                float4 temp_output_2_53 = tex2D( _MainTex, fbuv246_g1 );
                float4 temp_output_2_0_g2 = temp_output_2_53;
                float3 texRGB58 = (temp_output_2_0_g2).rgb;
                float4 break3 = temp_output_2_53;
                float texB57 = break3.b;
                float3 temp_cast_1 = (texB57).xxx;
                float texG56 = break3.g;
                float3 temp_cast_2 = (texG56).xxx;
                float maskR72 = saturate( ( ( texRGB58 - temp_cast_1 ) - temp_cast_2 ) ).x;
                float4 lerpResult84 = lerp( float4( 0,0,0,0 ) , _RChannel , maskR72);
                float3 temp_cast_3 = (texB57).xxx;
                float texR55 = break3.r;
                float3 temp_cast_4 = (texR55).xxx;
                float maskG73 = saturate( ( ( texRGB58 - temp_cast_3 ) - temp_cast_4 ) ).y;
                float4 lerpResult87 = lerp( lerpResult84 , _GChannel , maskG73);
                float3 temp_cast_5 = (texG56).xxx;
                float3 temp_cast_6 = (texR55).xxx;
                float maskB74 = saturate( ( ( texRGB58 - temp_cast_5 ) - temp_cast_6 ) ).z;
                float4 lerpResult89 = lerp( lerpResult87 , _BChannel , maskB74);
                float2 texCoord20_g4 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float2 break18_g4 = texCoord20_g4;
                float lerpResult17_g4 = lerp( break18_g4.x , break18_g4.y , 0.5);
                float mulTime108 = _Time.y * _Speed;
                float temp_output_4_0_g4 = sin( ( ( lerpResult17_g4 * _Float0 ) + mulTime108 ) );
                float temp_output_3_0_g4 = (  (0.0 + ( temp_output_4_0_g4 - -1.0 ) * ( 1.0 - 0.0 ) / ( 1.0 - -1.0 ) ) * _Float1 );
                float temp_output_15_0_g4 = 0.0;
                float lerpResult13_g4 = lerp( temp_output_3_0_g4 , step( temp_output_3_0_g4 , temp_output_15_0_g4 ) , ceil( temp_output_15_0_g4 ));
                float temp_output_100_0 = lerpResult13_g4;
                float4 lerpResult113 = lerp( ( lerpResult89 * 1.1 ) , lerpResult89 , step( temp_output_100_0 , ( _Float2 * _Float3 ) ));
                float4 lerpResult105 = lerp( _BorderColor2 , _BorderColor1 , step( temp_output_100_0 , _Float2 ));
                float maskW76 = saturate( ( texR55 - maskR72 ) );
                float4 lerpResult81 = lerp( float4( 0,0,0,0 ) , lerpResult105 , maskW76);
                float4 lerpResult80 = lerp( lerpResult113 , lerpResult81 , maskW76);
                float4 out26 = saturate( lerpResult80 );
                

                half4 color = out26;

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
Version=19908
Node;AmplifyShaderEditor.TemplateShaderPropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;1;-3456,-752;Inherit;False;0;0;_MainTex;Shader;False;0;5;SAMPLER2D;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;4;-3392,-592;Inherit;False;Property;_AtlasSize;Atlas Size;0;0;Create;True;0;0;0;False;0;False;3,1;3,2;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.IntNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;25;-3392,-672;Inherit;False;Property;_Index;Index;5;0;Create;True;0;0;0;False;0;False;0;0;False;0;0;0;1;INT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;2;-3168,-752;Inherit;False;Flipbook;-1;;1;53c2488c220f6564ca6c90721ee16673;3,68,0,217,0,244,0;11;51;SAMPLER2D;0.0;False;167;SAMPLERSTATE;0;False;13;FLOAT2;0,0;False;24;FLOAT;0;False;210;FLOAT;4;False;4;FLOAT;4;False;5;FLOAT;4;False;130;FLOAT;0;False;2;FLOAT;0;False;55;FLOAT;0;False;70;FLOAT;0;False;5;COLOR;53;FLOAT2;0;FLOAT;47;FLOAT;48;INT;218
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;59;-2800,-752;Inherit;False;Alpha Split;-1;;2;07dab7960105b86429ac8eebd729ed6d;0;1;2;COLOR;0,0,0,0;False;2;FLOAT3;0;FLOAT;6
Node;AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;3;-2736,-624;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;36;-2224,-768;Inherit;False;936;598;Separate the colors;18;65;64;63;61;35;9;62;60;32;31;34;21;30;33;7;66;67;68;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;57;-2544,-464;Inherit;False;texB;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;58;-2544,-752;Inherit;False;texRGB;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;56;-2544,-544;Inherit;False;texG;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;60;-2176,-720;Inherit;False;58;texRGB;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;63;-2176,-640;Inherit;False;57;texB;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;7;-1936,-704;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;64;-1968,-608;Inherit;False;56;texG;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;55;-2544,-624;Inherit;False;texR;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;61;-2176,-528;Inherit;False;58;texRGB;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;66;-2176,-448;Inherit;False;57;texB;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;33;-1712,-704;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;9;-1936,-528;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;65;-1968,-432;Inherit;False;55;texR;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;62;-2176,-352;Inherit;False;58;texRGB;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;68;-2176,-272;Inherit;False;56;texG;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;30;-1520,-704;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;34;-1712,-576;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;35;-1936,-352;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;67;-1968,-256;Inherit;False;55;texR;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;8;-1200,-704;Inherit;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;31;-1520,-576;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;21;-1712,-432;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;10;-1200,-576;Inherit;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;32;-1504,-432;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;72;-208,-672;Inherit;False;maskR;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;86;-208,-960;Inherit;False;Property;_RChannel;RChannel;8;0;Create;True;0;0;0;False;0;False;1,1,1,1;1,1,1,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;71;-768,-1200;Inherit;False;55;texR;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;75;-768,-1120;Inherit;False;72;maskR;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;20;-1200,-432;Inherit;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;73;-208,-528;Inherit;False;maskG;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;84;80,-960;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;1,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;88;64,-784;Inherit;False;Property;_GChannel;GChannel;9;0;Create;True;0;0;0;False;0;False;1,1,1,1;0.9937106,0.9937106,0.9937106,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;109;-944,-1760;Inherit;False;Property;_Speed;Speed;15;0;Create;True;0;0;0;False;0;False;0;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;101;-688,-1984;Inherit;False;Property;_Float0;Float 0;11;0;Create;True;0;0;0;False;0;False;0;6.44;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;102;-720,-1904;Inherit;False;Property;_Float1;Float 0;12;0;Create;True;0;0;0;False;0;False;0;0.51;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;54;-560,-1200;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;74;-208,-416;Inherit;False;maskB;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;87;336,-784;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;1,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;90;272,-576;Inherit;False;Property;_BChannel;BChannel;10;0;Create;True;0;0;0;False;0;False;1,1,1,1;1,1,1,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;104;-496,-1792;Inherit;False;Property;_Float2;Float 2;13;0;Create;True;0;0;0;False;0;False;0;0.45;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;111;-480,-1648;Inherit;False;Property;_Float3;Float 2;14;0;Create;True;0;0;0;False;0;False;0;1.09;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;108;-768,-1776;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;83;-384,-1200;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;107;-176,-1680;Inherit;False;Property;_BorderColor2;BorderColor2;7;0;Create;True;0;0;0;False;0;False;1,0,0,1;1,1,1,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;106;-192,-1488;Inherit;False;Property;_BorderColor1;BorderColor1;6;0;Create;True;0;0;0;False;0;False;1,0,0,1;0.6037736,0.6037736,0.6037736,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;89;608,-784;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;1,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;100;-464,-2016;Inherit;False;Sine Mask;-1;;4;1defe6340f89a2142b7c5511d772d51f;0;6;6;FLOAT2;0,0;False;7;FLOAT;93.15;False;9;FLOAT;1;False;8;FLOAT;0;False;12;FLOAT;0.5;False;15;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;103;-128,-1984;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;112;-320,-1760;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;115;229.4565,-1096.612;Inherit;False;Constant;_Float4;Float 4;16;0;Create;True;0;0;0;False;0;False;1.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;76;-224,-1200;Inherit;False;maskW;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;110;-128,-1840;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;105;272,-1552;Inherit;False;3;0;COLOR;0,0,0,1;False;1;COLOR;1,1,1,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;114;399.0565,-1121.145;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0.1;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;81;544,-1520;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;1,1,1,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;113;592,-1152;Inherit;False;3;0;COLOR;0,0,0,1;False;1;COLOR;1,1,1,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;80;816,-1456;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;116;997.1954,-1358.925;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;26;1216,-1456;Inherit;False;out;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;93;-720,-608;Inherit;False;Property;_SharpnessChanelR;SharpnessChanelR;2;0;Create;True;0;0;0;False;0;False;0;0.685;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;94;-560,-704;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;95;-720,-432;Inherit;False;Property;_SharpnessChanelG;SharpnessChanelG;3;0;Create;True;0;0;0;False;0;False;0;0.128;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;96;-560,-528;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;97;-720,-256;Inherit;False;Property;_SharpnessChanelB;SharpnessChanelB;4;0;Create;True;0;0;0;False;0;False;0;0.235;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;91;96,-1296;Inherit;False;Property;_SharpnessChanelW;SharpnessChanelW;1;0;Create;True;0;0;0;False;0;False;0;0.657;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;98;-560,-352;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;92;256,-1392;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;27;608,-320;Inherit;False;26;out;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;23;288,-112;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;77;-80,16;Inherit;False;72;maskR;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;78;-80,176;Inherit;False;74;maskB;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;79;-80,96;Inherit;False;73;maskG;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;69;144,48;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;70;288,48;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;0;1136,-400;Float;False;True;-1;3;AmplifyShaderEditor.MaterialInspector;0;12;S_UpgradeIcons;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;False;True;3;1;False;;10;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;True;_ColorMask;False;False;False;False;False;False;False;True;True;0;True;_Stencil;255;True;_StencilReadMask;255;True;_StencilWriteMask;0;True;_StencilComp;0;True;_StencilOp;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;0;True;unity_GUIZTestMode;False;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;2;51;1;0
WireConnection;2;24;25;0
WireConnection;2;4;4;1
WireConnection;2;5;4;2
WireConnection;59;2;2;53
WireConnection;3;0;2;53
WireConnection;57;0;3;2
WireConnection;58;0;59;0
WireConnection;56;0;3;1
WireConnection;7;0;60;0
WireConnection;7;1;63;0
WireConnection;55;0;3;0
WireConnection;33;0;7;0
WireConnection;33;1;64;0
WireConnection;9;0;61;0
WireConnection;9;1;66;0
WireConnection;30;0;33;0
WireConnection;34;0;9;0
WireConnection;34;1;65;0
WireConnection;35;0;62;0
WireConnection;35;1;68;0
WireConnection;8;0;30;0
WireConnection;31;0;34;0
WireConnection;21;0;35;0
WireConnection;21;1;67;0
WireConnection;10;0;31;0
WireConnection;32;0;21;0
WireConnection;72;0;8;0
WireConnection;20;0;32;0
WireConnection;73;0;10;1
WireConnection;84;1;86;0
WireConnection;84;2;72;0
WireConnection;54;0;71;0
WireConnection;54;1;75;0
WireConnection;74;0;20;2
WireConnection;87;0;84;0
WireConnection;87;1;88;0
WireConnection;87;2;73;0
WireConnection;108;0;109;0
WireConnection;83;0;54;0
WireConnection;89;0;87;0
WireConnection;89;1;90;0
WireConnection;89;2;74;0
WireConnection;100;7;101;0
WireConnection;100;9;102;0
WireConnection;100;8;108;0
WireConnection;103;0;100;0
WireConnection;103;1;104;0
WireConnection;112;0;104;0
WireConnection;112;1;111;0
WireConnection;76;0;83;0
WireConnection;110;0;100;0
WireConnection;110;1;112;0
WireConnection;105;0;107;0
WireConnection;105;1;106;0
WireConnection;105;2;103;0
WireConnection;114;0;89;0
WireConnection;114;1;115;0
WireConnection;81;1;105;0
WireConnection;81;2;76;0
WireConnection;113;0;114;0
WireConnection;113;1;89;0
WireConnection;113;2;110;0
WireConnection;80;0;113;0
WireConnection;80;1;81;0
WireConnection;80;2;76;0
WireConnection;116;0;80;0
WireConnection;26;0;116;0
WireConnection;94;0;93;0
WireConnection;94;1;8;0
WireConnection;96;0;95;0
WireConnection;96;1;10;1
WireConnection;98;0;97;0
WireConnection;98;1;20;2
WireConnection;92;0;91;0
WireConnection;92;1;76;0
WireConnection;23;0;72;0
WireConnection;23;1;73;0
WireConnection;23;2;74;0
WireConnection;23;3;70;0
WireConnection;69;0;77;0
WireConnection;69;1;79;0
WireConnection;69;2;78;0
WireConnection;70;0;69;0
WireConnection;0;0;27;0
ASEEND*/
//CHKSM=0AE64460C854BAE6D0F277F00ED760A54173BF7B