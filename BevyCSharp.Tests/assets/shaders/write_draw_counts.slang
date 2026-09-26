// Writes the counts of an indirect draw: six vertices a square, `squares` of them.

import bcs_compute;

RWByteAddressBuffer counts;
uniform uint squares;

[shader("compute")]
[numthreads(1, 1, 1)]
void main()
{
    counts.Store4(0, uint4(6, squares, 0, 0));
}
