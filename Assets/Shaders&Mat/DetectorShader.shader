Shader "UI/DynamicCurveBar_Standardized_WithAdjustableEnds"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _FillAmount ("Fill Amount", Range(0, 1)) = 1.0
        _WarningLerp ("Warning State (0-Normal, 1-Warn)", Range(0, 1)) = 0.0
        
        [Header(Background Shape Settings)]
        _BgColor1 ("Bg Color 1 (Top)", Color) = (0.2, 0.2, 0.2, 1.0)
        _BgColor2 ("Bg Color 2 (Bottom)", Color) = (0.05, 0.05, 0.05, 1.0)
        _BgAlpha ("Bg Total Alpha", Range(0, 1)) = 0.8
        // 新增：控制两端的粗细
        _BgEndThickness ("Bg End Thickness", Range(0.0, 0.5)) = 0.05
        // 新增：控制中间最粗处的粗细
        _BgCenterThickness ("Bg Center Thickness", Range(0.0, 0.5)) = 0.15

        [Header(Normal State Settings)]
        _NormalColor ("Normal Color", Color) = (0.0, 0.8, 1.0, 1.0)
        _NormalSpeed ("Normal Speed", Float) = 2.0
        _NormalFreq1 ("Normal Frequency 1", Float) = 15.0
        _NormalFreq2 ("Normal Frequency 2", Float) = 20.0
        _NormalAmp1 ("Normal Amplitude 1", Float) = 0.15
        _NormalAmp2 ("Normal Amplitude 2", Float) = 0.1

        [Header(Warning State Settings)]
        _WarningColor ("Warning Color", Color) = (1.0, 0.2, 0.0, 1.0)
        _WarningSpeed ("Warning Speed", Float) = 6.0
        _WarningFreq1 ("Warning Frequency 1", Float) = 15.0
        _WarningFreq2 ("Warning Frequency 2", Float) = 60.0
        _WarningAmp1 ("Warning Amp 1 (0 for straight line)", Float) = 0.0
        _WarningAmp2 ("Warning Amplitude 2", Float) = 0.1
        _WarningBlinkSpeed ("Warning Blink Speed", Float) = 10.0
        
        [Header(Global Visual Settings)]
        _Thickness ("Line Thickness", Float) = 0.01
        _GlowSpread ("Glow Spread", Float) = 15.0
        _CursorSize ("Cursor Size", Float) = 0.05
        
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

        Cull Off
        Lighting Off
        ZWrite Off
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

            // 控制参数
            float _FillAmount;
            float _WarningLerp;

            // 背景参数
            float4 _BgColor1;
            float4 _BgColor2;
            float _BgAlpha;
            float _BgEndThickness;
            float _BgCenterThickness;
            
            // 普通状态参数
            float4 _NormalColor;
            float _NormalSpeed;
            float _NormalFreq1;
            float _NormalFreq2;
            float _NormalAmp1;
            float _NormalAmp2;

            // 警告状态参数
            float4 _WarningColor;
            float _WarningSpeed;
            float _WarningFreq1;
            float _WarningFreq2;
            float _WarningAmp1;
            float _WarningAmp2;
            float _WarningBlinkSpeed;

            // 全局视觉参数
            float _Thickness;
            float _GlowSpread;
            float _CursorSize;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color;
                return OUT;
            }

            void GetCurveY(float u, out float y1, out float y2)
            {
                float envelope = smoothstep(0.0, 0.65, u) * smoothstep(1.0, 0.65, u);
                
                float currentSpeed = lerp(_NormalSpeed, _WarningSpeed, _WarningLerp);
                float currentFreq1 = lerp(_NormalFreq1, _WarningFreq1, _WarningLerp);
                float currentFreq2 = lerp(_NormalFreq2, _WarningFreq2, _WarningLerp);
                float currentAmp1  = lerp(_NormalAmp1, _WarningAmp1, _WarningLerp);
                float currentAmp2  = lerp(_NormalAmp2, _WarningAmp2, _WarningLerp);

                y1 = 0.5 + envelope * currentAmp1 * sin(u * currentFreq1 - _Time.y * currentSpeed);
                y2 = 0.5 + envelope * currentAmp2 * cos(u * currentFreq2 - _Time.y * currentSpeed);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                
                // --- 1. 计算改良后的渐变背景层 ---
                
                // 正弦包络线：两端为0，中间为1
                float bgEnvelope = sin(uv.x * UNITY_PI); 
                
                // 核心修改：使用 Lerp 在“两端粗细”和“中间粗细”之间进行插值计算
                float currentBgThickness = lerp(_BgEndThickness, _BgCenterThickness, bgEnvelope);
                
                float bgDist = abs(uv.y - 0.5);
                
                float bgShapeMask = smoothstep(currentBgThickness, max(0.0, currentBgThickness - 0.01), bgDist);
                
                float gradientFactor = saturate((uv.y - (0.5 - currentBgThickness)) / (currentBgThickness * 2.0 + 0.0001));
                
                fixed4 bgColor = lerp(_BgColor1, _BgColor2, gradientFactor);
                bgColor.a *= _BgAlpha * bgShapeMask * IN.color.a;

                // --- 2. 计算前景曲线层 ---
                
                float4 foregroundColor = fixed4(0,0,0,0);
                
                if (uv.x <= _FillAmount)
                {
                    float y1, y2;
                    GetCurveY(uv.x, y1, y2);

                    float dist1 = abs(uv.y - y1);
                    float dist2 = abs(uv.y - y2);

                    float line1 = smoothstep(_Thickness, 0.0, dist1);
                    float line2 = smoothstep(_Thickness, 0.0, dist2);

                    float glow1 = exp(-dist1 * _GlowSpread);
                    float glow2 = exp(-dist2 * _GlowSpread);

                    float cursorY1, cursorY2;
                    GetCurveY(_FillAmount, cursorY1, cursorY2);
                    float cursorY = (cursorY1 + cursorY2) * 0.5; 
                    
                    float2 cursorUV = float2((uv.x - _FillAmount) * 5.0, uv.y - cursorY);
                    float cursorDist = length(cursorUV);
                    float cursorGlow = smoothstep(_CursorSize, 0.0, cursorDist) * 1.5;

                    float totalAlpha = saturate(line1 + line2 + glow1 * 0.5 + glow2 * 0.5 + cursorGlow);
                    
                    float blink = lerp(1.0, (sin(_Time.y * _WarningBlinkSpeed) * 0.5 + 0.5) * 0.5 + 0.5, _WarningLerp);
                    foregroundColor = lerp(_NormalColor, _WarningColor, _WarningLerp) * blink;
                    
                    foregroundColor.a = totalAlpha * IN.color.a;
                }

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