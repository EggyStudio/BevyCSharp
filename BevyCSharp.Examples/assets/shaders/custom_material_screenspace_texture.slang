// Bevy's custom_material_screenspace_texture shader in Slang, a texture read at the point of the
// screen a pixel is on rather than at the mesh's UVs, so it stays still while the mesh turns.
import bcs;

Texture2D texture;
SamplerState texture_sampler;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    return texture.Sample(texture_sampler, bcs::screen_uv(mesh.position));
}
