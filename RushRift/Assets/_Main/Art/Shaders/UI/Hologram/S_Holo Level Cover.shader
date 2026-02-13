// Made with Amplify Shader Editor v1.9.8.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "S_Holo Level Cover"
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
        _Size("Size", Float) = 1
        _LocalScale("Local Scale", Vector) = (1,1,0,0)
        _Offset("Offset", Vector) = (1,1,0,0)
        _BorderTex("BorderTex", 2D) = "white" {}
        _TextureSample1("Texture Sample 1", 2D) = "white" {}
        _Alpha("Alpha", Range( 0 , 1)) = 0.58
        _BackgroundColor("Background Color", Color) = (0.0754717,0.03773584,0.03773584,0)
        _Float1("Float 1", Float) = 0
        _Float2("Float 2", Float) = 0
        _Vector0("Vector 0", Vector) = (0,0,0,0)
        _Float0("Float 0", Float) = 0
        [HideInInspector] _texcoord( "", 2D ) = "white" {}

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

            uniform float4 _BackgroundColor;
            uniform float2 _Vector0;
            uniform sampler2D _TextureSample1;
            uniform float _Float1;
            uniform float _Float2;
            uniform float2 _LocalScale;
            uniform float _Size;
            uniform float2 _Offset;
            uniform float _Float0;
            uniform sampler2D _BorderTex;
            uniform float4 _BorderTex_ST;
            uniform float _Alpha;


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

                float4 temp_output_2_0_g5 = IN.color;
                float2 temp_cast_1 = (_Float1).xx;
                float mulTime95 = _Time.y * _Float2;
                float2 temp_cast_2 = (mulTime95).xx;
                float2 texCoord92 = IN.texcoord.xy * temp_cast_1 + temp_cast_2;
                float smoothstepResult112 = smoothstep( _Vector0.x , _Vector0.y , tex2D( _TextureSample1, texCoord92 ).r);
                float2 texCoord6 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float2 uv48 = texCoord6;
                float2 appendResult42 = (float2(( _LocalScale.x * _Size ) , ( _LocalScale.y * _Size )));
                float2 temp_output_4_0_g2 = ( ( uv48 * appendResult42 ) + _Offset );
                float lines120 = smoothstepResult112;
                float2 appendResult122 = (float2(( lines120 * _Float0 ) , 0.0));
                float2 temp_output_4_0_g4 = ( ( temp_output_4_0_g2 * float2( 1,1 ) ) + appendResult122 );
                float2 preserverd_image_uv51 = temp_output_4_0_g4;
                float image58 = tex2D( _MainTex, preserverd_image_uv51 ).r;
                float2 uv_BorderTex = IN.texcoord.xy * _BorderTex_ST.xy + _BorderTex_ST.zw;
                float4 tex2DNode63 = tex2D( _BorderTex, uv_BorderTex );
                float lerpResult65 = lerp( image58 , 0.0 , tex2DNode63.r);
                float lerped_image87 = lerpResult65;
                float image_w_lines107 = ( smoothstepResult112 + lerped_image87 );
                float4 lerpResult77 = lerp( _BackgroundColor , float4( (temp_output_2_0_g5).rgb , 0.0 ) , image_w_lines107);
                float lerpResult106 = lerp( _Alpha , 1.0 , saturate( image_w_lines107 ));
                float alpha84 = lerpResult106;
                float alpha_mask82 = tex2DNode63.a;
                float lerpResult74 = lerp( 0.0 , alpha84 , alpha_mask82);
                float4 appendResult64 = (float4(lerpResult77.rgb , lerpResult74));
                float4 result60 = appendResult64;
                

                half4 color = result60;

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
Node;AmplifyShaderEditor.CommentaryNode;111;-3379.042,718;Inherit;False;1701.042;470.9271;Lines;11;90;107;99;97;92;95;93;96;112;114;120;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;96;-3360,912;Inherit;False;Property;_Float2;Float 2;9;0;Create;True;0;0;0;False;0;False;0;0.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;93;-3216,800;Inherit;False;Property;_Float1;Float 1;8;0;Create;True;0;0;0;False;0;False;0;0.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;95;-3168,912;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;92;-2992,800;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;90;-2768,768;Inherit;True;Property;_TextureSample1;Texture Sample 1;5;0;Create;True;0;0;0;False;0;False;-1;None;310c09f0f0650f045b5ce1d0d2f2c0ca;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.Vector2Node;114;-2608,992;Inherit;False;Property;_Vector0;Vector 0;10;0;Create;True;0;0;0;False;0;False;0,0;0.33,0.64;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.CommentaryNode;55;-2752,-1232;Inherit;False;564;210;Cach uv;2;6;48;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;52;-3392,-960;Inherit;False;1460.129;443.0146;Preserve Image Aspect Ratio;14;50;45;17;43;30;47;42;51;49;121;119;117;118;122;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SmoothstepOpNode;112;-2368,768;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;6;-2704,-1168;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;17;-3280,-640;Inherit;False;Property;_Size;Size;1;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;45;-3312,-768;Inherit;False;Property;_LocalScale;Local Scale;2;0;Create;True;0;0;0;False;0;False;1,1;1,0.7504568;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RegisterLocalVarNode;120;-2048,960;Inherit;False;lines;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;48;-2432,-1168;Inherit;False;uv;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;-3104,-672;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;43;-3104,-768;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;119;-2736,-592;Inherit;False;Property;_Float0;Float 0;11;0;Create;True;0;0;0;False;0;False;0;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;117;-2768,-672;Inherit;False;120;lines;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;42;-2928,-768;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;47;-2960,-656;Inherit;False;Property;_Offset;Offset;3;0;Create;True;0;0;0;False;0;False;1,1;0,0.1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.GetLocalVarNode;50;-3264,-912;Inherit;False;48;uv;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;118;-2560,-640;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;49;-2704,-912;Inherit;False;F_TillingAndOffset;-1;;2;992a7dac1d94f9a47be7a2d63002dd82;0;3;1;FLOAT2;0,0;False;3;FLOAT2;1,1;False;5;FLOAT2;0,0;False;3;FLOAT2;0;FLOAT;7;FLOAT;10
Node;AmplifyShaderEditor.DynamicAppendNode;122;-2352,-656;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;121;-2432,-912;Inherit;False;F_TillingAndOffset;-1;;4;992a7dac1d94f9a47be7a2d63002dd82;0;3;1;FLOAT2;0,0;False;3;FLOAT2;1,1;False;5;FLOAT2;0,0;False;3;FLOAT2;0;FLOAT;7;FLOAT;10
Node;AmplifyShaderEditor.CommentaryNode;61;-3072,-480;Inherit;False;884;354.6667;Image;4;4;53;3;58;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;51;-2192,-912;Inherit;False;preserverd_image_uv;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;4;-2992,-432;Inherit;True;Property;_MainTex;MainTex;0;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.GetLocalVarNode;53;-3024,-240;Inherit;False;51;preserverd_image_uv;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;3;-2752,-432;Inherit;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.CommentaryNode;81;-2992,-80;Inherit;False;792.9514;356.2822;Ceparate the image from the border;5;87;65;82;62;63;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;58;-2432,-432;Inherit;False;image;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;63;-2944,48;Inherit;True;Property;_BorderTex;BorderTex;4;0;Create;True;0;0;0;False;0;False;-1;None;463354571c0f29942a3a22e023aa3f69;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.GetLocalVarNode;62;-2848,-32;Inherit;False;58;image;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;65;-2624,-32;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;87;-2464,-32;Inherit;False;lerped_image;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;97;-2352,1008;Inherit;False;87;lerped_image;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;99;-2032,768;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;110;-2800,1376;Inherit;False;820;274.6666;Alpha;5;109;105;86;106;84;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;107;-1920,768;Inherit;False;image_w_lines;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;109;-2752,1536;Inherit;False;107;image_w_lines;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;105;-2528,1536;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;86;-2656,1424;Inherit;False;Property;_Alpha;Alpha;6;0;Create;True;0;0;0;False;0;False;0.58;0.75;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;79;-1056,-672;Inherit;False;708;465.3333;Apply Color;5;78;71;70;77;108;;1,1,1,1;0;0
Node;AmplifyShaderEditor.LerpOp;106;-2368,1424;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;70;-1008,-416;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;80;-624,-128;Inherit;False;298.3685;342.7713;Alpha Mask;3;83;74;85;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;82;-2656,160;Inherit;False;alpha_mask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;84;-2224,1456;Inherit;False;alpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;71;-784,-416;Inherit;False;Alpha Split;-1;;5;07dab7960105b86429ac8eebd729ed6d;0;1;2;COLOR;0,0,0,0;False;2;FLOAT3;0;FLOAT;6
Node;AmplifyShaderEditor.ColorNode;78;-816,-624;Inherit;False;Property;_BackgroundColor;Background Color;7;0;Create;True;0;0;0;False;0;False;0.0754717,0.03773584,0.03773584,0;0.07547138,0.03773584,0.03773584,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.GetLocalVarNode;83;-544,128;Inherit;False;82;alpha_mask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;85;-544,48;Inherit;False;84;alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;108;-816,-304;Inherit;False;107;image_w_lines;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;77;-496,-528;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;74;-512,-80;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;64;-208,-272;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;60;48,-272;Inherit;False;result;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;115;-2592,320;Inherit;False;offset;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;59;-48,16;Inherit;False;60;result;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;54;176,16;Float;False;True;-1;3;AmplifyShaderEditor.MaterialInspector;0;3;S_Holo Level Cover;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;False;True;3;1;False;;10;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;True;_ColorMask;False;False;False;False;False;False;False;True;True;0;True;_Stencil;255;True;_StencilReadMask;255;True;_StencilWriteMask;0;True;_StencilComp;0;True;_StencilOp;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;0;True;unity_GUIZTestMode;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;95;0;96;0
WireConnection;92;0;93;0
WireConnection;92;1;95;0
WireConnection;90;1;92;0
WireConnection;112;0;90;1
WireConnection;112;1;114;1
WireConnection;112;2;114;2
WireConnection;120;0;112;0
WireConnection;48;0;6;0
WireConnection;30;0;45;2
WireConnection;30;1;17;0
WireConnection;43;0;45;1
WireConnection;43;1;17;0
WireConnection;42;0;43;0
WireConnection;42;1;30;0
WireConnection;118;0;117;0
WireConnection;118;1;119;0
WireConnection;49;1;50;0
WireConnection;49;3;42;0
WireConnection;49;5;47;0
WireConnection;122;0;118;0
WireConnection;121;1;49;0
WireConnection;121;5;122;0
WireConnection;51;0;121;0
WireConnection;3;0;4;0
WireConnection;3;1;53;0
WireConnection;58;0;3;1
WireConnection;65;0;62;0
WireConnection;65;2;63;1
WireConnection;87;0;65;0
WireConnection;99;0;112;0
WireConnection;99;1;97;0
WireConnection;107;0;99;0
WireConnection;105;0;109;0
WireConnection;106;0;86;0
WireConnection;106;2;105;0
WireConnection;82;0;63;4
WireConnection;84;0;106;0
WireConnection;71;2;70;0
WireConnection;77;0;78;0
WireConnection;77;1;71;0
WireConnection;77;2;108;0
WireConnection;74;1;85;0
WireConnection;74;2;83;0
WireConnection;64;0;77;0
WireConnection;64;3;74;0
WireConnection;60;0;64;0
WireConnection;115;0;63;3
WireConnection;54;0;59;0
ASEEND*/
//CHKSM=C36A0C4A1FA56A900CAE665EE9D164A37D31E716