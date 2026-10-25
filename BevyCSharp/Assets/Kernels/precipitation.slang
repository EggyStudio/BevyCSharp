// The rain and the snow about a camera (Render.SetPrecipitation), each drop or flake a point moved
// here every frame, which the drops' draw (precipitation_drops.slang) draws as many as this counts
// and the splashes' draw (precipitation_splashes.slang) draws where they landed.
//
// The particles fall in two boxes about the camera, two in three in a near box and the rest in a far
// one, each held in the world rather than carried with the camera, so a particle leaving
// one side comes back in at the other and walking moves through the rain. A drop of rain falls at
// the speed a drop its size reaches in still air (Atlas's fit to Gunn and Kinzer's measurements),
// its size drawn from a spread that grows with how hard it rains, and a flake at about a meter a
// second, fluttering as it goes. Both drift on the wind.
//
// A particle ends where it falls below the height of what stands over the ground at its place, read
// from a map a camera looking down draws (the roofs entry, on that camera), or below its box, and
// comes back at the top of its box somewhere else, so the rain keeps out from under a roof. A drop of
// rain ending on what the map holds close to the camera leaves a splash there.
//
// Each particle in front of the camera is counted into the draw's count and written for it with the
// light it sends toward the eye, worked out once here rather than at each corner the draw makes,
// the sun's through the air, its shadows and the clouds', the sky's from the environment map and
// the lamps'.
import bcs_pass;

// Each particle, where it is in the world (xyz) and its seed from 0 to 1 (w), or below 0 where it
// sleeps. Whether it is a drop or a flake follows from its number and how the rain and the snow
// share the fall, worked out again every frame, so as the weather turns from rain to snow the drops
// in the air turn to flakes where they are, rather than the air emptying while flakes fall from the
// top of the box at a meter a second.
RWStructuredBuffer<float4> particles;

// Three for each particle drawn. Where it is (xyz) and how wide, in meters (w). How fast it moves
// (xyz) and whether it is a flake (w). The light it sends toward the eye, after the exposure (rgb),
// and how much of what is behind it hides (a).
RWStructuredBuffer<float4> drawn;

// The drops' draw's four counts, the vertices of each, how many, the first vertex and the first
// one, then the next splash's slot, counted up for ever.
RWStructuredBuffer<Atomic<uint>> counts;

// Two for each splash. Where it landed (xyz) and when (w), in the clock's seconds. The light it
// sends toward the eye (rgb) and how wide it is, in meters (a).
RWStructuredBuffer<float4> splashes;

// The heights of what stands over the ground, a texel each, below -1e29 where nothing does, and the
// map's frame, its corner in x and z, one over its size in meters, and 1 where the map is drawn.
Texture2D<float> roof_heights;
StructuredBuffer<float4> roof_frame;

// x: how hard it rains, from 0 to 1. y: how hard it snows. z: how many particles are awake. w: how
// many splashes are kept.
uniform float4 amount;

// x: half the near box's width, in meters. y: half the far box's. z: how far from the camera a drop
// splashes. w: unused.
uniform float4 reach;

// xyz: the wind, in meters a second. w: unused.
uniform float4 wind;

// x: the clock, in seconds, wrapping each hour. y: this frame's step, in seconds. z: the frame's
// number. w: how many particles there are.
uniform float4 clock;

// The clouds' shadow where the camera's clouds cast one, as the froxels read it (fog_froxels.slang).
Texture2D<float4> cloud_shadow;
SamplerState cloud_shadow_sampler;
uniform float4 cloud_frame;
uniform float4 cloud_across;
uniform float4 cloud_up;

static const float Pi = 3.14159265;

// Four numbers from 0 to 1, pcg4d's.
float4 hash4(uint4 v)
{
    v = v * 1664525u + 1013904223u;
    v.x += v.y * v.w;
    v.y += v.z * v.x;
    v.z += v.x * v.y;
    v.w += v.y * v.z;
    v ^= v >> 16u;
    v.x += v.y * v.w;
    v.y += v.z * v.x;
    v.z += v.x * v.y;
    v.w += v.y * v.z;
    return float4(v) / 4294967295.0;
}

[shader("compute")]
[numthreads(1, 1, 1)]
void clear(uint3 id : SV_DispatchThreadID)
{
    counts[0].store(6u);
    counts[1].store(0u);
    counts[2].store(0u);
    counts[3].store(0u);
}

// The roofs camera's depth turned into the heights of what stands over the ground, run on that
// camera, which looks straight down. Each pixel's height goes to the texel its place in the world
// falls in, and the map's frame is the camera's own, as it drew, so the two always agree whenever
// the camera was moved.
RWTexture2D<float> roof_map;
RWStructuredBuffer<float4> roof_map_frame;

