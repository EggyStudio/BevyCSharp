// Moves each particle along by its velocity times `step`, in a struct laid out the way the C# side
// lays it out.

import bcs_compute;

struct Particle
{
    float3 position;
    float3 velocity;
};

RWByteAddressBuffer particles;
uniform float step;

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    if (id.x >= bcs_compute::count<Particle>(particles))
        return;

    var particle = bcs_compute::element<Particle>(particles, id.x);
    particle.position += particle.velocity * step;
    bcs_compute::store(particles, id.x, particle);
}
