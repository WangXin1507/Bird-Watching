// Used by a DrawRenderers custom pass with Override Mode = Shader.
// Re-draws visible objects into the Custom Color Buffer as flat albedo (alpha = 1 where an object is).
// Property names match HDRP/Lit, so each object's own colour + texture carry over automatically.
Shader "Renderers/Painterly/PaintAlbedo"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BaseColorMap ("Base Map", 2D) = "white" {}
        _AlphaCutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
    }

    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
    #pragma multi_compile_instancing
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "FirstPass"
            Tags { "LightMode" = "FirstPass" }
            Blend Off
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #define _ALPHATEST_ON
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD0

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassRenderers.hlsl"

            TEXTURE2D(_BaseColorMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColorMap_ST;
                float4 _BaseColor;
            CBUFFER_END
            // _AlphaCutoff is already declared by HDRP's CustomPassRenderers.hlsl includes

            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 viewDirection, inout PositionInputs posInput,
                                          out SurfaceData surfaceData, out BuiltinData builtinData)
            {
                float2 uv = TRANSFORM_TEX(fragInputs.texCoord0.xy, _BaseColorMap);
                float4 c = SAMPLE_TEXTURE2D(_BaseColorMap, s_trilinear_repeat_sampler, uv) * _BaseColor;
                DoAlphaTest(c.a, _AlphaCutoff);

                ZERO_BUILTIN_INITIALIZE(builtinData);
                ZERO_INITIALIZE(SurfaceData, surfaceData);
                builtinData.opacity = 1;          // alpha 1 = "object here" mask for the full-screen passes
                builtinData.emissiveColor = 0;
                surfaceData.color = c.rgb;
            }

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl"
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
