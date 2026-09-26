// A pass comparing the picture with last frame's copy of it, which is half its size: red where the picture is more red than
// green now, green where the copy was, and blue wherever the copy holds anything at all.

import bcs_pass;

Texture2D lit_previous;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    let now = bcs_pass::sample(input.uv);
    let then = lit_previous.Load(int3(int2(input.position.xy * 0.5), 0));

    return float4(
        now.r > now.g ? 1.0 : 0.0,
        then.r > then.g ? 1.0 : 0.0,
        then.r + then.g > 0.1 ? 1.0 : 0.0,
        1.0);
}
