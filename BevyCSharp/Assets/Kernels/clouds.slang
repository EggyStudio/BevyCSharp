// The clouds a camera sees, marched through a shell of the sky above the planet at half the
// picture's size across and down (Render.SetClouds), which clouds_draw.slang lays over the sky.
//
// The shell's density follows Schneider's clouds for Horizon Zero Dawn (Nubis). A weather map says
// where the sky has cloud and what kind, a height profile rounds each cloud's base and lifts its
// top by its kind, the shape noise cuts the body and the detail noise wears its edges, all carried
// on the wind. Its light follows Hillaire's for Frostbite and his 2020 atmosphere. The sun's light
// reaches a point through Bevy's atmosphere, red at a low sun, dimmed through the cloud toward the
// sun by a short march, and scattered toward the eye by two lobes, a strong one forward that
// silvers a cloud's edge against the sun and a weak one back, with Wrenninge's octaves standing in
// for the light a cloud scatters many times over. The sky's own light reaches each point from above
// at the cloud's top and from below at its base. The light is added up as it fades through the
// cloud, a step at a time in closed form, so a thick step does not brighten it past what its light
// could give.
//
// The air between the eye and the cloud fades a far cloud into the sky's color along the way, read
// from the camera's environment map, which holds no sun disc, so the disc does not burn through a
// cloud in front of it. A pixel's march starts at a point the frame's blue noise moves along its
// first step, and blends with last frame's clouds where the cloud stood in last frame's view, so
// the steps blur into a smooth cloud over a few frames.
import bcs_pass;

// The clouds this frame and last, the light the clouds send toward the eye with the sky's color in
// front of them in the rgb, multiplied by how much they cover, and how much they cover in the alpha.
[format("rgba16f")] WTexture2D<float4> clouds;
Texture2D<float4> clouds_previous;

Texture3D<float4> cloud_shape;
Texture3D<float4> cloud_detail;
Texture3D<float4> cloud_weather;
SamplerState cloud_sampler;
SamplerState clouds_previous_sampler;

// x: the shell's base above the ground. y: its top. z: the planet's radius. w: how far along a way
// the clouds are marched at most.
uniform float4 layer;

// x: how much of the sky the clouds cover, from none to all. y: how much light a meter of the
// densest cloud takes away. z: one over the size of the shape noise. w: one over the detail's.
uniform float4 shape;

// xyz: how far the wind has carried the clouds. w: one over the size of the weather map.
uniform float4 wind;

// x: the steps along a way. y: the steps toward the sun. z: how far the detail wears the edges.
// w: 1 where last frame's clouds may be read.
uniform float4 march;

// x: the forward lobe's anisotropy. y: the back lobe's. z: the share of the back lobe. w: how strong
// the sky's own light is.
uniform float4 lobes;

// x: the share of the cover that is a flat deck of stratus rather than heaps. yzw: unused.
uniform float4 stratus;

// A strike of lightning's glow inside the clouds (Render.SetThunder), a light scattered evenly about
// a point, dying away as a bell from it. xyz: the point. w: the light there, in candelas a square
// meter of a cloud lit evenly by it, before the exposure, 0 where nothing strikes. The glow's color
// in rgb, and in w how far from the point the glow has fallen to some three fifths, in meters.
uniform float4 flash;
uniform float4 flash_color;

// The share of the sun's light the clouds let through to the ground over a square about the
// camera, in the red, which `shadows` writes and Bevy lays over the sun's light as its picture.
[format("rgba8")] WTexture2D<float4> cloud_shadow;

// The square the shadow lies over, in the sun's own axes. xyz: its middle. w: how far from the
// middle to each side.
uniform float4 shadow_frame;

// The sun's own across and up, the axes its picture is laid along, and in w how dark the shadows
// are, from none at 0 to all of the light taken at 1.
uniform float4 shadow_across;
uniform float4 shadow_up;

static const float Pi = 3.14159265;

float remap(float value, float low, float high, float toLow, float toHigh)
{
    return toLow + (value - low) / max(high - low, 1e-4) * (toHigh - toLow);
}

// Henyey and Greenstein's phase, the share of light scattered at an angle whose cosine is given.
float phase(float cosine, float g)
{
    float g2 = g * g;
    return (1.0 - g2) / (4.0 * Pi * pow(max(1.0 + g2 - 2.0 * g * cosine, 1e-4), 1.5));
}

