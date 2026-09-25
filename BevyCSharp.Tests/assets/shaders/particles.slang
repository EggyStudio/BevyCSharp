// Reads a C# struct out of the material's data, by index.
//
// The struct is declared here with the same fields in the same order as on the C# side, and Slang
// reads a raw buffer with the packing a sequential C# struct has, so the two agree without either
// spelling out any padding.

import bcs;

struct Particle
{
    float size;
    float3 color;
};

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    let particle = bcs::element<Particle>(2);
    return float4(particle.color * particle.size, 1.0);
}
