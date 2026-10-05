// Bevy's fullscreen_effect shader in Slang, the same chromatic aberration as post_processing.slang
// as a full screen material reads it.
import bcs_pass;

struct FullScreenEffect
{
    float intensity;
};

uniform FullScreenEffect settings;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    let offset_strength = settings.intensity;
    return float4(
        bcs_pass::sample(input.uv + float2(offset_strength, -offset_strength)).r,
        bcs_pass::sample(input.uv + float2(-offset_strength, 0.0)).g,
        bcs_pass::sample(input.uv + float2(0.0, offset_strength)).b,
        1.0);
}
