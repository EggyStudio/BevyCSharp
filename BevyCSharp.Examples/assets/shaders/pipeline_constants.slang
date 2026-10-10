// Bevy's pipeline_constants shader in Slang, a gradient across a quad cut into LEVELS steps, LEVELS
// a constant each material's pipelines are compiled with.
import bcs2d;

/// How many steps the gradient is cut into, which a material sets as it sets any value and its
/// pipelines are compiled with, four where it sets none.
[SpecializationConstant]
const float LEVELS = 4.0;

[shader("fragment")]
float4 fragment(bcs2d::VertexOutput input) : SV_Target
{
    // The UV across, cut into LEVELS steps, so exactly LEVELS colors show.
    let t = floor(input.uv.x * LEVELS) / LEVELS;
    return float4(t, t * 0.4, 1.0 - t, 1.0);
}