float lobed(float cosine, float shrink)
{
    return lerp(phase(cosine, lobes.x * shrink), phase(cosine, lobes.y * shrink), lobes.z);
}

// Where a ray from a point meets a sphere about the planet's middle, the near and far distance, or
// none where it misses.
float2 sphere(float3 from, float3 way, float radius)
{
    float b = dot(from, way);
    float c = dot(from, from) - radius * radius;
    float d = b * b - c;
    if (d < 0.0)
        return float2(-1.0, -1.0);
    float root = sqrt(d);
    return float2(-b - root, -b + root);
}

// How far a point stands within the shell, from 0 at its base to 1 at its top.
float within(float3 planet)
{
    return saturate((length(planet) - (layer.z + layer.x)) / max(layer.y - layer.x, 1.0));
}

// How much light a meter of cloud takes away at a world point, its planet point given, with the
// detail's erosion or without, which the march toward the sun does without.
float density(float3 world, float3 planet, bool detailed)
{
    float h = within(planet);
    float3 drifted = world + wind.xyz;
    // A soft threshold over the weather, which lies evenly between none and one, set lower than the
    // coverage alone would set it, since a cloud's dome fills less than its patch, and none at a
    // coverage of none.
    float4 weather = cloud_weather.SampleLevel(cloud_sampler, float3(drifted.xz * wind.w, 0.25), 0.0);
    float some = smoothstep(0.0, 0.1, shape.x);
    float cover = saturate(remap(weather.r, 0.95 - shape.x, 1.45 - shape.x, 0.0, 1.0)) * some;

    // A flat deck covers most of the sky at the same coverage, low in the layer, which an overcast
    // is. Its edge, where the weather thins it, comes out ragged rather than a wall, from a wide
    // threshold with the body's noise carried into it. It reaches past where the heaps have any
    // cover at all, so a point is empty only where neither has, or the deck would end along the
    // weather map's texels, a run of straight steps.
    float deckCover = stratus.x > 0.0 ? saturate(remap(weather.r, 0.55 - shape.x * 0.9, 1.05 - shape.x * 0.9, 0.0, 1.0)) * some : 0.0;
    if (cover <= 0.0 && deckCover <= 0.0)
        return 0.0;

    // A rounded base and a top that rises with the cloud's kind, flat heaps to towers, thinning over
    // most of the way up, so the coverage below narrows each cloud into a dome rather than a wall.
    float kind = weather.g;
    float top = lerp(0.35, 1.0, kind);
    float profile = smoothstep(0.0, 0.08, h) * (1.0 - smoothstep(top * 0.15, top, h));
    float deckProfile = smoothstep(0.0, 0.06, h) * (1.0 - smoothstep(0.3, 0.6, h));
    if (profile <= 0.0 && deckProfile * deckCover <= 0.0)
        return 0.0;

    float4 body = cloud_shape.SampleLevel(cloud_sampler, drifted * shape.z, 0.0);
    float heaps = body.g * 0.625 + body.b * 0.25 + body.a * 0.125;
    float base = saturate(remap(body.r, heaps - 1.0, 1.0, 0.0, 1.0));
    // None where the heaps have no cover, whose threshold would be a division by nothing.
    float cloud = cover > 0.0 ? saturate(remap(base * profile, 1.0 - cover, 1.0, 0.0, 1.0)) * cover : 0.0;
    if (deckCover > 0.0)
    {
        float deck = saturate(remap(lerp(0.7, base, 0.6) * deckProfile, 1.0 - deckCover, 1.0, 0.0, 1.0)) * deckCover;
        cloud = lerp(cloud, deck, saturate(stratus.x));
    }
    if (cloud <= 0.0)
        return 0.0;

    if (detailed && march.z > 0.0)
    {
        float4 fine = cloud_detail.SampleLevel(cloud_sampler, drifted * shape.w, 0.0);
        float wisps = fine.r * 0.625 + fine.g * 0.25 + fine.b * 0.125;
        // Wisps at the base turn to billows toward the top.
        float wear = lerp(wisps, 1.0 - wisps, saturate(h * 4.0)) * 0.75 * march.z * (1.0 - 0.6 * saturate(stratus.x));
        cloud = saturate(remap(cloud, wear, 1.0, 0.0, 1.0));
    }
    return cloud * shape.y;
}

