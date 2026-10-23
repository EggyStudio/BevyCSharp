// Bevy's extended_material_bindless shader in Slang, its standard material's gray, multiplied by an
// image and a color of the material's own before Bevy lights it.
import bcs;

uniform float4 base_color;
uniform float4 modulate_color;
Texture2D modulate_texture;
SamplerState modulate_sampler;

[shader("fragment")]
float4 fragment(MeshVertexOutput mesh) : SV_Target
{
    var surface = meshSurface(mesh);
    surface.base_color = base_color * modulate_texture.Sample(modulate_sampler, mesh.uv) * modulate_color;
    return meshLit(surface, mesh);
}
