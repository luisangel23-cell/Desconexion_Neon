// URP port of the built-in "UI/Lit/Detail" shader, which is a Built-in Render Pipeline
// surface shader and renders magenta under URP.
// Lights the UI with the main light and per-pixel additional lights (Forward or Forward+) using the built-in
// pipeline's Phong model and legacy point light falloff, so scenes lit for the built-in
// pipeline keep the same look.

Shader "UI/Lit/Detail (URP)"
{
	Properties
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_Specular ("Specular Color", Color) = (0,0,0,0)
		[PerRendererData] _MainTex ("Diffuse (RGB), Alpha (A)", 2D) = "white" {}
		_MainBump ("Diffuse Bump Map", 2D) = "bump" {}
		_DetailTex ("Detail (RGB)", 2D) = "white" {}
		_DetailBump ("Detail Bump Map", 2D) = "bump" {}
		_Strength ("Detail Strength", Range(0.0, 1.0)) = 0.2
		_Shininess ("Shininess", Range(0.01, 1.0)) = 0.2

		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_ColorMask ("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
			"RenderPipeline"="UniversalPipeline"
		}

		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass
		{
			Name "UniversalForward"
			Tags { "LightMode"="UniversalForward" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
			#pragma multi_compile_local _ UNITY_UI_ALPHACLIP
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _CLUSTER_LIGHT_LOOP

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);
			TEXTURE2D(_MainBump);   SAMPLER(sampler_MainBump);
			TEXTURE2D(_DetailTex);  SAMPLER(sampler_DetailTex);
			TEXTURE2D(_DetailBump); SAMPLER(sampler_DetailBump);

			CBUFFER_START(UnityPerMaterial)
				half4 _Color;
				half4 _Specular;
				float4 _MainTex_ST;
				float4 _MainBump_ST;
				float4 _DetailTex_ST;
				float4 _DetailTex_TexelSize;
				float4 _DetailBump_ST;
				float4 _DetailBump_TexelSize;
				half _Strength;
				half _Shininess;
				float4 _ClipRect;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				half4 color : COLOR;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				half4 color : COLOR;
				float2 uv : TEXCOORD0;
				float4 detailUV : TEXCOORD1;   // xy: detail texture, zw: detail bump
				float3 positionWS : TEXCOORD2;
				float4 positionOS : TEXCOORD3;
				half3 tangentWS : TEXCOORD4;
				half3 bitangentWS : TEXCOORD5;
				half3 normalWS : TEXCOORD6;
			};

			Varyings vert (Attributes v)
			{
				Varyings o;
				o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
				o.positionCS = TransformWorldToHClip(o.positionWS);
				o.positionOS = v.positionOS;
				o.color = v.color * _Color;
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				// Detail textures tile once per texel in canvas units, so they keep their
				// size whatever the shape of the sprite.
				o.detailUV.xy = TRANSFORM_TEX(v.positionOS.xy * _DetailTex_TexelSize.xy, _DetailTex);
				o.detailUV.zw = TRANSFORM_TEX(v.positionOS.xy * _DetailBump_TexelSize.xy, _DetailBump);

				// UI geometry is flat in the canvas plane and faces -Z.
				o.tangentWS = TransformObjectToWorldDir(float3(1, 0, 0));
				o.bitangentWS = TransformObjectToWorldDir(float3(0, 1, 0));
				o.normalWS = TransformObjectToWorldDir(float3(0, 0, -1));
				return o;
			}

			// Built-in pipeline Phong lighting from the original shader.
			half3 Phong (half3 albedo, half3 normal, half3 lightDir, half3 viewDir, half3 lightColor, half specStrength, half shininess)
			{
				half reflectiveFactor = max(0.0, dot(-viewDir, reflect(lightDir, normal)));
				half diffuseFactor = max(0.0, dot(normal, lightDir));
				half specularFactor = pow(reflectiveFactor, shininess) * specStrength;
				return (albedo * diffuseFactor + _Specular.rgb * specularFactor) * lightColor;
			}

			// Phong lighting from one of URP's visible additional lights, by its index in the light arrays.
			half3 AdditionalLightPhong (uint index, float3 positionWS, half3 albedo, half3 normal, half3 viewDir, half specStrength, half shininess)
			{
				#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
				float4 lightPositionWS = _AdditionalLightsBuffer[index].position;
				half3 lightColor = _AdditionalLightsBuffer[index].color.rgb;
				half4 attenuation = _AdditionalLightsBuffer[index].attenuation;
				#else
				float4 lightPositionWS = _AdditionalLightsPosition[index];
				half3 lightColor = _AdditionalLightsColor[index].rgb;
				half4 attenuation = _AdditionalLightsAttenuation[index];
				#endif

				float3 lightVector = lightPositionWS.xyz - positionWS * lightPositionWS.w;
				float distanceSqr = max(dot(lightVector, lightVector), HALF_MIN);
				half3 lightDir = half3(lightVector * rsqrt(distanceSqr));

				// Legacy built-in falloff (1 / (1 + 25 * (d / range)^2)) instead of URP's
				// inverse-square falloff, which would make these UI-scale lights nearly black.
				// attenuation.x holds 1 / range^2; directional lights have w == 0 and no falloff.
				float normalizedDistSqr = distanceSqr * attenuation.x;
				half falloff = lightPositionWS.w > 0.0
					? (half)(saturate(1.0 - normalizedDistSqr) / (1.0 + 25.0 * normalizedDistSqr))
					: 1.0;

				return Phong(albedo, normal, lightDir, viewDir, lightColor, specStrength, shininess) * falloff;
			}

			half4 frag (Varyings i) : SV_Target
			{
				half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
				half4 detail = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, i.detailUV.xy);
				col.rgb = lerp(col.rgb, col.rgb * detail.rgb, detail.a * _Strength);
				col *= i.color;

				#ifdef UNITY_UI_CLIP_RECT
				float2 inside = step(_ClipRect.xy, i.positionOS.xy) * step(i.positionOS.xy, _ClipRect.zw);
				col.a *= inside.x * inside.y;
				#endif

				#ifdef UNITY_UI_ALPHACLIP
				clip(col.a - 0.01);
				#endif

				half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_MainBump, sampler_MainBump, i.uv))
					+ UnpackNormal(SAMPLE_TEXTURE2D(_DetailBump, sampler_DetailBump, i.detailUV.zw));
				normalTS = normalize(normalTS);
				half3 normalWS = normalize(normalTS.x * i.tangentWS + normalTS.y * i.bitangentWS + normalTS.z * i.normalWS);

				half3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
				half shininess = _Shininess * 250.0 + 4.0;
				half specStrength = _Specular.a;

				half3 lit = col.rgb * SampleSH(normalWS);

				Light mainLight = GetMainLight();
				lit += Phong(col.rgb, normalWS, mainLight.direction, viewDir, mainLight.color, specStrength, shininess);

				#if USE_CLUSTER_LIGHT_LOOP
				// Forward+ (the default renderer in new URP projects): extra directional lights come
				// first in the light arrays, then the cluster loop visits the lights near this pixel.
				for (uint di = 0u; di < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); di++)
					lit += AdditionalLightPhong(di, i.positionWS, col.rgb, normalWS, viewDir, specStrength, shininess);

				InputData inputData = (InputData)0;
				inputData.positionWS = i.positionWS;
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
				LIGHT_LOOP_BEGIN(0)
					lit += AdditionalLightPhong(lightIndex, i.positionWS, col.rgb, normalWS, viewDir, specStrength, shininess);
				LIGHT_LOOP_END
				#elif defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
				// Forward: the lights affecting this renderer, as per-object indices.
				uint lightCount = GetAdditionalLightsCount();
				for (uint li = 0u; li < lightCount; li++)
					lit += AdditionalLightPhong(GetPerObjectLightIndex(li), i.positionWS, col.rgb, normalWS, viewDir, specStrength, shininess);
				#endif

				return half4(lit, col.a);
			}
			ENDHLSL
		}
	}
}
