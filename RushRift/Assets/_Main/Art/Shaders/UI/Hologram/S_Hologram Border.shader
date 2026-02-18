// Made with Amplify Shader Editor v1.9.8.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "S_Hologram Border"
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

        _BorderScale("Border Scale", Vector) = (0,0,0,0)
        _BorderWidth("Border Width", Float) = 0
        _BorderOffset("BorderOffset", Float) = 1
        _MainTex("MainTex", 2D) = "white" {}
        _LinesScale("Lines Scale", Float) = 1
        _Intensity("Intensity", Float) = 1
        [HDR]_BackgroundColor("Background Color", Color) = (0.0754717,0.03773584,0.03773584,0)
        _Speed("Speed", Float) = 0.25
        _LineIntensity("Line Intensity", Vector) = (0,0,0,0)
        _VertexColorbginfluence("Vertex Color bg influence", Float) = 0

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

            uniform float _VertexColorbginfluence;
            uniform float4 _BackgroundColor;
            uniform float _Intensity;
            uniform float2 _LineIntensity;
            uniform float _LinesScale;
            uniform float _Speed;
            uniform float _BorderOffset;
            uniform float _BorderWidth;
            uniform float2 _BorderScale;


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

                float4 color206 = IsGammaSpace() ? float4(1,1,1,1) : float4(1,1,1,1);
                float4 lerpResult205 = lerp( color206 , ( IN.color * _VertexColorbginfluence ) , ceil( _VertexColorbginfluence ));
                float4 bgColor183 = ( lerpResult205 * _BackgroundColor );
                float4 lightColor182 = ( IN.color * _Intensity );
                float2 texCoord6_g30 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float2 appendResult144 = (float2(1.0 , _LinesScale));
                float mulTime134 = _Time.y * _Speed;
                float2 appendResult135 = (float2(0.0 , mulTime134));
                float2 temp_output_4_0_g30 = ( ( texCoord6_g30 * appendResult144 ) + appendResult135 );
                float2 lines_uv208 = temp_output_4_0_g30;
                float smoothstepResult196 = smoothstep( _LineIntensity.x , _LineIntensity.y , tex2D( _MainTex, lines_uv208 ).r);
                float sinLines_bg112 = smoothstepResult196;
                float4 lerpResult123 = lerp( bgColor183 , lightColor182 , sinLines_bg112);
                float2 appendResult140 = (float2(0.0 , _BorderOffset));
                float2 temp_output_4_0_g31 = ( ( lines_uv208 * float2( 1,1 ) ) + appendResult140 );
                float smoothstepResult198 = smoothstep( _LineIntensity.x , _LineIntensity.y , tex2D( _MainTex, temp_output_4_0_g31 ).r);
                float sinLines57 = smoothstepResult198;
                float4 lerpResult126 = lerp( ( bgColor183 * 0.5 ) , lightColor182 , sinLines57);
                float2 texCoord13_g32 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float2 temp_output_6_0_g32 = ( 1.0 - ( saturate( abs( ( ( texCoord13_g32 * float2( 2,2 ) ) - float2( 1,1 ) ) ) ) - float2( 1,1 ) ) );
                float2 smoothstepResult14_g32 = smoothstep( float2( 0,0 ) , float2( 0,0 ) , temp_output_6_0_g32);
                float2 lerpResult17_g32 = lerp( step( temp_output_6_0_g32 , ( _BorderWidth * _BorderScale ) ) , smoothstepResult14_g32 , (float)0);
                float2 break8_g32 = lerpResult17_g32;
                float borderMask189 = saturate( ( break8_g32.x + break8_g32.y ) );
                float4 lerpResult113 = lerp( lerpResult123 , lerpResult126 , borderMask189);
                float4 color192 = lerpResult113;
                

                half4 color = color192;

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
Node;AmplifyShaderEditor.CommentaryNode;145;-1904.057,-2768;Inherit;False;2564.438;638.8242;Hologram Lines;17;208;131;135;144;134;143;137;57;112;198;196;195;139;129;138;140;142;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;137;-1696,-2576;Inherit;False;Property;_Speed;Speed;8;0;Create;True;0;0;0;False;0;False;0.25;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;143;-1520,-2688;Inherit;False;Property;_LinesScale;Lines Scale;5;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;134;-1520,-2576;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;144;-1328,-2688;Inherit;False;FLOAT2;4;0;FLOAT;1;False;1;FLOAT;1;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;135;-1328,-2576;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;131;-1168,-2688;Inherit;False;Tilling And Offset;-1;;30;992a7dac1d94f9a47be7a2d63002dd82;0;3;1;FLOAT2;0,0;False;3;FLOAT2;1,1;False;5;FLOAT2;0,0;False;3;FLOAT2;0;FLOAT;7;FLOAT;10
Node;AmplifyShaderEditor.RangedFloatNode;142;-928,-2464;Inherit;False;Property;_BorderOffset;BorderOffset;2;0;Create;True;0;0;0;False;0;False;1;-0.025;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;201;-2838.504,-2413.255;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;202;-2902.504,-2237.255;Inherit;False;Property;_VertexColorbginfluence;Vertex Color bg influence;10;0;Create;True;0;0;0;False;0;False;0;0.75;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;140;-736,-2464;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;199;-2678.504,-2157.255;Inherit;False;Property;_BackgroundColor;Background Color;7;1;[HDR];Create;True;0;0;0;False;0;False;0.0754717,0.03773584,0.03773584,0;0,0.04939228,0.0627451,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.CeilOpNode;203;-2614.504,-2237.255;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;204;-2646.504,-2413.255;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;206;-2710.504,-2605.255;Inherit;False;Constant;_Color0;Color 0;18;0;Create;True;0;0;0;False;0;False;1,1,1,1;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RegisterLocalVarNode;208;-608,-2688;Inherit;False;lines_uv;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;191;-1794,-2034;Inherit;False;1948.372;643.8335;Color;19;190;113;58;183;182;187;126;123;122;185;184;148;149;186;124;169;170;151;192;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;147;-1088,-912;Inherit;False;891.5015;441.3077;Border;7;28;31;30;36;29;37;189;;1,1,1,1;0;0
Node;AmplifyShaderEditor.FunctionNode;138;-432,-2496;Inherit;False;Tilling And Offset;-1;;31;992a7dac1d94f9a47be7a2d63002dd82;0;3;1;FLOAT2;0,0;False;3;FLOAT2;1,1;False;5;FLOAT2;0,0;False;3;FLOAT2;0;FLOAT;7;FLOAT;10
Node;AmplifyShaderEditor.LerpOp;205;-2454.504,-2605.255;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.WireNode;207;-2342.504,-2285.255;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.VertexColorNode;151;-1744,-1728;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;170;-1680,-1520;Inherit;False;Property;_Intensity;Intensity;6;0;Create;True;0;0;0;False;0;False;1;1.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;37;-1024,-864;Inherit;False;Property;_BorderWidth;Border Width;1;0;Create;True;0;0;0;False;0;False;0;1.01;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;29;-1040,-784;Inherit;False;Property;_BorderScale;Border Scale;0;0;Create;True;0;0;0;False;0;False;0,0;1.02,1.08;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SamplerNode;129;-224,-2720;Inherit;True;Property;_MainTex;MainTex;4;0;Create;True;0;0;0;False;0;False;-1;None;310c09f0f0650f045b5ce1d0d2f2c0ca;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;139;-224,-2496;Inherit;True;Property;_TextureSample1;Texture Sample 0;4;0;Create;True;0;0;0;False;0;False;-1;None;b6662a9cff1c7d345a378956d0faea28;True;0;False;white;Auto;False;Instance;129;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.Vector2Node;195;-128,-2288;Inherit;False;Property;_LineIntensity;Line Intensity;9;0;Create;True;0;0;0;False;0;False;0,0;-0.04,1.31;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;200;-2278.504,-2605.255;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;36;-864,-864;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;169;-1472,-1632;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;183;-1296,-1984;Inherit;False;bgColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.Vector2Node;30;-864,-752;Inherit;False;Constant;_Vector0;Vector 0;2;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;31;-864,-624;Inherit;False;Constant;_Vector1;Vector 1;3;0;Create;True;0;0;0;False;0;False;0,0;0.5,0.5;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SmoothstepOpNode;196;128,-2656;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;198;144,-2416;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;28;-672,-864;Inherit;False;Border Mask;-1;;32;5e46f25ec17629e41a65d4b125b40423;0;5;12;FLOAT2;0,0;False;11;FLOAT2;2,2;False;15;FLOAT2;2,2;False;16;FLOAT2;2,2;False;18;INT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;186;-928,-1696;Inherit;False;183;bgColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;149;-960,-1616;Inherit;False;Constant;_Color1Offset;Color 1 Offset;9;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;182;-1296,-1632;Inherit;False;lightColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;112;352,-2672;Inherit;False;sinLines_bg;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;57;416,-2432;Inherit;False;sinLines;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;189;-416,-864;Inherit;False;borderMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;148;-752,-1680;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;184;-800,-1968;Inherit;False;183;bgColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;185;-800,-1904;Inherit;False;182;lightColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;122;-816,-1840;Inherit;False;112;sinLines_bg;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;187;-784,-1584;Inherit;False;182;lightColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;58;-784,-1504;Inherit;False;57;sinLines;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;123;-592,-1968;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;126;-560,-1680;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;190;-560,-1552;Inherit;False;189;borderMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;113;-320,-1840;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;192;-128,-1840;Inherit;False;color;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;124;-1696,-1984;Inherit;False;Property;_Color1;Color 1;3;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0.04669762,0.06918186,0.6431373;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.GetLocalVarNode;193;688,-1184;Inherit;False;192;color;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;181;896,-1184;Float;False;True;-1;3;AmplifyShaderEditor.MaterialInspector;0;3;S_Hologram Border;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;False;True;3;1;False;;10;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;True;_ColorMask;False;False;False;False;False;False;False;True;True;0;True;_Stencil;255;True;_StencilReadMask;255;True;_StencilWriteMask;0;True;_StencilComp;0;True;_StencilOp;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;0;True;unity_GUIZTestMode;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;134;0;137;0
WireConnection;144;1;143;0
WireConnection;135;1;134;0
WireConnection;131;3;144;0
WireConnection;131;5;135;0
WireConnection;140;1;142;0
WireConnection;203;0;202;0
WireConnection;204;0;201;0
WireConnection;204;1;202;0
WireConnection;208;0;131;0
WireConnection;138;1;208;0
WireConnection;138;5;140;0
WireConnection;205;0;206;0
WireConnection;205;1;204;0
WireConnection;205;2;203;0
WireConnection;207;0;199;0
WireConnection;129;1;208;0
WireConnection;139;1;138;0
WireConnection;200;0;205;0
WireConnection;200;1;207;0
WireConnection;36;0;37;0
WireConnection;36;1;29;0
WireConnection;169;0;151;0
WireConnection;169;1;170;0
WireConnection;183;0;200;0
WireConnection;196;0;129;1
WireConnection;196;1;195;1
WireConnection;196;2;195;2
WireConnection;198;0;139;1
WireConnection;198;1;195;1
WireConnection;198;2;195;2
WireConnection;28;11;36;0
WireConnection;28;15;30;0
WireConnection;28;16;31;0
WireConnection;182;0;169;0
WireConnection;112;0;196;0
WireConnection;57;0;198;0
WireConnection;189;0;28;0
WireConnection;148;0;186;0
WireConnection;148;1;149;0
WireConnection;123;0;184;0
WireConnection;123;1;185;0
WireConnection;123;2;122;0
WireConnection;126;0;148;0
WireConnection;126;1;187;0
WireConnection;126;2;58;0
WireConnection;113;0;123;0
WireConnection;113;1;126;0
WireConnection;113;2;190;0
WireConnection;192;0;113;0
WireConnection;181;0;193;0
ASEEND*/
//CHKSM=9665F4BB4C71C96E8700BBB1DF287D24F58FEF95