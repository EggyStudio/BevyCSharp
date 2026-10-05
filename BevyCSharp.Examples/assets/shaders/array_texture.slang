// Bevy's array_texture shader in Slang, each cube lit as a standard material whose color is the
// layer of an array texture its MeshTag names.
import bcs;

Texture2DArray my_array_texture;
SamplerState my_array_texture_sampler;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    let layer = bcs::tag(mesh.instance_index);

    var surface = bcs::surface(mesh);
    surface.base_color = my_array_texture.Sample(my_array_texture_sampler, float3(mesh.uv, float(layer)));
    return bcs::lit(surface, mesh);
}
