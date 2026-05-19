Shader "URP/Particles/BloodEffect"
{
    Properties
    {
        [Header(Color Controls)]
        [HDR] _BaseColor ("Base Color", Color) = (1,1,1,1)
        _LightStr ("Lighting Strength", Float) = 0.85
        _AlphaMin ("Alpha Clip Min", Range(-0.01, 1.01)) = 0.1
        _AlphaSoft ("Alpha Clip Softness", Range(0, 1)) = 0.022
        _EdgeDarken ("Edge Darkening", Float) = 1.0

        [Header(Mask Controls)]
        _MainTex ("Mask Texture", 2D) = "white" {}
        _MaskStr ("Mask Strength", Float) = 0.7
        _Columns ("Flipbook Columns", Int) = 1
        _Rows ("Flipbook Rows", Int) = 1
        [Toggle] _FlipU("Flip U Randomly", Float) = 0
        [Toggle] _FlipV("Flip V Randomly", Float) = 0

        [Header(Noise and Warp)]
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _WarpTex ("Warp Texture", 2D) = "white" {}
        _WarpStr ("Warp Strength", Float) = 0.1
        _Randomize ("Randomize Noise", Float) = 1.0

        [Header(Vertex Physics)]
        _FallOffset ("Gravity Offset", Range(-1, 0)) = -1.0
        _FallRandomness ("Gravity Randomness", Float) = 0.25
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog // Enables fog variants

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 uv           : TEXCOORD0; 
                float3 customData   : TEXCOORD1; 
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float4 uv           : TEXCOORD0;
                float4 color        : COLOR;
                float3 customData   : TEXCOORD4;
                half fogFactor      : TEXCOORD5; // Professional URP Fog variable
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                float4 _WarpTex_ST;
                float _LightStr;
                float _AlphaMin;
                float _AlphaSoft;
                float _EdgeDarken;
                float _MaskStr;
                int _Columns;
                int _Rows;
                float _FlipU;
                float _FlipV;
                float _WarpStr;
                float _Randomize;
                float _FallOffset;
                float _FallRandomness;
            CBUFFER_END

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            sampler2D _WarpTex;

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                // Vertex Gravity Physics
                float lifetime = input.uv.w;
                float gravityForce = (_FallOffset + ((input.uv.z - 0.5f) * _FallRandomness)) * lifetime;
                float3 fallPos = float3(0, gravityForce * input.customData.z, 0);

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz) + fallPos;
                output.positionHCS = TransformWorldToHClip(worldPos);

                // UV Calculations
                float2 uvFlip = lerp(float1(1).xx, (round(frac(float2(input.uv.z * 13, input.uv.z * 8))) * 2 - 1), float2(_FlipU, _FlipV));
                output.uv.xy = TRANSFORM_TEX(input.uv.xy * uvFlip, _MainTex);
                output.uv.zw = output.uv.xy * float2(_Columns, _Rows) + input.uv.z * float2(3, 8) * _Randomize;

                output.color = input.color;
                output.customData = input.customData;
                
                // URP Fog Calculation
                output.fogFactor = ComputeFogFactor(output.positionHCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample Warp
                float2 warpUV = input.uv.zw * _WarpTex_ST.xy + _WarpTex_ST.zw * (input.customData.x + 1);
                float2 warp = (tex2D(_WarpTex, warpUV).xy * 2 - 1) * _WarpStr * input.customData.y;

                // Sample Mask
                half4 mask = tex2D(_MainTex, input.uv.xy + warp);
                mask = saturate(lerp(1.0, mask.r, _MaskStr));

                // Alpha Clipping Logic
                half noise = tex2D(_NoiseTex, input.uv.zw + warp).r;
                half alpha = mask * noise * input.color.a;
                
                half clippedAlpha = saturate((alpha - _AlphaMin) / max(0.001, _AlphaSoft));

                half3 finalColor = _BaseColor.rgb * input.color.rgb;
                finalColor *= lerp(1.0, clippedAlpha, _EdgeDarken);
                
                // Apply URP Fog
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, clippedAlpha);
            }
            ENDHLSL
        }
    }
}