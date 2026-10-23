// Writes Bevy's time and frame count into a buffer, for the test to read back.

import bcs_compute;

RWStructuredBuffer<float> into;

[shader("compute")]
[numthreads(1, 1, 1)]
void main()
{
    into[0] = computeGlobals.time;
    into[1] = computeGlobals.delta_time;
    into[2] = float(computeGlobals.frame_count);
}
