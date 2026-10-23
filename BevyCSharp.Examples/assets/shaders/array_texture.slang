// Bevy's array_texture shader in Slang, each cube lit as a standard material whose color is the
// layer of an array texture its MeshTag names.
import bcs;

Texture2DArray my_array_texture;
SamplerState my_array_texture_sampler;

[shader("fragment")]
float4 fragment(MeshVertexOutput mesh) : SV_Target
{
    let layer = meshTag(mesh.instance_index);

    var surface = meshSurface(mesh);
    surface.base_color = my_array_texture.Sample(my_array_texture_sampler, float3(mesh.uv, float(layer)));
    return meshLit(surface, mesh);
}
