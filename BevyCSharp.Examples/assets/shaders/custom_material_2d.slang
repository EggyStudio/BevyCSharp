// Bevy's custom_material_2d shader in Slang, the material's color times its texture on a 2D mesh,
// times a value imported from another module, which halves the alpha.
import bcs2d;
import custom_material_import;

uniform float4 material_color;
Texture2D base_color_texture;
SamplerState base_color_sampler;

[shader("fragment")]
float4 fragment(bcs2d::VertexOutput mesh) : SV_Target
{
    return material_color * base_color_texture.Sample(base_color_sampler, mesh.uv) * COLOR_MULTIPLIER;
}
