// Full-screen custom pass #2: brush strokes on paper (HDRP port of the URP PaintedPost).
//  1. Noise UV jitter re-rolled at _FPS (ragged edges + boil)
//  2. Smear colour along local edge direction (Sobel); flat areas -> horizontal strokes
//  3. Background becomes paper; object edges smear into it
//  4. Paper grain over everything
// FullScreen pass settings: Target = Camera, Fetch Color Buffer = ON. Runs after PaintedShading.
Shader "FullScreen/Painterly/PaintedStrokes"
{
    Properties
    {
        _PaperTex ("Paper Texture", 2D) = "white" {}
        _PaperScale ("Paper Scale", Float) = 1.5
        _PaperGrain ("Paper Grain Strength", Range(0, 1)) = 0.6
        _NoiseTex ("Jitter Noise (RG)", 2D) = "gray" {}
        _NoiseScale ("Jitter Noise Scale", Float) = 4
        _Jitter ("Edge Jitter (px)", Range(0, 12)) = 4
        _StrokeLength ("Stroke Length (px)", Range(0, 40)) = 14
        _Steps ("Stroke Samples (each side)", Range(1, 12)) = 6
        _HorizontalBias ("Flat-Area Horizontal Bias", Range(0, 50)) = 12
        _FPS ("Boil FPS", Range(1, 30)) = 12
        _SkyBlend ("Sky (0 = paper, 1 = painted sky)", Range(0, 1)) = 0
    }

    HLSLINCLUDE
    #pragma vertex Vert
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    TEXTURE2D(_PaperTex);
    TEXTURE2D(_NoiseTex);
    float _PaperScale, _PaperGrain, _NoiseScale, _Jitter, _StrokeLength, _Steps, _HorizontalBias, _FPS, _SkyBlend;

    float3 Scene(float2 uv)    { return CustomPassSampleCameraColor(uv, 0).rgb; }
    float  Luma(float2 uv)     { return Luminance(Scene(uv)); }
    float  IsObject(float2 uv) { return SampleCameraDepth(uv) != UNITY_RAW_FAR_CLIP_VALUE ? 1 : 0; }

    float4 FullScreenPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float2 px = _ScreenSize.zw;
        float2 uv0 = varyings.positionCS.xy * px;
        float2 uv = uv0;
        float aspect = _ScreenSize.x / _ScreenSize.y;

        // 1. Stepped-time noise jitter
        float t = floor(_Time.y * _FPS);
        float2 seed = frac(t * float2(0.3713, 0.7191));
        float2 n = SAMPLE_TEXTURE2D_LOD(_NoiseTex, s_linear_repeat_sampler,
                                        uv * float2(aspect, 1) * _NoiseScale + seed, 0).rg * 2 - 1;
        uv += n * _Jitter * px;

        // 2. Edge direction from Sobel on luminance
        float tl = Luma(uv + px * float2(-1,  1)), tc = Luma(uv + px * float2(0,  1)), tr = Luma(uv + px * float2(1,  1));
        float ml = Luma(uv + px * float2(-1,  0)),                                     mr = Luma(uv + px * float2(1,  0));
        float bl = Luma(uv + px * float2(-1, -1)), bc = Luma(uv + px * float2(0, -1)), br = Luma(uv + px * float2(1, -1));
        float2 g = float2((tr + 2 * mr + br) - (tl + 2 * ml + bl),
                          (tl + 2 * tc + tr) - (bl + 2 * bc + br));
        float gLen = length(g);

        float2 dir = gLen > 1e-4 ? float2(-g.y, g.x) / gLen : float2(1, 0);
        dir *= dir.x < 0 ? -1 : 1;
        dir = normalize(lerp(float2(1, 0), dir, saturate(gLen * _HorizontalBias)) + 1e-5);
        dir = normalize(dir + n * 0.25);

        // 3. Smear along stroke direction, tracking object coverage vs paper
        int steps = (int)_Steps;
        float3 acc = 0; float3 accAll = 0; float wObj = 0; float wSum = 0;
        [loop] for (int s = -steps; s <= steps; s++)
        {
            float w = 1.0 - abs(s) / (steps + 1.0);
            float2 suv = saturate(uv + dir * (s * _StrokeLength / steps) * px);
            float o = IsObject(suv);
            float3 c = Scene(suv);
            acc    += c * w * o;
            accAll += c * w;
            wObj   += w * o;
            wSum   += w;
        }
        float coverage = wObj / wSum;
        float3 paint = wObj > 0 ? acc / wObj : 0;
        float3 skyPaint = accAll / wSum;   // sky gets the same stroke smear

        // 4. Paper
        float3 paper = SAMPLE_TEXTURE2D_LOD(_PaperTex, s_linear_repeat_sampler,
                                            uv0 * float2(aspect, 1) * _PaperScale, 0).rgb;
        float grain = lerp(1, Luminance(paper) / 0.92, _PaperGrain);

        float3 background = lerp(paper, skyPaint * grain, _SkyBlend);
        float3 col = lerp(background, paint * grain, smoothstep(0.25, 0.6, coverage));
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
