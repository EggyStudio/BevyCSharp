// Paints the ambient occlusion Bevy's lighting reads, white where the sky is open and dark where
// something hides it, in place of the picture, for seeing what the rays found.

import bcs_pass;

Texture2D<float> ambient_occlusion;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    uint width;
    uint height;
    ambient_occlusion.GetDimensions(width, height);

    let picture = bcs_pass::picture_size();
    let texel = int2(input.position.xy * float2(width, height) / float2(picture));
    let open = ambient_occlusion.Load(int3(texel, 0));
    return float4(open, open, open, 1.0);
}
