// A pass that makes the picture look like it came through an old screen: the channels pulled
// apart toward the edges, and dark lines across it.
//
// Run after tonemapping, because it is about the picture as a picture rather than about light.
// `strength` and `line_count` are set from C# by name, and anything added here can be too.

import bcs_pass;

/// How strong the whole effect is, from zero for none to one.
uniform float strength;

/// How many dark lines cross the picture, or zero for one every pixel.
uniform float line_count;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    let from_middle = input.uv - 0.5;
    let shift = from_middle * dot(from_middle, from_middle) * 0.05 * strength;

    let color = float3(
        bcs_pass::sample(input.uv + shift).r,
        bcs_pass::sample(input.uv).g,
        bcs_pass::sample(input.uv - shift).b);

    let lines = line_count > 0.0 ? line_count : 1.0 / bcs_pass::pixel_size().y;
    let dark = 1.0 - strength * 0.25 * (0.5 + 0.5 * sin(input.uv.y * lines * 3.14159));

    return float4(color * dark, 1.0);
}