// x: the map's size, in meters. y: its texels a side.
uniform float4 roof_size;

[shader("compute")]
[numthreads(8, 8, 1)]
void roofs(uint3 id : SV_DispatchThreadID)
{
    uint texels = uint(roof_size.y);
    if (any(id.xy >= texels))
        return;
    float2 corner = passView.world_position.xz - roof_size.x * 0.5;
    if (all(id.xy == 0u))
        roof_map_frame[0] = float4(corner, 1.0 / roof_size.x, 1.0);

    float2 uv = (float2(id.xy) + 0.5) / float(texels);
    float depth = passHasDepth() ? passLoadDepth(int2(id.xy)) : 0.0;
    float3 world = passWorldPosition(uv, max(depth, 1e-7));
    int2 texel = int2(floor((world.xz - corner) * roof_size.y / roof_size.x));
    if (any(texel < 0) || any(texel >= int(texels)))
        return;
    roof_map[texel] = depth > 0.0 ? world.y : -1e30;
}

// The height of what stands over the ground at a place, or far below where the map holds nothing
// there or does not reach.
float roofAt(float2 place)
{
    float4 frame = roof_frame[0];
    if (frame.w < 0.5)
        return -1e30;
    uint texels;
    uint unused;
    roof_heights.GetDimensions(texels, unused);
    float2 uv = (place - frame.xy) * frame.z;
    if (any(uv < 0.0) || any(uv >= 1.0))
        return -1e30;
    return roof_heights.Load(int3(int2(uv * float(texels)), 0));
}

// How much of the sun's light the clouds let through to a point (fog_froxels.slang's).
float cloudShadow(float3 at)
{
    if (cloud_across.w < 0.5)
        return 1.0;
    float3 apart = at - cloud_frame.xyz;
    float2 local = float2(dot(apart, cloud_across.xyz), dot(apart, cloud_up.xyz)) / max(cloud_frame.w, 1e-3);
    float2 uv = local * float2(-0.5, 0.5) + 0.5;
    return cloud_shadow.SampleLevel(cloud_shadow_sampler, uv - floor(uv), 0.0).r;
}

