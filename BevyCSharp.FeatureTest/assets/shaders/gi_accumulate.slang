// Screen-space global illumination, the second step: this frame's rays blended into what the
// pixel gathered before, found where it was through the motion vectors, so a pixel's light is the
// average of many frames' single rays. Where the pixel was off screen last frame, it starts over.

import bcs_pass;

Texture2D gathered;
Texture2D gi_previous;
[format("rgba16f")] WTexture2D<float4> gi;

uniform float blend;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint width, height;
    gi.GetDimensions(width, height);

    if (id.x >= width || id.y >= height)
        return;

    let uv = (float2(id.xy) + 0.5) / float2(width, height);
    let pixel = int2(uv * float2(bcs_pass::picture_size()));
    let before = bcs_pass::previous_uv(uv, bcs_pass::load_motion(pixel));
    let fresh = gathered.Load(int3(id.xy, 0));

    if (any(before < 0.0) || any(before > 1.0))
    {
        gi.Store(id.xy, fresh);
        return;
    }

    let history = gi_previous.SampleLevel(bcs_pass::picture_sampler, before, 0.0);
    gi.Store(id.xy, lerp(history, fresh, blend));
}
