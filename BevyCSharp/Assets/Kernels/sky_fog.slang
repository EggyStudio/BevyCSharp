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

// x: 1 where the fog's froxels lie over the picture as far as they reach and the closed form goes
// on past them (fog_froxels.slang, fog_integrate.slang). y: the froxels deep. z: how far from the
// eye the last slice ends. w: unused.
uniform float4 froxels;

// The froxels added up from the eye, the light the fog in front of each slice's far end scatters
// toward the eye in the rgb and the share of the light beyond it that gets through in the alpha.
Texture3D<float4> fog_volume;
SamplerState fog_volume_sampler;

// The boxes the froxels thicken the fog inside, as fog_froxels.slang has them, which over the sky
// are laid in full, since the sky holds the layer's haze and not theirs.
uniform float4 boxes[16];
uniform uint4 counts;

// Where the first slice ends, as fog_froxels.slang has it.
static const float Near = 0.5;

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
    float level = sqrt(max(1.0 - up * up, 0.0));
    if (horizon.w > 0.5 || !passHasEnvironment())
        return color.rgb * passView.exposure;
    // Read nearly sharp, since the map is the sky's scattered light alone, with no sun disk to blur
    // away, and a rough read near the horizon takes in the dark ground under it, which leaves a seam.
    // It is read at five ways spread over thirty degrees about the way and averaged, as fog scatters
    // forward the light from about the way, so the clouds the camera's sky lighting holds along the
    // horizon do not stand up the picture as stripes.
    float3 light = 0.0;
    for (int k = -2; k <= 2; k++)
    {
        float turn = float(k) * 0.1309;
        float2 turned = float2(flat.x * cos(turn) - flat.y * sin(turn), flat.x * sin(turn) + flat.y * cos(turn));
        light += passEnvironmentSpecular(normalize(float3(turned.x * level, up, turned.y * level)), 0.08);
    }
    return light * 0.2 * passView.exposure;
}

