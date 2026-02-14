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
        _TextureSample1("Texture Sample 1", 2D) = "white" {}
        _Alpha("Alpha", Range( 0 , 1)) = 0.58
        [HDR]_BackgroundColor("Background Color", Color) = (0.0754717,0.03773584,0.03773584,0)
        _LinesTilling("Lines Tilling", Float) = 0
        _Float2("Float 2", Float) = 0
        _LinesIntensity("Lines Intensity", Vector) = (0,0,0,0)
        _Float0("Float 0", Float) = 0
        _BorderOutlineOutter("Border Outline Outter", Float) = 0.15
        _BorderOutlineInner("Border Outline Inner", Float) = 0.15
        _BorderOutlineIntensity("Border Outline Intensity", Float) = 0.5
        _BorderMask("Border Mask", 2D) = "white" {}
        _Depth("Depth", Float) = 0
        _ParallaxSteps("Parallax Steps", Vector) = (8,16,2,0)
        _VertexColorbginfluence("Vertex Color bg influence", Float) = 0
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
                float4 ase_tangent : TANGENT;
                float3 ase_normal : NORMAL;
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
                float4 ase_texcoord4 : TEXCOORD4;
                float4 ase_texcoord5 : TEXCOORD5;
                float4 ase_texcoord6 : TEXCOORD6;
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
            uniform float2 _LinesIntensity;
            uniform sampler2D _TextureSample1;
            uniform sampler2D _BorderMask;
            uniform float _Depth;
            uniform float3 _ParallaxSteps;
            uniform float4 _BorderMask_ST;
            uniform float _LinesTilling;
            uniform float _Float2;
            uniform float _BorderOutlineOutter;
            uniform float _BorderOutlineInner;
            uniform float _BorderOutlineIntensity;
            uniform float2 _LocalScale;
            uniform float _Size;
            uniform float2 _Offset;
            uniform float _Float0;
            uniform float _Alpha;
            inline float2 POM( sampler2D heightMap, float2 uvs, float2 dx, float2 dy, float3 normalWorld, float3 viewWorld, float3 viewDirTan, int minSamples, int maxSamples, int sidewallSteps, float parallax, float refPlane, float2 tilling, float2 curv, int index )
            {
            	float3 result = 0;
            	float stepIndex = 0;
            	float numSteps = floor( lerp( (float)maxSamples, (float)minSamples, saturate( dot( normalWorld, viewWorld ) ) ) );
            	float layerHeight = 1.0 / numSteps;
            	float2 plane = parallax * ( viewDirTan.xy / viewDirTan.z );
            	uvs.xy += refPlane * plane;
            	float2 deltaTex = -plane * layerHeight;
            	float2 prevTexOffset = 0;
            	float prevRayZ = 1.0f;
            	float prevHeight = 0.0f;
            	float2 currTexOffset = deltaTex;
            	float currRayZ = 1.0f - layerHeight;
            	float currHeight = 0.0f;
            	float intersection = 0;
            	float2 finalTexOffset = 0;
            	while ( stepIndex < numSteps + 1 )
            	{
            	 	currHeight = tex2Dgrad( heightMap, uvs + currTexOffset, dx, dy ).r;
            	 	if ( currHeight > currRayZ )
            	 	{
            	 	 	stepIndex = numSteps + 1;
            	 	}
            	 	else
            	 	{
            	 	 	stepIndex++;
            	 	 	prevTexOffset = currTexOffset;
            	 	 	prevRayZ = currRayZ;
            	 	 	prevHeight = currHeight;
            	 	 	currTexOffset += deltaTex;
            	 	 	currRayZ -= layerHeight;
            	 	}
            	}
            	float sectionSteps = sidewallSteps;
            	float sectionIndex = 0;
            	float newZ = 0;
            	float newHeight = 0;
            	while ( sectionIndex < sectionSteps )
            	{
            	 	intersection = ( prevHeight - prevRayZ ) / ( prevHeight - currHeight + currRayZ - prevRayZ );
            	 	finalTexOffset = prevTexOffset + intersection * deltaTex;
            	 	newZ = prevRayZ - intersection * layerHeight;
            	 	newHeight = tex2Dgrad( heightMap, uvs + finalTexOffset, dx, dy ).r;
            	 	if ( newHeight > newZ )
            	 	{
            	 	 	currTexOffset = finalTexOffset;
            	 	 	currHeight = newHeight;
            	 	 	currRayZ = newZ;
            	 	 	deltaTex = intersection * deltaTex;
            	 	 	layerHeight = intersection * layerHeight;
            	 	}
            	 	else
            	 	{
            	 	 	prevTexOffset = finalTexOffset;
            	 	 	prevHeight = newHeight;
            	 	 	prevRayZ = newZ;
            	 	 	deltaTex = ( 1 - intersection ) * deltaTex;
            	 	 	layerHeight = ( 1 - intersection ) * layerHeight;
            	 	}
            	 	sectionIndex++;
            	}
            	return uvs.xy + finalTexOffset;
            }
            


            v2f vert(appdata_t v )
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 ase_positionWS = mul( unity_ObjectToWorld, float4( ( v.vertex ).xyz, 1 ) ).xyz;
                OUT.ase_texcoord3.xyz = ase_positionWS;
                float3 ase_tangentWS = UnityObjectToWorldDir( v.ase_tangent );
                OUT.ase_texcoord4.xyz = ase_tangentWS;
                float3 ase_normalWS = UnityObjectToWorldNormal( v.ase_normal );
                OUT.ase_texcoord5.xyz = ase_normalWS;
                float ase_tangentSign = v.ase_tangent.w * ( unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0 );
                float3 ase_bitangentWS = cross( ase_normalWS, ase_tangentWS ) * ase_tangentSign;
                OUT.ase_texcoord6.xyz = ase_bitangentWS;
                
                
                //setting value to unused interpolator channels and avoid initialization warnings
                OUT.ase_texcoord3.w = 0;
                OUT.ase_texcoord4.w = 0;
                OUT.ase_texcoord5.w = 0;
                OUT.ase_texcoord6.w = 0;

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

                float4 color339 = IsGammaSpace() ? float4(1,1,1,1) : float4(1,1,1,1);
                float4 lerpResult338 = lerp( color339 , ( IN.color * _VertexColorbginfluence ) , ceil( _VertexColorbginfluence ));
                float4 temp_output_2_0_g28 = IN.color;
                float2 texCoord6 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float2 uv48 = texCoord6;
                float3 ase_positionWS = IN.ase_texcoord3.xyz;
                float3 ase_tangentWS = IN.ase_texcoord4.xyz;
                float3 ase_normalWS = IN.ase_texcoord5.xyz;
                float3 ase_bitangentWS = IN.ase_texcoord6.xyz;
                float3 tanToWorld0 = float3( ase_tangentWS.x, ase_bitangentWS.x, ase_normalWS.x );
                float3 tanToWorld1 = float3( ase_tangentWS.y, ase_bitangentWS.y, ase_normalWS.y );
                float3 tanToWorld2 = float3( ase_tangentWS.z, ase_bitangentWS.z, ase_normalWS.z );
                float3 ase_viewVectorTS =  tanToWorld0 * ( _WorldSpaceCameraPos.xyz - ase_positionWS ).x + tanToWorld1 * ( _WorldSpaceCameraPos.xyz - ase_positionWS ).y  + tanToWorld2 * ( _WorldSpaceCameraPos.xyz - ase_positionWS ).z;
                float3 ase_viewDirTS = normalize( ase_viewVectorTS );
                float3 ase_viewVectorWS = ( _WorldSpaceCameraPos.xyz - ase_positionWS );
                float3 ase_viewDirWS = normalize( ase_viewVectorWS );
                float2 OffsetPOM318 = POM( _BorderMask, uv48, ddx(uv48), ddy(uv48), ase_normalWS, ase_viewDirWS, ase_viewDirTS, (int)abs( _ParallaxSteps.x ), (int)abs( _ParallaxSteps.y ), (int)abs( _ParallaxSteps.z ), _Depth, 0, _BorderMask_ST.xy, float2(0,0), 0 );
                float2 parallax_uv299 = OffsetPOM318;
                float2 uv_BorderMask = IN.texcoord.xy * _BorderMask_ST.xy + _BorderMask_ST.zw;
                float4 tex2DNode63 = tex2D( _BorderMask, uv_BorderMask );
                float border_r248 = tex2DNode63.r;
                float2 lerpResult304 = lerp( parallax_uv299 , uv48 , border_r248);
                float2 temp_cast_4 = (_LinesTilling).xx;
                float mulTime95 = _Time.y * _Float2;
                float2 temp_cast_5 = (mulTime95).xx;
                float2 temp_output_4_0_g18 = ( ( lerpResult304 * temp_cast_4 ) + temp_cast_5 );
                float smoothstepResult112 = smoothstep( _LinesIntensity.x , _LinesIntensity.y , tex2D( _TextureSample1, temp_output_4_0_g18 ).r);
                float2 temp_cast_6 = (_BorderOutlineOutter).xx;
                float2 temp_output_4_0_g15 = ( ( uv48 * float2( 1,1 ) ) + temp_cast_6 );
                float temp_output_184_0 = ( _BorderOutlineOutter * -1.0 );
                float2 temp_cast_7 = (temp_output_184_0).xx;
                float2 temp_output_4_0_g14 = ( ( uv48 * float2( 1,1 ) ) + temp_cast_7 );
                float2 appendResult183 = (float2(_BorderOutlineOutter , temp_output_184_0));
                float2 temp_output_4_0_g16 = ( ( uv48 * float2( 1,1 ) ) + appendResult183 );
                float2 appendResult188 = (float2(temp_output_184_0 , _BorderOutlineOutter));
                float2 temp_output_4_0_g17 = ( ( uv48 * float2( 1,1 ) ) + appendResult188 );
                float lerpResult139 = lerp( 0.0 , ( tex2D( _BorderMask, temp_output_4_0_g15 ).r * tex2D( _BorderMask, temp_output_4_0_g14 ).r * tex2D( _BorderMask, temp_output_4_0_g16 ).r * tex2D( _BorderMask, temp_output_4_0_g17 ).r ) , border_r248);
                float border_outline_outter152 = lerpResult139;
                float2 appendResult204 = (float2(0.0 , _BorderOutlineInner));
                float2 temp_output_4_0_g10 = ( ( uv48 * float2( 1,1 ) ) + appendResult204 );
                float temp_output_210_0 = ( _BorderOutlineInner * -1.0 );
                float2 appendResult209 = (float2(0.0 , temp_output_210_0));
                float2 temp_output_4_0_g11 = ( ( uv48 * float2( 1,1 ) ) + appendResult209 );
                float2 appendResult219 = (float2(_BorderOutlineInner , 0.0));
                float2 temp_output_4_0_g12 = ( ( uv48 * float2( 1,1 ) ) + appendResult219 );
                float2 appendResult218 = (float2(temp_output_210_0 , 0.0));
                float2 temp_output_4_0_g13 = ( ( uv48 * float2( 1,1 ) ) + appendResult218 );
                float border_b249 = tex2DNode63.b;
                float lerpResult194 = lerp( saturate( ( ( border_r248 - tex2D( _BorderMask, temp_output_4_0_g10 ).r ) + ( border_r248 - tex2D( _BorderMask, temp_output_4_0_g11 ).r ) + ( border_r248 - tex2D( _BorderMask, temp_output_4_0_g12 ).r ) + ( border_r248 - tex2D( _BorderMask, temp_output_4_0_g13 ).r ) ) ) , 0.0 , border_b249);
                float border_outline_inner191 = lerpResult194;
                float border_outline173 = ( ( border_outline_outter152 + border_outline_inner191 ) * _BorderOutlineIntensity );
                float lines120 = ( smoothstepResult112 + border_outline173 );
                float2 appendResult42 = (float2(( _LocalScale.x * _Size ) , ( _LocalScale.y * _Size )));
                float2 temp_output_4_0_g25 = ( ( parallax_uv299 * appendResult42 ) + _Offset );
                float2 appendResult122 = (float2(( lines120 * _Float0 ) , 0.0));
                float2 temp_output_4_0_g26 = ( ( temp_output_4_0_g25 * float2( 1,1 ) ) + appendResult122 );
                float2 preserverd_image_uv51 = temp_output_4_0_g26;
                float border_g322 = tex2D( _BorderMask, parallax_uv299 ).g;
                float lerpResult325 = lerp( 0.0 , tex2D( _MainTex, preserverd_image_uv51 ).r , border_g322);
                float image58 = lerpResult325;
                float lerpResult65 = lerp( image58 , 0.0 , border_r248);
                float lerped_image87 = lerpResult65;
                float image_w_lines107 = ( lines120 + lerped_image87 );
                float4 lerpResult77 = lerp( ( lerpResult338 * _BackgroundColor ) , float4( (temp_output_2_0_g28).rgb , 0.0 ) , image_w_lines107);
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
Node;AmplifyShaderEditor.CommentaryNode;242;-5474,1886;Inherit;False;2823.501;2647.09;Outline;43;210;246;250;191;194;251;145;147;146;125;124;241;240;183;188;184;239;237;238;176;173;174;223;175;152;139;224;213;245;244;243;253;255;257;259;260;261;262;263;264;265;266;267;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;81;-4192,48;Inherit;False;1639.896;507.2319;Ceparate the image from the border;10;82;87;65;249;248;63;281;279;319;62;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;55;-2592,-1936;Inherit;False;629.2156;200.4843;Cach uv;2;48;6;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;243;-5440,3600;Inherit;False;Property;_BorderOutlineInner;Border Outline Inner;12;0;Create;True;0;0;0;False;0;False;0.15;0.015;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;279;-4080,160;Inherit;True;Property;_BorderMask;Border Mask;14;0;Create;True;0;0;0;False;0;False;None;463354571c0f29942a3a22e023aa3f69;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;210;-5184,3600;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;6;-2528,-1872;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;259;-4928,4048;Inherit;False;1012;361;right;6;258;214;218;222;274;275;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;257;-4912,3680;Inherit;False;1008.323;337.7124;Left;6;215;256;219;221;272;273;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;255;-4944,3344;Inherit;False;972.0017;319.8374;Up;6;254;211;207;209;270;271;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;253;-4944,2960;Inherit;False;980;361;Down;6;252;206;201;204;268;269;;1,1,1,1;0;0
Node;AmplifyShaderEditor.WireNode;244;-5200,3776;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;245;-5200,3408;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;176;-5440,2384;Inherit;False;Property;_BorderOutlineOutter;Border Outline Outter;11;0;Create;True;0;0;0;False;0;False;0.15;0.03;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;246;-5008,4032;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;281;-3792,176;Inherit;False;border_tex;-1;True;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;319;-3792,256;Inherit;False;border_ss;-1;True;1;0;SAMPLERSTATE;;False;1;SAMPLERSTATE;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;48;-2192,-1872;Inherit;False;uv;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WireNode;238;-5088,2544;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;204;-4880,3152;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;209;-4880,3472;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;219;-4864,3856;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;218;-4880,4176;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;269;-4912,3056;Inherit;False;48;uv;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;271;-4880,3376;Inherit;False;48;uv;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;273;-4880,3760;Inherit;False;48;uv;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;275;-4864,4080;Inherit;False;48;uv;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;63;-3568,176;Inherit;True;Property;_BorderTex;BorderTex;4;0;Create;True;0;0;0;False;0;False;-1;None;463354571c0f29942a3a22e023aa3f69;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.WireNode;237;-5088,2688;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;239;-4896,2544;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;184;-5056,2384;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;268;-4736,3072;Inherit;False;F_TillingAndOffset;-1;;10;992a7dac1d94f9a47be7a2d63002dd82;0;3;1;FLOAT2;0,0;False;3;FLOAT2;1,1;False;5;FLOAT2;0,0;False;3;FLOAT2;0;FLOAT;7;FLOAT;10
Node;AmplifyShaderEditor.FunctionNode;270;-4704,3392;Inherit;False;F_TillingAndOffset;-1;;11;992a7dac1d94f9a47be7a2d63002dd82;0;3;1;FLOAT2;0,0;False;3;FLOAT2;1,1;False;5;FLOAT2;0,0;False;3;FLOAT2;0;FLOAT;7;FLOAT;10
Node;AmplifyShaderEditor.FunctionNode;272;-4704,3776;Inherit;False;F_TillingAndOffset;-1;;12;992a7dac1d94f9a47be7a2d63002dd82;0;3;1;FLOAT2;0,0;False;3;FLOAT2;1,1;False;5;FLOAT2;0,0;False;3;FLOAT2;0;FLOAT;7;FLOAT;10
Node;AmplifyShaderEditor.FunctionNode;274;-4688,4096;Inherit;False;F_TillingAndOffset;-1;;13;992a7dac1d94f9a47be7a2d63002dd82;0;3;1;FLOAT2;0,0;False;3;FLOAT2;1,1;False;5;FLOAT2;0,0;False;3;FLOAT2;0;FLOAT;7;FLOAT;10
Node;AmplifyShaderEditor.RegisterLocalVarNode;248;-3232,176;Inherit;False;border_r;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;333;-2928,-1712;Inherit;False;980;722.6667;Parallax;11;282;320;284;318;330;328;329;331;321;299;332;;1,1,1,1;0;0
Node;AmplifyShaderEditor.WireNode;240;-5152,2160;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;241;-4880,2304;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;201;-4432,3104;Inherit;True;Property;_BorderTex12;BorderTex;4;0;Create;True;0;0;0;False;0;False;-1;None;463354571c0f29942a3a22e023aa3f69;True;0;False;white;Auto;False;Instance;63;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.GetLocalVarNode;252;-4336,3024;Inherit;False;248;border_r;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;207;-4432,3472;Inherit;True;Property;_BorderTex13;BorderTex;4;0;Create;True;0;0;0;False;0;False;-1;None;463354571c0f29942a3a22e023aa3f69;True;0;False;white;Auto;False;Instance;63;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.GetLocalVarNode;254;-4336,3392;Inherit;False;248;border_r;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;215;-4416,3808;Inherit;True;Property;_BorderTex15;BorderTex;4;0;Create;True;0;0;0;False;0;False;-1;None;463354571c0f29942a3a22e023aa3f69;True;0;False;white;Auto;False;Instance;63;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.GetLocalVarNode;256;-4320,3728;Inherit;False;248;border_r;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;214;-4432,4176;Inherit;True;Property;_BorderTex14;BorderTex;4;0;Create;True;0;0;0;False;0;False;-1;None;463354571c0f29942a3a22e023aa3f69;True;0;False;white;Auto;False;Instance;63;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.GetLocalVarNode;258;-4336,4096;Inherit;False;248;border_r;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;261;-4784,2032;Inherit;False;48;uv;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;263;-4784,2192;Inherit;False;48;uv;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;265;-4784,2384;Inherit;False;48;uv;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;183;-4752,2464;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;188;-4752,2720;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;267;-4784,2624;Inherit;False;48;uv;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;206;-4128,3088;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;211;-4112,3440;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;221;-4080,3824;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;222;-4096,4160;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;262;-4592,2192;Inherit;False;F_TillingAndOffset;-1;;14;992a7dac1d94f9a47be7a2d63002dd82;0;3;1;FLOAT2;0,0;False;3;FLOAT2;1,1;False;5;FLOAT2;0,0;False;3;FLOAT2;0;FLOAT;7;FLOAT;10
Node;AmplifyShaderEditor.FunctionNode;260;-4592,2032;Inherit;False;F_TillingAndOffset;-1;;15;992a7dac1d94f9a47be7a2d63002dd82;0;3;1;FLOAT2;0,0;False;3;FLOAT2;1,1;False;5;FLOAT2;0,0;False;3;FLOAT2;0;FLOAT;7;FLOAT;10
Node;AmplifyShaderEditor.FunctionNode;264;-4592,2384;Inherit;False;F_TillingAndOffset;-1;;16;992a7dac1d94f9a47be7a2d63002dd82;0;3;1;FLOAT2;0,0;False;3;FLOAT2;1,1;False;5;FLOAT2;0,0;False;3;FLOAT2;0;FLOAT;7;FLOAT;10
Node;AmplifyShaderEditor.FunctionNode;266;-4592,2624;Inherit;False;F_TillingAndOffset;-1;;17;992a7dac1d94f9a47be7a2d63002dd82;0;3;1;FLOAT2;0,0;False;3;FLOAT2;1,1;False;5;FLOAT2;0,0;False;3;FLOAT2;0;FLOAT;7;FLOAT;10
Node;AmplifyShaderEditor.Vector3Node;330;-2880,-1200;Inherit;False;Property;_ParallaxSteps;Parallax Steps;16;0;Create;True;0;0;0;False;0;False;8,16,2;2,6,9.28;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SamplerNode;124;-4304,2032;Inherit;True;Property;_BorderTex1;BorderTex;4;0;Create;True;0;0;0;False;0;False;-1;None;463354571c0f29942a3a22e023aa3f69;True;0;False;white;Auto;False;Instance;63;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;125;-4304,2224;Inherit;True;Property;_BorderTex2;BorderTex;4;0;Create;True;0;0;0;False;0;False;-1;None;463354571c0f29942a3a22e023aa3f69;True;0;False;white;Auto;False;Instance;63;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;146;-4304,2432;Inherit;True;Property;_BorderTex4;BorderTex;4;0;Create;True;0;0;0;False;0;False;-1;None;463354571c0f29942a3a22e023aa3f69;True;0;False;white;Auto;False;Instance;63;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;147;-4304,2624;Inherit;True;Property;_BorderTex5;BorderTex;4;0;Create;True;0;0;0;False;0;False;-1;None;463354571c0f29942a3a22e023aa3f69;True;0;False;white;Auto;False;Instance;63;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleAddOpNode;213;-3888,3392;Inherit;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;249;-3232,336;Inherit;False;border_b;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;282;-2880,-1584;Inherit;False;281;border_tex;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode;320;-2880,-1520;Inherit;False;319;border_ss;1;0;OBJECT;;False;1;SAMPLERSTATE;0
Node;AmplifyShaderEditor.RangedFloatNode;284;-2880,-1456;Inherit;False;Property;_Depth;Depth;15;0;Create;True;0;0;0;False;0;False;0;0.08;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;328;-2688,-1232;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;329;-2688,-1168;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;331;-2688,-1104;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;321;-2880,-1376;Inherit;False;Tangent;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;332;-2880,-1664;Inherit;False;48;uv;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;224;-3769.962,3402.596;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;145;-3888,2160;Inherit;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;251;-3792,3600;Inherit;False;249;border_b;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;250;-3920,2320;Inherit;False;248;border_r;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ParallaxOcclusionMappingNode;318;-2448,-1584;Inherit;False;0;8;False;;16;False;;2;0.02;0;False;1,1;False;0,0;11;0;FLOAT2;0,0;False;1;SAMPLER2D;;False;7;SAMPLERSTATE;;False;2;FLOAT;0.02;False;3;FLOAT3;0,0,0;False;8;INT;0;False;9;INT;0;False;10;INT;0;False;4;FLOAT;0;False;5;FLOAT2;0,0;False;6;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;111;-5335.35,704;Inherit;False;2356.246;544.4846;Lines;17;107;99;97;120;155;112;153;114;90;276;304;95;93;277;306;303;96;;1,1,1,1;0;0
Node;AmplifyShaderEditor.LerpOp;139;-3680,2176;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;194;-3600,3408;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;299;-2192,-1584;Inherit;False;parallax_uv;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;306;-5136,832;Inherit;False;48;uv;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;152;-3488,2176;Inherit;False;border_outline_outter;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;191;-3440,3408;Inherit;False;border_outline_inner;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;277;-5136,768;Inherit;False;299;parallax_uv;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;96;-5312,1088;Inherit;False;Property;_Float2;Float 2;8;0;Create;True;0;0;0;False;0;False;0;0.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;303;-5136,912;Inherit;False;248;border_r;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;304;-4928,768;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;175;-3392,2672;Inherit;False;Property;_BorderOutlineIntensity;Border Outline Intensity;13;0;Create;True;0;0;0;False;0;False;0.5;0.73;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;223;-3233.361,2382.111;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;95;-5120,1088;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;93;-4944,912;Inherit;False;Property;_LinesTilling;Lines Tilling;7;0;Create;True;0;0;0;False;0;False;0;0.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;276;-4720,768;Inherit;False;F_TillingAndOffset;-1;;18;992a7dac1d94f9a47be7a2d63002dd82;0;3;1;FLOAT2;0,0;False;3;FLOAT2;1,1;False;5;FLOAT2;0,0;False;3;FLOAT2;0;FLOAT;7;FLOAT;10
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;174;-3120,2576;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;90;-4480,752;Inherit;True;Property;_TextureSample1;Texture Sample 1;4;0;Create;True;0;0;0;False;0;False;-1;None;310c09f0f0650f045b5ce1d0d2f2c0ca;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RegisterLocalVarNode;173;-2935.004,2572.355;Inherit;False;border_outline;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;114;-4384,960;Inherit;False;Property;_LinesIntensity;Lines Intensity;9;0;Create;True;0;0;0;False;0;False;0,0;0.21,1.05;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.GetLocalVarNode;153;-3952,1008;Inherit;False;173;border_outline;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;112;-4080,752;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;155;-3712,752;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;52;-3392,-960;Inherit;False;1460.129;443.0146;Preserve Image Aspect Ratio;14;50;45;17;43;30;47;42;51;49;121;119;117;118;122;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;17;-3280,-640;Inherit;False;Property;_Size;Size;1;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;45;-3312,-768;Inherit;False;Property;_LocalScale;Local Scale;2;0;Create;True;0;0;0;False;0;False;1,1;1,0.7504568;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RegisterLocalVarNode;120;-3552,752;Inherit;False;lines;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;-3104,-672;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;43;-3104,-768;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;119;-2736,-592;Inherit;False;Property;_Float0;Float 0;10;0;Create;True;0;0;0;False;0;False;0;0.05;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;117;-2768,-672;Inherit;False;120;lines;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;42;-2928,-768;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;47;-2960,-656;Inherit;False;Property;_Offset;Offset;3;0;Create;True;0;0;0;False;0;False;1,1;0,0.1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;118;-2560,-640;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;50;-3264,-912;Inherit;False;299;parallax_uv;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;49;-2704,-912;Inherit;False;F_TillingAndOffset;-1;;25;992a7dac1d94f9a47be7a2d63002dd82;0;3;1;FLOAT2;0,0;False;3;FLOAT2;1,1;False;5;FLOAT2;0,0;False;3;FLOAT2;0;FLOAT;7;FLOAT;10
Node;AmplifyShaderEditor.DynamicAppendNode;122;-2352,-656;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;121;-2432,-912;Inherit;False;F_TillingAndOffset;-1;;26;992a7dac1d94f9a47be7a2d63002dd82;0;3;1;FLOAT2;0,0;False;3;FLOAT2;1,1;False;5;FLOAT2;0,0;False;3;FLOAT2;0;FLOAT;7;FLOAT;10
Node;AmplifyShaderEditor.GetLocalVarNode;324;-5360,208;Inherit;False;299;parallax_uv;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;61;-3008,-496;Inherit;False;1073.8;359.8666;Image;6;58;3;53;4;325;326;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;51;-2192,-912;Inherit;False;preserverd_image_uv;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;323;-5152,192;Inherit;True;Property;_BorderTex3;BorderTex;4;0;Create;True;0;0;0;False;0;False;-1;None;463354571c0f29942a3a22e023aa3f69;True;0;False;white;Auto;False;Instance;63;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.TexturePropertyNode;4;-2928,-448;Inherit;True;Property;_MainTex;MainTex;0;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.GetLocalVarNode;53;-2960,-256;Inherit;False;51;preserverd_image_uv;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;322;-4736,256;Inherit;False;border_g;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;3;-2688,-448;Inherit;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.GetLocalVarNode;326;-2432,-240;Inherit;False;322;border_g;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;325;-2320,-416;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;58;-2144,-416;Inherit;False;image;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;62;-3200,96;Inherit;False;58;image;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;65;-2976,96;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;87;-2816,96;Inherit;False;lerped_image;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;97;-3664,992;Inherit;False;87;lerped_image;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;99;-3328,752;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;110;-3920,1280;Inherit;False;969.4077;438.77;Alpha;5;84;106;86;105;109;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;107;-3200,752;Inherit;False;image_w_lines;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;109;-3728,1440;Inherit;False;107;image_w_lines;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;79;-1744.865,-896;Inherit;False;1362.411;757.1605;Apply Color;12;77;337;108;71;335;78;70;336;334;338;339;341;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SaturateNode;105;-3504,1440;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;86;-3632,1328;Inherit;False;Property;_Alpha;Alpha;5;0;Create;True;0;0;0;False;0;False;0.58;0.65;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;334;-1632,-624;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;336;-1696,-448;Inherit;False;Property;_VertexColorbginfluence;Vertex Color bg influence;17;0;Create;True;0;0;0;False;0;False;0;1.21;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;106;-3344,1328;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;339;-1504,-816;Inherit;False;Constant;_Color0;Color 0;18;0;Create;True;0;0;0;False;0;False;1,1,1,1;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;335;-1440,-624;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CeilOpNode;341;-1408,-448;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;80;-624,-32;Inherit;False;298.3685;342.7713;Alpha Mask;3;83;74;85;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;84;-3200,1360;Inherit;False;alpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;82;-3232,416;Inherit;False;alpha_mask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;70;-1088,-432;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;338;-1104,-816;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;78;-1088,-640;Inherit;False;Property;_BackgroundColor;Background Color;6;1;[HDR];Create;True;0;0;0;False;0;False;0.0754717,0.03773584,0.03773584,0;0.0625,0.05592105,0,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.GetLocalVarNode;83;-544,224;Inherit;False;82;alpha_mask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;85;-544,144;Inherit;False;84;alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;71;-896,-432;Inherit;False;Alpha Split;-1;;28;07dab7960105b86429ac8eebd729ed6d;0;1;2;COLOR;0,0,0,0;False;2;FLOAT3;0;FLOAT;6
Node;AmplifyShaderEditor.GetLocalVarNode;108;-896,-336;Inherit;False;107;image_w_lines;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;337;-753.5792,-730.6108;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;74;-512,16;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;77;-560,-752;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.DynamicAppendNode;64;-208,-272;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;60;64,-352;Inherit;False;result;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;59;-48,128;Inherit;False;60;result;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;54;176,16;Float;False;True;-1;3;AmplifyShaderEditor.MaterialInspector;0;3;S_Holo Level Cover;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;False;True;3;1;False;;10;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;True;_ColorMask;False;False;False;False;False;False;False;True;True;0;True;_Stencil;255;True;_StencilReadMask;255;True;_StencilWriteMask;0;True;_StencilComp;0;True;_StencilOp;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;0;True;unity_GUIZTestMode;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;210;0;243;0
WireConnection;244;0;243;0
WireConnection;245;0;243;0
WireConnection;246;0;210;0
WireConnection;281;0;279;0
WireConnection;319;0;279;1
WireConnection;48;0;6;0
WireConnection;238;0;176;0
WireConnection;204;1;245;0
WireConnection;209;1;210;0
WireConnection;219;0;244;0
WireConnection;218;0;246;0
WireConnection;63;0;281;0
WireConnection;63;7;319;0
WireConnection;237;0;176;0
WireConnection;239;0;238;0
WireConnection;184;0;176;0
WireConnection;268;1;269;0
WireConnection;268;5;204;0
WireConnection;270;1;271;0
WireConnection;270;5;209;0
WireConnection;272;1;273;0
WireConnection;272;5;219;0
WireConnection;274;1;275;0
WireConnection;274;5;218;0
WireConnection;248;0;63;1
WireConnection;240;0;176;0
WireConnection;241;0;184;0
WireConnection;201;1;268;0
WireConnection;207;1;270;0
WireConnection;215;1;272;0
WireConnection;214;1;274;0
WireConnection;183;0;239;0
WireConnection;183;1;184;0
WireConnection;188;0;184;0
WireConnection;188;1;237;0
WireConnection;206;0;252;0
WireConnection;206;1;201;1
WireConnection;211;0;254;0
WireConnection;211;1;207;1
WireConnection;221;0;256;0
WireConnection;221;1;215;1
WireConnection;222;0;258;0
WireConnection;222;1;214;1
WireConnection;262;1;263;0
WireConnection;262;5;241;0
WireConnection;260;1;261;0
WireConnection;260;5;240;0
WireConnection;264;1;265;0
WireConnection;264;5;183;0
WireConnection;266;1;267;0
WireConnection;266;5;188;0
WireConnection;124;1;260;0
WireConnection;125;1;262;0
WireConnection;146;1;264;0
WireConnection;147;1;266;0
WireConnection;213;0;206;0
WireConnection;213;1;211;0
WireConnection;213;2;221;0
WireConnection;213;3;222;0
WireConnection;249;0;63;3
WireConnection;328;0;330;1
WireConnection;329;0;330;2
WireConnection;331;0;330;3
WireConnection;224;0;213;0
WireConnection;145;0;124;1
WireConnection;145;1;125;1
WireConnection;145;2;146;1
WireConnection;145;3;147;1
WireConnection;318;0;332;0
WireConnection;318;1;282;0
WireConnection;318;7;320;0
WireConnection;318;2;284;0
WireConnection;318;3;321;0
WireConnection;318;8;328;0
WireConnection;318;9;329;0
WireConnection;318;10;331;0
WireConnection;139;1;145;0
WireConnection;139;2;250;0
WireConnection;194;0;224;0
WireConnection;194;2;251;0
WireConnection;299;0;318;0
WireConnection;152;0;139;0
WireConnection;191;0;194;0
WireConnection;304;0;277;0
WireConnection;304;1;306;0
WireConnection;304;2;303;0
WireConnection;223;0;152;0
WireConnection;223;1;191;0
WireConnection;95;0;96;0
WireConnection;276;1;304;0
WireConnection;276;3;93;0
WireConnection;276;5;95;0
WireConnection;174;0;223;0
WireConnection;174;1;175;0
WireConnection;90;1;276;0
WireConnection;173;0;174;0
WireConnection;112;0;90;1
WireConnection;112;1;114;1
WireConnection;112;2;114;2
WireConnection;155;0;112;0
WireConnection;155;1;153;0
WireConnection;120;0;155;0
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
WireConnection;323;1;324;0
WireConnection;322;0;323;2
WireConnection;3;0;4;0
WireConnection;3;1;53;0
WireConnection;325;1;3;1
WireConnection;325;2;326;0
WireConnection;58;0;325;0
WireConnection;65;0;62;0
WireConnection;65;2;248;0
WireConnection;87;0;65;0
WireConnection;99;0;120;0
WireConnection;99;1;97;0
WireConnection;107;0;99;0
WireConnection;105;0;109;0
WireConnection;106;0;86;0
WireConnection;106;2;105;0
WireConnection;335;0;334;0
WireConnection;335;1;336;0
WireConnection;341;0;336;0
WireConnection;84;0;106;0
WireConnection;82;0;63;4
WireConnection;338;0;339;0
WireConnection;338;1;335;0
WireConnection;338;2;341;0
WireConnection;71;2;70;0
WireConnection;337;0;338;0
WireConnection;337;1;78;0
WireConnection;74;1;85;0
WireConnection;74;2;83;0
WireConnection;77;0;337;0
WireConnection;77;1;71;0
WireConnection;77;2;108;0
WireConnection;64;0;77;0
WireConnection;64;3;74;0
WireConnection;60;0;64;0
WireConnection;54;0;59;0
ASEEND*/
//CHKSM=550CE4941CFC24F5C2114E010EC70B9D87E1E661