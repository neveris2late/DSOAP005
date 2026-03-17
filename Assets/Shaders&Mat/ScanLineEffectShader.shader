Shader "UI/DeathStranding_DepthScanner"
{
    Properties
    {
        // UI系统会自动将Image组件的图片赋值给 _MainTex
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // 你的深度图（需要你在材质面板手动赋值）
        _DepthTex ("Depth Map (Z-Axis)", 2D) = "black" {}

        [Header(Scanner Settings)]
        [HDR] _ScanColor ("Scan Color", Color) = (0.0, 0.8, 1.0, 1.0)
        _ScanSpeed ("Propagation Speed (Z-Axis)", Float) = 0.5
        _ScanFreq ("Wave Frequency (Z-Axis)", Float) = 1.0
        _ScanWidth ("Wave Width", Range(0.001, 0.5)) = 0.05
        
        [Header(Horizontal Line Settings)]
        _LineDensity ("Y-Axis Line Density", Float) = 100.0
        _LineSharpness ("Y-Axis Line Sharpness", Range(0.1, 10.0)) = 2.0
        
        // Unity UI 必须的属性，用于支持遮罩(Mask)等功能
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
            #include "UnityUI.cginc"

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

            sampler2D _MainTex;
            sampler2D _DepthTex;
            fixed4 _Color;
            
            float4 _ScanColor;
            float _ScanSpeed;
            float _ScanFreq;
            float _ScanWidth;
            
            float _LineDensity;
            float _LineSharpness;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 1. 采样原图颜色
                half4 mainColor = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                // 2. 采样深度图 (读取R通道作为Z轴深度)
                float depth = tex2D(_DepthTex, IN.texcoord).r;

                // 3. 计算Z轴的传播进度 (Time驱动)
                // frac() 保证波纹周期性重复
                float wavePhase = frac(depth * _ScanFreq - _Time.y * _ScanSpeed);

                // 4. 生成扫描波的主体和拖尾
                // lineCore 是最亮的前端，trail 是后面的余晖
                float lineCore = smoothstep(1.0 - _ScanWidth, 1.0, wavePhase);
                float trail = smoothstep(1.0 - _ScanWidth * 5.0, 1.0, wavePhase) * 0.3;
                float depthWave = lineCore + trail;

                // 5. 生成Y轴方向的横线 (类似百叶窗或CRT扫描线)
                // 将 Y轴 UV 乘以密度，放入正弦函数
                float yLines = sin(IN.texcoord.y * _LineDensity);
                // 增加对比度让线条更锐利
                yLines = pow(yLines * 0.5 + 0.5, _LineSharpness);

                // 6. 混合：将Z轴波纹与Y轴横线相乘，再乘以你暴露的颜色
                float3 finalScanColor = _ScanColor.rgb * (depthWave * yLines);

                // 7. 将扫描颜色叠加到原图上，并保护原图的Alpha透明度
                mainColor.rgb += finalScanColor * _ScanColor.a;

                // 如果原图是全透明的地方，不显示扫描线 (保留UI边缘轮廓)
                mainColor.a = mainColor.a;

                return mainColor;
            }
            ENDCG
        }
    }
}