// Made with Amplify Shader Editor v1.9.8.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Game/FX/Pixel Scan v2"
{
	Properties
	{
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		_Range("Range", Float) = 4.94
		_Falloff("Falloff", Float) = 4.94
		_TextureSample0("Texture Sample 0", 2D) = "white" {}
		_Float0("Float 0", Float) = 0
		_Float3("Float 0", Float) = 0
		_Float4("Float 0", Float) = 0
		_DisplacementScale("Displacement Scale", Float) = 0
		_DisplacementStrength("Displacement Strength", Range( 0 , 1)) = 0
		_Blocks("Blocks", Int) = 0
		_Float6("Float 6", Float) = 0.5
		_Speed("Speed", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}


		//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		//_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		//_TessMin( "Tess Min Distance", Float ) = 10
		//_TessMax( "Tess Max Distance", Float ) = 25
		//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		//_TessMaxDisp( "Tess Max Displacement", Float ) = 25

		[HideInInspector] _QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector] _QueueControl("_QueueControl", Float) = -1

        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}

		[HideInInspector][ToggleOff] _ReceiveShadows("Receive Shadows", Float) = 1.0
	}

	SubShader
	{
		LOD 0

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "UniversalMaterialType"="Unlit" }

		Cull Back
		AlphaToMask Off

		

		HLSLINCLUDE
		#pragma target 4.5
		#pragma prefer_hlslcc gles
		// ensure rendering platforms toggle list is visible

		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

		#ifndef ASE_TESS_FUNCS
		#define ASE_TESS_FUNCS
		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}

		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
			return f;
		}

		float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
		{
			float4 tess;
			tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
			tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
			tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
			tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
			return tess;
		}

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}

		float4 EdgeLengthBasedTessCull( float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6] )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;

			if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
			{
				tess = 0.0f;
			}
			else
			{
				tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			}
			return tess;
		}
		#endif //ASE_TESS_FUNCS
		ENDHLSL

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForwardOnly" }

			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			ZWrite Off
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA

			

			HLSLPROGRAM

			

			#pragma multi_compile_fragment _ALPHATEST_ON
			#pragma shader_feature_local _RECEIVE_SHADOWS_OFF
			#pragma multi_compile_instancing
			#pragma instancing_options renderinglayer
			#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19801
			#define ASE_SRP_VERSION 140012


			

			#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3

			

			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
			#pragma multi_compile_fragment _ DEBUG_DISPLAY

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS SHADERPASS_UNLIT

			
            #if ASE_SRP_VERSION >=140007
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#endif
		

			
			#if ASE_SRP_VERSION >=140007
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#endif
		

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"

			
			#if ASE_SRP_VERSION >=140010
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#endif
		

			

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_FRAG_WORLD_POSITION


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 clipPosV : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				#if defined(ASE_FOG) || defined(_ADDITIONAL_LIGHTS_VERTEX)
					half4 fogFactorAndVertexLight : TEXCOORD2;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord : TEXCOORD3;
				#endif
				#if defined(LIGHTMAP_ON)
					float4 lightmapUVOrVertexSH : TEXCOORD4;
				#endif
				#if defined(DYNAMICLIGHTMAP_ON)
					float2 dynamicLightmapUV : TEXCOORD5;
				#endif
				float4 ase_texcoord6 : TEXCOORD6;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _TextureSample0_ST;
			float _Range;
			float _Falloff;
			int _Blocks;
			float _DisplacementScale;
			float _Speed;
			float _DisplacementStrength;
			float _Float6;
			float _Float0;
			float _Float3;
			float _Float4;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _TextureSample0;


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
			

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_texcoord6.xy = input.texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord6.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				#if defined(LIGHTMAP_ON)
					OUTPUT_LIGHTMAP_UV(input.texcoord1, unity_LightmapST, output.lightmapUVOrVertexSH.xy);
				#endif
				#if defined(DYNAMICLIGHTMAP_ON)
					output.dynamicLightmapUV.xy = input.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
				#endif

				#if defined(ASE_FOG) || defined(_ADDITIONAL_LIGHTS_VERTEX)
					output.fogFactorAndVertexLight = 0;
					#if defined(ASE_FOG) && !defined(_FOG_FRAGMENT)
						output.fogFactorAndVertexLight.x = ComputeFogFactor(vertexInput.positionCS.z);
					#endif
					#ifdef _ADDITIONAL_LIGHTS_VERTEX
						half3 vertexLight = VertexLighting( vertexInput.positionWS, normalInput.normalWS );
						output.fogFactorAndVertexLight.yzw = vertexLight;
					#endif
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					output.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				output.positionCS = vertexInput.positionCS;
				output.clipPosV = vertexInput.positionCS;
				output.positionWS = vertexInput.positionWS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag ( PackedVaryings input
						#ifdef ASE_DEPTH_WRITE_ON
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						#ifdef _WRITE_RENDERING_LAYERS
						, out float4 outRenderingLayers : SV_Target1
						#endif
						 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				float3 WorldPosition = input.positionWS;
				float3 WorldViewDirection = GetWorldSpaceNormalizeViewDir( WorldPosition );
				float4 ShadowCoords = float4( 0, 0, 0, 0 );
				float4 ClipPos = input.clipPosV;
				float4 ScreenPos = ComputeScreenPos( input.clipPosV );

				float2 NormalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = input.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float2 break50_g94 = float2( -0.5,0.9 );
				float temp_output_13_0_g103 = _Range;
				float3 temp_output_15_0_g95 = ( ( WorldPosition * float3( 1,1,1 ) ) + float3( 0,0,0 ) );
				float3 temp_cast_0 = _Blocks;
				float3 blocks_amount22_g95 = temp_cast_0;
				float3 blocks8_g98 = abs( blocks_amount22_g95 );
				float3 mosaicUV15_g98 = ( floor( ( temp_output_15_0_g95 * blocks8_g98 ) ) / blocks8_g98 );
				float3 result17_g98 = mosaicUV15_g98;
				float3 in_float3230_g101 = result17_g98;
				float3 in232_g101 = in_float3230_g101;
				float3 temp_cast_1 = (_DisplacementScale).xxx;
				float3 turb_scale23_g95 = temp_cast_1;
				float mulTime12 = _TimeParameters.x * _Speed;
				float3 temp_cast_2 = (mulTime12).xxx;
				float3 turb_offset24_g95 = temp_cast_2;
				float3 coords_mapped_float3177_g101 = (in_float3230_g101*turb_scale23_g95 + turb_offset24_g95);
				float3 coords26_g102 = coords_mapped_float3177_g101;
				float noise_s26_g95 = 1.0;
				float noise_scale205_g101 = noise_s26_g95;
				float scale25_g102 = noise_scale205_g101;
				float simplePerlin2D2_g102 = snoise( (coords26_g102).xy*scale25_g102 );
				simplePerlin2D2_g102 = simplePerlin2D2_g102*0.5 + 0.5;
				float simplePerlin2D5_g102 = snoise( (coords26_g102).yz*scale25_g102 );
				simplePerlin2D5_g102 = simplePerlin2D5_g102*0.5 + 0.5;
				float simplePerlin2D8_g102 = snoise( (coords26_g102).xz*scale25_g102 );
				simplePerlin2D8_g102 = simplePerlin2D8_g102*0.5 + 0.5;
				float3 appendResult23_g102 = (float3(simplePerlin2D2_g102 , simplePerlin2D5_g102 , simplePerlin2D8_g102));
				float3 temp_output_248_0_g101 = appendResult23_g102;
				float3 temp_cast_3 = (1.0).xxx;
				float3 noise118_g101 = saturate( ( ( saturate( temp_output_248_0_g101 ) * 2.0 ) - temp_cast_3 ) );
				float3 distortion_map27_g101 = noise118_g101;
				float3 temp_cast_4 = (-1.0).xxx;
				float turb_strenght25_g95 = _DisplacementStrength;
				float temp_output_20_0_g95 = saturate( turb_strenght25_g95 );
				float temp_output_6_0_g101 = temp_output_20_0_g95;
				float3 temp_cast_5 = (temp_output_6_0_g101).xxx;
				float3 distortedUV37_g101 = ( in232_g101 + saturate( (( distortion_map27_g101 * temp_output_6_0_g101 ) + (distortion_map27_g101 - temp_cast_4) * (temp_cast_5 - ( distortion_map27_g101 * temp_output_6_0_g101 )) / (float3( 1,1,1 ) - temp_cast_4)) ) );
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float dist19_g103 = distance( ( distortedUV37_g101 + ( temp_output_20_0_g95 * -0.5 ) ) , ase_objectPosition );
				float smoothstepResult22_g103 = smoothstep( temp_output_13_0_g103 , ( temp_output_13_0_g103 + ( 1.0 - _Falloff ) ) , dist19_g103);
				float mask29_g103 = ( 1.0 - smoothstepResult22_g103 );
				float revealMaskRaw44_g94 = ( 1.0 - saturate( mask29_g103 ) );
				float smoothstepResult48_g94 = smoothstep( break50_g94.x , break50_g94.y , revealMaskRaw44_g94);
				float temp_output_28_0 = saturate( smoothstepResult48_g94 );
				float2 break50_g84 = float2( -0.5,0.9 );
				float temp_output_13_0_g93 = _Range;
				float3 temp_output_15_0_g85 = ( ( (WorldPosition*1.0 + _Float6) * float3( 1,1,1 ) ) + float3( 0,0,0 ) );
				float3 temp_cast_6 = _Blocks;
				float3 blocks_amount22_g85 = temp_cast_6;
				float3 blocks8_g88 = abs( blocks_amount22_g85 );
				float3 mosaicUV15_g88 = ( floor( ( temp_output_15_0_g85 * blocks8_g88 ) ) / blocks8_g88 );
				float3 result17_g88 = mosaicUV15_g88;
				float3 in_float3230_g91 = result17_g88;
				float3 in232_g91 = in_float3230_g91;
				float3 temp_cast_7 = (_DisplacementScale).xxx;
				float3 turb_scale23_g85 = temp_cast_7;
				float3 temp_cast_8 = (mulTime12).xxx;
				float3 turb_offset24_g85 = temp_cast_8;
				float3 coords_mapped_float3177_g91 = (in_float3230_g91*turb_scale23_g85 + turb_offset24_g85);
				float3 coords26_g92 = coords_mapped_float3177_g91;
				float noise_s26_g85 = 1.0;
				float noise_scale205_g91 = noise_s26_g85;
				float scale25_g92 = noise_scale205_g91;
				float simplePerlin2D2_g92 = snoise( (coords26_g92).xy*scale25_g92 );
				simplePerlin2D2_g92 = simplePerlin2D2_g92*0.5 + 0.5;
				float simplePerlin2D5_g92 = snoise( (coords26_g92).yz*scale25_g92 );
				simplePerlin2D5_g92 = simplePerlin2D5_g92*0.5 + 0.5;
				float simplePerlin2D8_g92 = snoise( (coords26_g92).xz*scale25_g92 );
				simplePerlin2D8_g92 = simplePerlin2D8_g92*0.5 + 0.5;
				float3 appendResult23_g92 = (float3(simplePerlin2D2_g92 , simplePerlin2D5_g92 , simplePerlin2D8_g92));
				float3 temp_output_248_0_g91 = appendResult23_g92;
				float3 temp_cast_9 = (1.0).xxx;
				float3 noise118_g91 = saturate( ( ( saturate( temp_output_248_0_g91 ) * 2.0 ) - temp_cast_9 ) );
				float3 distortion_map27_g91 = noise118_g91;
				float3 temp_cast_10 = (-1.0).xxx;
				float turb_strenght25_g85 = _DisplacementStrength;
				float temp_output_20_0_g85 = saturate( turb_strenght25_g85 );
				float temp_output_6_0_g91 = temp_output_20_0_g85;
				float3 temp_cast_11 = (temp_output_6_0_g91).xxx;
				float3 distortedUV37_g91 = ( in232_g91 + saturate( (( distortion_map27_g91 * temp_output_6_0_g91 ) + (distortion_map27_g91 - temp_cast_10) * (temp_cast_11 - ( distortion_map27_g91 * temp_output_6_0_g91 )) / (float3( 1,1,1 ) - temp_cast_10)) ) );
				float dist19_g93 = distance( ( distortedUV37_g91 + ( temp_output_20_0_g85 * -0.5 ) ) , ase_objectPosition );
				float smoothstepResult22_g93 = smoothstep( temp_output_13_0_g93 , ( temp_output_13_0_g93 + ( 1.0 - _Falloff ) ) , dist19_g93);
				float mask29_g93 = ( 1.0 - smoothstepResult22_g93 );
				float revealMaskRaw44_g84 = ( 1.0 - saturate( mask29_g93 ) );
				float smoothstepResult48_g84 = smoothstep( break50_g84.x , break50_g84.y , revealMaskRaw44_g84);
				float temp_output_30_0 = saturate( smoothstepResult48_g84 );
				float lerpResult52 = lerp( 1.0 , ( temp_output_28_0 - temp_output_30_0 ) , step( temp_output_28_0 , 0.8 ));
				float pixel_effect44 = lerpResult52;
				float4 appendResult37 = (float4(step( pixel_effect44 , _Float0 ) , step( pixel_effect44 , _Float3 ) , step( pixel_effect44 , _Float4 ) , 0.0));
				float2 uv_TextureSample0 = input.ase_texcoord6.xy * _TextureSample0_ST.xy + _TextureSample0_ST.zw;
				float4 lerpResult16 = lerp( appendResult37 , float4( tex2D( _TextureSample0, uv_TextureSample0 ).rgb , 0.0 ) , pixel_effect44);
				
				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = lerpResult16.xyz;
				float Alpha = ceil( pixel_effect44 );
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;

				#ifdef ASE_DEPTH_WRITE_ON
					float DepthValue = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				InputData inputData = (InputData)0;
				inputData.positionWS = WorldPosition;
				inputData.viewDirectionWS = WorldViewDirection;

				#ifdef ASE_FOG
					inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.fogFactorAndVertexLight.x);
				#endif
				#ifdef _ADDITIONAL_LIGHTS_VERTEX
					inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
				#endif

				inputData.normalizedScreenSpaceUV = NormalizedScreenSpaceUV;

				#if defined(_DBUFFER)
					ApplyDecalToBaseColor(input.positionCS, Color);
				#endif

				#ifdef ASE_FOG
					#ifdef TERRAIN_SPLAT_ADDPASS
						Color.rgb = MixFogColor(Color.rgb, half3(0,0,0), inputData.fogCoord);
					#else
						Color.rgb = MixFog(Color.rgb, inputData.fogCoord);
					#endif
				#endif

				#ifdef ASE_DEPTH_WRITE_ON
					outputDepth = DepthValue;
				#endif

				#ifdef _WRITE_RENDERING_LAYERS
					uint renderingLayers = GetMeshRenderingLayer();
					outRenderingLayers = float4( EncodeMeshRenderingLayer( renderingLayers ), 0, 0, 0 );
				#endif

				return half4( Color, Alpha );
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }

			ZWrite On
			ZTest LEqual
			AlphaToMask Off
			ColorMask 0

			HLSLPROGRAM

			

			#pragma multi_compile _ALPHATEST_ON
			#pragma multi_compile_instancing
			#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
			#define ASE_FOG 1
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19801
			#define ASE_SRP_VERSION 140012


			

			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS SHADERPASS_SHADOWCASTER

			
            #if ASE_SRP_VERSION >=140007
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#endif
		

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_FRAG_WORLD_POSITION


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 clipPosV : TEXCOORD0;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 positionWS : TEXCOORD1;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord : TEXCOORD2;
				#endif
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _TextureSample0_ST;
			float _Range;
			float _Falloff;
			int _Blocks;
			float _DisplacementScale;
			float _Speed;
			float _DisplacementStrength;
			float _Float6;
			float _Float0;
			float _Float3;
			float _Float4;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			

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
			

			float3 _LightDirection;
			float3 _LightPosition;

			PackedVaryings VertexFunction( Attributes input )
			{
				PackedVaryings output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );

				

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				float3 positionWS = TransformObjectToWorld( input.positionOS.xyz );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					output.positionWS = positionWS;
				#endif

				float3 normalWS = TransformObjectToWorldDir(input.normalOS);

				#if _CASTING_PUNCTUAL_LIGHT_SHADOW
					float3 lightDirectionWS = normalize(_LightPosition - positionWS);
				#else
					float3 lightDirectionWS = _LightDirection;
				#endif

				float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

				#if UNITY_REVERSED_Z
					positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
				#else
					positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = positionCS;
					output.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				output.positionCS = positionCS;
				output.clipPosV = positionCS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input
						#ifdef ASE_DEPTH_WRITE_ON
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( input );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 WorldPosition = input.positionWS;
				#endif

				float4 ShadowCoords = float4( 0, 0, 0, 0 );
				float4 ClipPos = input.clipPosV;
				float4 ScreenPos = ComputeScreenPos( input.clipPosV );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = input.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float2 break50_g94 = float2( -0.5,0.9 );
				float temp_output_13_0_g103 = _Range;
				float3 temp_output_15_0_g95 = ( ( WorldPosition * float3( 1,1,1 ) ) + float3( 0,0,0 ) );
				float3 temp_cast_0 = _Blocks;
				float3 blocks_amount22_g95 = temp_cast_0;
				float3 blocks8_g98 = abs( blocks_amount22_g95 );
				float3 mosaicUV15_g98 = ( floor( ( temp_output_15_0_g95 * blocks8_g98 ) ) / blocks8_g98 );
				float3 result17_g98 = mosaicUV15_g98;
				float3 in_float3230_g101 = result17_g98;
				float3 in232_g101 = in_float3230_g101;
				float3 temp_cast_1 = (_DisplacementScale).xxx;
				float3 turb_scale23_g95 = temp_cast_1;
				float mulTime12 = _TimeParameters.x * _Speed;
				float3 temp_cast_2 = (mulTime12).xxx;
				float3 turb_offset24_g95 = temp_cast_2;
				float3 coords_mapped_float3177_g101 = (in_float3230_g101*turb_scale23_g95 + turb_offset24_g95);
				float3 coords26_g102 = coords_mapped_float3177_g101;
				float noise_s26_g95 = 1.0;
				float noise_scale205_g101 = noise_s26_g95;
				float scale25_g102 = noise_scale205_g101;
				float simplePerlin2D2_g102 = snoise( (coords26_g102).xy*scale25_g102 );
				simplePerlin2D2_g102 = simplePerlin2D2_g102*0.5 + 0.5;
				float simplePerlin2D5_g102 = snoise( (coords26_g102).yz*scale25_g102 );
				simplePerlin2D5_g102 = simplePerlin2D5_g102*0.5 + 0.5;
				float simplePerlin2D8_g102 = snoise( (coords26_g102).xz*scale25_g102 );
				simplePerlin2D8_g102 = simplePerlin2D8_g102*0.5 + 0.5;
				float3 appendResult23_g102 = (float3(simplePerlin2D2_g102 , simplePerlin2D5_g102 , simplePerlin2D8_g102));
				float3 temp_output_248_0_g101 = appendResult23_g102;
				float3 temp_cast_3 = (1.0).xxx;
				float3 noise118_g101 = saturate( ( ( saturate( temp_output_248_0_g101 ) * 2.0 ) - temp_cast_3 ) );
				float3 distortion_map27_g101 = noise118_g101;
				float3 temp_cast_4 = (-1.0).xxx;
				float turb_strenght25_g95 = _DisplacementStrength;
				float temp_output_20_0_g95 = saturate( turb_strenght25_g95 );
				float temp_output_6_0_g101 = temp_output_20_0_g95;
				float3 temp_cast_5 = (temp_output_6_0_g101).xxx;
				float3 distortedUV37_g101 = ( in232_g101 + saturate( (( distortion_map27_g101 * temp_output_6_0_g101 ) + (distortion_map27_g101 - temp_cast_4) * (temp_cast_5 - ( distortion_map27_g101 * temp_output_6_0_g101 )) / (float3( 1,1,1 ) - temp_cast_4)) ) );
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float dist19_g103 = distance( ( distortedUV37_g101 + ( temp_output_20_0_g95 * -0.5 ) ) , ase_objectPosition );
				float smoothstepResult22_g103 = smoothstep( temp_output_13_0_g103 , ( temp_output_13_0_g103 + ( 1.0 - _Falloff ) ) , dist19_g103);
				float mask29_g103 = ( 1.0 - smoothstepResult22_g103 );
				float revealMaskRaw44_g94 = ( 1.0 - saturate( mask29_g103 ) );
				float smoothstepResult48_g94 = smoothstep( break50_g94.x , break50_g94.y , revealMaskRaw44_g94);
				float temp_output_28_0 = saturate( smoothstepResult48_g94 );
				float2 break50_g84 = float2( -0.5,0.9 );
				float temp_output_13_0_g93 = _Range;
				float3 temp_output_15_0_g85 = ( ( (WorldPosition*1.0 + _Float6) * float3( 1,1,1 ) ) + float3( 0,0,0 ) );
				float3 temp_cast_6 = _Blocks;
				float3 blocks_amount22_g85 = temp_cast_6;
				float3 blocks8_g88 = abs( blocks_amount22_g85 );
				float3 mosaicUV15_g88 = ( floor( ( temp_output_15_0_g85 * blocks8_g88 ) ) / blocks8_g88 );
				float3 result17_g88 = mosaicUV15_g88;
				float3 in_float3230_g91 = result17_g88;
				float3 in232_g91 = in_float3230_g91;
				float3 temp_cast_7 = (_DisplacementScale).xxx;
				float3 turb_scale23_g85 = temp_cast_7;
				float3 temp_cast_8 = (mulTime12).xxx;
				float3 turb_offset24_g85 = temp_cast_8;
				float3 coords_mapped_float3177_g91 = (in_float3230_g91*turb_scale23_g85 + turb_offset24_g85);
				float3 coords26_g92 = coords_mapped_float3177_g91;
				float noise_s26_g85 = 1.0;
				float noise_scale205_g91 = noise_s26_g85;
				float scale25_g92 = noise_scale205_g91;
				float simplePerlin2D2_g92 = snoise( (coords26_g92).xy*scale25_g92 );
				simplePerlin2D2_g92 = simplePerlin2D2_g92*0.5 + 0.5;
				float simplePerlin2D5_g92 = snoise( (coords26_g92).yz*scale25_g92 );
				simplePerlin2D5_g92 = simplePerlin2D5_g92*0.5 + 0.5;
				float simplePerlin2D8_g92 = snoise( (coords26_g92).xz*scale25_g92 );
				simplePerlin2D8_g92 = simplePerlin2D8_g92*0.5 + 0.5;
				float3 appendResult23_g92 = (float3(simplePerlin2D2_g92 , simplePerlin2D5_g92 , simplePerlin2D8_g92));
				float3 temp_output_248_0_g91 = appendResult23_g92;
				float3 temp_cast_9 = (1.0).xxx;
				float3 noise118_g91 = saturate( ( ( saturate( temp_output_248_0_g91 ) * 2.0 ) - temp_cast_9 ) );
				float3 distortion_map27_g91 = noise118_g91;
				float3 temp_cast_10 = (-1.0).xxx;
				float turb_strenght25_g85 = _DisplacementStrength;
				float temp_output_20_0_g85 = saturate( turb_strenght25_g85 );
				float temp_output_6_0_g91 = temp_output_20_0_g85;
				float3 temp_cast_11 = (temp_output_6_0_g91).xxx;
				float3 distortedUV37_g91 = ( in232_g91 + saturate( (( distortion_map27_g91 * temp_output_6_0_g91 ) + (distortion_map27_g91 - temp_cast_10) * (temp_cast_11 - ( distortion_map27_g91 * temp_output_6_0_g91 )) / (float3( 1,1,1 ) - temp_cast_10)) ) );
				float dist19_g93 = distance( ( distortedUV37_g91 + ( temp_output_20_0_g85 * -0.5 ) ) , ase_objectPosition );
				float smoothstepResult22_g93 = smoothstep( temp_output_13_0_g93 , ( temp_output_13_0_g93 + ( 1.0 - _Falloff ) ) , dist19_g93);
				float mask29_g93 = ( 1.0 - smoothstepResult22_g93 );
				float revealMaskRaw44_g84 = ( 1.0 - saturate( mask29_g93 ) );
				float smoothstepResult48_g84 = smoothstep( break50_g84.x , break50_g84.y , revealMaskRaw44_g84);
				float temp_output_30_0 = saturate( smoothstepResult48_g84 );
				float lerpResult52 = lerp( 1.0 , ( temp_output_28_0 - temp_output_30_0 ) , step( temp_output_28_0 , 0.8 ));
				float pixel_effect44 = lerpResult52;
				

				float Alpha = ceil( pixel_effect44 );
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;

				#ifdef ASE_DEPTH_WRITE_ON
					float DepthValue = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					#ifdef _ALPHATEST_SHADOW_ON
						clip(Alpha - AlphaClipThresholdShadow);
					#else
						clip(Alpha - AlphaClipThreshold);
					#endif
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#ifdef ASE_DEPTH_WRITE_ON
					outputDepth = DepthValue;
				#endif

				return 0;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask R
			AlphaToMask Off

			HLSLPROGRAM

			

			#pragma multi_compile _ALPHATEST_ON
			#pragma multi_compile_instancing
			#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
			#define ASE_FOG 1
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19801
			#define ASE_SRP_VERSION 140012


			

			#pragma vertex vert
			#pragma fragment frag

			
            #if ASE_SRP_VERSION >=140007
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#endif
		

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_FRAG_WORLD_POSITION


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 clipPosV : TEXCOORD0;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 positionWS : TEXCOORD1;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord : TEXCOORD2;
				#endif
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _TextureSample0_ST;
			float _Range;
			float _Falloff;
			int _Blocks;
			float _DisplacementScale;
			float _Speed;
			float _DisplacementStrength;
			float _Float6;
			float _Float0;
			float _Float3;
			float _Float4;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			

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
			

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					output.positionWS = vertexInput.positionWS;
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					output.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				output.positionCS = vertexInput.positionCS;
				output.clipPosV = vertexInput.positionCS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input
						#ifdef ASE_DEPTH_WRITE_ON
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = input.positionWS;
				#endif

				float4 ShadowCoords = float4( 0, 0, 0, 0 );
				float4 ClipPos = input.clipPosV;
				float4 ScreenPos = ComputeScreenPos( input.clipPosV );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = input.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float2 break50_g94 = float2( -0.5,0.9 );
				float temp_output_13_0_g103 = _Range;
				float3 temp_output_15_0_g95 = ( ( WorldPosition * float3( 1,1,1 ) ) + float3( 0,0,0 ) );
				float3 temp_cast_0 = _Blocks;
				float3 blocks_amount22_g95 = temp_cast_0;
				float3 blocks8_g98 = abs( blocks_amount22_g95 );
				float3 mosaicUV15_g98 = ( floor( ( temp_output_15_0_g95 * blocks8_g98 ) ) / blocks8_g98 );
				float3 result17_g98 = mosaicUV15_g98;
				float3 in_float3230_g101 = result17_g98;
				float3 in232_g101 = in_float3230_g101;
				float3 temp_cast_1 = (_DisplacementScale).xxx;
				float3 turb_scale23_g95 = temp_cast_1;
				float mulTime12 = _TimeParameters.x * _Speed;
				float3 temp_cast_2 = (mulTime12).xxx;
				float3 turb_offset24_g95 = temp_cast_2;
				float3 coords_mapped_float3177_g101 = (in_float3230_g101*turb_scale23_g95 + turb_offset24_g95);
				float3 coords26_g102 = coords_mapped_float3177_g101;
				float noise_s26_g95 = 1.0;
				float noise_scale205_g101 = noise_s26_g95;
				float scale25_g102 = noise_scale205_g101;
				float simplePerlin2D2_g102 = snoise( (coords26_g102).xy*scale25_g102 );
				simplePerlin2D2_g102 = simplePerlin2D2_g102*0.5 + 0.5;
				float simplePerlin2D5_g102 = snoise( (coords26_g102).yz*scale25_g102 );
				simplePerlin2D5_g102 = simplePerlin2D5_g102*0.5 + 0.5;
				float simplePerlin2D8_g102 = snoise( (coords26_g102).xz*scale25_g102 );
				simplePerlin2D8_g102 = simplePerlin2D8_g102*0.5 + 0.5;
				float3 appendResult23_g102 = (float3(simplePerlin2D2_g102 , simplePerlin2D5_g102 , simplePerlin2D8_g102));
				float3 temp_output_248_0_g101 = appendResult23_g102;
				float3 temp_cast_3 = (1.0).xxx;
				float3 noise118_g101 = saturate( ( ( saturate( temp_output_248_0_g101 ) * 2.0 ) - temp_cast_3 ) );
				float3 distortion_map27_g101 = noise118_g101;
				float3 temp_cast_4 = (-1.0).xxx;
				float turb_strenght25_g95 = _DisplacementStrength;
				float temp_output_20_0_g95 = saturate( turb_strenght25_g95 );
				float temp_output_6_0_g101 = temp_output_20_0_g95;
				float3 temp_cast_5 = (temp_output_6_0_g101).xxx;
				float3 distortedUV37_g101 = ( in232_g101 + saturate( (( distortion_map27_g101 * temp_output_6_0_g101 ) + (distortion_map27_g101 - temp_cast_4) * (temp_cast_5 - ( distortion_map27_g101 * temp_output_6_0_g101 )) / (float3( 1,1,1 ) - temp_cast_4)) ) );
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float dist19_g103 = distance( ( distortedUV37_g101 + ( temp_output_20_0_g95 * -0.5 ) ) , ase_objectPosition );
				float smoothstepResult22_g103 = smoothstep( temp_output_13_0_g103 , ( temp_output_13_0_g103 + ( 1.0 - _Falloff ) ) , dist19_g103);
				float mask29_g103 = ( 1.0 - smoothstepResult22_g103 );
				float revealMaskRaw44_g94 = ( 1.0 - saturate( mask29_g103 ) );
				float smoothstepResult48_g94 = smoothstep( break50_g94.x , break50_g94.y , revealMaskRaw44_g94);
				float temp_output_28_0 = saturate( smoothstepResult48_g94 );
				float2 break50_g84 = float2( -0.5,0.9 );
				float temp_output_13_0_g93 = _Range;
				float3 temp_output_15_0_g85 = ( ( (WorldPosition*1.0 + _Float6) * float3( 1,1,1 ) ) + float3( 0,0,0 ) );
				float3 temp_cast_6 = _Blocks;
				float3 blocks_amount22_g85 = temp_cast_6;
				float3 blocks8_g88 = abs( blocks_amount22_g85 );
				float3 mosaicUV15_g88 = ( floor( ( temp_output_15_0_g85 * blocks8_g88 ) ) / blocks8_g88 );
				float3 result17_g88 = mosaicUV15_g88;
				float3 in_float3230_g91 = result17_g88;
				float3 in232_g91 = in_float3230_g91;
				float3 temp_cast_7 = (_DisplacementScale).xxx;
				float3 turb_scale23_g85 = temp_cast_7;
				float3 temp_cast_8 = (mulTime12).xxx;
				float3 turb_offset24_g85 = temp_cast_8;
				float3 coords_mapped_float3177_g91 = (in_float3230_g91*turb_scale23_g85 + turb_offset24_g85);
				float3 coords26_g92 = coords_mapped_float3177_g91;
				float noise_s26_g85 = 1.0;
				float noise_scale205_g91 = noise_s26_g85;
				float scale25_g92 = noise_scale205_g91;
				float simplePerlin2D2_g92 = snoise( (coords26_g92).xy*scale25_g92 );
				simplePerlin2D2_g92 = simplePerlin2D2_g92*0.5 + 0.5;
				float simplePerlin2D5_g92 = snoise( (coords26_g92).yz*scale25_g92 );
				simplePerlin2D5_g92 = simplePerlin2D5_g92*0.5 + 0.5;
				float simplePerlin2D8_g92 = snoise( (coords26_g92).xz*scale25_g92 );
				simplePerlin2D8_g92 = simplePerlin2D8_g92*0.5 + 0.5;
				float3 appendResult23_g92 = (float3(simplePerlin2D2_g92 , simplePerlin2D5_g92 , simplePerlin2D8_g92));
				float3 temp_output_248_0_g91 = appendResult23_g92;
				float3 temp_cast_9 = (1.0).xxx;
				float3 noise118_g91 = saturate( ( ( saturate( temp_output_248_0_g91 ) * 2.0 ) - temp_cast_9 ) );
				float3 distortion_map27_g91 = noise118_g91;
				float3 temp_cast_10 = (-1.0).xxx;
				float turb_strenght25_g85 = _DisplacementStrength;
				float temp_output_20_0_g85 = saturate( turb_strenght25_g85 );
				float temp_output_6_0_g91 = temp_output_20_0_g85;
				float3 temp_cast_11 = (temp_output_6_0_g91).xxx;
				float3 distortedUV37_g91 = ( in232_g91 + saturate( (( distortion_map27_g91 * temp_output_6_0_g91 ) + (distortion_map27_g91 - temp_cast_10) * (temp_cast_11 - ( distortion_map27_g91 * temp_output_6_0_g91 )) / (float3( 1,1,1 ) - temp_cast_10)) ) );
				float dist19_g93 = distance( ( distortedUV37_g91 + ( temp_output_20_0_g85 * -0.5 ) ) , ase_objectPosition );
				float smoothstepResult22_g93 = smoothstep( temp_output_13_0_g93 , ( temp_output_13_0_g93 + ( 1.0 - _Falloff ) ) , dist19_g93);
				float mask29_g93 = ( 1.0 - smoothstepResult22_g93 );
				float revealMaskRaw44_g84 = ( 1.0 - saturate( mask29_g93 ) );
				float smoothstepResult48_g84 = smoothstep( break50_g84.x , break50_g84.y , revealMaskRaw44_g84);
				float temp_output_30_0 = saturate( smoothstepResult48_g84 );
				float lerpResult52 = lerp( 1.0 , ( temp_output_28_0 - temp_output_30_0 ) , step( temp_output_28_0 , 0.8 ));
				float pixel_effect44 = lerpResult52;
				

				float Alpha = ceil( pixel_effect44 );
				float AlphaClipThreshold = 0.5;

				#ifdef ASE_DEPTH_WRITE_ON
					float DepthValue = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#ifdef ASE_DEPTH_WRITE_ON
					outputDepth = DepthValue;
				#endif

				return 0;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "SceneSelectionPass"
			Tags { "LightMode"="SceneSelectionPass" }

			Cull Off
			AlphaToMask Off

			HLSLPROGRAM

			

			#define ASE_FOG 1
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19801
			#define ASE_SRP_VERSION 140012


			

			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define SHADERPASS SHADERPASS_DEPTHONLY

			
            #if ASE_SRP_VERSION >=140007
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#endif
		

			
			#if ASE_SRP_VERSION >=140007
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#endif
		

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"

			
			#if ASE_SRP_VERSION >=140010
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#endif
		

			

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _TextureSample0_ST;
			float _Range;
			float _Falloff;
			int _Blocks;
			float _DisplacementScale;
			float _Speed;
			float _DisplacementStrength;
			float _Float6;
			float _Float0;
			float _Float3;
			float _Float4;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			

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
			

			int _ObjectId;
			int _PassValue;

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction(Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 ase_positionWS = TransformObjectToWorld( ( input.positionOS ).xyz );
				output.ase_texcoord.xyz = ase_positionWS;
				
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord.w = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				float3 positionWS = TransformObjectToWorld( input.positionOS.xyz );

				output.positionCS = TransformWorldToHClip(positionWS);

				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input ) : SV_Target
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float2 break50_g94 = float2( -0.5,0.9 );
				float temp_output_13_0_g103 = _Range;
				float3 ase_positionWS = input.ase_texcoord.xyz;
				float3 temp_output_15_0_g95 = ( ( ase_positionWS * float3( 1,1,1 ) ) + float3( 0,0,0 ) );
				float3 temp_cast_0 = _Blocks;
				float3 blocks_amount22_g95 = temp_cast_0;
				float3 blocks8_g98 = abs( blocks_amount22_g95 );
				float3 mosaicUV15_g98 = ( floor( ( temp_output_15_0_g95 * blocks8_g98 ) ) / blocks8_g98 );
				float3 result17_g98 = mosaicUV15_g98;
				float3 in_float3230_g101 = result17_g98;
				float3 in232_g101 = in_float3230_g101;
				float3 temp_cast_1 = (_DisplacementScale).xxx;
				float3 turb_scale23_g95 = temp_cast_1;
				float mulTime12 = _TimeParameters.x * _Speed;
				float3 temp_cast_2 = (mulTime12).xxx;
				float3 turb_offset24_g95 = temp_cast_2;
				float3 coords_mapped_float3177_g101 = (in_float3230_g101*turb_scale23_g95 + turb_offset24_g95);
				float3 coords26_g102 = coords_mapped_float3177_g101;
				float noise_s26_g95 = 1.0;
				float noise_scale205_g101 = noise_s26_g95;
				float scale25_g102 = noise_scale205_g101;
				float simplePerlin2D2_g102 = snoise( (coords26_g102).xy*scale25_g102 );
				simplePerlin2D2_g102 = simplePerlin2D2_g102*0.5 + 0.5;
				float simplePerlin2D5_g102 = snoise( (coords26_g102).yz*scale25_g102 );
				simplePerlin2D5_g102 = simplePerlin2D5_g102*0.5 + 0.5;
				float simplePerlin2D8_g102 = snoise( (coords26_g102).xz*scale25_g102 );
				simplePerlin2D8_g102 = simplePerlin2D8_g102*0.5 + 0.5;
				float3 appendResult23_g102 = (float3(simplePerlin2D2_g102 , simplePerlin2D5_g102 , simplePerlin2D8_g102));
				float3 temp_output_248_0_g101 = appendResult23_g102;
				float3 temp_cast_3 = (1.0).xxx;
				float3 noise118_g101 = saturate( ( ( saturate( temp_output_248_0_g101 ) * 2.0 ) - temp_cast_3 ) );
				float3 distortion_map27_g101 = noise118_g101;
				float3 temp_cast_4 = (-1.0).xxx;
				float turb_strenght25_g95 = _DisplacementStrength;
				float temp_output_20_0_g95 = saturate( turb_strenght25_g95 );
				float temp_output_6_0_g101 = temp_output_20_0_g95;
				float3 temp_cast_5 = (temp_output_6_0_g101).xxx;
				float3 distortedUV37_g101 = ( in232_g101 + saturate( (( distortion_map27_g101 * temp_output_6_0_g101 ) + (distortion_map27_g101 - temp_cast_4) * (temp_cast_5 - ( distortion_map27_g101 * temp_output_6_0_g101 )) / (float3( 1,1,1 ) - temp_cast_4)) ) );
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float dist19_g103 = distance( ( distortedUV37_g101 + ( temp_output_20_0_g95 * -0.5 ) ) , ase_objectPosition );
				float smoothstepResult22_g103 = smoothstep( temp_output_13_0_g103 , ( temp_output_13_0_g103 + ( 1.0 - _Falloff ) ) , dist19_g103);
				float mask29_g103 = ( 1.0 - smoothstepResult22_g103 );
				float revealMaskRaw44_g94 = ( 1.0 - saturate( mask29_g103 ) );
				float smoothstepResult48_g94 = smoothstep( break50_g94.x , break50_g94.y , revealMaskRaw44_g94);
				float temp_output_28_0 = saturate( smoothstepResult48_g94 );
				float2 break50_g84 = float2( -0.5,0.9 );
				float temp_output_13_0_g93 = _Range;
				float3 temp_output_15_0_g85 = ( ( (ase_positionWS*1.0 + _Float6) * float3( 1,1,1 ) ) + float3( 0,0,0 ) );
				float3 temp_cast_6 = _Blocks;
				float3 blocks_amount22_g85 = temp_cast_6;
				float3 blocks8_g88 = abs( blocks_amount22_g85 );
				float3 mosaicUV15_g88 = ( floor( ( temp_output_15_0_g85 * blocks8_g88 ) ) / blocks8_g88 );
				float3 result17_g88 = mosaicUV15_g88;
				float3 in_float3230_g91 = result17_g88;
				float3 in232_g91 = in_float3230_g91;
				float3 temp_cast_7 = (_DisplacementScale).xxx;
				float3 turb_scale23_g85 = temp_cast_7;
				float3 temp_cast_8 = (mulTime12).xxx;
				float3 turb_offset24_g85 = temp_cast_8;
				float3 coords_mapped_float3177_g91 = (in_float3230_g91*turb_scale23_g85 + turb_offset24_g85);
				float3 coords26_g92 = coords_mapped_float3177_g91;
				float noise_s26_g85 = 1.0;
				float noise_scale205_g91 = noise_s26_g85;
				float scale25_g92 = noise_scale205_g91;
				float simplePerlin2D2_g92 = snoise( (coords26_g92).xy*scale25_g92 );
				simplePerlin2D2_g92 = simplePerlin2D2_g92*0.5 + 0.5;
				float simplePerlin2D5_g92 = snoise( (coords26_g92).yz*scale25_g92 );
				simplePerlin2D5_g92 = simplePerlin2D5_g92*0.5 + 0.5;
				float simplePerlin2D8_g92 = snoise( (coords26_g92).xz*scale25_g92 );
				simplePerlin2D8_g92 = simplePerlin2D8_g92*0.5 + 0.5;
				float3 appendResult23_g92 = (float3(simplePerlin2D2_g92 , simplePerlin2D5_g92 , simplePerlin2D8_g92));
				float3 temp_output_248_0_g91 = appendResult23_g92;
				float3 temp_cast_9 = (1.0).xxx;
				float3 noise118_g91 = saturate( ( ( saturate( temp_output_248_0_g91 ) * 2.0 ) - temp_cast_9 ) );
				float3 distortion_map27_g91 = noise118_g91;
				float3 temp_cast_10 = (-1.0).xxx;
				float turb_strenght25_g85 = _DisplacementStrength;
				float temp_output_20_0_g85 = saturate( turb_strenght25_g85 );
				float temp_output_6_0_g91 = temp_output_20_0_g85;
				float3 temp_cast_11 = (temp_output_6_0_g91).xxx;
				float3 distortedUV37_g91 = ( in232_g91 + saturate( (( distortion_map27_g91 * temp_output_6_0_g91 ) + (distortion_map27_g91 - temp_cast_10) * (temp_cast_11 - ( distortion_map27_g91 * temp_output_6_0_g91 )) / (float3( 1,1,1 ) - temp_cast_10)) ) );
				float dist19_g93 = distance( ( distortedUV37_g91 + ( temp_output_20_0_g85 * -0.5 ) ) , ase_objectPosition );
				float smoothstepResult22_g93 = smoothstep( temp_output_13_0_g93 , ( temp_output_13_0_g93 + ( 1.0 - _Falloff ) ) , dist19_g93);
				float mask29_g93 = ( 1.0 - smoothstepResult22_g93 );
				float revealMaskRaw44_g84 = ( 1.0 - saturate( mask29_g93 ) );
				float smoothstepResult48_g84 = smoothstep( break50_g84.x , break50_g84.y , revealMaskRaw44_g84);
				float temp_output_30_0 = saturate( smoothstepResult48_g84 );
				float lerpResult52 = lerp( 1.0 , ( temp_output_28_0 - temp_output_30_0 ) , step( temp_output_28_0 , 0.8 ));
				float pixel_effect44 = lerpResult52;
				

				surfaceDescription.Alpha = ceil( pixel_effect44 );
				surfaceDescription.AlphaClipThreshold = 0.5;

				#if _ALPHATEST_ON
					float alphaClipThreshold = 0.01f;
					#if ALPHA_CLIP_THRESHOLD
						alphaClipThreshold = surfaceDescription.AlphaClipThreshold;
					#endif
					clip(surfaceDescription.Alpha - alphaClipThreshold);
				#endif

				half4 outColor = half4(_ObjectId, _PassValue, 1.0, 1.0);
				return outColor;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "ScenePickingPass"
			Tags { "LightMode"="Picking" }

			AlphaToMask Off

			HLSLPROGRAM

			

			#define ASE_FOG 1
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19801
			#define ASE_SRP_VERSION 140012


			

			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT

			#define SHADERPASS SHADERPASS_DEPTHONLY

			
            #if ASE_SRP_VERSION >=140007
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#endif
		

			
			#if ASE_SRP_VERSION >=140007
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#endif
		

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"

			
			#if ASE_SRP_VERSION >=140010
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#endif
		

			

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _TextureSample0_ST;
			float _Range;
			float _Falloff;
			int _Blocks;
			float _DisplacementScale;
			float _Speed;
			float _DisplacementStrength;
			float _Float6;
			float _Float0;
			float _Float3;
			float _Float4;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			

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
			

			float4 _SelectionID;

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction(Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 ase_positionWS = TransformObjectToWorld( ( input.positionOS ).xyz );
				output.ase_texcoord.xyz = ase_positionWS;
				
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord.w = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				float3 positionWS = TransformObjectToWorld( input.positionOS.xyz );
				output.positionCS = TransformWorldToHClip(positionWS);
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input ) : SV_Target
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float2 break50_g94 = float2( -0.5,0.9 );
				float temp_output_13_0_g103 = _Range;
				float3 ase_positionWS = input.ase_texcoord.xyz;
				float3 temp_output_15_0_g95 = ( ( ase_positionWS * float3( 1,1,1 ) ) + float3( 0,0,0 ) );
				float3 temp_cast_0 = _Blocks;
				float3 blocks_amount22_g95 = temp_cast_0;
				float3 blocks8_g98 = abs( blocks_amount22_g95 );
				float3 mosaicUV15_g98 = ( floor( ( temp_output_15_0_g95 * blocks8_g98 ) ) / blocks8_g98 );
				float3 result17_g98 = mosaicUV15_g98;
				float3 in_float3230_g101 = result17_g98;
				float3 in232_g101 = in_float3230_g101;
				float3 temp_cast_1 = (_DisplacementScale).xxx;
				float3 turb_scale23_g95 = temp_cast_1;
				float mulTime12 = _TimeParameters.x * _Speed;
				float3 temp_cast_2 = (mulTime12).xxx;
				float3 turb_offset24_g95 = temp_cast_2;
				float3 coords_mapped_float3177_g101 = (in_float3230_g101*turb_scale23_g95 + turb_offset24_g95);
				float3 coords26_g102 = coords_mapped_float3177_g101;
				float noise_s26_g95 = 1.0;
				float noise_scale205_g101 = noise_s26_g95;
				float scale25_g102 = noise_scale205_g101;
				float simplePerlin2D2_g102 = snoise( (coords26_g102).xy*scale25_g102 );
				simplePerlin2D2_g102 = simplePerlin2D2_g102*0.5 + 0.5;
				float simplePerlin2D5_g102 = snoise( (coords26_g102).yz*scale25_g102 );
				simplePerlin2D5_g102 = simplePerlin2D5_g102*0.5 + 0.5;
				float simplePerlin2D8_g102 = snoise( (coords26_g102).xz*scale25_g102 );
				simplePerlin2D8_g102 = simplePerlin2D8_g102*0.5 + 0.5;
				float3 appendResult23_g102 = (float3(simplePerlin2D2_g102 , simplePerlin2D5_g102 , simplePerlin2D8_g102));
				float3 temp_output_248_0_g101 = appendResult23_g102;
				float3 temp_cast_3 = (1.0).xxx;
				float3 noise118_g101 = saturate( ( ( saturate( temp_output_248_0_g101 ) * 2.0 ) - temp_cast_3 ) );
				float3 distortion_map27_g101 = noise118_g101;
				float3 temp_cast_4 = (-1.0).xxx;
				float turb_strenght25_g95 = _DisplacementStrength;
				float temp_output_20_0_g95 = saturate( turb_strenght25_g95 );
				float temp_output_6_0_g101 = temp_output_20_0_g95;
				float3 temp_cast_5 = (temp_output_6_0_g101).xxx;
				float3 distortedUV37_g101 = ( in232_g101 + saturate( (( distortion_map27_g101 * temp_output_6_0_g101 ) + (distortion_map27_g101 - temp_cast_4) * (temp_cast_5 - ( distortion_map27_g101 * temp_output_6_0_g101 )) / (float3( 1,1,1 ) - temp_cast_4)) ) );
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float dist19_g103 = distance( ( distortedUV37_g101 + ( temp_output_20_0_g95 * -0.5 ) ) , ase_objectPosition );
				float smoothstepResult22_g103 = smoothstep( temp_output_13_0_g103 , ( temp_output_13_0_g103 + ( 1.0 - _Falloff ) ) , dist19_g103);
				float mask29_g103 = ( 1.0 - smoothstepResult22_g103 );
				float revealMaskRaw44_g94 = ( 1.0 - saturate( mask29_g103 ) );
				float smoothstepResult48_g94 = smoothstep( break50_g94.x , break50_g94.y , revealMaskRaw44_g94);
				float temp_output_28_0 = saturate( smoothstepResult48_g94 );
				float2 break50_g84 = float2( -0.5,0.9 );
				float temp_output_13_0_g93 = _Range;
				float3 temp_output_15_0_g85 = ( ( (ase_positionWS*1.0 + _Float6) * float3( 1,1,1 ) ) + float3( 0,0,0 ) );
				float3 temp_cast_6 = _Blocks;
				float3 blocks_amount22_g85 = temp_cast_6;
				float3 blocks8_g88 = abs( blocks_amount22_g85 );
				float3 mosaicUV15_g88 = ( floor( ( temp_output_15_0_g85 * blocks8_g88 ) ) / blocks8_g88 );
				float3 result17_g88 = mosaicUV15_g88;
				float3 in_float3230_g91 = result17_g88;
				float3 in232_g91 = in_float3230_g91;
				float3 temp_cast_7 = (_DisplacementScale).xxx;
				float3 turb_scale23_g85 = temp_cast_7;
				float3 temp_cast_8 = (mulTime12).xxx;
				float3 turb_offset24_g85 = temp_cast_8;
				float3 coords_mapped_float3177_g91 = (in_float3230_g91*turb_scale23_g85 + turb_offset24_g85);
				float3 coords26_g92 = coords_mapped_float3177_g91;
				float noise_s26_g85 = 1.0;
				float noise_scale205_g91 = noise_s26_g85;
				float scale25_g92 = noise_scale205_g91;
				float simplePerlin2D2_g92 = snoise( (coords26_g92).xy*scale25_g92 );
				simplePerlin2D2_g92 = simplePerlin2D2_g92*0.5 + 0.5;
				float simplePerlin2D5_g92 = snoise( (coords26_g92).yz*scale25_g92 );
				simplePerlin2D5_g92 = simplePerlin2D5_g92*0.5 + 0.5;
				float simplePerlin2D8_g92 = snoise( (coords26_g92).xz*scale25_g92 );
				simplePerlin2D8_g92 = simplePerlin2D8_g92*0.5 + 0.5;
				float3 appendResult23_g92 = (float3(simplePerlin2D2_g92 , simplePerlin2D5_g92 , simplePerlin2D8_g92));
				float3 temp_output_248_0_g91 = appendResult23_g92;
				float3 temp_cast_9 = (1.0).xxx;
				float3 noise118_g91 = saturate( ( ( saturate( temp_output_248_0_g91 ) * 2.0 ) - temp_cast_9 ) );
				float3 distortion_map27_g91 = noise118_g91;
				float3 temp_cast_10 = (-1.0).xxx;
				float turb_strenght25_g85 = _DisplacementStrength;
				float temp_output_20_0_g85 = saturate( turb_strenght25_g85 );
				float temp_output_6_0_g91 = temp_output_20_0_g85;
				float3 temp_cast_11 = (temp_output_6_0_g91).xxx;
				float3 distortedUV37_g91 = ( in232_g91 + saturate( (( distortion_map27_g91 * temp_output_6_0_g91 ) + (distortion_map27_g91 - temp_cast_10) * (temp_cast_11 - ( distortion_map27_g91 * temp_output_6_0_g91 )) / (float3( 1,1,1 ) - temp_cast_10)) ) );
				float dist19_g93 = distance( ( distortedUV37_g91 + ( temp_output_20_0_g85 * -0.5 ) ) , ase_objectPosition );
				float smoothstepResult22_g93 = smoothstep( temp_output_13_0_g93 , ( temp_output_13_0_g93 + ( 1.0 - _Falloff ) ) , dist19_g93);
				float mask29_g93 = ( 1.0 - smoothstepResult22_g93 );
				float revealMaskRaw44_g84 = ( 1.0 - saturate( mask29_g93 ) );
				float smoothstepResult48_g84 = smoothstep( break50_g84.x , break50_g84.y , revealMaskRaw44_g84);
				float temp_output_30_0 = saturate( smoothstepResult48_g84 );
				float lerpResult52 = lerp( 1.0 , ( temp_output_28_0 - temp_output_30_0 ) , step( temp_output_28_0 , 0.8 ));
				float pixel_effect44 = lerpResult52;
				

				surfaceDescription.Alpha = ceil( pixel_effect44 );
				surfaceDescription.AlphaClipThreshold = 0.5;

				#if _ALPHATEST_ON
					float alphaClipThreshold = 0.01f;
					#if ALPHA_CLIP_THRESHOLD
						alphaClipThreshold = surfaceDescription.AlphaClipThreshold;
					#endif
					clip(surfaceDescription.Alpha - alphaClipThreshold);
				#endif

				half4 outColor = 0;
				outColor = _SelectionID;

				return outColor;
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthNormals"
			Tags { "LightMode"="DepthNormalsOnly" }

			ZTest LEqual
			ZWrite On

			HLSLPROGRAM

			

        	#pragma multi_compile _ALPHATEST_ON
        	#pragma multi_compile_instancing
        	#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
        	#define ASE_FOG 1
        	#define _SURFACE_TYPE_TRANSPARENT 1
        	#define ASE_VERSION 19801
        	#define ASE_SRP_VERSION 140012


			

        	#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

			

			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define VARYINGS_NEED_NORMAL_WS

			#define SHADERPASS SHADERPASS_DEPTHNORMALSONLY

			
            #if ASE_SRP_VERSION >=140007
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#endif
		

			
			#if ASE_SRP_VERSION >=140007
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#endif
		

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"

			
			#if ASE_SRP_VERSION >=140010
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#endif
		

			

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

            #if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_FRAG_WORLD_POSITION


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 clipPosV : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float3 normalWS : TEXCOORD2;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _TextureSample0_ST;
			float _Range;
			float _Falloff;
			int _Blocks;
			float _DisplacementScale;
			float _Speed;
			float _DisplacementStrength;
			float _Float6;
			float _Float0;
			float _Float3;
			float _Float4;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			

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
			

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = vertexInput.positionCS;
				output.clipPosV = vertexInput.positionCS;
				output.positionWS = vertexInput.positionWS;
				output.normalWS = TransformObjectToWorldNormal( input.normalOS );
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				float3 normalOS : NORMAL;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			void frag(PackedVaryings input
						, out half4 outNormalWS : SV_Target0
						#ifdef ASE_DEPTH_WRITE_ON
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						#ifdef _WRITE_RENDERING_LAYERS
						, out float4 outRenderingLayers : SV_Target1
						#endif
						 )
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );
				float3 WorldPosition = input.positionWS;
				float3 WorldNormal = input.normalWS;
				float4 ClipPos = input.clipPosV;
				float4 ScreenPos = ComputeScreenPos( input.clipPosV );

				float2 break50_g94 = float2( -0.5,0.9 );
				float temp_output_13_0_g103 = _Range;
				float3 temp_output_15_0_g95 = ( ( WorldPosition * float3( 1,1,1 ) ) + float3( 0,0,0 ) );
				float3 temp_cast_0 = _Blocks;
				float3 blocks_amount22_g95 = temp_cast_0;
				float3 blocks8_g98 = abs( blocks_amount22_g95 );
				float3 mosaicUV15_g98 = ( floor( ( temp_output_15_0_g95 * blocks8_g98 ) ) / blocks8_g98 );
				float3 result17_g98 = mosaicUV15_g98;
				float3 in_float3230_g101 = result17_g98;
				float3 in232_g101 = in_float3230_g101;
				float3 temp_cast_1 = (_DisplacementScale).xxx;
				float3 turb_scale23_g95 = temp_cast_1;
				float mulTime12 = _TimeParameters.x * _Speed;
				float3 temp_cast_2 = (mulTime12).xxx;
				float3 turb_offset24_g95 = temp_cast_2;
				float3 coords_mapped_float3177_g101 = (in_float3230_g101*turb_scale23_g95 + turb_offset24_g95);
				float3 coords26_g102 = coords_mapped_float3177_g101;
				float noise_s26_g95 = 1.0;
				float noise_scale205_g101 = noise_s26_g95;
				float scale25_g102 = noise_scale205_g101;
				float simplePerlin2D2_g102 = snoise( (coords26_g102).xy*scale25_g102 );
				simplePerlin2D2_g102 = simplePerlin2D2_g102*0.5 + 0.5;
				float simplePerlin2D5_g102 = snoise( (coords26_g102).yz*scale25_g102 );
				simplePerlin2D5_g102 = simplePerlin2D5_g102*0.5 + 0.5;
				float simplePerlin2D8_g102 = snoise( (coords26_g102).xz*scale25_g102 );
				simplePerlin2D8_g102 = simplePerlin2D8_g102*0.5 + 0.5;
				float3 appendResult23_g102 = (float3(simplePerlin2D2_g102 , simplePerlin2D5_g102 , simplePerlin2D8_g102));
				float3 temp_output_248_0_g101 = appendResult23_g102;
				float3 temp_cast_3 = (1.0).xxx;
				float3 noise118_g101 = saturate( ( ( saturate( temp_output_248_0_g101 ) * 2.0 ) - temp_cast_3 ) );
				float3 distortion_map27_g101 = noise118_g101;
				float3 temp_cast_4 = (-1.0).xxx;
				float turb_strenght25_g95 = _DisplacementStrength;
				float temp_output_20_0_g95 = saturate( turb_strenght25_g95 );
				float temp_output_6_0_g101 = temp_output_20_0_g95;
				float3 temp_cast_5 = (temp_output_6_0_g101).xxx;
				float3 distortedUV37_g101 = ( in232_g101 + saturate( (( distortion_map27_g101 * temp_output_6_0_g101 ) + (distortion_map27_g101 - temp_cast_4) * (temp_cast_5 - ( distortion_map27_g101 * temp_output_6_0_g101 )) / (float3( 1,1,1 ) - temp_cast_4)) ) );
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float dist19_g103 = distance( ( distortedUV37_g101 + ( temp_output_20_0_g95 * -0.5 ) ) , ase_objectPosition );
				float smoothstepResult22_g103 = smoothstep( temp_output_13_0_g103 , ( temp_output_13_0_g103 + ( 1.0 - _Falloff ) ) , dist19_g103);
				float mask29_g103 = ( 1.0 - smoothstepResult22_g103 );
				float revealMaskRaw44_g94 = ( 1.0 - saturate( mask29_g103 ) );
				float smoothstepResult48_g94 = smoothstep( break50_g94.x , break50_g94.y , revealMaskRaw44_g94);
				float temp_output_28_0 = saturate( smoothstepResult48_g94 );
				float2 break50_g84 = float2( -0.5,0.9 );
				float temp_output_13_0_g93 = _Range;
				float3 temp_output_15_0_g85 = ( ( (WorldPosition*1.0 + _Float6) * float3( 1,1,1 ) ) + float3( 0,0,0 ) );
				float3 temp_cast_6 = _Blocks;
				float3 blocks_amount22_g85 = temp_cast_6;
				float3 blocks8_g88 = abs( blocks_amount22_g85 );
				float3 mosaicUV15_g88 = ( floor( ( temp_output_15_0_g85 * blocks8_g88 ) ) / blocks8_g88 );
				float3 result17_g88 = mosaicUV15_g88;
				float3 in_float3230_g91 = result17_g88;
				float3 in232_g91 = in_float3230_g91;
				float3 temp_cast_7 = (_DisplacementScale).xxx;
				float3 turb_scale23_g85 = temp_cast_7;
				float3 temp_cast_8 = (mulTime12).xxx;
				float3 turb_offset24_g85 = temp_cast_8;
				float3 coords_mapped_float3177_g91 = (in_float3230_g91*turb_scale23_g85 + turb_offset24_g85);
				float3 coords26_g92 = coords_mapped_float3177_g91;
				float noise_s26_g85 = 1.0;
				float noise_scale205_g91 = noise_s26_g85;
				float scale25_g92 = noise_scale205_g91;
				float simplePerlin2D2_g92 = snoise( (coords26_g92).xy*scale25_g92 );
				simplePerlin2D2_g92 = simplePerlin2D2_g92*0.5 + 0.5;
				float simplePerlin2D5_g92 = snoise( (coords26_g92).yz*scale25_g92 );
				simplePerlin2D5_g92 = simplePerlin2D5_g92*0.5 + 0.5;
				float simplePerlin2D8_g92 = snoise( (coords26_g92).xz*scale25_g92 );
				simplePerlin2D8_g92 = simplePerlin2D8_g92*0.5 + 0.5;
				float3 appendResult23_g92 = (float3(simplePerlin2D2_g92 , simplePerlin2D5_g92 , simplePerlin2D8_g92));
				float3 temp_output_248_0_g91 = appendResult23_g92;
				float3 temp_cast_9 = (1.0).xxx;
				float3 noise118_g91 = saturate( ( ( saturate( temp_output_248_0_g91 ) * 2.0 ) - temp_cast_9 ) );
				float3 distortion_map27_g91 = noise118_g91;
				float3 temp_cast_10 = (-1.0).xxx;
				float turb_strenght25_g85 = _DisplacementStrength;
				float temp_output_20_0_g85 = saturate( turb_strenght25_g85 );
				float temp_output_6_0_g91 = temp_output_20_0_g85;
				float3 temp_cast_11 = (temp_output_6_0_g91).xxx;
				float3 distortedUV37_g91 = ( in232_g91 + saturate( (( distortion_map27_g91 * temp_output_6_0_g91 ) + (distortion_map27_g91 - temp_cast_10) * (temp_cast_11 - ( distortion_map27_g91 * temp_output_6_0_g91 )) / (float3( 1,1,1 ) - temp_cast_10)) ) );
				float dist19_g93 = distance( ( distortedUV37_g91 + ( temp_output_20_0_g85 * -0.5 ) ) , ase_objectPosition );
				float smoothstepResult22_g93 = smoothstep( temp_output_13_0_g93 , ( temp_output_13_0_g93 + ( 1.0 - _Falloff ) ) , dist19_g93);
				float mask29_g93 = ( 1.0 - smoothstepResult22_g93 );
				float revealMaskRaw44_g84 = ( 1.0 - saturate( mask29_g93 ) );
				float smoothstepResult48_g84 = smoothstep( break50_g84.x , break50_g84.y , revealMaskRaw44_g84);
				float temp_output_30_0 = saturate( smoothstepResult48_g84 );
				float lerpResult52 = lerp( 1.0 , ( temp_output_28_0 - temp_output_30_0 ) , step( temp_output_28_0 , 0.8 ));
				float pixel_effect44 = lerpResult52;
				

				float Alpha = ceil( pixel_effect44 );
				float AlphaClipThreshold = 0.5;

				#ifdef ASE_DEPTH_WRITE_ON
					float DepthValue = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#ifdef ASE_DEPTH_WRITE_ON
					outputDepth = DepthValue;
				#endif

				#if defined(_GBUFFER_NORMALS_OCT)
					float3 normalWS = normalize(input.normalWS);
					float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
					float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
					half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
					outNormalWS = half4(packedNormalWS, 0.0);
				#else
					float3 normalWS = input.normalWS;
					outNormalWS = half4(NormalizeNormalPerPixel(normalWS), 0.0);
				#endif

				#ifdef _WRITE_RENDERING_LAYERS
					uint renderingLayers = GetMeshRenderingLayer();
					outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
				#endif
			}
			ENDHLSL
		}

	
	}
	
	CustomEditor "UnityEditor.ShaderGraphUnlitGUI"
	FallBack "Hidden/Shader Graph/FallbackError"
	
	Fallback Off
}
/*ASEBEGIN
Version=19801
Node;AmplifyShaderEditor.WorldPosInputsNode;29;-1008,-560;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;43;-1024,-288;Inherit;False;Property;_Float6;Float 6;17;0;Create;True;0;0;0;False;0;False;0.5;0.19;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;58;-1216,32;Inherit;False;Property;_Speed;Speed;19;0;Create;True;0;0;0;False;0;False;0;0.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;23;-1568,0;Inherit;False;Property;_DisplacementStrength;Displacement Strength;13;0;Create;True;0;0;0;False;0;False;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;14;-1040,480;Inherit;False;Property;_Falloff;Falloff;7;0;Create;True;0;0;0;False;0;False;4.94;3.34;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;13;-1040,400;Inherit;False;Property;_Range;Range;6;0;Create;True;0;0;0;False;0;False;4.94;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode;32;-1024,-176;Inherit;False;Property;_Blocks;Blocks;15;0;Create;True;0;0;0;False;0;False;0;15;False;0;1;INT;0
Node;AmplifyShaderEditor.RangedFloatNode;21;-1104,-96;Inherit;False;Property;_DisplacementScale;Displacement Scale;12;0;Create;True;0;0;0;False;0;False;0;2.2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;42;-832,-336;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleTimeNode;12;-1040,32;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;30;-496,240;Inherit;True;Pixel Reveal Sphere Mask;0;;84;b4ad8d98449f2b5438a9ad59308576c2;0;12;51;FLOAT3;0,0,0;False;3;FLOAT3;1,1,1;False;13;FLOAT3;0,0,0;False;16;FLOAT3;5,5,5;False;26;FLOAT3;1.4,1.4,1.4;False;34;FLOAT3;0,0,0;False;35;FLOAT;0.005;False;28;FLOAT;1;False;39;FLOAT3;0,0,0;False;41;FLOAT;4;False;42;FLOAT;4.4;False;49;FLOAT2;-0.5,0.9;False;2;FLOAT;0;FLOAT;47
Node;AmplifyShaderEditor.FunctionNode;28;-448,-256;Inherit;True;Pixel Reveal Sphere Mask;0;;94;b4ad8d98449f2b5438a9ad59308576c2;0;12;51;FLOAT3;0,0,0;False;3;FLOAT3;1,1,1;False;13;FLOAT3;0,0,0;False;16;FLOAT3;5,5,5;False;26;FLOAT3;1.4,1.4,1.4;False;34;FLOAT3;0,0,0;False;35;FLOAT;0.005;False;28;FLOAT;1;False;39;FLOAT3;0,0,0;False;41;FLOAT;4;False;42;FLOAT;4.4;False;49;FLOAT2;-0.5,0.9;False;2;FLOAT;0;FLOAT;47
Node;AmplifyShaderEditor.SimpleSubtractOpNode;51;-16,-176;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;56;-80,320;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.8;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;52;304,336;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;44;576,352;Inherit;False;pixel_effect;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;50;1024,368;Inherit;False;44;pixel_effect;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;40;-1360,224;Inherit;False;Property;_Float5;Float 5;16;0;Create;True;0;0;0;False;0;False;0;-0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;39;-1184,160;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-1072,320;Inherit;False;Property;_NoiseScale;Noise Scale;14;0;Create;True;0;0;0;False;0;False;0;0.86;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;55;-1152,-384;Inherit;False;Property;_Vector0;Vector 0;18;0;Create;True;0;0;0;False;0;False;0.01,-0.01,0.02;0.01,-0.01,0.03;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;53;-31.698,18.06372;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;16;1288,0;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0.1,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.StepOpNode;18;624,-288;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.11;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;33;640,-128;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.11;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;35;672,0;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.11;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;19;432,-208;Inherit;False;Property;_Float0;Float 0;9;0;Create;True;0;0;0;False;0;False;0;0.001;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;34;448,-32;Inherit;False;Property;_Float3;Float 0;10;0;Create;True;0;0;0;False;0;False;0;0.005;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;46;416,-112;Inherit;False;44;pixel_effect;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;47;416,48;Inherit;False;44;pixel_effect;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;36;448,128;Inherit;False;Property;_Float4;Float 0;11;0;Create;True;0;0;0;False;0;False;0;0.0085;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;45;400,-288;Inherit;False;44;pixel_effect;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;49;976,208;Inherit;False;44;pixel_effect;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;54;-752,-560;Inherit;False;pos;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CeilOpNode;57;1241.614,413.8865;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;17;864,0;Inherit;True;Property;_TextureSample0;Texture Sample 0;8;0;Create;True;0;0;0;False;0;False;-1;None;feace049a474eac41a1289b5d1ac909f;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.DynamicAppendNode;37;832,-288;Inherit;True;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SamplerNode;59;1392,-416;Inherit;True;Property;_TextureSample1;Texture Sample 0;5;0;Create;True;0;0;0;False;0;False;-1;None;feace049a474eac41a1289b5d1ac909f;True;0;False;white;Auto;False;Instance;17;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp;60;896,-496;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;62;1361.597,231.9557;Inherit;False;Constant;_Float7;Float 7;22;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;61;528,-560;Inherit;False;Grid;-1;;104;cb0ff472f8979c34babf314eeb6506a3;0;6;35;FLOAT4;5,0,0,1;False;36;FLOAT4;255,255,255,255;False;37;FLOAT;0.05;False;38;FLOAT;5;False;39;FLOAT3;0,0,0;False;40;FLOAT3;0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ColorNode;63;224,-704;Inherit;False;Constant;_Color0;Color 0;22;0;Create;True;0;0;0;False;0;False;1,1,1,1;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;64;224,-512;Inherit;False;Constant;_Color1;Color 1;22;0;Create;True;0;0;0;False;0;False;0.2970481,1,0,1;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;2;0,0;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;False;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=ShadowCaster;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;3;0,0;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;True;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;False;False;True;1;LightMode=DepthOnly;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;4;0,0;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;5;0,0;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Universal2D;0;5;Universal2D;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;True;1;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;1;LightMode=Universal2D;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;6;0,0;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;SceneSelectionPass;0;6;SceneSelectionPass;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=SceneSelectionPass;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;7;0,0;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ScenePickingPass;0;7;ScenePickingPass;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Picking;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;8;0,0;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthNormals;0;8;DepthNormals;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=DepthNormalsOnly;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;9;0,0;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthNormalsOnly;0;9;DepthNormalsOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=DepthNormalsOnly;False;True;9;d3d11;metal;vulkan;xboxone;xboxseries;playstation;ps4;ps5;switch;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;528,-272;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;True;1;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;0;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1;1520,0;Float;False;True;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;13;Game/FX/Pixel Scan v2;2992e84f91cbeb14eab234972e07ea9d;True;Forward;0;1;Forward;9;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;True;1;5;False;;10;False;;1;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;3;False;;True;True;0;False;;0;False;;True;1;LightMode=UniversalForwardOnly;False;False;0;;0;0;Standard;25;Surface;1;639069608146019339;  Blend;0;0;Two Sided;1;0;Alpha Clipping;1;0;  Use Shadow Threshold;0;0;Forward Only;0;0;Cast Shadows;1;0;Receive Shadows;1;0;GPU Instancing;1;0;LOD CrossFade;1;0;Built-in Fog;1;0;Meta Pass;0;0;Extra Pre Pass;0;0;Tessellation;0;0;  Phong;0;0;  Strength;0.5,False,;0;  Type;0;0;  Tess;16,False,;0;  Min;10,False,;0;  Max;25,False,;0;  Edge Length;16,False,;0;  Max Displacement;25,False,;0;Write Depth;0;0;  Early Z;0;0;Vertex Position,InvertActionOnDeselection;1;0;0;10;False;True;True;True;False;False;True;True;True;False;False;;False;0
WireConnection;42;0;29;0
WireConnection;42;2;43;0
WireConnection;12;0;58;0
WireConnection;30;51;42;0
WireConnection;30;16;32;0
WireConnection;30;26;21;0
WireConnection;30;34;12;0
WireConnection;30;35;23;0
WireConnection;30;41;13;0
WireConnection;30;42;14;0
WireConnection;28;51;29;0
WireConnection;28;16;32;0
WireConnection;28;26;21;0
WireConnection;28;34;12;0
WireConnection;28;35;23;0
WireConnection;28;41;13;0
WireConnection;28;42;14;0
WireConnection;51;0;28;0
WireConnection;51;1;30;0
WireConnection;56;0;28;0
WireConnection;52;1;51;0
WireConnection;52;2;56;0
WireConnection;44;0;52;0
WireConnection;39;0;23;0
WireConnection;39;1;40;0
WireConnection;53;0;28;0
WireConnection;53;1;30;0
WireConnection;16;0;37;0
WireConnection;16;1;17;5
WireConnection;16;2;49;0
WireConnection;18;0;45;0
WireConnection;18;1;19;0
WireConnection;33;0;46;0
WireConnection;33;1;34;0
WireConnection;35;0;47;0
WireConnection;35;1;36;0
WireConnection;54;0;29;0
WireConnection;57;0;50;0
WireConnection;37;0;18;0
WireConnection;37;1;33;0
WireConnection;37;2;35;0
WireConnection;59;1;37;0
WireConnection;60;0;61;0
WireConnection;60;2;45;0
WireConnection;61;35;63;0
WireConnection;61;36;64;0
WireConnection;1;2;16;0
WireConnection;1;3;57;0
ASEEND*/
//CHKSM=C8345E9008A0C43DB2FBE9D8FD0A5C2DE3B97F3D