// The directional light that lights the scene most through the air, the sun by day and the moon
// at night, or none (clouds.slang's).
int brightest()
{
    int best = -1;
    float most = 0.0;
    for (uint i = 0u; i < passDirectionalLightCount(); i++)
    {
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

// A drop of rain's width, in meters, from its seed, most small and a few large, the large ones
// larger the harder it rains, as Marshall and Palmer found.
float rainWidth(float seed)
{
    float largest = lerp(1.2, 4.5, saturate(amount.x));
    return lerp(0.5, largest, seed * seed) * 0.001;
}

// Whether a particle is a flake, from its number and the share of the fall that is rain.
bool isFlake(uint i, float rainy)
{
    return hash4(uint4(i, 61u, 13u, 2u)).x >= rainy;
}

// How fast a particle moves, falling at its own speed, carried on the wind and, for a flake,
// fluttering about its way down.
float3 velocityOf(float4 particle, bool flake)
{
    float seed = frac(particle.w);
    if (flake)
    {
        float t = clock.x;
        float phase = seed * 61.0;
        float3 flutter = float3(sin(t * (1.1 + seed) + phase), 0.0, cos(t * (0.9 + seed * 0.8) + phase * 1.7)) * 0.4;
        return float3(0.0, -lerp(0.7, 1.4, seed), 0.0) + wind.xyz + flutter;
    }
    float millimeters = rainWidth(seed) * 1000.0;
    float speed = max(9.65 - 10.3 * exp(-0.6 * millimeters), 0.8);
    return float3(0.0, -speed, 0.0) + wind.xyz;
}

// The light a particle sends toward the eye, before the exposure. A flake is white and scatters
// what lights it. A drop bends the sky behind and about it toward the eye, so it shows the sky's
// light, and the sun and the lamps glint in it, the sun brightest seen past the drop toward it.
float3 lightOf(float3 world, bool flake)
{
    float3 toEye = normalize(passView.world_position - world);
    float3 sky = 0.0;
    if (passHasEnvironment())
        sky = 0.5 * (passEnvironmentDiffuse(float3(0.0, 1.0, 0.0)) + passEnvironmentDiffuse(-toEye));

    float3 sun = 0.0;
    float facing = 0.0;
    int light = brightest();
    if (light >= 0)
    {
        float3 toSun = normalize(passLights.directional_lights[light].direction_to_light);
        float shadow = passDirectionalShadow(uint(light), world, toSun, passViewDepth(world)) * cloudShadow(world);
        sun = passLights.directional_lights[light].color.rgb * passAtmosphereTransmittance(world, toSun) * shadow;
        facing = saturate(dot(-toEye, toSun));
    }

    float3 lamps = 0.0;
    for (uint i = 0u; i < min(passPointLightCount(), 32u); i++)
        lamps += passPointLightRadiance(i, world);

    if (flake)
        return 0.9 * sky + 0.9 / Pi * (0.5 * sun + lamps);
    return 0.8 * sky + (sun * (0.02 + 1.5 * pow(facing, 24.0)) + lamps * 0.3) / Pi;
}

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint i = id.x;
    if (i >= uint(clock.w))
        return;

    float4 particle = particles[i];
    if (i >= uint(amount.z))
    {
        if (particle.w >= 0.0)
            particles[i] = float4(0.0, 0.0, 0.0, -1.0);
        return;
    }

    float3 eye = passView.world_position;
    float side = i % 3u != 0u ? reach.x : reach.y;
    float bottom = eye.y - side * 0.5;
    float top = eye.y + side;
    uint frame = uint(clock.z);
    float rainy = amount.x / max(amount.x + amount.y, 1e-4);

    // Woken, anywhere in its box, so the air fills at once rather than under a front falling from
    // the top.
    if (particle.w < 0.0)
    {
        float4 h = hash4(uint4(i, frame, 17u, 3u));
        particle.xyz = float3(eye.x + (h.x * 2.0 - 1.0) * side, lerp(bottom, top, h.y), eye.z + (h.z * 2.0 - 1.0) * side);
        particle.w = h.w;
    }

    bool flake = isFlake(i, rainy);
    float3 velocity = velocityOf(particle, flake);
    particle.xyz += velocity * clock.y;

    // Back in at the other side of the box where it left one, and down by the box's height where the
    // camera went down past it.
    float2 off = particle.xz - eye.xz;
    off -= 2.0 * side * floor((off + side) / (2.0 * side));
    particle.xz = eye.xz + off;
    if (particle.y > top)
        particle.y -= top - bottom;

    // Ended under what stands over the ground or below the box, a drop splashing on what it landed
    // on close by, and back at the top of the box somewhere else, a little way down so the drops do
    // not come in as one sheet.
    float roof = roofAt(particle.xz);
    if (particle.y < max(roof, bottom))
    {
        float2 apart = particle.xz - eye.xz;
        if (!flake && roof >= bottom && roof < eye.y + 3.0 && dot(apart, apart) < reach.z * reach.z && amount.w > 0.0)
        {
            uint slot = counts[4].add(1u) % uint(amount.w);
            float3 at = float3(particle.x, roof, particle.z);
            splashes[slot * 2u] = float4(at, clock.x);
            splashes[slot * 2u + 1u] = float4(1.5 * lightOf(at + float3(0.0, 0.03, 0.0), false) * passView.exposure, lerp(0.08, 0.16, particle.w));
        }

        float4 h = hash4(uint4(i, frame, 41u, 7u));
        float fall = length(velocity) * max(clock.y, 1.0 / 240.0);
        particle.xyz = float3(eye.x + (h.x * 2.0 - 1.0) * side, top - h.y * max(fall, 0.05), eye.z + (h.z * 2.0 - 1.0) * side);
        particle.w = h.w;
        velocity = velocityOf(particle, flake);
    }
    particles[i] = particle;

    // Drawn where it stands in front of the camera, with room for a streak's length about the
    // picture's edge.
    float4 clip = passTransform(passView.clip_from_world, float4(particle.xyz, 1.0));
    if (clip.w < 0.05 || any(abs(clip.xy) > clip.w * 1.2) || particle.y < roofAt(particle.xz))
        return;

    // Fading in over the first two meters from the lens, where a drop is out of the eye's focus and
    // would cross much of the picture in a streak, and out toward the box's edge, so the box's side
    // does not show.
    float2 apart = abs(particle.xz - eye.xz) / side;
    float edge = 1.0 - smoothstep(0.75, 1.0, max(apart.x, apart.y));
    float near = smoothstep(0.4, 1.8, length(particle.xyz - eye));
    float seed = frac(particle.w);
    float width = flake ? lerp(0.006, 0.02, seed * seed) : rainWidth(seed);
    float opacity = (flake ? 0.95 : 0.7) * edge * near;
    if (opacity <= 0.0)
        return;

    uint slot = counts[1].add(1u);
    drawn[slot * 3u] = float4(particle.xyz, width);
    drawn[slot * 3u + 1u] = float4(velocity, flake ? 1.0 : 0.0);
    drawn[slot * 3u + 2u] = float4(lightOf(particle.xyz, flake) * passView.exposure, opacity);
}
