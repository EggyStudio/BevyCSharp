// Reads a C# struct out of a raw buffer, by index.
//
// The struct is declared here with the same fields in the same order as on the C# side, and a raw
// buffer is read with the packing a sequential C# struct has, so the two agree without either
// spelling out any padding.

import bcs;

struct Particle
{
    float size;
    float3 color;
};

ByteAddressBuffer particles;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    let particle = particles.Load<Particle>(2 * sizeof(Particle));
    return float4(particle.color * particle.size, 1.0);
}
