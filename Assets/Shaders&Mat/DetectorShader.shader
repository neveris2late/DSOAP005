Shader "UI/DynamicCurveBar_3Stages_Ascending"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _FillAmount ("Fill Amount", Range(0, 1)) = 1.0
        
        [Header(Stage Thresholds (Ascending Danger))]
        _Threshold1 ("Stage 1 to 2 Threshold", Range(0, 1)) = 0.4
        _Threshold2 ("Stage 2 to 3 Threshold", Range(0, 1)) = 0.7
        _BlendSoftness ("State Transition Softness", Range(0.001, 0.2)) = 0.05

        [Header(Background Shape Settings)]
        _BgColor1 ("Bg Color 1 (Top)", Color) = (0.2, 0.2, 0.2, 1.0)
        _BgColor2 ("Bg Color 2 (Bottom)", Color) = (0.05, 0.05, 0.05, 1.0)
        _BgAlpha ("Bg Total Alpha", Range(0, 1)) = 0.8
        _BgEndThickness ("Bg End Thickness", Range(0.0, 0.5)) = 0.05
        _BgCenterThickness ("Bg Center Thickness", Range(0.0, 0.5)) = 0.15

        [Header(Stage 1 Settings (Low Fill Stable))]
        _Stage1Color ("Color", Color) = (0.0, 0.8, 1.0, 1.0)
        _Stage1Speed ("Speed", Float) = 2.0
        _Stage1Freq1 ("Frequency 1", Float) = 15.0
        _Stage1Freq2 ("Frequency 2", Float) = 20.0
        _Stage1Amp1 ("Amplitude 1", Float) = 0.15
        _Stage1Amp2 ("Amplitude 2", Float) = 0.1
        _Stage1Thickness ("Line Thickness", Float) = 0.01

        [Header(Stage 2 Settings (Medium Fill Warning))]
        _Stage2Color ("Color", Color) = (1.0, 0.8, 0.0, 1.0)
        _Stage2Speed ("Speed", Float) = 4.0
        _Stage2Freq1 ("Frequency 1", Float) = 15.0
        _Stage2Freq2 ("Frequency 2", Float) = 40.0
        _Stage2Amp1 ("Amplitude 1", Float) = 0.1
        _Stage2Amp2 ("Amplitude 2", Float) = 0.1
        _Stage2Thickness ("Line Thickness", Float) = 0.015

        [Header(Stage 3 Settings (High Fill Dangerous))]
        _Stage3Color ("Color", Color) = (1.0, 0.2, 0.0, 1.0)
        _Stage3Speed ("Speed", Float) = 6.0
        _Stage3Freq1 ("Frequency 1", Float) = 15.0
        _Stage3Freq2 ("Frequency 2", Float) = 60.0
        _Stage3Amp1 ("Amplitude 1", Float) = 0.0
        _Stage3Amp2 ("Amplitude 2", Float) = 0.1
        _Stage3Thickness ("Line Thickness", Float) = 0.025
        _Stage3BlinkSpeed ("Blink Speed", Float) = 10.0
        
        [Header(Global Visual Settings)]
        _GlowSpread ("Glow Spread", Float) = 15.0
        
        [Header(Cursor Settings)]
        [HDR] _CursorColor ("Cursor Color (HDR)", Color) = (1.0, 1.0, 1.0, 1.0)
        _CursorWidth ("Cursor Thickness (Width)", Range(0.001, 0.1)) = 0.005
        _CursorHeight ("Cursor Height", Range(0.01, 0.5)) = 0.1
        _CursorGlow ("Cursor Glow Intensity", Range(0.0, 10.0)) = 2.0
        
        // UI Standard Properties
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off Lighting Off ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            float _FillAmount;
            
            // Thresholds
            float _Threshold1;
            float _Threshold2;
            float _BlendSoftness;

            // Background
            float4 _BgColor1;
            float4 _BgColor2;
            float _BgAlpha;
            float _BgEndThickness;
            float _BgCenterThickness;
            
            // Stage 1
            float4 _Stage1Color; float _Stage1Speed; float _Stage1Freq1; float _Stage1Freq2; float _Stage1Amp1; float _Stage1Amp2; float _Stage1Thickness;
            // Stage 2
            float4 _Stage2Color; float _Stage2Speed; float _Stage2Freq1; float _Stage2Freq2; float _Stage2Amp1; float _Stage2Amp2; float _Stage2Thickness;
            // Stage 3
            float4 _Stage3Color; float _Stage3Speed; float _Stage3Freq1; float _Stage3Freq2; float _Stage3Amp1; float _Stage3Amp2; float _Stage3BlinkSpeed; float _Stage3Thickness;

            float _GlowSpread;
            
            // Cursor
            float4 _CursorColor;
            float _CursorWidth;
            float _CursorHeight;
            float _CursorGlow;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color;
                return OUT;
            }

            // 计算当前FillAmount处于哪个阶段的权重 (FillAmount越高越危险)
            void GetStageWeights(out float w1, out float w2, out float w3)
            {
                float t1 = smoothstep(_Threshold1 - _BlendSoftness, _Threshold1 + _BlendSoftness, _FillAmount);
                float t2 = smoothstep(_Threshold2 - _BlendSoftness, _Threshold2 + _BlendSoftness, _FillAmount);

                w1 = 1.0 - t1;
                w2 = t1 - t2;
                w3 = t2;
            }

            void GetCurveY(float u, out float y1, out float y2)
            {
                float w1, w2, w3;
                GetStageWeights(w1, w2, w3);

                // 根据权重混合三个阶段的参数
                float currentSpeed = _Stage1Speed * w1 + _Stage2Speed * w2 + _Stage3Speed * w3;
                float currentFreq1 = _Stage1Freq1 * w1 + _Stage2Freq1 * w2 + _Stage3Freq1 * w3;
                float currentFreq2 = _Stage1Freq2 * w1 + _Stage2Freq2 * w2 + _Stage3Freq2 * w3;
                float currentAmp1  = _Stage1Amp1 * w1 + _Stage2Amp1 * w2 + _Stage3Amp1 * w3;
                float currentAmp2  = _Stage1Amp2 * w1 + _Stage2Amp2 * w2 + _Stage3Amp2 * w3;

                float envelope = smoothstep(0.0, 0.65, u) * smoothstep(1.0, 0.65, u);
                
                y1 = 0.5 + envelope * currentAmp1 * sin(u * currentFreq1 - _Time.y * currentSpeed);
                y2 = 0.5 + envelope * currentAmp2 * cos(u * currentFreq2 - _Time.y * currentSpeed);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;

                // --- 1. 计算渐变背景层 ---
                float bgEnvelope = sin(uv.x * UNITY_PI);
                float currentBgThickness = lerp(_BgEndThickness, _BgCenterThickness, bgEnvelope);
                float bgDist = abs(uv.y - 0.5);
                float bgShapeMask = smoothstep(currentBgThickness, max(0.0, currentBgThickness - 0.01), bgDist);
                float gradientFactor = saturate((uv.y - (0.5 - currentBgThickness)) / (currentBgThickness * 2.0 + 0.0001));
                fixed4 bgColor = lerp(_BgColor1, _BgColor2, gradientFactor);
                bgColor.a *= _BgAlpha * bgShapeMask * IN.color.a;

                // 获取阶段权重以计算颜色、闪烁效果和线宽
                float w1, w2, w3;
                GetStageWeights(w1, w2, w3);

                // --- 2. 计算前景曲线与光标层 ---
                float y1, y2;
                GetCurveY(uv.x, y1, y2);
                
                float dist1 = abs(uv.y - y1);
                float dist2 = abs(uv.y - y2);
                
                // 动态插值计算当前线宽
                float currentThickness = _Stage1Thickness * w1 + _Stage2Thickness * w2 + _Stage3Thickness * w3;

                // 使用计算好的动态线宽平滑绘制线条
                float line1 = smoothstep(currentThickness, 0.0, dist1);
                float line2 = smoothstep(currentThickness, 0.0, dist2);
                
                float glow1 = exp(-dist1 * _GlowSpread);
                float glow2 = exp(-dist2 * _GlowSpread);

                float lineMask = step(uv.x, _FillAmount);
                float lineAlpha = saturate(line1 + line2 + glow1 * 0.5 + glow2 * 0.5) * lineMask;

                // 光标垂直位置固定在 0.5，不随波浪晃动
                float cursorY = 0.5;
                float2 cursorUV = float2(uv.x - _FillAmount, uv.y - cursorY);
                cursorUV.x /= max(_CursorWidth, 0.0001);
                cursorUV.y /= max(_CursorHeight, 0.0001);
                float cursorDist = length(cursorUV);
                float cursorCore = smoothstep(1.0, 0.2, cursorDist);
                float cursorGlowArea = exp(-cursorDist * 2.0) * _CursorGlow;
                float totalCursorAlpha = saturate(cursorCore + cursorGlowArea);

                // 只有在Stage 3 (最高危险阶段) 时才表现出明显的闪烁效果
                float blink = lerp(1.0, (sin(_Time.y * _Stage3BlinkSpeed) * 0.5 + 0.5) * 0.5 + 0.5, w3);
                float4 baseLineColor = (_Stage1Color * w1 + _Stage2Color * w2 + _Stage3Color * w3) * blink;
                
                float totalFgAlpha = saturate(lineAlpha + totalCursorAlpha);
                float cursorWeight = totalCursorAlpha / max(totalFgAlpha, 0.0001);
                float3 finalFgRGB = lerp(baseLineColor.rgb, _CursorColor.rgb, cursorWeight);

                float4 foregroundColor = float4(finalFgRGB, totalFgAlpha * IN.color.a);

                // --- 3. 图层混合 ---
                fixed4 finalColor;
                finalColor.rgb = foregroundColor.rgb * foregroundColor.a + bgColor.rgb * bgColor.a * (1.0 - foregroundColor.a);
                finalColor.a = foregroundColor.a + bgColor.a * (1.0 - foregroundColor.a);
                
                return finalColor;
            }
            ENDCG
        }
    }
}