// The share of a point's light the fog takes away between the eye and the point, through a layer
// whose density falls exponentially with height, its optical depth integrated in closed form, the
// point at `at`, from `start` along the way.
float fogged(float3 eye, float3 at, float start)
{
    float total = distance(eye, at);
    start = max(start, 0.0);
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

// The froxels' light and the share of the light that gets through, in front of a view depth as far
// as they reach.
float4 froxelsAt(float2 uv, float depth)
{
    float slices = froxels.y;
    float far = froxels.z;
    float first = Near * pow(far / Near, 1.0 / slices);
    float reached = log(max(depth, Near) / Near) / log(far / Near) * slices;
    if (reached < 1.0)
    {
        float4 front = fog_volume.SampleLevel(fog_volume_sampler, float3(uv, 0.5 / slices), 0.0);
        return lerp(float4(0.0, 0.0, 0.0, 1.0), front, saturate(depth / first));
    }
    return fog_volume.SampleLevel(fog_volume_sampler, float3(uv, (min(reached, slices) - 0.5) / slices), 0.0);
}

// The picture times what gets through plus the fog's light, as a color and a share blended over
// the picture by that share, which is the same.
float4 over(float3 light, float through)
{
    float share = 1.0 - through;
    if (share <= 1e-4)
        discard;
    return float4(light / share, share);
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

    if (froxels.x > 0.5)
        return throughFroxels(input.uv, depth, way, sky);

    if (depth <= 0.0)
    {
        // The sky above the horizon is the atmosphere's own; below it, where the camera draws
        // nothing, the horizon's color darkens toward the ground's share over the band.
        if (way.y >= 0.0)
            discard;
        return float4(band(way, sky), 1.0);
    }

    float3 eye = passView.world_position;
    float3 world = passWorldPosition(input.uv, depth);
    float share = fogged(eye, world, fog.y);
    if (share <= 0.0)
        discard;
    return float4(sky, share);
}

// The band's color below the horizon along a way, the horizon's darkening toward the ground's share.
float3 band(float3 way, float3 sky)
{
    float below = degrees(asin(clamp(-way.y, 0.0, 1.0)));
    return sky * lerp(1.0, horizon.y, smoothstep(0.0, max(horizon.z, 1e-3), below));
}

// The froxels' fog over the sky above the horizon, which is the shadows and the lamps in it alone,
// as the closed form leaves the sky as the atmosphere draws it.
//
// Along one way the fog's light, where nothing shadows it, is the sky's along that way at every
// depth, so the light of the fog in front of the sky with no shadow and no lamp in it is that light
// times the share the fog takes away, however its density lumps. What the froxels hold past that is
// the shadows' darkening and the lamps' light, and those alone are laid over the sky, the darkening
// as a share of the sky's light taken away and the lamps' as light added.
//
// The boxes are the program's own fog, which the sky does not hold, so they are laid over it in
// full, lit by the same light, their share of the sky's light taken away worked out in closed form
// along the way through each.
float4 overSky(float4 near, float3 sky, float3 eye, float3 way)
{
    static const float3 Luma = float3(0.2126, 0.7152, 0.0722);
    float3 apart = near.rgb - sky * (1.0 - near.a);
    float3 darker = max(-apart, 0.0);
    float3 brighter = max(apart, 0.0);

    // Under a hundredth of the fog's own light is the froxels' cells and steps, not a shadow.
    float own = dot(sky * (1.0 - near.a), Luma);
    float taken = dot(darker, Luma) > own * 0.01 ? saturate(dot(darker, Luma) / max(dot(sky, Luma), 1e-6)) : 0.0;
    if (dot(brighter, Luma) <= own * 0.01)
        brighter = 0.0;

    float boxed = boxesThrough(eye, way, froxels.z * 1.5);
    if (taken <= 0.0 && dot(brighter, Luma) <= 0.0 && boxed >= 0.9999)
        discard;

    // The sky behind seen through the boxes and darkened by the shadows, the boxes' light and the
    // lamps' added, blended over the picture by the share taken away, where a share of a thousandth
    // stands in for none so the light added still reaches the picture.
    float share = max(1.0 - boxed * (1.0 - taken), 1e-3);
    return float4((sky * (1.0 - boxed) + brighter) / share, share);
}

// The share of light the program's boxes let through along a way from the eye as far as given,
// each as thick as it is inside its faded edge.
float boxesThrough(float3 eye, float3 way, float far)
{
    float optical = 0.0;
    for (uint i = 0u; i < counts.x; i++)
    {
        float4 middle = boxes[i * 2u];
        float4 extent = boxes[i * 2u + 1u];
        float3 inner = max(extent.xyz - extent.w * 0.5, 1e-3);
        float3 inverse = 1.0 / select(abs(way) > 1e-6, way, 1e-6);
        float3 low = (middle.xyz - inner - eye) * inverse;
        float3 high = (middle.xyz + inner - eye) * inverse;
        float enter = max(max(min(low.x, high.x), min(low.y, high.y)), max(min(low.z, high.z), 0.0));
        float leave = min(min(max(low.x, high.x), max(low.y, high.y)), min(max(low.z, high.z), far));
        optical += middle.w * max(leave - enter, 0.0);
    }
    return exp(-optical);
}

// The fog over a pixel with the froxels on, their fog as far as they reach and the closed form's
// past them, over the sky, the band below the horizon, or the geometry there.
float4 throughFroxels(float2 uv, float depth, float3 way, float3 sky)
{
    float4 near = froxelsAt(uv, froxels.z);
    if (depth <= 0.0)
    {
        if (way.y >= 0.0)
            return overSky(near, sky, passView.world_position, way);
        return float4(near.rgb + near.a * band(way, sky), 1.0);
    }

    float3 eye = passView.world_position;
    float3 world = passWorldPosition(uv, depth);
    float along = passViewDepth(world);
    if (along <= froxels.z)
    {
        float4 front = froxelsAt(uv, along);
        return over(front.rgb, front.a);
    }

    // Past the froxels the closed form takes the layer on from where they end along the way.
    float total = distance(eye, world);
    float share = fogged(eye, world, froxels.z * total / max(along, 1e-3));
    return over(near.rgb + near.a * share * sky, near.a * (1.0 - share));
}