// The directional light that lights the scene most through the air, the sun by day and the moon
// at night, or none.
int brightest()
{
    int best = -1;
    float most = 0.0;
    for (uint i = 0u; i < passDirectionalLightCount(); i++)
    {
        // By the light that gets through the air to here, so a sun set behind the planet gives way
        // to the moon at night.
        float3 through = passAtmosphereTransmittance(passView.world_position, passLights.directional_lights[i].direction_to_light);
        float bright = dot(passLights.directional_lights[i].color.rgb * through, float3(0.2126, 0.7152, 0.0722));
        if (bright > most)
        {
            most = bright;
            best = int(i);
        }
    }
    return best;
}

[format("rgba16f")] WTexture3D<float4> sky_cloud_cube;
Texture3D<float4> sky_cloud_layer;
TextureCube<float4> sky_source;
[format("rgba16f")] WTexture3D<float4> sky_held;
Texture3D<float4> sky_copied;
[format("rgba16f")] WTexture2DArray<float4> sky_cube;
SamplerState sky_sampler;

// The way a cube's texel looks along as the GPU samples a cube, Bevy's `sample_cube_dir`, a face
// numbered +X, -X, +Y, -Y, +Z and -Z.
float3 cubeDir(float2 uv, uint face)
{
    float2 c = uv * 2.0 - 1.0;
    float3 d;
    if (face == 0u) d = float3(1.0, -c.y, -c.x);
    else if (face == 1u) d = float3(-1.0, -c.y, c.x);
    else if (face == 2u) d = float3(c.x, 1.0, c.y);
    else if (face == 3u) d = float3(c.x, -1.0, -c.y);
    else if (face == 4u) d = float3(c.x, -c.y, 1.0);
    else d = float3(-c.x, -c.y, -1.0);
    return normalize(d);
}

// 1 in x where the camera is lit by the sky, whose clear sky `sky_copy` has copied out of the cube
// before the clouds are marched and laid over it, so the clouds are lit by the clear sky above them
// and fade into its color along the way rather than into the clouded cube the scene is lit by,
// which would light them by their own undersides.
uniform float4 sky_lit;

// The face of a cube a way the GPU samples it along falls on, and where on it, the inverse of
// `cubeDir`.
void cubeFace(float3 d, out uint face, out float2 uv)
{
    float3 a = abs(d);
    float2 c;
    if (a.x >= a.y && a.x >= a.z)
    {
        face = d.x > 0.0 ? 0u : 1u;
        c = float2(d.x > 0.0 ? -d.z : d.z, -d.y) / a.x;
    }
    else if (a.y >= a.z)
    {
        face = d.y > 0.0 ? 2u : 3u;
        c = float2(d.x, d.y > 0.0 ? d.z : -d.z) / a.y;
    }
    else
    {
        face = d.z > 0.0 ? 4u : 5u;
        c = float2(d.z > 0.0 ? d.x : -d.x, -d.y) / a.z;
    }
    uv = c * 0.5 + 0.5;
}

// The clear sky's light along a world way, from the copy of the atmosphere's cube, before the
// exposure.
float3 clearSky(float3 way)
{
    uint face;
    float2 uv;
    cubeFace(float3(way.xy, -way.z), face, uv);
    return sky_copied.SampleLevel(sky_sampler, float3(uv, (float(face) + 0.5) / 6.0), 0.0).rgb * passSceneInfo.environment_intensity;
}

// The clear sky's light about the up or down way, from the pole and a ring sixty degrees from it,
// each weighed by how squarely it lights a surface facing that way, as Bevy's diffuse map holds it.
float3 clearAmbient(float up)
{
    float3 light = clearSky(float3(0.0, up, 0.0));
    float weight = 1.0;
    for (int i = 0; i < 8; i++)
    {
        float angle = float(i) * 0.785398;
        light += 0.5 * clearSky(float3(cos(angle) * 0.866, up * 0.5, sin(angle) * 0.866));
        weight += 0.5;
    }
    return light / weight;
}

