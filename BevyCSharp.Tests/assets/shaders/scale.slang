// A compute shader that multiplies every number in `numbers` by `factor`.

import bcs_compute;

uniform float factor;
RWStructuredBuffer<float> numbers;

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint count;
    uint stride;
    numbers.GetDimensions(count, stride);

    if (id.x < count)
        numbers[id.x] *= factor;
}
