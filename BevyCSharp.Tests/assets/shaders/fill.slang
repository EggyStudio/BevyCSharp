// A compute shader that writes `color` into the first element of `colors`, which a material bound
// to the same buffer then paints with.

import bcs_compute;

uniform float4 color;
RWStructuredBuffer<float4> colors;

[shader("compute")]
[numthreads(1, 1, 1)]
void main()
{
    colors[0] = color;
}