// The clouds along a way from an eye, the light they send toward it with the sky's color in front
// of them, multiplied by how much they cover, and how much they cover, after the exposure, marched
// in so many steps from a point the jitter, from 0 to 1, moves along the first, and the depth the
// light came from on average, or the farthest the march reaches where none came.
float4 marchClouds(float3 eye, float3 way, float steps, float jitter, out float meanDepth)
{
    // The planet's middle under the world's origin, the ground there at height zero.
    float3 center = float3(0.0, -layer.z, 0.0);
    float3 from = eye - center;

    float2 inner = sphere(from, way, layer.z + layer.x);
    float2 outer = sphere(from, way, layer.z + layer.y);
    float below = length(from) - layer.z;
    float enter, leave;
    if (below < layer.x)
    {
        enter = inner.y;
        leave = outer.y;
    }
    else if (below < layer.y)
    {
        enter = 0.0;
        leave = inner.x > 0.0 ? inner.x : outer.y;
    }
    else
    {
        enter = outer.x;
        leave = inner.x > 0.0 ? inner.x : outer.y;
    }
    leave = min(leave, layer.w);

    float4 now = float4(0.0, 0.0, 0.0, 0.0);
    meanDepth = layer.w;
    if (enter >= 0.0 && leave > enter)
    {
        float stride = (leave - enter) / steps;
        float t = enter + stride * jitter;

        int sun = brightest();
        float3 toSun = sun >= 0 ? normalize(passLights.directional_lights[sun].direction_to_light) : float3(0.0, 1.0, 0.0);
        float3 sunLight = sun >= 0 ? passLights.directional_lights[sun].color.rgb * passView.exposure : 0.0;
        float cosine = dot(way, toSun);

        float3 skyAbove = passHasEnvironment() ? passEnvironmentDiffuse(float3(0.0, 1.0, 0.0)) * passView.exposure : 0.0;
        float3 skyBelow = passHasEnvironment() ? passEnvironmentDiffuse(float3(0.0, -1.0, 0.0)) * passView.exposure : 0.0;
        if (sky_lit.x > 0.5)
        {
            skyAbove = clearAmbient(1.0) * passView.exposure;
            skyBelow = clearAmbient(-1.0) * passView.exposure;
        }

        float3 light = 0.0;
        float through = 1.0;
        float weighed = 0.0;
        float depthSum = 0.0;
        for (float i = 0.0; i < steps && through > 0.01; i += 1.0)
        {
            float3 world = eye + way * t;
            float3 planet = world - center;
            float thick = density(world, planet, true);
            if (thick > 0.0)
            {
                // How much cloud lies toward the sun, a short march of steps doubling in length.
                float toward = 0.0;
                float reach = 40.0;
                float travelled = 0.0;
                for (float j = 0.0; j < march.y; j += 1.0)
                {
                    travelled += reach * 0.5;
                    float3 lit = world + toSun * travelled;
                    // At half the density the eye's march sees, as Schneider's does, which stands in
                    // for the light carried into a cloud's shade by scattering many times.
                    toward += density(lit, lit - center, false) * reach * 0.5;
                    travelled += reach * 0.5;
                    reach *= 2.0;
                }

                // Wrenninge's octaves, each scattering again with less of the light taken away and a
                // softer lobe.
                float3 sunHere = sunLight * passAtmosphereTransmittance(world, toSun);
                float scattered = 0.0;
                float a = 1.0, b = 1.0, c = 1.0;
                for (int octave = 0; octave < 4; octave++)
                {
                    scattered += a * lobed(cosine, c) * exp(-b * toward);
                    a *= 0.6;
                    b *= 0.35;
                    c *= 0.6;
                }
                // The sky's light from all around, scattered evenly, the sky's from above at the
                // cloud's top and the dimmer light from below at its base.
                float h = within(planet);
                float3 ambient = lerp(skyBelow * 0.5, skyAbove, h) * lobes.w;
                float3 glow = 0.0;
                if (flash.w > 0.0)
                {
                    float3 apart = world - flash.xyz;
                    glow = flash_color.rgb * flash.w * passView.exposure * exp(-dot(apart, apart) / (2.0 * flash_color.w * flash_color.w));
                }
                float3 source = thick * (sunHere * scattered + ambient + glow);

                float passed = exp(-thick * stride);
                light += through * (source - source * passed) / thick;
                float share = through * (1.0 - passed);
                depthSum += t * share;
                weighed += share;
                through *= passed;
            }
            t += stride;
        }

        // The march stops once under a hundredth gets through, and what is left is the cloud's,
        // since a hundredth of the sun's disc is still far brighter than white.
        if (through <= 0.01)
            through = 0.0;

        if (weighed > 1e-4)
        {
            meanDepth = depthSum / weighed;

            // The air in front of the cloud fades it toward the sky's color along the way, as the
            // atmosphere lets the cloud's light through to the eye.
            float3 point = eye + way * meanDepth;
            float3 air = passAtmosphereTransmittance(eye, way) / max(passAtmosphereTransmittance(point, way), 1e-4);
            air = saturate(air);
            float3 skyColor = passHasEnvironment() ? passEnvironmentSpecular(way, 0.0) * passView.exposure : 0.0;
            if (sky_lit.x > 0.5)
                skyColor = clearSky(way) * passView.exposure;
            float cover = 1.0 - through;

            // Faded out toward the farthest the march reaches, so the clouds do not end in a line.
            float fade = 1.0 - smoothstep(layer.w * 0.6, layer.w, meanDepth);
            float3 shown = light * air + cover * (1.0 - air) * skyColor;
            now = float4(shown, cover) * fade;
        }
    }

    return now;
}

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint2 size;
    clouds.GetDimensions(size.x, size.y);
    if (any(id.xy >= size))
        return;

    float2 uv = (float2(id.xy) + 0.5) / float2(size);
    float3 way = normalize(passRayDirection(uv));
    float3 eye = passView.world_position;

    // Geometry over the whole of this texel leaves no sky for the clouds to lie over.
    int2 pixel = int2(id.xy) * 2;
    bool sky = passLoadDepth(pixel) <= 0.0 || passLoadDepth(pixel + int2(1, 0)) <= 0.0
        || passLoadDepth(pixel + int2(0, 1)) <= 0.0 || passLoadDepth(pixel + int2(1, 1)) <= 0.0;

    float4 now = float4(0.0, 0.0, 0.0, 0.0);
    float meanDepth = layer.w;
    if (sky && passHasDepth())
        now = marchClouds(eye, way, max(march.x, 1.0), passBlueNoise(int2(id.xy)).x, meanDepth);

    // Last frame's clouds where they stood in last frame's view.
    if (march.w > 0.5)
    {
        float2 before = passPreviousUvOf(eye + way * meanDepth);
        if (all(before >= 0.0) && all(before <= 1.0))
        {
            // Where this frame's cover differs much from last frame's, a cloud's edge has moved or
            // the history holds a cloud that is not there, and this frame's is taken mostly as it
            // is, since a cover lagging toward full lets the sun's disc burn through.
            float4 history = clouds_previous.SampleLevel(clouds_previous_sampler, before, 0.0);
            float differs = saturate(abs(history.a - now.a) * 4.0);
            now = lerp(history, now, lerp(0.12, 1.0, differs));
        }
    }
    clouds.Store(id.xy, now);
}

