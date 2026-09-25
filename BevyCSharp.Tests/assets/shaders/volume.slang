// Samples a 3D texture at the back, and a 2D array texture's second layer, and adds them, so a
// picture of it says both shapes were bound where the shader declares them.

import bcs;

Texture3D fog;
Texture2DArray stack;
SamplerState nearest;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    let back = fog.Sample(nearest, float3(0.5, 0.5, 0.9));
    let second = stack.Sample(nearest, float3(0.5, 0.5, 1.0));
    return float4(back.rgb + second.rgb, 1.0);
}
