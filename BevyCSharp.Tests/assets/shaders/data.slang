// Paints the last of however many colors the buffer bound to `colors` holds.
//
// The last rather than the first, so a picture of it says the whole buffer arrived.

import bcs;

StructuredBuffer<float4> colors;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    uint count;
    uint stride;
    colors.GetDimensions(count, stride);
    return colors[count - 1];
}
