// Moves each particle along by its velocity, scaled by the first float, in a struct laid out the
// way the C# side lays it out.

import bcs_compute;

struct Particle
{
    float3 position;
    float3 velocity;
};

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    if (id.x >= bcs_compute::count<Particle>(bcs_compute::buffer0))
        return;

    var particle = bcs_compute::element<Particle>(bcs_compute::buffer0, id.x);
    particle.position += particle.velocity * bcs_compute::number(0);
    bcs_compute::store(bcs_compute::buffer0, id.x, particle);
}
