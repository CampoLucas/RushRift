// Made with Amplify Shader Editor v1.9.8.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Game/UI/S_Hologram Border"
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

        _Scale("Scale", Vector) = (0,0,0,0)
        _Width("Width", Float) = 0
        _Color1("Color 1", Color) = (0,0,0,0)
        _MainTex("MainTex", 2D) = "white" {}
        _Speed("Speed", Float) = 0
        _BorderOffset("BorderOffset", Float) = 1.38
        _Float2("Float 2", Float) = 0
        _Intensity("Intensity", Float) = 1

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

            uniform float4 _Color1;
            uniform float _Intensity;
            uniform float _Float2;
            uniform float _Speed;
            uniform float _BorderOffset;
            uniform float _Width;
            uniform float2 _Scale;


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

                float4 temp_output_169_0 = ( IN.color * _Intensity );
                float2 texCoord6_g30 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float2 appendResult144 = (float2(1.0 , _Float2));
                float mulTime134 = _Time.y * _Speed;
                float2 appendResult135 = (float2(0.0 , mulTime134));
                float2 temp_output_4_0_g30 = ( ( texCoord6_g30 * appendResult144 ) + appendResult135 );
                float2 temp_output_131_0 = temp_output_4_0_g30;
                float sinLines_bg112 = tex2D( _MainTex, temp_output_131_0 ).r;
                float4 lerpResult123 = lerp( _Color1 , temp_output_169_0 , sinLines_bg112);
                float2 appendResult140 = (float2(0.0 , _BorderOffset));
                float2 temp_output_4_0_g31 = ( ( temp_output_131_0 * float2( 1,1 ) ) + appendResult140 );
                float sinLines57 = tex2D( _MainTex, temp_output_4_0_g31 ).r;
                float4 lerpResult126 = lerp( ( _Color1 * 0.5 ) , temp_output_169_0 , sinLines57);
                float2 texCoord13_g32 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float2 temp_output_6_0_g32 = ( 1.0 - ( saturate( abs( ( ( texCoord13_g32 * float2( 2,2 ) ) - float2( 1,1 ) ) ) ) - float2( 1,1 ) ) );
                float2 smoothstepResult14_g32 = smoothstep( float2( 0,0 ) , float2( 0,0 ) , temp_output_6_0_g32);
                float2 lerpResult17_g32 = lerp( step( temp_output_6_0_g32 , ( _Width * _Scale ) ) , smoothstepResult14_g32 , (float)0);
                float2 break8_g32 = lerpResult17_g32;
                float4 lerpResult113 = lerp( lerpResult123 , lerpResult126 , saturate( ( break8_g32.x + break8_g32.y ) ));
                

                half4 color = lerpResult113;

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
Node;AmplifyShaderEditor.CommentaryNode;145;-1200,-2768;Inherit;False;1812;633;Hologram Lines;13;137;134;143;135;142;144;131;140;138;129;139;112;57;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;137;-1184,-2400;Inherit;False;Property;_Speed;Speed;5;0;Create;True;0;0;0;False;0;False;0;0.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;134;-1008,-2400;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;143;-1152,-2576;Inherit;False;Property;_Float2;Float 2;7;0;Create;True;0;0;0;False;0;False;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;135;-816,-2400;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;142;-912,-2272;Inherit;False;Property;_BorderOffset;BorderOffset;6;0;Create;True;0;0;0;False;0;False;1.38;-0.025;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;144;-976,-2576;Inherit;False;FLOAT2;4;0;FLOAT;1;False;1;FLOAT;1;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;140;-720,-2272;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;131;-592,-2720;Inherit;False;F_TillingAndOffset;-1;;30;992a7dac1d94f9a47be7a2d63002dd82;0;3;1;FLOAT2;0,0;False;3;FLOAT2;1,1;False;5;FLOAT2;0,0;False;3;FLOAT2;0;FLOAT;7;FLOAT;10
Node;AmplifyShaderEditor.CommentaryNode;147;-1088,-912;Inherit;False;692;434;Border;6;37;29;36;30;31;28;;1,1,1,1;0;0
Node;AmplifyShaderEditor.FunctionNode;138;-336,-2368;Inherit;False;F_TillingAndOffset;-1;;31;992a7dac1d94f9a47be7a2d63002dd82;0;3;1;FLOAT2;0,0;False;3;FLOAT2;1,1;False;5;FLOAT2;0,0;False;3;FLOAT2;0;FLOAT;7;FLOAT;10
Node;AmplifyShaderEditor.RangedFloatNode;37;-1008,-864;Inherit;False;Property;_Width;Width;1;0;Create;True;0;0;0;False;0;False;0;1.01;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;29;-1040,-784;Inherit;False;Property;_Scale;Scale;0;0;Create;True;0;0;0;False;0;False;0,0;1.02,1.08;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SamplerNode;129;-160,-2624;Inherit;True;Property;_MainTex;MainTex;4;0;Create;True;0;0;0;False;0;False;-1;None;310c09f0f0650f045b5ce1d0d2f2c0ca;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;139;-112,-2368;Inherit;True;Property;_TextureSample1;Texture Sample 0;4;0;Create;True;0;0;0;False;0;False;-1;None;b6662a9cff1c7d345a378956d0faea28;True;0;False;white;Auto;False;Instance;129;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;36;-864,-864;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;30;-864,-752;Inherit;False;Constant;_Vector0;Vector 0;2;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;31;-864,-640;Inherit;False;Constant;_Vector1;Vector 1;3;0;Create;True;0;0;0;False;0;False;0,0;0.5,0.5;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RegisterLocalVarNode;112;352,-2576;Inherit;False;sinLines_bg;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;149;-992,-1456;Inherit;False;Constant;_Color1Offset;Color 1 Offset;9;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;124;-1088,-1888;Inherit;False;Property;_Color1;Color 1;2;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0.04669776,0.069182,0.6431373;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RegisterLocalVarNode;57;352,-2368;Inherit;False;sinLines;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;151;-1568,-1696;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;170;-1504,-1488;Inherit;False;Property;_Intensity;Intensity;8;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;28;-672,-864;Inherit;False;F_BorderMask;-1;;32;5e46f25ec17629e41a65d4b125b40423;0;5;12;FLOAT2;0,0;False;11;FLOAT2;2,2;False;15;FLOAT2;2,2;False;16;FLOAT2;2,2;False;18;INT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;122;-1056,-1264;Inherit;False;112;sinLines_bg;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;58;-1056,-1184;Inherit;False;57;sinLines;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;148;-768,-1520;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;169;-1296,-1600;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.WireNode;150;16,-928;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;123;-560,-1680;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;126;-560,-1520;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;125;-1360,-1872;Inherit;False;Property;_Color2;Color 2;3;1;[HDR];Create;True;0;0;0;False;0;False;0,0,0,0;0,0.7427804,1.108367,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp;113;128,-1168;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;181;896,-1184;Float;False;True;-1;3;AmplifyShaderEditor.MaterialInspector;0;3;Game/UI/S_Hologram Border;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;False;True;3;1;False;;10;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;True;_ColorMask;False;False;False;False;False;False;False;True;True;0;True;_Stencil;255;True;_StencilReadMask;255;True;_StencilWriteMask;0;True;_StencilComp;0;True;_StencilOp;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;0;True;unity_GUIZTestMode;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;134;0;137;0
WireConnection;135;1;134;0
WireConnection;144;1;143;0
WireConnection;140;1;142;0
WireConnection;131;3;144;0
WireConnection;131;5;135;0
WireConnection;138;1;131;0
WireConnection;138;5;140;0
WireConnection;129;1;131;0
WireConnection;139;1;138;0
WireConnection;36;0;37;0
WireConnection;36;1;29;0
WireConnection;112;0;129;1
WireConnection;57;0;139;1
WireConnection;28;11;36;0
WireConnection;28;15;30;0
WireConnection;28;16;31;0
WireConnection;148;0;124;0
WireConnection;148;1;149;0
WireConnection;169;0;151;0
WireConnection;169;1;170;0
WireConnection;150;0;28;0
WireConnection;123;0;124;0
WireConnection;123;1;169;0
WireConnection;123;2;122;0
WireConnection;126;0;148;0
WireConnection;126;1;169;0
WireConnection;126;2;58;0
WireConnection;113;0;123;0
WireConnection;113;1;126;0
WireConnection;113;2;150;0
WireConnection;181;0;113;0
ASEEND*/
//CHKSM=F35F6D264E8CF04EEE8528CF33B46F1E14401C31