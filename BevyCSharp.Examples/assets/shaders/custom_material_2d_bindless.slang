// Bevy's custom_material_2d_bindless shader in Slang, the material's color times its texture,
// sampled at the top mip level, on a 2D mesh.
import bcs2d;

struct Color
{
    float4 base_color;
};

uniform Color material_color;
Texture2D material_color_texture;
SamplerState material_color_sampler;

[shader("fragment")]
float4 fragment(bcs2d::VertexOutput mesh) : SV_Target
{
    return material_color.base_color * material_color_texture.SampleLevel(material_color_sampler, mesh.uv, 0.0);
}
