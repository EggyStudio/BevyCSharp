// Copies where the instance in slot zero is, this frame and on the previous one, into a buffer, for
// the test to read back.

import bcs_compute;
import bcs_scene;

StructuredBuffer<Instance> instances;
RWStructuredBuffer<float4> into;

[shader("compute")]
[numthreads(1, 1, 1)]
void main()
{
    into[0] = float4(instancePosition(instances[0]), 1.0);
    into[1] = float4(instancePreviousPosition(instances[0]), 1.0);
}
