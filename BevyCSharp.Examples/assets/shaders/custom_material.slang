// Bevy's custom_material shader in Slang, the material's color times its texture, times a value
// imported from another module, which halves the alpha.
import bcs;
import custom_material_import;

uniform float4 material_color;
Texture2D material_color_texture;
SamplerState material_color_sampler;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    return material_color * material_color_texture.Sample(material_color_sampler, mesh.uv) * COLOR_MULTIPLIER;
}
