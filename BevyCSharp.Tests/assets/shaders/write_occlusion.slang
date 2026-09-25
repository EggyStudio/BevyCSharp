// Replaces the ambient occlusion Bevy's lighting reads with `value` everywhere, which is the
// smallest proof that occlusion of a shader's own reaches Bevy's materials.

import bcs_pass;

[format("r16f")] RWTexture2D<float> ambient_occlusion;
uniform float value;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint width;
    uint height;
    ambient_occlusion.GetDimensions(width, height);

    if (id.x < width && id.y < height)
        ambient_occlusion[id.xy] = value;
}
