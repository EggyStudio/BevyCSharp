// Copies where the instance in slot zero is, this frame and on the previous one, into a buffer, for
// the test to read back.

import bcs_compute;
import bcs_scene;

StructuredBuffer<bcs_scene::Instance> instances;
RWStructuredBuffer<float4> into;

[shader("compute")]
[numthreads(1, 1, 1)]
void main()
{
    into[0] = float4(bcs_scene::position(instances[0]), 1.0);
    into[1] = float4(bcs_scene::previous_position(instances[0]), 1.0);
}
