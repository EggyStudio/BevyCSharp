// A compute shader that fills `image` with `color`, and a float image beside it with numbers no
// eight-bit image could hold.

import bcs_compute;

uniform float4 color;
[format("rgba8")] RWTexture2D<float4> image;
[format("rgba32f")] RWTexture2D<float4> precise;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint width;
    uint height;
    image.GetDimensions(width, height);

    if (id.x >= width || id.y >= height)
        return;

    image[id.xy] = color;
    precise[id.xy] = float4(1000.5, -2.25, float(id.x), 1.0);
}
