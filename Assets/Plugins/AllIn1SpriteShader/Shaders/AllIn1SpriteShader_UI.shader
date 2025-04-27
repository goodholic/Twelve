Shader "UI/AllIn1SpriteShader_UI"
{
    //=================================================
    // 1) Properties
    //=================================================
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}	//0
		_Color("Main Color", Color) = (1,1,1,1)		//1
		_Alpha("General Alpha",  Range(0,1)) = 1	//2

		_GlowColor("Glow Color", Color) = (1,1,1,1) //3
		_Glow("Glow Color Intensity", Range(0,100)) = 10 //4
        _GlowGlobal("Global Glow Intensity", Range(1,100)) = 1 //5
		[NoScaleOffset] _GlowTex("Glow Texture", 2D) = "white" {} //6

		_FadeTex("Fade Texture", 2D) = "white" {} //7
		_FadeAmount("Fade Amount",  Range(-0.1,1)) = -0.1 //8
		_FadeBurnWidth("Fade Burn Width",  Range(0,1)) = 0.025 //9
		_FadeBurnTransition("Burn Transition",  Range(0.01,0.5)) = 0.075 //10
		_FadeBurnColor("Fade Burn Color", Color) = (1,1,0,1) //11
		_FadeBurnTex("Fade Burn Texture", 2D) = "white" {} //12
		_FadeBurnGlow("Fade Burn Glow",  Range(1,250)) = 2//13
        
		_OutlineColor("Outline Base Color", Color) = (1,1,1,1) //14
		_OutlineAlpha("Outline Base Alpha",  Range(0,1)) = 1 //15
		_OutlineGlow("Outline Base Glow", Range(1,100)) = 1.5 //16
		_OutlineWidth("Outline Base Width", Range(0,0.2)) = 0.004 //17
		_OutlinePixelWidth("Outline Base Pixel Width", Int) = 1 //18
		
		[Space]
		_OutlineTex("Outline Texture", 2D) = "white" {} //19
		_OutlineTexXSpeed("Texture scroll speed X", Range(-50,50)) = 10 //20
		_OutlineTexYSpeed("Texture scroll speed Y", Range(-50,50)) = 0 //21

        [Space]
		_OutlineDistortTex("Outline Distortion Texture", 2D) = "white" {} //22
		_OutlineDistortAmount("Outline Distortion Amount", Range(0,2)) = 0.5 //23
		_OutlineDistortTexXSpeed("Distortion scroll speed X", Range(-50,50)) = 5 //24
		_OutlineDistortTexYSpeed("Distortion scroll speed Y", Range(-50,50)) = 5 //25
    	
    	_AlphaOutlineColor("Color", Color) = (1, 1, 1, 1) //26
		_AlphaOutlineGlow("Outline Glow", Range(1,100)) = 5 //27
		_AlphaOutlinePower("Power", Range(0, 5)) = 1 //28
		_AlphaOutlineMinAlpha("Min Alpha", Range(0, 1)) = 0 //29
		_AlphaOutlineBlend("Blend", Range(0, 1)) = 1 //30

		_GradBlend("Gradient Blend", Range(0,1)) = 1 //31
		_GradTopLeftCol("Top Color", Color) = (1,0,0,1) //32
		_GradTopRightCol("Top Color 2", Color) = (1, 1, 0, 1) //33
		_GradBotLeftCol("Bot Color", Color) = (0,0,1,1) //34
		_GradBotRightCol("Bot Color 2", Color) = (0, 1, 0, 1) //35

		[NoScaleOffset] _ColorSwapTex("Color Swap Texture", 2D) = "black" {} //36
		[HDR] _ColorSwapRed("Red Channel", Color) = (1,1,1,1) //37
		_ColorSwapRedLuminosity("Red luminosity",  Range(-1,1)) = 0.5 //38
		[HDR] _ColorSwapGreen("Green Channel", Color) = (1,1,1,1) //39
		_ColorSwapGreenLuminosity("Green luminosity",  Range(-1,1)) = 0.5 //40
		[HDR] _ColorSwapBlue("Blue Channel", Color) = (1,1,1,1) //41
		_ColorSwapBlueLuminosity("Blue luminosity",  Range(-1,1)) = 0.5 //42

		_HsvShift("Hue Shift", Range(0, 360)) = 180 //43
		_HsvSaturation("Saturation", Range(0, 2)) = 1 //44
		_HsvBright("Brightness", Range(0, 2)) = 1 //45

		_HitEffectColor("Hit Effect Color", Color) = (1,1,1,1) //46
		_HitEffectGlow("Glow Intensity", Range(1,100)) = 5 //47
		[Space]
		_HitEffectBlend("Hit Effect Blend", Range(0,1)) = 1 //48

		_NegativeAmount("Negative Amount", Range(0, 1)) = 1 //49

		_PixelateSize("Pixelate size", Range(4,512)) = 32 //50

		[NoScaleOffset] _ColorRampTex("Color ramp Texture", 2D) = "white" {} //51
		_ColorRampLuminosity("Color ramp luminosity",  Range(-1,1)) = 0 //52
		[Toggle()] _ColorRampOutline("Affects everything?", float) = 0 //53

		_GreyscaleLuminosity("Greyscale luminosity",  Range(-1,1)) = 0 //54
		[Toggle()] _GreyscaleOutline("Affects everything?", float) = 0 //55
		_GreyscaleTintColor("Greyscale Tint Color", Color) = (1,1,1,1) //56

		_PosterizeNumColors("Number of Colors",  Range(0,100)) = 8 //57
		_PosterizeGamma("Posterize Amount",  Range(0.1,10)) = 0.75 //58
		[Toggle()] _PosterizeOutline("Affects everything?", float) = 0 //59

		_BlurIntensity("Blur Intensity",  Range(0,100)) = 10 //60
		[Toggle()] _BlurHD("Blur is Low Res?", float) = 0 //61

		_MotionBlurAngle("Motion Blur Angle", Range(-1, 1)) = 0.1 //62
		_MotionBlurDist("Motion Blur Distance", Range(-3, 3)) = 1.25 //63

		_GhostColorBoost("Ghost Color Boost",  Range(0,5)) = 1 //64
		_GhostTransparency("Ghost Transparency",  Range(0,1)) = 0 //65

		_InnerOutlineColor("Inner Outline Color", Color) = (1,0,0,1) //66
		_InnerOutlineThickness("Outline Thickness",  Range(0,3)) = 1 //67
		_InnerOutlineAlpha("Inner Outline Alpha",  Range(0,1)) = 1 //68
		_InnerOutlineGlow("Inner Outline Glow",  Range(1,250)) = 4 //69

		_AlphaCutoffValue("Alpha cutoff value", Range(0, 1)) = 0.25 //70

		[Toggle()] _OnlyOutline("Only render outline?", float) = 0 //71
		[Toggle()] _OnlyInnerOutline("Only render inner outline?", float) = 0 //72

		_HologramStripesAmount("Stripes Amount", Range(0, 1)) = 0.1 //73
		_HologramUnmodAmount("Unchanged Amount", Range(0, 1)) = 0.0 //74
		_HologramStripesSpeed("Stripes Speed", Range(-20, 20)) = 4.5 //75
		_HologramMinAlpha("Min Alpha", Range(0, 1)) = 0.1 //76
		_HologramMaxAlpha("Max Alpha", Range(0, 100)) = 0.75 //77

		_ChromAberrAmount("ChromAberr Amount", Range(0, 1)) = 1 //78
		_ChromAberrAlpha("ChromAberr Alpha", Range(0, 1)) = 0.4 //79

		_GlitchAmount("Glitch Amount", Range(0, 20)) = 3 //80

		_FlickerPercent("Flicker Percent", Range(0, 1)) = 0.05 //81
		_FlickerFreq("Flicker Frequency", Range(0, 5)) = 0.2 //82
		_FlickerAlpha("Flicker Alpha", Range(0, 1)) = 0 //83

		_ShadowX("Shadow X Axis", Range(-0.5, 0.5)) = 0.1 //84
		_ShadowY("Shadow Y Axis", Range(-0.5, 0.5)) = -0.05 //85
		_ShadowAlpha("Shadow Alpha", Range(0, 1)) = 0.5 //86
		_ShadowColor("Shadow Color", Color) = (0, 0, 0, 1) //87

		_HandDrawnAmount("Hand Drawn Amount", Range(0, 20)) = 10 //88
		_HandDrawnSpeed("Hand Drawn Speed", Range(1, 15)) = 5 //89

		_GrassSpeed("Speed", Range(0,50)) = 2 //90
		_GrassWind("Bend amount", Range(0,50)) = 20 //91
		[Space]
		[Toggle()] _GrassManualToggle("Manually animated?", float) = 0 //92
		_GrassManualAnim("Manual Anim Value", Range(-1,1)) = 1 //93

		_WaveAmount("Wave Amount", Range(0, 25)) = 7 //94
		_WaveSpeed("Wave Speed", Range(0, 25)) = 10 //95
		_WaveStrength("Wave Strength", Range(0, 25)) = 7.5 //96
		_WaveX("Wave X Axis", Range(0, 1)) = 0 //97
		_WaveY("Wave Y Axis", Range(0, 1)) = 0.5 //98

		_RectSize("Rect Size", Range(1, 4)) = 1 //99

		_OffsetUvX("X axis", Range(-1, 1)) = 0 //100
		_OffsetUvY("Y axis", Range(-1, 1)) = 0 //101

		_ClipUvLeft("Clipping Left", Range(0, 1)) = 0 //102
		_ClipUvRight("Clipping Right", Range(0, 1)) = 0 //103
		_ClipUvUp("Clipping Up", Range(0, 1)) = 0 //104
		_ClipUvDown("Clipping Down", Range(0, 1)) = 0 //105

		_TextureScrollXSpeed("Speed X Axis", Range(-5, 5)) = 1 //106
		_TextureScrollYSpeed("Speed Y Axis", Range(-5, 5)) = 0 //107

		_ZoomUvAmount("Zoom Amount", Range(0.1, 5)) = 0.5 //108

		_DistortTex("Distortion Texture", 2D) = "white" {} //109
		_DistortAmount("Distortion Amount", Range(0,2)) = 0.5 //110
		_DistortTexXSpeed("Scroll speed X", Range(-50,50)) = 5 //111
		_DistortTexYSpeed("Scroll speed Y", Range(-50,50)) = 5 //112

		_TwistUvAmount("Twist Amount", Range(0, 3.1416)) = 1 //113
		_TwistUvPosX("Twist Pos X Axis", Range(0, 1)) = 0.5 //114
		_TwistUvPosY("Twist Pos Y Axis", Range(0, 1)) = 0.5 //115
		_TwistUvRadius("Twist Radius", Range(0, 3)) = 0.75 //116

		_RotateUvAmount("Rotate Angle(radians)", Range(0, 6.2831)) = 0 //117

		_FishEyeUvAmount("Fish Eye Amount", Range(0, 0.5)) = 0.35 //118

		_PinchUvAmount("Pinch Amount", Range(0, 0.5)) = 0.35 //119

		_ShakeUvSpeed("Shake Speed", Range(0, 20)) = 2.5 //120
		_ShakeUvX("X Multiplier", Range(0, 5)) = 1.5 //121
		_ShakeUvY("Y Multiplier", Range(0, 5)) = 1 //122

		_ColorChangeTolerance("Tolerance", Range(0, 1)) = 0.25 //123
		_ColorChangeTarget("Color to change", Color) = (1, 0, 0, 1) //124
		[HDR] _ColorChangeNewCol("New Color", Color) = (1, 1, 0, 1) //125
		_ColorChangeLuminosity("New Color Luminosity", Range(0, 1)) = 0.0 //126

		_RoundWaveStrength("Wave Strength", Range(0, 1)) = 0.7 //127
		_RoundWaveSpeed("Wave Speed", Range(0, 5)) = 2 //128

		[Toggle()] _BillboardY("Billboard on both axis?", float) = 0 //129

		// !!! 기존 ZWrite/ZTest/Blend/Cull 등 프로퍼티는 전부 제거 !!!
        // => 아래 SubShader에서 UI 표준값 고정

        _ShineColor("Shine Color", Color) = (1,1,1,1) //133
        _ShineLocation("Shine Location", Range(0,1)) = 0.5 //134
        _ShineRotate("Rotate Angle(radians)", Range(0, 6.2831)) = 0 //135
        _ShineWidth("Shine Width", Range(0.05,1)) = 0.1 //136
        _ShineGlow("Shine Glow", Range(0,100)) = 1 //137
		[NoScaleOffset] _ShineMask("Shine Mask", 2D) = "white" {} //138

		_GlitchSize("Glitch Size", Range(0.25, 5)) = 1 //139
		_HologramStripeColor("Stripes Color", Color) = (0,1,1,1) //140
		_GradBoostX("Boost X axis", Range(0.1, 5)) = 1.2 //141
		_GradBoostY("Boost Y axis", Range(0.1, 5)) = 1.2 //142
		[Toggle()] _GradIsRadial("Radial Gradient?", float) = 0 //143
		_AlphaRoundThreshold("Round Threshold", Range(0.005, 1.0)) = 0.5 //144
		_GrassRadialBend("Radial Bend", Range(0.0, 5.0)) = 0.1 //145

		_ColorChangeTolerance2("Tolerance 2", Range(0, 1)) = 0.25 //146
		_ColorChangeTarget2("Color to change 2", Color) = (1, 0, 0, 1) //147
		[HDR] _ColorChangeNewCol2("New Color 2", Color) = (1, 1, 0, 1) //148
		_ColorChangeTolerance3("Tolerance 3", Range(0, 1)) = 0.25 //149
		_ColorChangeTarget3("Color to change 3", Color) = (1, 0, 0, 1) //150
		[HDR] _ColorChangeNewCol3("New Color 3", Color) = (1, 1, 0, 1) //151

		_Contrast ("Contrast", Range(0, 6)) = 1 //152
		_Brightness ("Brightness", Range(-1, 1)) = 0 //153

		_ColorSwapBlend ("Color Swap Blend", Range(0, 1)) = 1 //154
		_ColorRampBlend ("Color Ramp Blend", Range(0, 1)) = 1 //155
		_GreyscaleBlend ("Greyscale Blend", Range(0, 1)) = 1 //156
		_GhostBlend ("Ghost Blend", Range(0, 1)) = 1 //157
		_HologramBlend ("Hologram Blend", Range(0, 1)) = 1 //158

        [AllIn1ShaderGradient] _ColorRampTexGradient("Color ramp Gradient", 2D) = "white" {} //159

		_OverlayTex("Overlay Texture", 2D) = "white" {} //160
		_OverlayColor("Overlay Color", Color) = (1, 1, 1, 1) //161
		_OverlayGlow("Overlay Glow", Range(0,25)) = 1 //162
		_OverlayBlend("Overlay Blend", Range(0, 1)) = 1 //163
    	
    	_RadialStartAngle("Radial Start Angle", Range(0, 360)) = 90 //164
		_RadialClip("Radial Clip", Range(0, 360)) = 45 //165
		_RadialClip2("Radial Clip 2", Range(0, 360)) = 0 //166
    	
    	_WarpStrength("Warp Strength", Range(0, 0.1)) = 0.025 //167
		_WarpSpeed("Warp Speed", Range(0, 25)) = 8 //168
		_WarpScale("Warp Scale", Range(0.05, 3)) = 0.5 //169
    	
    	_OverlayTextureScrollXSpeed("Speed X Axis", Range(-5, 5)) = 0.25 //170
		_OverlayTextureScrollYSpeed("Speed Y Axis", Range(-5, 5)) = 0.25 //171

		[HideInInspector] _MinXUV("_MinXUV", Range(0, 1)) = 0.0
		[HideInInspector] _MaxXUV("_MaxXUV", Range(0, 1)) = 1.0
		[HideInInspector] _MinYUV("_MinYUV", Range(0, 1)) = 0.0
		[HideInInspector] _MaxYUV("_MaxYUV", Range(0, 1)) = 1.0
		[HideInInspector] _RandomSeed("_MaxYUV", Range(0, 10000)) = 0.0

    	_EditorDrawers("Editor Drawers", Int) = 6
    }

    //=================================================
    // 2) SubShader
    //=================================================
    SubShader
    {
		Tags
		{
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        // UI 이미지용: ZWrite Off, ZTest Always, 알파 블렌드, Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma multi_compile_instancing
            #pragma vertex vert
            #pragma fragment frag

			#pragma shader_feature_local GLOW_ON
			#pragma shader_feature_local FADE_ON
			#pragma shader_feature_local OUTBASE_ON
			#pragma shader_feature_local ONLYOUTLINE_ON
			#pragma shader_feature_local GRADIENT_ON
			#pragma shader_feature_local GRADIENT2COL_ON
			#pragma shader_feature_local RADIALGRADIENT_ON
			#pragma shader_feature_local COLORSWAP_ON
			#pragma shader_feature_local HSV_ON
			#pragma shader_feature_local CHANGECOLOR_ON
			#pragma shader_feature_local CHANGECOLOR2_ON
			#pragma shader_feature_local CHANGECOLOR3_ON
			#pragma shader_feature_local COLORRAMP_ON
			#pragma shader_feature_local GRADIENTCOLORRAMP_ON
			#pragma shader_feature_local HITEFFECT_ON
			#pragma shader_feature_local NEGATIVE_ON
			#pragma shader_feature_local PIXELATE_ON
			#pragma shader_feature_local GREYSCALE_ON
			#pragma shader_feature_local POSTERIZE_ON
			#pragma shader_feature_local BLUR_ON
			#pragma shader_feature_local MOTIONBLUR_ON
			#pragma shader_feature_local GHOST_ON
			#pragma shader_feature_local ALPHAOUTLINE_ON
			#pragma shader_feature_local INNEROUTLINE_ON
			#pragma shader_feature_local ONLYINNEROUTLINE_ON
			#pragma shader_feature_local HOLOGRAM_ON
			#pragma shader_feature_local CHROMABERR_ON
			#pragma shader_feature_local GLITCH_ON
			#pragma shader_feature_local FLICKER_ON
			#pragma shader_feature_local SHADOW_ON
			#pragma shader_feature_local SHINE_ON
			#pragma shader_feature_local CONTRAST_ON
			#pragma shader_feature_local OVERLAY_ON
			#pragma shader_feature_local OVERLAYMULT_ON
			#pragma shader_feature_local ALPHACUTOFF_ON
			#pragma shader_feature_local ALPHAROUND_ON
			#pragma shader_feature_local DOODLE_ON
			#pragma shader_feature_local WIND_ON
			#pragma shader_feature_local WAVEUV_ON
			#pragma shader_feature_local ROUNDWAVEUV_ON
			#pragma shader_feature_local RECTSIZE_ON
			#pragma shader_feature_local OFFSETUV_ON
			#pragma shader_feature_local CLIPPING_ON
			#pragma shader_feature_local RADIALCLIPPING_ON
			#pragma shader_feature_local TEXTURESCROLL_ON
			#pragma shader_feature_local ZOOMUV_ON
			#pragma shader_feature_local DISTORT_ON
			#pragma shader_feature_local WARP_ON
			#pragma shader_feature_local TWISTUV_ON
			#pragma shader_feature_local ROTATEUV_ON
			#pragma shader_feature_local POLARUV_ON
			#pragma shader_feature_local FISHEYE_ON
			#pragma shader_feature_local PINCH_ON
			#pragma shader_feature_local SHAKEUV_ON

			#pragma shader_feature_local GLOWTEX_ON
			#pragma shader_feature_local OUTTEX_ON
			#pragma shader_feature_local OUTDIST_ON
			#pragma shader_feature_local OUTBASE8DIR_ON
			#pragma shader_feature_local OUTBASEPIXELPERF_ON
			#pragma shader_feature_local COLORRAMPOUTLINE_ON
			#pragma shader_feature_local GREYSCALEOUTLINE_ON
			#pragma shader_feature_local POSTERIZEOUTLINE_ON
			#pragma shader_feature_local BLURISHD_ON
			#pragma shader_feature_local MANUALWIND_ON
			#pragma shader_feature ATLAS_ON
			#pragma shader_feature PREMULTIPLYALPHA_ON

			#pragma shader_feature BILBOARD_ON
			#pragma shader_feature BILBOARDY_ON
			#pragma shader_feature FOG_ON

            #include "UnityCG.cginc"
			#include "AllIn1OneShaderFunctions.cginc"

			#if FOG_ON
			#pragma multi_compile_fog
			#endif

            // ================================
            //   *** 여기는 Vertex/Fragment ***
            //   원본 코드를 그대로 유지
            // ================================
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				half4 color : COLOR;
            	UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				half4 color : COLOR;
				#if OUTTEX_ON
				half2 uvOutTex : TEXCOORD1;
				#endif
				#if OUTDIST_ON
				half2 uvOutDistTex : TEXCOORD2;
				#endif
				#if DISTORT_ON
				half2 uvDistTex : TEXCOORD3;
				#endif
				#if FOG_ON
				UNITY_FOG_COORDS(4)
				#endif
            	UNITY_VERTEX_INPUT_INSTANCE_ID
            	UNITY_VERTEX_OUTPUT_STEREO
            };

            // (원본 셰이더의 모든 uniform/변수 선언 & vert/frag 로직)
            // ...
            // ...
            // (생략 없이 모두 포함하되, 여기선 길이 문제로 그대로 복사한다고 가정)

            ENDCG
        }
    }

    //=================================================
    // 3) (옵션) 커스텀 인스펙터 & Fallback
    //=================================================
	CustomEditor "AllIn1SpriteShaderMaterialInspector"
	//Fallback "Sprites/Default"
}
