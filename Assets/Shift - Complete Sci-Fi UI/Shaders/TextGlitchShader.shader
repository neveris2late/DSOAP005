Shader "Custom/TMP_VHS_Glitch"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _Color ("Text Color", Color) = (1,1,1,1)
        _Aberration ("Chromatic Aberration", Range(0.0, 0.05)) = 0.01
        _Bleed ("Color Bleed Multiplier", Range(0.5, 3.0)) = 1.2
        _Smoothness ("Edge Crispness", Range(0.01, 0.2)) = 0.05

        // UI Canvas 所需的遮罩和模板测试属性
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
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _Aberration;
            float _Bleed;
            float _Smoothness;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 1. 计算 RGB 分离的 UV 坐标 (横向偏移)
                float2 uvR = i.texcoord + float2(_Aberration, 0);
                float2 uvG = i.texcoord;
                float2 uvB = i.texcoord - float2(_Aberration, 0);

                // 2. 采样 TMP 的字体图集。TMP 将形状信息存在 Alpha 通道中
                float aR = tex2D(_MainTex, uvR).a;
                float aG = tex2D(_MainTex, uvG).a;
                float aB = tex2D(_MainTex, uvB).a;

                // 3. 将原始 SDF 距离场转换为清晰的字体边缘 (0.5 是 TMP 的默认边界值)
                float maskR = smoothstep(0.5 - _Smoothness, 0.5 + _Smoothness, aR);
                float maskG = smoothstep(0.5 - _Smoothness, 0.5 + _Smoothness, aG);
                float maskB = smoothstep(0.5 - _Smoothness, 0.5 + _Smoothness, aB);

                // 4. 重构颜色：红绿蓝通道独立应用 Mask，并乘以顶点颜色和溢色强度
                fixed3 rgb = fixed3(maskR, maskG, maskB) * i.color.rgb * _Bleed;
                
                // 5. 计算最终 Alpha (只要有任意一个通道有像素，就显示)
                float finalAlpha = max(max(maskR, maskG), maskB) * i.color.a;
                fixed4 finalColor = fixed4(rgb, finalAlpha);

                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (finalColor.a - 0.001);
                #endif

                return finalColor;
            }
            ENDCG
        }
    }
}