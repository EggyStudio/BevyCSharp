// A sprite's material that draws the sprite as Bevy would and multiplies it by `tint`.
import bcs2d;
import bcs_sprite;

uniform float4 tint;

[shader("fragment")]
float4 fragment(bcs2d::VertexOutput input) : SV_Target
{
    return bcs_sprite::sample_final_color(input.uv) * tint;
}
