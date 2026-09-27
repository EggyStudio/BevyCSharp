// Reads Bevy's depth pyramid on a camera: how many levels it has, the first level's size, the
// depth at the middle of the first level, and the one texel of the last level.

import bcs_pass;

Texture2D<float> depth_pyramid;
RWStructuredBuffer<float> into;

[shader("compute")]
[numthreads(1, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint width;
    uint height;
    uint levels;
    depth_pyramid.GetDimensions(0, width, height, levels);

    into[0] = float(levels);
    into[1] = float(width);
    into[2] = float(height);
    into[3] = depth_pyramid.Load(int3(width / 2, height / 2, 0));
    into[4] = depth_pyramid.Load(int3(0, 0, levels - 1));
}
