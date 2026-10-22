// Full-screen custom pass #1: replaces physically-based shading with painted shading.
//   lighting = sceneColor / albedo  (recovers HDRP's real light + shadows + ambient per pixel)
//   -> broken up by world-space brush strokes -> hard-ish step -> albedo or albedo * shadow tint
// Needs: PaintAlbedo DrawRenderers pass earlier in the same volume (fills the Custom Color Buffer).
// FullScreen pass settings: Target = Camera, Fetch Color Buffer = ON.
Shader "FullScreen/Painterly/PaintedShading"
{
    Properties
    {
        _ShadowTint ("Shadow Tint (x albedo)", Color) = (1.0, 0.55, 0.45, 1)
        _VariationTint ("Stroke Colour Variation (x albedo)", Color) = (1.0, 0.72, 0.6, 1)
        _VariationAmount ("Variation Amount", Range(0, 1)) = 0.8
        _StrokeTex ("Stroke Texture", 2D) = "gray" {}
        _StrokeScale ("Stroke Scale (world)", Float) = 1.5
        _StrokeAmount ("Stroke Breakup", Range(0, 1)) = 0.45
        _Threshold ("Light Threshold", Range(0, 1)) = 0.35
        _Softness ("Edge Softness", Range(0.001, 0.5)) = 0.04
        _SunLux ("Sun Intensity (lux)", Float) = 100000
    }

    HLSLINCLUDE
    #pragma vertex Vert
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

    TEXTURE2D(_StrokeTex);
    float4 _ShadowTint, _VariationTint;
    float _VariationAmount, _StrokeScale, _StrokeAmount, _Threshold, _Softness, _SunLux;

    float StrokeTriplanar(float3 p, float3 n)
    {
        float3 w = pow(abs(n), 4);
        w /= dot(w, 1);
        p *= _StrokeScale;
        float x = SAMPLE_TEXTURE2D_LOD(_StrokeTex, s_linear_repeat_sampler, p.zy, 0).r;
        float y = SAMPLE_TEXTURE2D_LOD(_StrokeTex, s_linear_repeat_sampler, p.xz, 0).r;
        float z = SAMPLE_TEXTURE2D_LOD(_StrokeTex, s_linear_repeat_sampler, p.xy, 0).r;
        return x * w.x + y * w.y + z * w.z;
    }

    float4 FullScreenPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float2 pss = varyings.positionCS.xy;

        float3 scene  = CustomPassLoadCameraColor(pss, 0);
        float4 albedo = CustomPassLoadCustomColor(pss);
        if (albedo.a < 0.5)
            return float4(scene, 1);                    // sky/background: strokes pass turns it into paper

        float depth = LoadCameraDepth(pss);
        PositionInputs posInput = GetPositionInput(pss, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        float3 positionWS = GetAbsolutePositionWS(posInput.positionWS);
        NormalData nd;
        DecodeFromNormalBuffer(posInput.positionSS, nd);

        float stroke  = StrokeTriplanar(positionWS, nd.normalWS);
        float stroke2 = StrokeTriplanar(positionWS * 0.43 + 17.1, nd.normalWS);

        // HDRP diffuse = albedo/PI * E * NdotL, pre-exposed. Divide both out -> ~NdotL*shadow (+ambient)
        float lit = Luminance(scene) / (GetCurrentExposureMultiplier() * max(Luminance(albedo.rgb), 0.02));
        lit *= PI / max(_SunLux, 1e-3);
        lit += (stroke - 0.5) * _StrokeAmount;
        lit = smoothstep(_Threshold - _Softness, _Threshold + _Softness, lit);

        float3 baseCol = albedo.rgb * lerp(1, _VariationTint.rgb, stroke2 * _VariationAmount);
        float3 col = baseCol * lerp(_ShadowTint.rgb, 1, lit);
        return float4(col, 1);
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "Custom Pass 0"
            ZWrite Off ZTest Always Blend Off Cull Off
            HLSLPROGRAM
            #pragma fragment FullScreenPass
            ENDHLSL
        }
    }
    Fallback Off
}
