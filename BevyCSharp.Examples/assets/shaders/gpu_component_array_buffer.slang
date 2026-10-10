// Bevy's gpu_component_array_buffer shader in Slang, a cube's texture tinted by the color its
// entity's component holds, read from the component array at the cube's mesh tag.
import bcs;

/// One entity's entry, as the component lays it out, padded to sixteen bytes.
struct CustomMaterialData
{
    float3 color;
    uint pad;
};

StructuredBuffer<CustomMaterialData> material_data;
Texture2D color_texture;
SamplerState color_texture_sampler;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    // The tag is the entry's place in the array, which the array moves as cubes go.
    let color = material_data[bcs::tag(mesh.instance_index)].color;
    let texture_color = color_texture.Sample(color_texture_sampler, mesh.uv).rgb;
    return float4(color * texture_color, 1.0);
}