// The clouds' shadow over a square about the camera, each texel marched from the ground under it
// toward the sun through the shell, how much light gets through.
//
// The texel's place across and up is in the sun's own axes, as Bevy lays a directional light's
// picture, its across reading backward, and the ground under it is where the line through it along
// the sun's light meets the ground's height.
[shader("compute")]
[numthreads(8, 8, 1)]
void shadows(uint3 id : SV_DispatchThreadID)
{
    uint2 size;
    cloud_shadow.GetDimensions(size.x, size.y);
    if (any(id.xy >= size))
        return;

    int sun = brightest();
    if (sun < 0)
    {
        cloud_shadow.Store(id.xy, float4(1.0, 1.0, 1.0, 1.0));
        return;
    }
    float3 toSun = normalize(passLights.directional_lights[sun].direction_to_light);

    float2 uv = (float2(id.xy) + 0.5) / float2(size);
    float2 local = float2((0.5 - uv.x) * 2.0, (uv.y - 0.5) * 2.0) * shadow_frame.w;
    float3 onPlane = shadow_frame.xyz + shadow_across.xyz * local.x + shadow_up.xyz * local.y;

    // Along the sun's light to the ground's height, then up toward the sun through the shell.
    float down = onPlane.y / max(toSun.y, 0.05);
    float3 ground = onPlane - toSun * down;
    float3 center = float3(0.0, -layer.z, 0.0);
    float2 inner = sphere(ground - center, toSun, layer.z + layer.x);
    float2 outer = sphere(ground - center, toSun, layer.z + layer.y);
    float enter = max(inner.y, 0.0);
    float leave = outer.y;

    float optical = 0.0;
    if (leave > enter)
    {
        static const float Steps = 16.0;
        float stride = (leave - enter) / Steps;
        for (float i = 0.5; i < Steps; i += 1.0)
        {
            float3 world = ground + toSun * (enter + stride * i);
            optical += density(world, world - center, false) * stride;
        }
    }

    float through = lerp(1.0, exp(-optical), saturate(shadow_up.w));
    cloud_shadow.Store(id.xy, float4(through, through, through, 1.0));
}

