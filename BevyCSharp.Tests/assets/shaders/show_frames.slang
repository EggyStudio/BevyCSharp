// A pass that paints green where the camera's frame count has passed `threshold`, and red before.

import bcs_pass;

Texture2D<float> frames;
uniform float threshold;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    let seen = frames.Load(int3(int2(input.position.xy), 0));
    return seen > threshold ? float4(0.0, 1.0, 0.0, 1.0) : float4(1.0, 0.0, 0.0, 1.0);
}
