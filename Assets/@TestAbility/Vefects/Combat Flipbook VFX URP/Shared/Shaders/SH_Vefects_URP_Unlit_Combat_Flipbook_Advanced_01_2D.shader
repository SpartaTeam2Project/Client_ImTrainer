// Made with Amplify Shader Editor v1.9.7.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Vefects/SH_Vefects_URP_Unlit_Combat_Flipbook_Advanced_01_2D"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[Space(33)][Header(Flipbook Frames)][Space(13)]_FlipbookX("Flipbook X", Float) = 4
		_FlipbookY("Flipbook Y", Float) = 4
		[Space(33)][Header(Mask Texture)][Space(13)]_MaskTexture("Mask Texture", 2D) = "white" {}
		_MaskUVScale("Mask UV Scale", Vector) = (1,1,0,0)
		_MaskUVPan("Mask UV Pan", Vector) = (0,0,0,0)
		[HDR]_R("R", Color) = (1,0.9719134,0.5896226,0)
		[HDR]_G("G", Color) = (1,0.7230805,0.25,0)
		[HDR]_B("B", Color) = (0.5943396,0.259371,0.09812209,0)
		[HDR]_Outline("Outline", Color) = (0.2169811,0.03320287,0.02354041,0)
		_FlatColor("Flat Color", Range( 0 , 1)) = 0
		_Emissive("Emissive", Float) = 1
		[Space(33)][Header(Alpha Texture)][Space(13)]_AlphaTexture("Alpha Texture", 2D) = "white" {}
		_AlphaGlow("Alpha Glow", Float) = 0
		[Space(33)][Header(Erosion Noise)][Space(13)]_ErosionNoise("Erosion Noise", 2D) = "white" {}
		_ErosionNoiseUVScaleOverall("Erosion Noise UV Scale Overall", Float) = 1
		_ErosionNoiseUVPan("Erosion Noise UV Pan", Vector) = (0,0,0,0)
		_ErosionIntensity("Erosion Intensity", Float) = 0
		_ErosionSmoothness("Erosion Smoothness", Float) = 1
		_DepthFade("Depth Fade", Float) = 0.1
		[Space(33)][Header(Distortion)][Space(13)]_DistortionTexture("Distortion Texture", 2D) = "white" {}
		_DistortionLerp("Distortion Lerp", Range( 0 , 0.1)) = 0
		_DistortionUVScale("Distortion UV Scale", Vector) = (1,1,0,0)
		_DistortionUVPan("Distortion UV Pan", Vector) = (0.1,-0.2,0,0)
		[Space(33)][Header(Pixelate)][Space(13)][Toggle(_PIXELATE_ON)] _Pixelate("Pixelate", Float) = 0
		_PixelsMultiplier("Pixels Multiplier", Float) = 1
		_PixelsX("Pixels X", Float) = 32
		_PixelsY("Pixels Y", Float) = 32
		[Space(33)][Header(AR)][Space(13)]_Cull("Cull", Float) = 2
		_Src("Src", Float) = 5
		_Dst("Dst", Float) = 10
		_ZWrite("ZWrite", Float) = 0
		_ZTest("ZTest", Float) = 2

		[HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
	}

	SubShader
	{
		LOD 0

		

        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "UniversalMaterialType"="Lit" "Queue"="Transparent" "ShaderGraphShader"="true" }

		Cull [_Cull]
		Blend [_Src] [_Dst], One OneMinusSrcAlpha
		ZTest [_ZTest]
		ZWrite [_ZWrite]
		Offset 0 , 0
		ColorMask RGBA
		

		HLSLINCLUDE
		#pragma target 2.0
		#pragma prefer_hlslcc gles
		// ensure rendering platforms toggle list is visible

		ENDHLSL

		
		Pass
		{
			
			Name "Sprite Lit"
            Tags { "LightMode"="Universal2D" }

			HLSLPROGRAM

			#define ASE_VERSION 19701
			#define ASE_SRP_VERSION 150006
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_COLOR
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_COLOR
            #define VARYINGS_NEED_SCREENPOSITION
            #define FEATURES_GRAPH_VERTEX

			#define SHADERPASS SHADERPASS_SPRITELIT

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"

			#define ASE_NEEDS_FRAG_COLOR
			#pragma shader_feature_local _PIXELATE_ON


			struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_SKINNED_VERTEX_INPUTS
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 texCoord0 : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float4 color : TEXCOORD2;
				float4 screenPosition : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            struct SurfaceDescription
			{
				float3 BaseColor;
				float Alpha;
			};

			sampler2D _MaskTexture;
			sampler2D _DistortionTexture;
			sampler2D _AlphaTexture;
			sampler2D _ErosionNoise;
			CBUFFER_START( UnityPerMaterial )
			float4 _Outline;
			float4 _B;
			float4 _R;
			float4 _G;
			float2 _ErosionNoiseUVPan;
			float2 _MaskUVPan;
			float2 _MaskUVScale;
			float2 _DistortionUVPan;
			float2 _DistortionUVScale;
			float _Src;
			float _ErosionNoiseUVScaleOverall;
			float _ErosionSmoothness;
			float _AlphaGlow;
			float _Emissive;
			float _FlatColor;
			float _PixelsY;
			float _DistortionLerp;
			float _PixelsX;
			float _ErosionIntensity;
			float _FlipbookY;
			float _FlipbookX;
			float _Cull;
			float _ZWrite;
			float _ZTest;
			float _Dst;
			float _PixelsMultiplier;
			float _DepthFade;
			CBUFFER_END


			
			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_SKINNED_VERTEX_COMPUTE(v);

				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );

				o.ase_texcoord4 = v.ase_texcoord1;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif
				v.normal = v.normal;
				v.tangent.xyz = v.tangent.xyz;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS);

				o.positionCS = vertexInput.positionCS;
				o.positionWS.xyz = vertexInput.positionWS;
				o.texCoord0.xyzw = v.uv0;
				o.color.xyzw =  v.color;
				o.screenPosition.xyzw = vertexInput.positionNDC;

				return o;
			}

			half4 frag( VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float4 positionCS = IN.positionCS;
				float3 positionWS = IN.positionWS;

				float2 texCoord131 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float2 panner140 = ( 1.0 * _Time.y * _MaskUVPan + ( texCoord131 * _MaskUVScale ));
				float2 texCoord123 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float2 appendResult151 = (float2(_FlipbookX , _FlipbookY));
				float2 FlipbookFrames220 = appendResult151;
				float2 panner127 = ( 1.0 * _Time.y * _DistortionUVPan + ( ( texCoord123 * _DistortionUVScale ) * FlipbookFrames220 ));
				float Random_Offset159 = IN.ase_texcoord4.x;
				float2 lerpResult139 = lerp( float2( 0,0 ) , ( ( (tex2D( _DistortionTexture, ( panner127 + Random_Offset159 ) )).rg + -0.5 ) * 2.0 ) , _DistortionLerp);
				float2 PrePixelateUVs149 = ( panner140 + lerpResult139 );
				float pixelWidth115 =  1.0f / ( _PixelsX * _PixelsMultiplier );
				float pixelHeight115 = 1.0f / ( _PixelsY * _PixelsMultiplier );
				half2 pixelateduv115 = half2((int)(PrePixelateUVs149.x / pixelWidth115) * pixelWidth115, (int)(PrePixelateUVs149.y / pixelHeight115) * pixelHeight115);
				#ifdef _PIXELATE_ON
				float2 staticSwitch118 = pixelateduv115;
				#else
				float2 staticSwitch118 = PrePixelateUVs149;
				#endif
				float2 FinalUVs224 = staticSwitch118;
				float4 tex2DNode45 = tex2D( _MaskTexture, FinalUVs224 );
				float4 lerpResult97 = lerp( _Outline , _B , tex2DNode45.b);
				float4 lerpResult112 = lerp( lerpResult97 , _G , tex2DNode45.g);
				float4 lerpResult111 = lerp( lerpResult112 , _R , tex2DNode45.r);
				float4 lerpResult88 = lerp( ( IN.color * lerpResult111 ) , IN.color , _FlatColor);
				float4 color183 = ( lerpResult88 * _Emissive );
				
				float4 tex2DNode185 = tex2D( _AlphaTexture, FinalUVs224 );
				float lerpResult186 = lerp( tex2DNode185.g , tex2DNode185.r , saturate( _AlphaGlow ));
				float mainTex_alpha48 = saturate( lerpResult186 );
				float Opacity_VTC_W158 = IN.texCoord0.z;
				float temp_output_208_0 = saturate( Opacity_VTC_W158 );
				float2 texCoord230 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float2 panner216 = ( 1.0 * _Time.y * _ErosionNoiseUVPan + ( ( texCoord230 * FlipbookFrames220 ) * _ErosionNoiseUVScaleOverall ));
				float2 DistortUVs231 = lerpResult139;
				float smoothstepResult200 = smoothstep( temp_output_208_0 , ( temp_output_208_0 + _ErosionSmoothness ) , tex2D( _ErosionNoise, ( ( panner216 + Random_Offset159 ) + DistortUVs231 ) ).g);
				float lerpResult196 = lerp( mainTex_alpha48 , saturate( ( mainTex_alpha48 * saturate( smoothstepResult200 ) ) ) , _ErosionIntensity);
				float mainTex_VC_alha52 = IN.color.a;
				float screenDepth211 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( IN.screenPosition.xy ),_ZBufferParams);
				float distanceDepth211 = saturate( ( screenDepth211 - LinearEyeDepth( IN.screenPosition.z,_ZBufferParams ) ) / ( _DepthFade ) );
				float OpacityRegister179 = saturate( ( saturate( ( saturate( lerpResult196 ) * mainTex_VC_alha52 ) ) * saturate( distanceDepth211 ) ) );
				
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;
				surfaceDescription.BaseColor = color183.rgb;
				surfaceDescription.Alpha = OpacityRegister179;

				half4 color = half4(surfaceDescription.BaseColor, surfaceDescription.Alpha);

				#if defined(DEBUG_DISPLAY)
				SurfaceData2D surfaceData;
				InitializeSurfaceData(color.rgb, color.a, surfaceData);
				InputData2D inputData;
				InitializeInputData(positionWS.xy, half2(IN.texCoord0.xy), inputData);
				half4 debugColor = 0;

				SETUP_DEBUG_DATA_2D(inputData, IN.positionWS);

				if (CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
				{
					return debugColor;
				}
				#endif

				color *= IN.color * unity_SpriteColor;
				return color;
			}

			ENDHLSL
		}

		
		Pass
		{
			
            Name "Sprite Normal"
            Tags { "LightMode"="NormalsRendering" }

			HLSLPROGRAM

			#define ASE_VERSION 19701
			#define ASE_SRP_VERSION 150006
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ SKINNED_SPRITE

			#define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define FEATURES_GRAPH_VERTEX

			#define SHADERPASS SHADERPASS_SPRITENORMAL

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#pragma shader_feature_local _PIXELATE_ON


			sampler2D _AlphaTexture;
			sampler2D _DistortionTexture;
			sampler2D _ErosionNoise;
			CBUFFER_START( UnityPerMaterial )
			float4 _Outline;
			float4 _B;
			float4 _R;
			float4 _G;
			float2 _ErosionNoiseUVPan;
			float2 _MaskUVPan;
			float2 _MaskUVScale;
			float2 _DistortionUVPan;
			float2 _DistortionUVScale;
			float _Src;
			float _ErosionNoiseUVScaleOverall;
			float _ErosionSmoothness;
			float _AlphaGlow;
			float _Emissive;
			float _FlatColor;
			float _PixelsY;
			float _DistortionLerp;
			float _PixelsX;
			float _ErosionIntensity;
			float _FlipbookY;
			float _FlipbookX;
			float _Cull;
			float _ZWrite;
			float _ZTest;
			float _Dst;
			float _PixelsMultiplier;
			float _DepthFade;
			CBUFFER_END


			struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;
				UNITY_SKINNED_VERTEX_INPUTS
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float3 normalWS : TEXCOORD0;
				float4 tangentWS : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_color : COLOR;
				float4 ase_texcoord4 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            struct SurfaceDescription
			{
				float3 NormalTS;
				float Alpha;
			};

			
			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_SKINNED_VERTEX_COMPUTE(v);

				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );

				float4 ase_clipPos = TransformObjectToHClip((v.positionOS).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord4 = screenPos;
				
				o.ase_texcoord2 = v.ase_texcoord;
				o.ase_texcoord3 = v.ase_texcoord1;
				o.ase_color = v.ase_color;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif
				v.normal = v.normal;
				v.tangent.xyz = v.tangent.xyz;

				float3 positionWS = TransformObjectToWorld(v.positionOS);
				float4 tangentWS = float4(TransformObjectToWorldDir(v.tangent.xyz), v.tangent.w);

				o.positionCS = TransformWorldToHClip(positionWS);
				o.normalWS.xyz =  -GetViewForwardDir();
				o.tangentWS.xyzw =  tangentWS;
				return o;
			}

			half4 frag( VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float2 texCoord131 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 panner140 = ( 1.0 * _Time.y * _MaskUVPan + ( texCoord131 * _MaskUVScale ));
				float2 texCoord123 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 appendResult151 = (float2(_FlipbookX , _FlipbookY));
				float2 FlipbookFrames220 = appendResult151;
				float2 panner127 = ( 1.0 * _Time.y * _DistortionUVPan + ( ( texCoord123 * _DistortionUVScale ) * FlipbookFrames220 ));
				float Random_Offset159 = IN.ase_texcoord3.x;
				float2 lerpResult139 = lerp( float2( 0,0 ) , ( ( (tex2D( _DistortionTexture, ( panner127 + Random_Offset159 ) )).rg + -0.5 ) * 2.0 ) , _DistortionLerp);
				float2 PrePixelateUVs149 = ( panner140 + lerpResult139 );
				float pixelWidth115 =  1.0f / ( _PixelsX * _PixelsMultiplier );
				float pixelHeight115 = 1.0f / ( _PixelsY * _PixelsMultiplier );
				half2 pixelateduv115 = half2((int)(PrePixelateUVs149.x / pixelWidth115) * pixelWidth115, (int)(PrePixelateUVs149.y / pixelHeight115) * pixelHeight115);
				#ifdef _PIXELATE_ON
				float2 staticSwitch118 = pixelateduv115;
				#else
				float2 staticSwitch118 = PrePixelateUVs149;
				#endif
				float2 FinalUVs224 = staticSwitch118;
				float4 tex2DNode185 = tex2D( _AlphaTexture, FinalUVs224 );
				float lerpResult186 = lerp( tex2DNode185.g , tex2DNode185.r , saturate( _AlphaGlow ));
				float mainTex_alpha48 = saturate( lerpResult186 );
				float Opacity_VTC_W158 = IN.ase_texcoord2.z;
				float temp_output_208_0 = saturate( Opacity_VTC_W158 );
				float2 texCoord230 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 panner216 = ( 1.0 * _Time.y * _ErosionNoiseUVPan + ( ( texCoord230 * FlipbookFrames220 ) * _ErosionNoiseUVScaleOverall ));
				float2 DistortUVs231 = lerpResult139;
				float smoothstepResult200 = smoothstep( temp_output_208_0 , ( temp_output_208_0 + _ErosionSmoothness ) , tex2D( _ErosionNoise, ( ( panner216 + Random_Offset159 ) + DistortUVs231 ) ).g);
				float lerpResult196 = lerp( mainTex_alpha48 , saturate( ( mainTex_alpha48 * saturate( smoothstepResult200 ) ) ) , _ErosionIntensity);
				float mainTex_VC_alha52 = IN.ase_color.a;
				float4 screenPos = IN.ase_texcoord4;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float screenDepth211 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				float distanceDepth211 = saturate( ( screenDepth211 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _DepthFade ) );
				float OpacityRegister179 = saturate( ( saturate( ( saturate( lerpResult196 ) * mainTex_VC_alha52 ) ) * saturate( distanceDepth211 ) ) );
				
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;
				surfaceDescription.NormalTS = float3(0.0f, 0.0f, 1.0f);
				surfaceDescription.Alpha = OpacityRegister179;

				half crossSign = (IN.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
				half3 bitangent = crossSign * cross(IN.normalWS.xyz, IN.tangentWS.xyz);
				half4 color = half4(1.0,1.0,1.0, surfaceDescription.Alpha);

				return NormalsRenderingShared(color, surfaceDescription.NormalTS, IN.tangentWS.xyz, bitangent, IN.normalWS);
			}

            ENDHLSL
        }

		
        Pass
        {
			
            Name "SceneSelectionPass"
            Tags { "LightMode"="SceneSelectionPass" }

            Cull Off
			Blend Off
			ZTest LEqual
			ZWrite On

            HLSLPROGRAM

			#define ASE_VERSION 19701
			#define ASE_SRP_VERSION 150006
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX

            #define SHADERPASS SHADERPASS_DEPTHONLY
			#define SCENESELECTIONPASS 1

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#pragma shader_feature_local _PIXELATE_ON


			sampler2D _AlphaTexture;
			sampler2D _DistortionTexture;
			sampler2D _ErosionNoise;
			CBUFFER_START( UnityPerMaterial )
			float4 _Outline;
			float4 _B;
			float4 _R;
			float4 _G;
			float2 _ErosionNoiseUVPan;
			float2 _MaskUVPan;
			float2 _MaskUVScale;
			float2 _DistortionUVPan;
			float2 _DistortionUVScale;
			float _Src;
			float _ErosionNoiseUVScaleOverall;
			float _ErosionSmoothness;
			float _AlphaGlow;
			float _Emissive;
			float _FlatColor;
			float _PixelsY;
			float _DistortionLerp;
			float _PixelsX;
			float _ErosionIntensity;
			float _FlipbookY;
			float _FlipbookX;
			float _Cull;
			float _ZWrite;
			float _ZTest;
			float _Dst;
			float _PixelsMultiplier;
			float _DepthFade;
			CBUFFER_END


            struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;
				UNITY_SKINNED_VERTEX_INPUTS
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            int _ObjectId;
            int _PassValue;

            struct SurfaceDescription
			{
				float Alpha;
			};

			
			VertexOutput vert(VertexInput v )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				UNITY_SKINNED_VERTEX_COMPUTE(v);

				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );

				float4 ase_clipPos = TransformObjectToHClip((v.positionOS).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord2 = screenPos;
				
				o.ase_texcoord = v.ase_texcoord;
				o.ase_texcoord1 = v.ase_texcoord1;
				o.ase_color = v.ase_color;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif

				float3 positionWS = TransformObjectToWorld(v.positionOS);
				o.positionCS = TransformWorldToHClip(positionWS);

				return o;
			}

			half4 frag(VertexOutput IN) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float2 texCoord131 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float2 panner140 = ( 1.0 * _Time.y * _MaskUVPan + ( texCoord131 * _MaskUVScale ));
				float2 texCoord123 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float2 appendResult151 = (float2(_FlipbookX , _FlipbookY));
				float2 FlipbookFrames220 = appendResult151;
				float2 panner127 = ( 1.0 * _Time.y * _DistortionUVPan + ( ( texCoord123 * _DistortionUVScale ) * FlipbookFrames220 ));
				float Random_Offset159 = IN.ase_texcoord1.x;
				float2 lerpResult139 = lerp( float2( 0,0 ) , ( ( (tex2D( _DistortionTexture, ( panner127 + Random_Offset159 ) )).rg + -0.5 ) * 2.0 ) , _DistortionLerp);
				float2 PrePixelateUVs149 = ( panner140 + lerpResult139 );
				float pixelWidth115 =  1.0f / ( _PixelsX * _PixelsMultiplier );
				float pixelHeight115 = 1.0f / ( _PixelsY * _PixelsMultiplier );
				half2 pixelateduv115 = half2((int)(PrePixelateUVs149.x / pixelWidth115) * pixelWidth115, (int)(PrePixelateUVs149.y / pixelHeight115) * pixelHeight115);
				#ifdef _PIXELATE_ON
				float2 staticSwitch118 = pixelateduv115;
				#else
				float2 staticSwitch118 = PrePixelateUVs149;
				#endif
				float2 FinalUVs224 = staticSwitch118;
				float4 tex2DNode185 = tex2D( _AlphaTexture, FinalUVs224 );
				float lerpResult186 = lerp( tex2DNode185.g , tex2DNode185.r , saturate( _AlphaGlow ));
				float mainTex_alpha48 = saturate( lerpResult186 );
				float Opacity_VTC_W158 = IN.ase_texcoord.z;
				float temp_output_208_0 = saturate( Opacity_VTC_W158 );
				float2 texCoord230 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float2 panner216 = ( 1.0 * _Time.y * _ErosionNoiseUVPan + ( ( texCoord230 * FlipbookFrames220 ) * _ErosionNoiseUVScaleOverall ));
				float2 DistortUVs231 = lerpResult139;
				float smoothstepResult200 = smoothstep( temp_output_208_0 , ( temp_output_208_0 + _ErosionSmoothness ) , tex2D( _ErosionNoise, ( ( panner216 + Random_Offset159 ) + DistortUVs231 ) ).g);
				float lerpResult196 = lerp( mainTex_alpha48 , saturate( ( mainTex_alpha48 * saturate( smoothstepResult200 ) ) ) , _ErosionIntensity);
				float mainTex_VC_alha52 = IN.ase_color.a;
				float4 screenPos = IN.ase_texcoord2;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float screenDepth211 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				float distanceDepth211 = saturate( ( screenDepth211 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _DepthFade ) );
				float OpacityRegister179 = saturate( ( saturate( ( saturate( lerpResult196 ) * mainTex_VC_alha52 ) ) * saturate( distanceDepth211 ) ) );
				
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;
				surfaceDescription.Alpha = OpacityRegister179;

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

			Cull Off
			Blend Off
			ZTest LEqual
			ZWrite On

            HLSLPROGRAM

			#define ASE_VERSION 19701
			#define ASE_SRP_VERSION 150006
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX

            #define SHADERPASS SHADERPASS_DEPTHONLY
			#define SCENEPICKINGPASS 1

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

        	#pragma shader_feature_local _PIXELATE_ON


			sampler2D _AlphaTexture;
			sampler2D _DistortionTexture;
			sampler2D _ErosionNoise;
			CBUFFER_START( UnityPerMaterial )
			float4 _Outline;
			float4 _B;
			float4 _R;
			float4 _G;
			float2 _ErosionNoiseUVPan;
			float2 _MaskUVPan;
			float2 _MaskUVScale;
			float2 _DistortionUVPan;
			float2 _DistortionUVScale;
			float _Src;
			float _ErosionNoiseUVScaleOverall;
			float _ErosionSmoothness;
			float _AlphaGlow;
			float _Emissive;
			float _FlatColor;
			float _PixelsY;
			float _DistortionLerp;
			float _PixelsX;
			float _ErosionIntensity;
			float _FlipbookY;
			float _FlipbookX;
			float _Cull;
			float _ZWrite;
			float _ZTest;
			float _Dst;
			float _PixelsMultiplier;
			float _DepthFade;
			CBUFFER_END


            struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;
				UNITY_SKINNED_VERTEX_INPUTS
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            float4 _SelectionID;

            struct SurfaceDescription
			{
				float Alpha;
			};

			
			VertexOutput vert(VertexInput v  )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_SKINNED_VERTEX_COMPUTE(v);

				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );

				float4 ase_clipPos = TransformObjectToHClip((v.positionOS).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord2 = screenPos;
				
				o.ase_texcoord = v.ase_texcoord;
				o.ase_texcoord1 = v.ase_texcoord1;
				o.ase_color = v.ase_color;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif

				float3 positionWS = TransformObjectToWorld(v.positionOS);
				o.positionCS = TransformWorldToHClip(positionWS);

				return o;
			}

			half4 frag(VertexOutput IN ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float2 texCoord131 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float2 panner140 = ( 1.0 * _Time.y * _MaskUVPan + ( texCoord131 * _MaskUVScale ));
				float2 texCoord123 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float2 appendResult151 = (float2(_FlipbookX , _FlipbookY));
				float2 FlipbookFrames220 = appendResult151;
				float2 panner127 = ( 1.0 * _Time.y * _DistortionUVPan + ( ( texCoord123 * _DistortionUVScale ) * FlipbookFrames220 ));
				float Random_Offset159 = IN.ase_texcoord1.x;
				float2 lerpResult139 = lerp( float2( 0,0 ) , ( ( (tex2D( _DistortionTexture, ( panner127 + Random_Offset159 ) )).rg + -0.5 ) * 2.0 ) , _DistortionLerp);
				float2 PrePixelateUVs149 = ( panner140 + lerpResult139 );
				float pixelWidth115 =  1.0f / ( _PixelsX * _PixelsMultiplier );
				float pixelHeight115 = 1.0f / ( _PixelsY * _PixelsMultiplier );
				half2 pixelateduv115 = half2((int)(PrePixelateUVs149.x / pixelWidth115) * pixelWidth115, (int)(PrePixelateUVs149.y / pixelHeight115) * pixelHeight115);
				#ifdef _PIXELATE_ON
				float2 staticSwitch118 = pixelateduv115;
				#else
				float2 staticSwitch118 = PrePixelateUVs149;
				#endif
				float2 FinalUVs224 = staticSwitch118;
				float4 tex2DNode185 = tex2D( _AlphaTexture, FinalUVs224 );
				float lerpResult186 = lerp( tex2DNode185.g , tex2DNode185.r , saturate( _AlphaGlow ));
				float mainTex_alpha48 = saturate( lerpResult186 );
				float Opacity_VTC_W158 = IN.ase_texcoord.z;
				float temp_output_208_0 = saturate( Opacity_VTC_W158 );
				float2 texCoord230 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float2 panner216 = ( 1.0 * _Time.y * _ErosionNoiseUVPan + ( ( texCoord230 * FlipbookFrames220 ) * _ErosionNoiseUVScaleOverall ));
				float2 DistortUVs231 = lerpResult139;
				float smoothstepResult200 = smoothstep( temp_output_208_0 , ( temp_output_208_0 + _ErosionSmoothness ) , tex2D( _ErosionNoise, ( ( panner216 + Random_Offset159 ) + DistortUVs231 ) ).g);
				float lerpResult196 = lerp( mainTex_alpha48 , saturate( ( mainTex_alpha48 * saturate( smoothstepResult200 ) ) ) , _ErosionIntensity);
				float mainTex_VC_alha52 = IN.ase_color.a;
				float4 screenPos = IN.ase_texcoord2;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float screenDepth211 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				float distanceDepth211 = saturate( ( screenDepth211 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _DepthFade ) );
				float OpacityRegister179 = saturate( ( saturate( ( saturate( lerpResult196 ) * mainTex_VC_alha52 ) ) * saturate( distanceDepth211 ) ) );
				
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;
				surfaceDescription.Alpha = OpacityRegister179;

				#if _ALPHATEST_ON
					float alphaClipThreshold = 0.01f;
					#if ALPHA_CLIP_THRESHOLD
						alphaClipThreshold = surfaceDescription.AlphaClipThreshold;
					#endif
					clip(surfaceDescription.Alpha - alphaClipThreshold);
				#endif

				half4 outColor = _SelectionID;
				return outColor;
			}

            ENDHLSL
        }

		
		Pass
		{
			
            Name "Sprite Forward"
            Tags { "LightMode"="UniversalForward" }

			HLSLPROGRAM

			#define ASE_VERSION 19701
			#define ASE_SRP_VERSION 150006
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ SKINNED_SPRITE

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_COLOR
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_COLOR
            #define FEATURES_GRAPH_VERTEX

			#define SHADERPASS SHADERPASS_SPRITEFORWARD

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"

			#define ASE_NEEDS_FRAG_COLOR
			#pragma shader_feature_local _PIXELATE_ON


			sampler2D _MaskTexture;
			sampler2D _DistortionTexture;
			sampler2D _AlphaTexture;
			sampler2D _ErosionNoise;
			CBUFFER_START( UnityPerMaterial )
			float4 _Outline;
			float4 _B;
			float4 _R;
			float4 _G;
			float2 _ErosionNoiseUVPan;
			float2 _MaskUVPan;
			float2 _MaskUVScale;
			float2 _DistortionUVPan;
			float2 _DistortionUVScale;
			float _Src;
			float _ErosionNoiseUVScaleOverall;
			float _ErosionSmoothness;
			float _AlphaGlow;
			float _Emissive;
			float _FlatColor;
			float _PixelsY;
			float _DistortionLerp;
			float _PixelsX;
			float _ErosionIntensity;
			float _FlipbookY;
			float _FlipbookX;
			float _Cull;
			float _ZWrite;
			float _ZTest;
			float _Dst;
			float _PixelsMultiplier;
			float _DepthFade;
			CBUFFER_END


			struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_SKINNED_VERTEX_INPUTS
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 texCoord0 : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float4 color : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            struct SurfaceDescription
			{
				float3 BaseColor;
				float Alpha;
				float3 NormalTS;
			};

			
			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_SKINNED_VERTEX_COMPUTE(v);

				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );

				float4 ase_clipPos = TransformObjectToHClip((v.positionOS).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord4 = screenPos;
				
				o.ase_texcoord3 = v.ase_texcoord1;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3( 0, 0, 0 );
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif
				v.normal = v.normal;
				v.tangent.xyz = v.tangent.xyz;

				float3 positionWS = TransformObjectToWorld(v.positionOS);

				o.positionCS = TransformWorldToHClip(positionWS);
				o.positionWS.xyz = positionWS;
				o.texCoord0.xyzw = v.uv0;
				o.color.xyzw = v.color;

				return o;
			}

			half4 frag( VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float4 positionCS = IN.positionCS;
				float3 positionWS = IN.positionWS;

				float2 texCoord131 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float2 panner140 = ( 1.0 * _Time.y * _MaskUVPan + ( texCoord131 * _MaskUVScale ));
				float2 texCoord123 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float2 appendResult151 = (float2(_FlipbookX , _FlipbookY));
				float2 FlipbookFrames220 = appendResult151;
				float2 panner127 = ( 1.0 * _Time.y * _DistortionUVPan + ( ( texCoord123 * _DistortionUVScale ) * FlipbookFrames220 ));
				float Random_Offset159 = IN.ase_texcoord3.x;
				float2 lerpResult139 = lerp( float2( 0,0 ) , ( ( (tex2D( _DistortionTexture, ( panner127 + Random_Offset159 ) )).rg + -0.5 ) * 2.0 ) , _DistortionLerp);
				float2 PrePixelateUVs149 = ( panner140 + lerpResult139 );
				float pixelWidth115 =  1.0f / ( _PixelsX * _PixelsMultiplier );
				float pixelHeight115 = 1.0f / ( _PixelsY * _PixelsMultiplier );
				half2 pixelateduv115 = half2((int)(PrePixelateUVs149.x / pixelWidth115) * pixelWidth115, (int)(PrePixelateUVs149.y / pixelHeight115) * pixelHeight115);
				#ifdef _PIXELATE_ON
				float2 staticSwitch118 = pixelateduv115;
				#else
				float2 staticSwitch118 = PrePixelateUVs149;
				#endif
				float2 FinalUVs224 = staticSwitch118;
				float4 tex2DNode45 = tex2D( _MaskTexture, FinalUVs224 );
				float4 lerpResult97 = lerp( _Outline , _B , tex2DNode45.b);
				float4 lerpResult112 = lerp( lerpResult97 , _G , tex2DNode45.g);
				float4 lerpResult111 = lerp( lerpResult112 , _R , tex2DNode45.r);
				float4 lerpResult88 = lerp( ( IN.color * lerpResult111 ) , IN.color , _FlatColor);
				float4 color183 = ( lerpResult88 * _Emissive );
				
				float4 tex2DNode185 = tex2D( _AlphaTexture, FinalUVs224 );
				float lerpResult186 = lerp( tex2DNode185.g , tex2DNode185.r , saturate( _AlphaGlow ));
				float mainTex_alpha48 = saturate( lerpResult186 );
				float Opacity_VTC_W158 = IN.texCoord0.z;
				float temp_output_208_0 = saturate( Opacity_VTC_W158 );
				float2 texCoord230 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float2 panner216 = ( 1.0 * _Time.y * _ErosionNoiseUVPan + ( ( texCoord230 * FlipbookFrames220 ) * _ErosionNoiseUVScaleOverall ));
				float2 DistortUVs231 = lerpResult139;
				float smoothstepResult200 = smoothstep( temp_output_208_0 , ( temp_output_208_0 + _ErosionSmoothness ) , tex2D( _ErosionNoise, ( ( panner216 + Random_Offset159 ) + DistortUVs231 ) ).g);
				float lerpResult196 = lerp( mainTex_alpha48 , saturate( ( mainTex_alpha48 * saturate( smoothstepResult200 ) ) ) , _ErosionIntensity);
				float mainTex_VC_alha52 = IN.color.a;
				float4 screenPos = IN.ase_texcoord4;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float screenDepth211 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				float distanceDepth211 = saturate( ( screenDepth211 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _DepthFade ) );
				float OpacityRegister179 = saturate( ( saturate( ( saturate( lerpResult196 ) * mainTex_VC_alha52 ) ) * saturate( distanceDepth211 ) ) );
				
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;
				surfaceDescription.BaseColor = color183.rgb;
				surfaceDescription.NormalTS = float3(0.0f, 0.0f, 1.0f);
				surfaceDescription.Alpha = OpacityRegister179;


				half4 color = half4(surfaceDescription.BaseColor, surfaceDescription.Alpha);

				#if defined(DEBUG_DISPLAY)
					SurfaceData2D surfaceData;
					InitializeSurfaceData(color.rgb, color.a, surfaceData);
					InputData2D inputData;
					InitializeInputData(positionWS.xy, half2(IN.texCoord0.xy), inputData);
					half4 debugColor = 0;

					SETUP_DEBUG_DATA_2D(inputData, IN.positionWS);

					if (CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
					{
						return debugColor;
					}
				#endif

				color *= IN.color;
				return color;
			}

            ENDHLSL
        }
		
	}
	CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
	FallBack "Hidden/Shader Graph/FallbackError"
	
	Fallback Off
}
/*ASEBEGIN
Version=19701
Node;AmplifyShaderEditor.CommentaryNode;222;-4658,1742;Inherit;False;836;290.95;Flipbook Frames;4;138;137;151;220;Flipbook Frames;0,0,0,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;137;-4608,1792;Inherit;False;Property;_FlipbookX;Flipbook X;0;0;Create;True;0;0;0;False;3;Space(33);Header(Flipbook Frames);Space(13);False;4;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;138;-4608,1920;Inherit;False;Property;_FlipbookY;Flipbook Y;1;0;Create;True;0;0;0;False;0;False;4;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;122;-4736,-1664;Inherit;False;1992;995;Distortion;23;149;145;140;139;136;135;134;133;132;131;130;129;128;127;126;125;124;123;192;193;219;223;231;;0,0,0,1;0;0
Node;AmplifyShaderEditor.DynamicAppendNode;151;-4352,1792;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;141;-4720,-256;Inherit;False;1768.791;450.8129;Opacity;14;179;177;175;159;158;153;152;148;207;209;210;211;212;213;;0,0,0,1;0;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;123;-4736,-1024;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;124;-4480,-896;Inherit;False;Property;_DistortionUVScale;Distortion UV Scale;21;0;Create;True;0;0;0;False;0;False;1,1;4,4;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RegisterLocalVarNode;220;-4096,1792;Inherit;False;FlipbookFrames;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;153;-4608,0;Inherit;False;1;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;223;-4224,-896;Inherit;False;220;FlipbookFrames;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;125;-4480,-1024;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;159;-4224,0;Inherit;False;Random Offset;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;219;-4224,-1024;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;126;-3968,-896;Inherit;False;Property;_DistortionUVPan;Distortion UV Pan;22;0;Create;True;0;0;0;False;0;False;0.1,-0.2;0.05,-0.2;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.PannerNode;127;-3968,-1024;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;193;-3712,-1152;Inherit;False;159;Random Offset;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;192;-3712,-1024;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;128;-3584,-1024;Inherit;True;Property;_DistortionTexture;Distortion Texture;19;0;Create;True;0;0;0;False;3;Space(33);Header(Distortion);Space(13);False;-1;4a897e6098e8f804a9ec44f2426669c2;4a897e6098e8f804a9ec44f2426669c2;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.TextureCoordinatesNode;131;-4608,-1600;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;129;-3328,-1152;Inherit;False;True;True;False;False;1;0;COLOR;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;130;-4224,-1472;Inherit;False;Property;_MaskUVScale;Mask UV Scale;3;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;136;-4224,-1600;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;133;-3584,-1280;Inherit;False;Property;_DistortionLerp;Distortion Lerp;20;0;Create;True;0;0;0;False;0;False;0;0.003;0;0.1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;135;-3072,-1152;Inherit;False;ConstantBiasScale;-1;;2;63208df05c83e8e49a48ffbdce2e43a0;0;3;3;FLOAT2;0,0;False;1;FLOAT;-0.5;False;2;FLOAT;2;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;132;-3968,-1472;Inherit;False;Property;_MaskUVPan;Mask UV Pan;4;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;134;-3584,-1408;Inherit;False;Constant;_Vector0;Vector 0;8;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.PannerNode;140;-3968,-1600;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp;139;-3200,-1408;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;225;-3762,-2610;Inherit;False;932;802.95;Pixelate;9;117;116;121;119;120;115;118;38;224;Pixelate;0,0,0,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;145;-3136,-1616;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;121;-3712,-1920;Inherit;False;Property;_PixelsMultiplier;Pixels Multiplier;24;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;117;-3712,-2048;Inherit;False;Property;_PixelsY;Pixels Y;26;0;Create;True;0;0;0;False;0;False;32;128;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;116;-3712,-2176;Inherit;False;Property;_PixelsX;Pixels X;25;0;Create;True;0;0;0;False;0;False;32;128;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;149;-2976,-1616;Inherit;False;PrePixelateUVs;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;119;-3456,-2176;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;120;-3456,-2048;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;142;-4704,208;Inherit;False;1759;1201.453;DisolveMaping;24;157;165;169;166;215;214;199;196;198;197;194;205;195;200;203;171;204;208;201;156;216;217;221;230;;0.1037736,0.1037736,0.1037736,1;0;0
Node;AmplifyShaderEditor.GetLocalVarNode;38;-3712,-2560;Inherit;False;149;PrePixelateUVs;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TFHCPixelate;115;-3712,-2304;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;221;-4352,1024;Inherit;False;220;FlipbookFrames;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;230;-4608,896;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;118;-3456,-2560;Inherit;False;Property;_Pixelate;Pixelate;23;0;Create;True;0;0;0;False;3;Space(33);Header(Pixelate);Space(13);False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;156;-4352,896;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;157;-4096,1280;Inherit;False;Property;_ErosionNoiseUVScaleOverall;Erosion Noise UV Scale Overall;14;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;36;-2416,-1728;Inherit;False;1896;1857.979;Color;20;48;45;112;111;106;105;101;97;96;93;92;88;52;47;185;186;189;190;191;226;;0,0,0,1;0;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;148;-4608,-192;Inherit;False;0;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;224;-3072,-2560;Inherit;False;FinalUVs;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;217;-3968,1152;Inherit;False;Property;_ErosionNoiseUVPan;Erosion Noise UV Pan;15;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;165;-4096,896;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;189;-1792,0;Inherit;False;Property;_AlphaGlow;Alpha Glow;12;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;158;-4224,-192;Inherit;False;Opacity_VTC_W;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;216;-3968,896;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;166;-3712,1152;Inherit;False;159;Random Offset;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;226;-2304,-640;Inherit;False;224;FinalUVs;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;231;-2944,-1408;Inherit;False;DistortUVs;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;190;-1536,0;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;201;-3840,512;Inherit;False;158;Opacity_VTC_W;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;185;-1792,-256;Inherit;True;Property;_AlphaTexture;Alpha Texture;11;0;Create;True;0;0;0;False;3;Space(33);Header(Alpha Texture);Space(13);False;-1;f793340976f7177468341cc3d511ef13;f793340976f7177468341cc3d511ef13;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleAddOpNode;169;-3584,896;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;215;-3456,1152;Inherit;False;231;DistortUVs;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp;186;-1408,-256;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;208;-3584,512;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;204;-3840,640;Inherit;False;Property;_ErosionSmoothness;Erosion Smoothness;17;0;Create;True;0;0;0;False;0;False;1;0.03;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;214;-3456,896;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;191;-1280,-256;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;171;-3328,896;Inherit;True;Property;_ErosionNoise;Erosion Noise;13;0;Create;True;0;0;0;False;3;Space(33);Header(Erosion Noise);Space(13);False;-1;4a897e6098e8f804a9ec44f2426669c2;4a897e6098e8f804a9ec44f2426669c2;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleAddOpNode;203;-3456,512;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;48;-1024,-256;Inherit;False;mainTex_alpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;200;-3456,384;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;195;-4608,384;Inherit;False;48;mainTex_alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;205;-3200,384;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;194;-4352,640;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;47;-1280,-1024;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;197;-4096,640;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;198;-4224,512;Inherit;False;Property;_ErosionIntensity;Erosion Intensity;16;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;92;-2304,-1152;Inherit;False;Property;_B;B;7;1;[HDR];Create;True;0;0;0;False;0;False;0.5943396,0.259371,0.09812209,0;9.082412,2.369872,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;93;-2304,-896;Inherit;False;Property;_Outline;Outline;8;1;[HDR];Create;True;0;0;0;False;0;False;0.2169811,0.03320287,0.02354041,0;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;45;-1792,-640;Inherit;True;Property;_MaskTexture;Mask Texture;2;0;Create;True;0;0;0;False;3;Space(33);Header(Mask Texture);Space(13);False;-1;b1b69c961c2e3854798854b5df548783;b1b69c961c2e3854798854b5df548783;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RegisterLocalVarNode;52;-1280,-768;Inherit;False;mainTex_VC_alha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;196;-4224,384;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;96;-2304,-1408;Inherit;False;Property;_G;G;6;1;[HDR];Create;True;0;0;0;False;0;False;1,0.7230805,0.25,0;1.976675,0.374629,0,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp;97;-1792,-1280;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;175;-3840,0;Inherit;False;52;mainTex_VC_alha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;199;-3968,384;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;212;-3456,128;Inherit;False;Property;_DepthFade;Depth Fade;18;0;Create;True;0;0;0;False;0;False;0.1;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;112;-1408,-1408;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;101;-2304,-1664;Inherit;False;Property;_R;R;5;1;[HDR];Create;True;0;0;0;False;0;False;1,0.9719134,0.5896226,0;0.06662595,0.05448028,0.04091521,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;177;-3840,-128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;211;-3456,0;Inherit;False;True;True;False;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;111;-1152,-1664;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;207;-3712,-128;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;213;-3200,0;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;105;-768,-1152;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;106;-768,-384;Inherit;False;Property;_FlatColor;Flat Color;9;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;209;-3456,-128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;88;-768,-640;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;184;-384,-512;Inherit;False;Property;_Emissive;Emissive;10;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;210;-3328,-128;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;180;-384,-640;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;63;336,-48;Inherit;False;1243;166;AR;5;110;80;78;82;83;;0,0,0,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;179;-3200,-128;Inherit;False;OpacityRegister;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;183;-128,-640;Inherit;False;color;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;78;640,0;Inherit;False;Property;_Src;Src;28;0;Create;True;0;0;0;True;0;False;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;110;896,0;Inherit;False;Property;_Dst;Dst;29;0;Create;True;0;0;0;True;0;False;10;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;82;1408,0;Inherit;False;Property;_ZTest;ZTest;31;0;Create;True;0;0;0;True;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;83;1152,0;Inherit;False;Property;_ZWrite;ZWrite;30;0;Create;True;0;0;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;181;-384,0;Inherit;False;183;color;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;152;-4224,-112;Inherit;False;VTC_T;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;73;-384,128;Inherit;False;179;OpacityRegister;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;80;384,0;Inherit;False;Property;_Cull;Cull;27;0;Create;True;0;0;0;True;3;Space(33);Header(AR);Space(13);False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;232;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI;0;1;New Amplify Shader;ece0159bad6633944bf6b818f4dd296c;True;Sprite Lit;0;0;Sprite Lit;0;False;True;2;5;False;;10;False;;3;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;3;False;;True;True;0;False;;0;False;;True;5;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;UniversalMaterialType=Lit;Queue=Transparent=Queue=0;ShaderGraphShader=true;True;0;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Universal2D;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;233;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI;0;1;New Amplify Shader;ece0159bad6633944bf6b818f4dd296c;True;Sprite Normal;0;1;Sprite Normal;0;False;True;2;5;False;;10;False;;3;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;3;False;;True;True;0;False;;0;False;;True;5;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;UniversalMaterialType=Lit;Queue=Transparent=Queue=0;ShaderGraphShader=true;True;0;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=NormalsRendering;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;234;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI;0;1;New Amplify Shader;ece0159bad6633944bf6b818f4dd296c;True;SceneSelectionPass;0;2;SceneSelectionPass;0;False;True;2;5;False;;10;False;;3;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;3;False;;True;True;0;False;;0;False;;True;5;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;UniversalMaterialType=Lit;Queue=Transparent=Queue=0;ShaderGraphShader=true;True;0;True;12;all;0;False;True;0;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=SceneSelectionPass;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;235;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI;0;1;New Amplify Shader;ece0159bad6633944bf6b818f4dd296c;True;ScenePickingPass;0;3;ScenePickingPass;0;False;True;2;5;False;;10;False;;3;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;3;False;;True;True;0;False;;0;False;;True;5;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;UniversalMaterialType=Lit;Queue=Transparent=Queue=0;ShaderGraphShader=true;True;0;True;12;all;0;False;True;0;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=Picking;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;236;0,0;Float;False;True;-1;2;UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI;0;17;Vefects/SH_Vefects_URP_Unlit_Combat_Flipbook_Advanced_01_2D;ece0159bad6633944bf6b818f4dd296c;True;Sprite Forward;0;4;Sprite Forward;6;True;True;2;0;True;_Src;0;True;_Dst;3;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;True;True;2;True;_Cull;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;True;True;2;True;_ZWrite;True;3;True;_ZTest;True;True;0;False;;0;False;;True;5;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;UniversalMaterialType=Lit;Queue=Transparent=Queue=0;ShaderGraphShader=true;True;0;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=UniversalForward;False;False;0;;0;0;Standard;2;Vertex Position;1;0;Debug Display;0;0;0;5;True;True;True;True;True;False;;False;0
Node;AmplifyShaderEditor.CommentaryNode;113;336,-224;Inherit;False;304;100;Ge Lush was here! <3;0;Ge Lush was here! <3;0,0,0,1;0;0
WireConnection;151;0;137;0
WireConnection;151;1;138;0
WireConnection;220;0;151;0
WireConnection;125;0;123;0
WireConnection;125;1;124;0
WireConnection;159;0;153;1
WireConnection;219;0;125;0
WireConnection;219;1;223;0
WireConnection;127;0;219;0
WireConnection;127;2;126;0
WireConnection;192;0;127;0
WireConnection;192;1;193;0
WireConnection;128;1;192;0
WireConnection;129;0;128;0
WireConnection;136;0;131;0
WireConnection;136;1;130;0
WireConnection;135;3;129;0
WireConnection;140;0;136;0
WireConnection;140;2;132;0
WireConnection;139;0;134;0
WireConnection;139;1;135;0
WireConnection;139;2;133;0
WireConnection;145;0;140;0
WireConnection;145;1;139;0
WireConnection;149;0;145;0
WireConnection;119;0;116;0
WireConnection;119;1;121;0
WireConnection;120;0;117;0
WireConnection;120;1;121;0
WireConnection;115;0;38;0
WireConnection;115;1;119;0
WireConnection;115;2;120;0
WireConnection;118;1;38;0
WireConnection;118;0;115;0
WireConnection;156;0;230;0
WireConnection;156;1;221;0
WireConnection;224;0;118;0
WireConnection;165;0;156;0
WireConnection;165;1;157;0
WireConnection;158;0;148;3
WireConnection;216;0;165;0
WireConnection;216;2;217;0
WireConnection;231;0;139;0
WireConnection;190;0;189;0
WireConnection;185;1;226;0
WireConnection;169;0;216;0
WireConnection;169;1;166;0
WireConnection;186;0;185;2
WireConnection;186;1;185;1
WireConnection;186;2;190;0
WireConnection;208;0;201;0
WireConnection;214;0;169;0
WireConnection;214;1;215;0
WireConnection;191;0;186;0
WireConnection;171;1;214;0
WireConnection;203;0;208;0
WireConnection;203;1;204;0
WireConnection;48;0;191;0
WireConnection;200;0;171;2
WireConnection;200;1;208;0
WireConnection;200;2;203;0
WireConnection;205;0;200;0
WireConnection;194;0;195;0
WireConnection;194;1;205;0
WireConnection;197;0;194;0
WireConnection;45;1;226;0
WireConnection;52;0;47;4
WireConnection;196;0;195;0
WireConnection;196;1;197;0
WireConnection;196;2;198;0
WireConnection;97;0;93;0
WireConnection;97;1;92;0
WireConnection;97;2;45;3
WireConnection;199;0;196;0
WireConnection;112;0;97;0
WireConnection;112;1;96;0
WireConnection;112;2;45;2
WireConnection;177;0;199;0
WireConnection;177;1;175;0
WireConnection;211;0;212;0
WireConnection;111;0;112;0
WireConnection;111;1;101;0
WireConnection;111;2;45;1
WireConnection;207;0;177;0
WireConnection;213;0;211;0
WireConnection;105;0;47;0
WireConnection;105;1;111;0
WireConnection;209;0;207;0
WireConnection;209;1;213;0
WireConnection;88;0;105;0
WireConnection;88;1;47;0
WireConnection;88;2;106;0
WireConnection;210;0;209;0
WireConnection;180;0;88;0
WireConnection;180;1;184;0
WireConnection;179;0;210;0
WireConnection;183;0;180;0
WireConnection;152;0;148;4
WireConnection;236;0;181;0
WireConnection;236;2;73;0
ASEEND*/
//CHKSM=E23F55B9FF422E736E0E08CDD6BCA76286AAE894