// The clouds over the sky the scene is lit by (Render.SetSkyLighting), passes on the camera after
// the prepass, after Bevy's atmosphere has drawn the sky into the cube its lighting is filtered from
// and before the next frame filters it. `sky_copy` copies the atmosphere's cube, before the clouds
// are marched, for both marches to be lit by and for `sky_merge`, since a pass cannot read and write
// the cube at once. `sky_clouds` marches the clouds along the middle of each texel of a small cube
// of its own, six slices of a 3D image, from the camera in the steps the picture's march takes,
// twice from points a step apart and averaged, keeping their light before the exposure, as the atmosphere's cube keeps the sky's, and how
// much of the sky behind them gets through, and `sky_merge` lays the small cube's clouds over the
// copy, read between their texels, and writes the cube back. So an overcast grays and dims the light
// the ground, the rain and the clouds themselves are lit by, and a clear sky's patch lets its blue
// through.
[shader("compute")]
[numthreads(8, 8, 1)]
void sky_clouds(uint3 id : SV_DispatchThreadID)
{
    uint3 size;
    sky_cloud_cube.GetDimensions(size.x, size.y, size.z);
    if (any(id >= size))
        return;

    // The world's way for the texel, its z turned, since a cube's faces are laid out for a
    // left-handed world, as Bevy draws the atmosphere's cube. Below the horizon the planet stands
    // in the way.
    float2 uv = (float2(id.xy) + 0.5) / float2(size.xy);
    float3 face = cubeDir(uv, id.z);
    float3 way = float3(face.xy, -face.z);

    // Two marches from points a quarter and three quarters along the first step, averaged, in the
    // picture's steps, since the fog reads this cube nearly sharp along the horizon, where a deck
    // seen edge on is marched far, and the step's aliasing from one texel to the next would show
    // in it as stripes up the picture.
    float4 cloud = 0.0;
    if (way.y > -0.02)
    {
        float meanDepth;
        float steps = max(march.x, 16.0);
        cloud = 0.5 * (marchClouds(passView.world_position, way, steps, 0.25, meanDepth)
            + marchClouds(passView.world_position, way, steps, 0.75, meanDepth));
    }
    sky_cloud_cube.Store(id, float4(cloud.rgb / max(passView.exposure, 1e-10), 1.0 - cloud.a));
}

[shader("compute")]
[numthreads(8, 8, 1)]
void sky_copy(uint3 id : SV_DispatchThreadID)
{
    uint3 size;
    sky_held.GetDimensions(size.x, size.y, size.z);
    if (any(id >= size))
        return;
    float2 uv = (float2(id.xy) + 0.5) / float2(size.xy);
    sky_held.Store(id, sky_source.SampleLevel(sky_sampler, cubeDir(uv, id.z), 0.0));
}

[shader("compute")]
[numthreads(8, 8, 1)]
void sky_merge(uint3 id : SV_DispatchThreadID)
{
    uint3 size;
    sky_cube.GetDimensions(size.x, size.y, size.z);
    if (any(id.xy >= size.xy) || id.z >= 6u)
        return;
    float2 uv = (float2(id.xy) + 0.5) / float2(size.xy);
    float3 sky = sky_copied.Load(int4(int3(id), 0)).rgb;
    float4 cloud = sky_cloud_layer.SampleLevel(sky_sampler, float3(uv, (float(id.z) + 0.5) / 6.0), 0.0);
    sky_cube.Store(id, float4(cloud.rgb + cloud.a * sky, 1.0));
}
