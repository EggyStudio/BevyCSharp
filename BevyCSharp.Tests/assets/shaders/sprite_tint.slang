// A sprite's material that draws the sprite as Bevy would and multiplies it by `tint`.
import bcs2d;
import bcs_sprite;

uniform float4 tint;

[shader("fragment")]
float4 fragment(Mesh2dVertexOutput input) : SV_Target
{
    return spriteSampleFinalColor(input.uv) * tint;
}
