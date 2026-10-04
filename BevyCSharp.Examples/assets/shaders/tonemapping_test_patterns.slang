// Bevy's tonemapping_test_patterns.wgsl in Slang: a sweep of hues over fifteen stops of exposure
// on the top half, and a continuous hue sweep growing brighter on the bottom half, written in
// linear light for the camera's tonemapping to map.
import bcs;

static const float PI = 3.14159265358979;

// Hues down the y axis and values from nothing to fifteen stops across the x axis, both in 24
// steps, and a grey row under them.
float3 color_sweep(float2 uv_input)
{
    var uv = uv_input;
    let steps = 24.0;
    uv.y = uv.y * (1.0 + 1.0 / steps);
    let ratio = 2.0;

    let h = PI * 2.0 * floor(1.0 + steps * uv.y) / steps;
    let L = floor(uv.x * steps * ratio) / (steps * ratio) - 0.5;

    var color = float3(0.0);
    if (uv.y < 1.0)
    {
        color = cos(h + float3(0.0, 1.0, 2.0) * PI * 2.0 / 3.0);
        let maxRGB = max(color.r, max(color.g, color.b));
        let minRGB = min(color.r, min(color.g, color.b));
        color = exp(15.0 * L) * (color - minRGB) / (maxRGB - minRGB);
    }
    else
    {
        color = float3(exp(15.0 * L));
    }
    return color;
}

float3 hsv_to_srgb(float3 c)
{
    let K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    let p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, clamp(p - K.xxx, float3(0.0), float3(1.0)), c.y);
}

// A continuous sweep of hues.
float3 continuous_hue(float2 uv)
{
    return hsv_to_srgb(float3(uv.x, 1.0, 1.0)) * max(0.0, exp2(uv.y * 9.0) - 1.0);
}

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    var uv = mesh.uv;
    var result = float3(0.0);
    if (uv.y > 0.5)
    {
        uv.y = 1.0 - uv.y;
        result = color_sweep(float2(uv.x, uv.y * 2.0));
    }
    else
    {
        result = continuous_hue(float2(uv.y * 2.0, uv.x));
    }
    return float4(result, 1.0);
}
