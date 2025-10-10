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

        _PinMaskStep("PinMaskStep", Range( -1 , 1)) = 0
        _PinMaskPower("PinMaskPower", Range( -1 , 1)) = 0
        _ColorMaskPower("ColorMaskPower", Float) = 0
        _Min("Min", Float) = 0
        _Max("Max", Float) = 0
        _LightColor("LightColor", Color) = (0,0,0,0)
        _MainTex("_MainTex", 2D) = "white" {}
        _AnimSpeed("AnimSpeed", Float) = 0
        _Animate("Animate", Float) = 0

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
            uniform float _ColorMaskPower;
            uniform float4 _LightColor;
            uniform float _Min;
            uniform float _Max;
            uniform float _PinMaskPower;
            uniform float _PinMaskStep;


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
                float4 tex2DNode3 = tex2D( _MainTex, fbuv45 );
                float temp_output_20_0 = pow( tex2DNode3.b , _ColorMaskPower );
                float smoothstepResult28 = smoothstep( _Min , _Max , temp_output_20_0);
                float4 lerpResult30 = lerp( IN.color , _LightColor , smoothstepResult28);
                float4 Main10 = saturate( ( temp_output_20_0 * lerpResult30 ) );
                float4 Image14 = tex2DNode3;
                float PinMask9 = step( pow( tex2DNode3.b , _PinMaskPower ) , _PinMaskStep );
                float4 lerpResult13 = lerp( Main10 , ( Image14 * IN.color ) , PinMask9);
                float Alpha18 = tex2DNode3.a;
                float4 lerpResult4 = lerp( float4( 0,0,0,0 ) , lerpResult13 , Alpha18);
                float4 break24 = lerpResult4;
                float4 appendResult25 = (float4(break24.r , break24.g , break24.b , Alpha18));
                

                half4 color = appendResult25;

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
Node;AmplifyShaderEditor.RangedFloatNode;48;-2192,-752;Inherit;False;Property;_AnimSpeed;AnimSpeed;7;0;Create;True;0;0;0;False;0;False;0;12;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;51;-2160,-672;Inherit;False;Property;_Animate;Animate;9;0;Create;True;0;0;0;False;0;False;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;47;-2000,-640;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;50;-1968,-768;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;46;-2032,-896;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCFlipBookUVAnimation;45;-1696,-864;Inherit;False;0;0;7;0;FLOAT2;0,0;False;1;FLOAT;14;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;-1;False;4;FLOAT2;0;FLOAT;1;FLOAT;2;INT;3
Node;AmplifyShaderEditor.RangedFloatNode;21;-1184,-400;Inherit;False;Property;_ColorMaskPower;ColorMaskPower;2;0;Create;True;0;0;0;False;0;False;0;1.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;3;-1280,-896;Inherit;True;Property;_MainTex;_MainTex;6;0;Fetch;True;0;0;0;False;0;False;-1;None;f586b733fa421c3479695eb579ce8efb;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.PowerNode;20;-928,-448;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;27;-928,-304;Inherit;False;Property;_Min;Min;3;0;Create;True;0;0;0;False;0;False;0;0.08;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;29;-928,-208;Inherit;False;Property;_Max;Max;4;0;Create;True;0;0;0;False;0;False;0;1.31;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;28;-704,-336;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;31;-736,0;Inherit;False;Property;_LightColor;LightColor;5;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0.9910492,1,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.VertexColorNode;1;-704,-176;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;8;-784,-544;Inherit;False;Property;_PinMaskPower;PinMaskPower;1;0;Create;True;0;0;0;False;0;False;0;0.63;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;30;-416,-336;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;1,1,1,1;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.PowerNode;7;-656,-656;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;6;-480,-544;Inherit;False;Property;_PinMaskStep;PinMaskStep;0;0;Create;True;0;0;0;False;0;False;0;0.03;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;22;-240,-448;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StepOpNode;5;-320,-656;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;23;-48,-448;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;14;-896,-816;Inherit;False;Image;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;10;144,-448;Inherit;False;Main;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;9;-160,-656;Inherit;False;PinMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;15;-1168,496;Inherit;False;14;Image;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;18;-1168,-624;Inherit;False;Alpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;11;-720,592;Inherit;False;9;PinMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;16;-720,416;Inherit;False;10;Main;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;32;-907.045,569.5164;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;19;-464,688;Inherit;False;18;Alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;13;-432,432;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;4;-240,432;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;1,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.BreakToComponentsNode;24;-48,432;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.Vector2Node;49;-1696,-640;Inherit;False;Property;_StartEnd;StartEnd;8;0;Create;True;0;0;0;False;0;False;55,60;0,30;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.DynamicAppendNode;25;128,432;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;336,432;Float;False;True;-1;3;AmplifyShaderEditor.MaterialInspector;0;3;S_Medal_Animated;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;False;True;3;1;False;;10;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;True;_ColorMask;False;False;False;False;False;False;False;True;True;0;True;_Stencil;255;True;_StencilReadMask;255;True;_StencilWriteMask;0;True;_StencilComp;0;True;_StencilOp;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;0;True;unity_GUIZTestMode;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;50;1;48;0
WireConnection;50;2;51;0
WireConnection;45;0;46;0
WireConnection;45;3;50;0
WireConnection;45;5;47;0
WireConnection;3;1;45;0
WireConnection;20;0;3;3
WireConnection;20;1;21;0
WireConnection;28;0;20;0
WireConnection;28;1;27;0
WireConnection;28;2;29;0
WireConnection;30;0;1;0
WireConnection;30;1;31;0
WireConnection;30;2;28;0
WireConnection;7;0;3;3
WireConnection;7;1;8;0
WireConnection;22;0;20;0
WireConnection;22;1;30;0
WireConnection;5;0;7;0
WireConnection;5;1;6;0
WireConnection;23;0;22;0
WireConnection;14;0;3;0
WireConnection;10;0;23;0
WireConnection;9;0;5;0
WireConnection;18;0;3;4
WireConnection;32;0;15;0
WireConnection;32;1;1;0
WireConnection;13;0;16;0
WireConnection;13;1;32;0
WireConnection;13;2;11;0
WireConnection;4;1;13;0
WireConnection;4;2;19;0
WireConnection;24;0;4;0
WireConnection;25;0;24;0
WireConnection;25;1;24;1
WireConnection;25;2;24;2
WireConnection;25;3;19;0
WireConnection;0;0;25;0
ASEEND*/
//CHKSM=D8FF39D8612413D212262C033BB8D8B72EBE5511