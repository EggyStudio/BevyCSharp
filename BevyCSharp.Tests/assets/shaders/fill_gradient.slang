// Fills a camera's image with a quarter everywhere, which a watch shows scaled.

import bcs_pass;

[format("r32f")] RWTexture2D<float> amount;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint width;
    uint height;
    amount.GetDimensions(width, height);

    if (id.x < width && id.y < height)
        amount[id.xy] = 0.25;
}
