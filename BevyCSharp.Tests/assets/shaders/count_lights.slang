// Writes how many directional and point lights a camera sees into a buffer, for the test to read
// back.

import bcs_pass;

RWStructuredBuffer<uint> counts;

[shader("compute")]
[numthreads(1, 1, 1)]
void main()
{
    counts[0] = bcs_pass::directional_light_count();
    counts[1] = bcs_pass::point_light_count();
}
