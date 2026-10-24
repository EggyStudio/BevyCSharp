// The fog over a camera's picture, in the color the sky has at the horizon along each way, drawn
// over the lit picture before tonemapping and blended by its alpha, so distant geometry fades into
// the sky behind it and the sky below the horizon, where the camera draws nothing, takes the
// horizon's color in place of the atmosphere's ground (Render.SetSkyFog).
//
// The sky's color along a way is read from the camera's environment map, which an atmosphere lights
// each frame (Render.SetSkyLighting), at a way raised to the horizon and a few degrees above it, so
// fog toward the sun is warm and away from it blue, as the sky is, and a building fades into the
// sky beside it. Where the camera has no environment map, the color given stands in for it. The fog
// thickens as an exponential layer over the ground, the light a ray loses through it worked out in
// closed form between the eye's height and the point's, so a hill's top stands clear of a valley's
// haze. 3DEngine draws below its horizon a color the day's sky sets and leaves the fog to a program;
// here both are the sky's along each way.
import bcs_pass;

// x: how far the fog takes away 95% of a point's light at the layer's base, in world units. y: how
// far from the eye it starts. z: the layer's scale height, how far up its density falls by e. w: the
// height of the layer's base.
uniform float4 fog;

// x: how many degrees above the horizon the fog's color is read at, the way's own height held
// between the horizon and this. y: the share of the horizon's color left at the bottom of the band
// below the horizon. z: over how many degrees below the horizon the band darkens to it. w: 1 where
// the color given stands in for the sky's.
uniform float4 horizon;

// The fog's color where the camera has no environment map or the program gives its own, linear,
// in the picture's units before the exposure.
uniform float4 color;

[shader("vertex")]
FullscreenOutput vertex(uint index : SV_VertexID)
{
    // At the near plane, which every surface's depth passes against, so the sky is drawn over too.
    var corner = fullscreenTriangle(index);
    corner.position.z = 1.0;
    return corner;
}

// The sky's color along a way raised to the horizon, the way's own height held between the horizon
// and `horizon.x` degrees, in the picture's units.
float3 skyAlong(float3 way)
{
    float top = sin(radians(max(horizon.x, 0.0)));
    float2 flat = way.xz;
    float across = length(flat);
    flat = across > 1e-4 ? flat / across : float2(0.0, -1.0);
    float up = clamp(way.y, 0.0, top);
    float3 raised = normalize(float3(flat.x * sqrt(max(1.0 - up * up, 0.0)), up, flat.y * sqrt(max(1.0 - up * up, 0.0))));
    if (horizon.w > 0.5 || !passHasEnvironment())
        return color.rgb * passView.exposure;
    // Read nearly sharp. The map is the sky's scattered light alone, with no sun disk to blur away,
    // and a rough read near the horizon takes in the dark ground under it, which leaves a seam.
    return passEnvironmentSpecular(raised, 0.08) * passView.exposure;
}

// The share of a point's light the fog takes away between the eye and the point, through a layer
// whose density falls exponentially with height, its optical depth integrated in closed form, the
// point at `at`.
float fogged(float3 eye, float3 at)
{
    float total = distance(eye, at);
    float start = max(fog.y, 0.0);
    if (total <= start || fog.x <= 0.0)
        return 0.0;
    // The ray from where the fog starts along the way to the point.
    float3 from = eye + (at - eye) * (start / total);
    float length = total - start;
    float density = 3.0 / fog.x;
    float scale = max(fog.z, 1e-3);
    float a = (from.y - fog.w) / scale, b = (at.y - fog.w) / scale;
    // The mean of exp(-height / scale) along the ray, which is exp(-a) where the ray is level.
    float rise = b - a;
    float mean = abs(rise) > 1e-4 ? (exp(-a) - exp(-b)) / rise : exp(-a);
    return 1.0 - exp(-density * length * mean);
}

[shader("fragment")]
float4 fragment(FullscreenOutput input) : SV_Target
{
    // A camera whose depth cannot be read, one drawing several samples a pixel, reads zeros, the
    // depth of an empty pixel everywhere, which would lay the band below the horizon over the
    // ground, so it gets no fog at all.
    if (!passHasDepth())
        discard;
    int2 pixel = int2(input.position.xy);
    float depth = passLoadDepth(pixel);
    float3 way = normalize(passRayDirection(input.uv));
    float3 sky = skyAlong(way);

    if (depth <= 0.0)
    {
        // The sky above the horizon is the atmosphere's own; below it, where the camera draws
        // nothing, the horizon's color darkens toward the ground's share over the band.
        if (way.y >= 0.0)
            discard;
        float below = degrees(asin(clamp(-way.y, 0.0, 1.0)));
        float shade = lerp(1.0, horizon.y, smoothstep(0.0, max(horizon.z, 1e-3), below));
        return float4(sky * shade, 1.0);
    }

    float3 eye = passView.world_position;
    float3 world = passWorldPosition(input.uv, depth);
    float share = fogged(eye, world);
    if (share <= 0.0)
        discard;
    return float4(sky, share);
}
