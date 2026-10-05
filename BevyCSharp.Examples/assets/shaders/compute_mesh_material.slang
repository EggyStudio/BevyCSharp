// Draws the cube compute_mesh.slang wrote, lit as a standard material of the material's color. Each
// of the mesh's vertices stands for one index the compute shader wrote, and its position, normal
// and UV are read from the vertex that index names, in the main pass and in the prepass alike, so
// the cube casts its own shadow.
import bcs;

StructuredBuffer<float> vertex_data;
StructuredBuffer<uint> index_data;
uniform float4 color;

struct Generated
{
    float3 position;
    float3 normal;
    float2 uv;
};

Generated generated(uint instance, uint vertex_id)
{
    let start = index_data[bcs::vertex_number(instance, vertex_id)] * 8;
    Generated vertex;
    vertex.position = float3(vertex_data[start], vertex_data[start + 1], vertex_data[start + 2]);
    vertex.normal = float3(vertex_data[start + 3], vertex_data[start + 4], vertex_data[start + 5]);
    vertex.uv = float2(vertex_data[start + 6], vertex_data[start + 7]);
    return vertex;
}

[shader("vertex")]
bcs::VertexOutput vertex(uint vertex_id : SV_VertexID, uint instance_index : SV_InstanceID)
{
    let made = generated(instance_index, vertex_id);
    bcs::Vertex vertex;
    vertex.position = made.position;
    vertex.normal = made.normal;
    vertex.uv = made.uv;
    vertex.instance_index = instance_index;
    return bcs::standard_vertex(vertex);
}

[shader("vertex")]
bcs::PrepassVertexOutput prepass_vertex(uint vertex_id : SV_VertexID, uint instance_index : SV_InstanceID)
{
    let made = generated(instance_index, vertex_id);
    return bcs::prepass_output(instance_index, made.position, made.normal, made.uv);
}

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    var surface = bcs::surface(mesh);
    surface.base_color = color;
    return bcs::lit(surface, mesh);
}
