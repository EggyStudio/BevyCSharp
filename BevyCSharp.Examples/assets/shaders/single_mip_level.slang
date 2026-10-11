// Bevy's single_mip_level shader in Slang, which shows one mip level of a texture and no other.
import bcs2d;

uniform uint mip_level;
Texture2D base_color_texture;
SamplerState base_color_sampler;

[shader("fragment")]
float4 fragment(bcs2d::VertexOutput mesh) : SV_Target
{
    return base_color_texture.SampleLevel(base_color_sampler, mesh.uv, float(mip_level));
}
