// Counts the frames each pixel has been seen, in an image the camera keeps with its history, so it
// adds one this frame to what it wrote last frame.

import bcs_pass;

[format("r32f")] RWTexture2D<float> frames;
Texture2D<float> frames_previous;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    let size = bcs_pass::picture_size();

    if (id.x >= size.x || id.y >= size.y)
        return;

    frames[id.xy] = frames_previous.Load(int3(id.xy, 0)) + 1.0;
}
