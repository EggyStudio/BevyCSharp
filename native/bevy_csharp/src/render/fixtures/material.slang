import bcs;
struct Light { float3 color; float power; };
uniform float roughness;
uniform float4 tint;
uniform float weights[1000];
uniform Light lights[4];
uniform float4x4 twist;
uniform int mode;
Texture2D albedo;
SamplerState albedo_sampler;
Texture2D layers[64];
TextureCube skies[16];
Texture3D fog;
StructuredBuffer<float4> points;
ByteAddressBuffer raw;
ConstantBuffer<Light> sun;
[shader("vertex")]
bcs::VertexOutput vertex(bcs::Vertex v) { v.position.y += weights[2] * roughness; return bcs::standard_vertex(v); }
[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    float4 c = albedo.Sample(albedo_sampler, mesh.uv) * tint * roughness * weights[999] * bcs::globals.time;
    c += layers[63].Sample(albedo_sampler, mesh.uv) + skies[15].Sample(albedo_sampler, mesh.world_normal) + fog.Sample(albedo_sampler, mesh.world_position.xyz);
    c += points[3] + float4(lights[3].color, lights[3].power) + mul(twist, c) + float(mode) + raw.Load<float4>(0) + float4(sun.color, sun.power);
    return c;
}
