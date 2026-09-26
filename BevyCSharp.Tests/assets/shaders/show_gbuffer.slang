// A pass that paints what Bevy's G-buffer says about each pixel: base color's red, metallic and
// roughness as red, green and blue, or with `show` at one the normal, mapped into colors.

import bcs_pass;

uniform uint show;
Texture2D<uint4> gbuffer;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    let surface = bcs_pass::surface_of(gbuffer.Load(int3(int2(input.position.xy), 0)));

    if (show == 1) return float4(surface.normal * 0.5 + 0.5, 1.0);

    return float4(surface.base_color.r, surface.metallic, surface.perceptual_roughness, 1.0);
}
