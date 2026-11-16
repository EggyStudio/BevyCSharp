// Marks each pixel of the view one where the camera's depth holds something and zero where it holds
// nothing, for a pass to show.

import bcs_pass;

[format("r32f")] RWTexture2D<float> marked;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    let size = passPictureSize();

    if (id.x >= size.x || id.y >= size.y)
        return;

    marked[id.xy] = passLoadDepth(int2(id.xy)) > 0.0 ? 1.0 : 0.0;
}
