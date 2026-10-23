// A sphere found by a ray from each pixel of the view and written into the camera's deferred
// buffers, which Bevy's deferred lighting then lights, and into the shadow maps by its shadow
// stage.

import bcs_pass;

/// The sphere's center, and its radius in the last component.
uniform float4 sphere;
uniform float4 color;

[shader("vertex")]
FullscreenOutput vertex(uint index : SV_VertexID)
{
    return fullscreenTriangle(index);
}

// How far along the ray through the pixel at `uv` it meets the sphere, or a negative distance
// where it misses.
float hit(float2 uv)
{
    let from_center = passRayOrigin(uv) - sphere.xyz;
    let along = dot(from_center, passRayDirection(uv));
    let gap = along * along - (dot(from_center, from_center) - sphere.w * sphere.w);

    if (gap < 0.0)
        return -1.0;

    return -along - sqrt(gap);
}

[shader("fragment")]
PassDeferred fragment(FullscreenOutput input)
{
    let distance = hit(input.uv);
    if (distance < 0.0)
        discard;

    let world = passRayOrigin(input.uv) + passRayDirection(input.uv) * distance;
    var surface = passSurface(normalize(world - sphere.xyz));
    surface.base_color = color.rgb;
    surface.perceptual_roughness = 0.6;
    return passDeferred(surface, world);
}

[shader("fragment")]
PassShadowDepth shadow(FullscreenOutput input)
{
    let distance = hit(input.uv);
    if (distance < 0.0)
        discard;

    return passShadowDepth(passRayOrigin(input.uv) + passRayDirection(input.uv) * distance);
}
