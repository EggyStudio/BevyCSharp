// The clouds clouds.slang marched laid over the sky of a camera's picture before tonemapping, blended
// by how much they cover it, read from the half-sized image between its texels (Render.SetClouds).
// Geometry stands in front of them, since every cloud is above anything a level holds.
import bcs_pass;

Texture2D<float4> clouds;
SamplerState clouds_sampler;

[shader("vertex")]
FullscreenOutput vertex(uint index : SV_VertexID)
{
    // At the near plane, which every surface's depth passes against, so the sky is drawn over too.
    var corner = fullscreenTriangle(index);
    corner.position.z = 1.0;
    return corner;
}

[shader("fragment")]
float4 fragment(FullscreenOutput input) : SV_Target
{
    if (!passHasDepth() || passLoadDepth(int2(input.position.xy)) > 0.0)
        discard;
    float4 cloud = clouds.SampleLevel(clouds_sampler, input.uv, 0.0);
    if (cloud.a <= 1e-3)
        discard;
    return float4(cloud.rgb / cloud.a, saturate(cloud.a));
}
