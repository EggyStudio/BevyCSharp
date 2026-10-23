// Writes how many directional and point lights a camera sees into a buffer, for the test to read
// back.

import bcs_pass;

RWStructuredBuffer<uint> counts;

[shader("compute")]
[numthreads(1, 1, 1)]
void main()
{
    counts[0] = passDirectionalLightCount();
    counts[1] = passPointLightCount();
}
