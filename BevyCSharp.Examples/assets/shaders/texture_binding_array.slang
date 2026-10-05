// Bevy's texture_binding_array shader in Slang, a face cut into four by four, each part drawn from
// its own texture of an array of them, picked by where on the face a pixel is.
import bcs;

Texture2D textures[16];
SamplerState nearest_sampler;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    let coords = clamp(uint2(mesh.uv * 4.0), uint2(0), uint2(3));
    let index = coords.y * 4 + coords.x;
    let inner_uv = frac(mesh.uv * 4.0);
    // An index that differs from pixel to pixel, which WGSL allows where the GPU can index such an
    // array that way.
    return textures[index].Sample(nearest_sampler, inner_uv);
}
