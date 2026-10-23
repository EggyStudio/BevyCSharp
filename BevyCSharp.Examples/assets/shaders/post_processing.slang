// Bevy's post_processing shader in Slang, a chromatic aberration that reads each color of the
// picture a little apart from the others, by as much as the settings say.
import bcs_pass;

struct PostProcessSettings
{
    float intensity;
};

uniform PostProcessSettings settings;

[shader("fragment")]
float4 fragment(FullscreenOutput input) : SV_Target
{
    let offset_strength = settings.intensity;
    return float4(
        passSample(input.uv + float2(offset_strength, -offset_strength)).r,
        passSample(input.uv + float2(-offset_strength, 0.0)).g,
        passSample(input.uv + float2(0.0, offset_strength)).b,
        1.0);
}
