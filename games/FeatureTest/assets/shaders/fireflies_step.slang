// Moves every firefly along a looping path around its home, from the time Bevy keeps.
//
// Run as a compute shader every frame, over a buffer the fireflies' material reads, so the
// positions never leave the GPU. Edit this while the sample runs and the swarm changes shape.

import bcs_compute;

/// One firefly, laid out as the C# record is. A `float3` followed by a `float` fills sixteen bytes
/// under either set of rules, so the two agree without padding.
struct Firefly
{
    float3 position;
    float phase;
    float3 home;
    float speed;
};

RWStructuredBuffer<Firefly> flies;

/// How far from home a firefly strays.
uniform float reach;

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint count;
    uint stride;
    flies.GetDimensions(count, stride);

    if (id.x >= count)
        return;

    var fly = flies[id.x];
    let t = bcs_compute::globals.time * fly.speed + fly.phase;

    fly.position = fly.home + float3(sin(t) * reach, sin(t * 1.7) * reach * 0.5, cos(t * 1.3) * reach);
    flies[id.x] = fly;
}
