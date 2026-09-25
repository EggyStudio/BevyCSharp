// A constant buffer of a struct holding an array of structs, painted from one field of one of
// them, so a picture of it says a name with a path and an index finds the right bytes.

import bcs;

struct Light
{
    float3 color;
    float power;
};

struct Lighting
{
    float4 ambient;
    Light lights[8];
    int count;
};

ConstantBuffer<Lighting> lighting;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    let light = lighting.lights[lighting.count - 1];
    return float4(lighting.ambient.rgb + light.color * light.power, 1.0);
}
