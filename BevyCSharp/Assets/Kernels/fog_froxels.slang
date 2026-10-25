// The light the fog scatters toward the camera in each froxel, a cell of the view's frustum cut into
// slices deepening away from the eye, and how thick the fog is there (Render.SetSkyFog with
// SkyFogSettings.Froxels). fog_integrate.slang then adds the slices up from the eye.
//
// The fog is the same layer the sky's fog works out in closed form, densest at its base and
// thinning by e every scale height above it, made lumpy by a noise drifting on the wind and
// thickened inside the boxes the program names. Its light is the sky's color along the way, the
// same the closed form colors the distance with, so where the froxels end and the closed form goes
// on there is no seam. That light is split into the sky's own, which reaches every point, and the
// sun's share past it, which the sun's shadow takes away, so a wall's shadow is a darker shaft
// through the fog and the light between two buildings a bright one. Point and spot lights add their
// own, scattered evenly, so a lamp in the fog has a glow about it.
//
// A froxel's point moves through its slice from one frame to the next and the light found there is
// blended with the froxel's light last frame, found where the point stood in last frame's view, so
// the slices' steps blur into a smooth fog over a few frames as the camera moves.
import bcs_pass;

// This frame's froxels, the light scattered toward the eye a meter in the rgb, and how much light a
// meter of the fog takes away in the alpha.
[format("rgba16f")] WTexture3D<float4> fog_scatter;

// Last frame's.
Texture3D<float4> fog_scatter_previous;
SamplerState fog_sampler;

// x, y, z: the froxels across, down and deep. w: how far from the eye the last slice ends.
uniform float4 grid;

// x: how much light a meter of the fog takes away at the layer's base. y: the layer's scale height.
// z: the height of its base. w: unused.
uniform float4 layer;

// x: how far the noise moves the density from the layer's, from none at 0. y: one over the size of a
// lump. z: where in its slice this frame's point lies, from 0 to 1. w: 1 where last frame's froxels
// may be read.
uniform float4 lumps;

// xyz: how far the wind has carried the lumps, in world units. w: how many point and spot lights
// are read at most.
uniform float4 wind;

// x: how many degrees above the horizon the fog's color is read at. w: 1 where the color given
// stands in for the sky's.
uniform float4 horizon;

// The fog's color where the camera has no environment map or the program gives its own, linear,
// in the picture's units before the exposure.
uniform float4 color;

// The boxes the fog is thickened inside, two of these a box, its middle and how much light a meter
// of it takes away, then half its size and over how far its edge fades.
uniform float4 boxes[16];
uniform uint4 counts;

// Where the first slice ends, the rest deepening from there to the grid's end each by the same
// share, so a slice near the eye is a few centimeters deep and one far off tens of meters.
static const float Near = 0.5;

// How far from the eye a point along the slices lies, from 0 at the eye to the grid's depth.
float sliceDepth(float slice)
{
    if (slice <= 0.0)
        return 0.0;
    return Near * pow(grid.w / Near, slice / grid.z);
}

// Where in the slices a view depth lies, the inverse of sliceDepth.
float sliceAt(float depth)
{
    return log(max(depth, Near) / Near) / log(grid.w / Near) * grid.z;
}

// The sky's color along a way raised to the horizon, as sky_fog.slang reads it, in the picture's
// units.
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
    return passEnvironmentSpecular(raised, 0.08) * passView.exposure;
}

// The light of the sky alone, the even part of the fog's light every point gets whatever shadow it
// stands in, the mean of the light reaching a point from above and from below.
float3 skyAmbient()
{
    if (horizon.w > 0.5 || !passHasEnvironment())
        return color.rgb * passView.exposure * 0.35;
    float3 above = passEnvironmentDiffuse(float3(0.0, 1.0, 0.0));
    float3 below = passEnvironmentDiffuse(float3(0.0, -1.0, 0.0));
    return 0.5 * (above + below) * passView.exposure;
}

// A hash of a lattice point, from 0 to 1.
float hash(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

// Value noise between the lattice's points, smoothly, from 0 to 1.
float valueNoise(float3 p)
{
    float3 cell = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(
        lerp(lerp(hash(cell), hash(cell + float3(1, 0, 0)), f.x), lerp(hash(cell + float3(0, 1, 0)), hash(cell + float3(1, 1, 0)), f.x), f.y),
        lerp(lerp(hash(cell + float3(0, 0, 1)), hash(cell + float3(1, 0, 1)), f.x), lerp(hash(cell + float3(0, 1, 1)), hash(cell + float3(1, 1, 1)), f.x), f.y),
        f.z);
}

// Three octaves of it, a lump with smaller lumps on it, about 0.5 on average.
float lumpy(float3 p)
{
    return valueNoise(p) * 0.57 + valueNoise(p * 2.03 + 11.7) * 0.29 + valueNoise(p * 4.11 + 23.1) * 0.14;
}

// How much light a meter of the fog takes away at a point.
float density(float3 at)
{
    float above = (at.y - layer.z) / max(layer.y, 1e-3);
    // Held ten scale heights under the base, so a point far down a valley stays finite.
    float thick = layer.x * exp(-clamp(above, -10.0, 40.0));
    thick *= lerp(1.0, 2.0 * lumpy((at + wind.xyz) * lumps.y), saturate(lumps.x));

    for (uint i = 0u; i < counts.x; i++)
    {
        float4 middle = boxes[i * 2u];
        float4 extent = boxes[i * 2u + 1u];
        float3 outside = abs(at - middle.xyz) - extent.xyz;
        float inside = -max(outside.x, max(outside.y, outside.z));
        thick += middle.w * saturate(inside / max(extent.w, 1e-3));
    }
    return thick;
}

// The directional light that lights the scene most, whose shadow the sun's share of the fog's light
// falls into, or none.
int brightest()
{
    int best = -1;
    float most = 0.0;
    for (uint i = 0u; i < passDirectionalLightCount(); i++)
    {
        float bright = dot(passLights.directional_lights[i].color.rgb, float3(0.2126, 0.7152, 0.0722));
        if (bright > most)
        {
            most = bright;
            best = int(i);
        }
    }
    return best;
}

[shader("compute")]
[numthreads(4, 4, 4)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint3 size = uint3(grid.xyz);
    if (any(id >= size))
        return;

    float2 uv = (float2(id.xy) + 0.5) / grid.xy;
    float3 way = normalize(passRayDirection(uv));
    float3 forward = -normalize(passView.world_from_view[2].xyz);
    float slant = max(dot(way, forward), 1e-3);

    // This frame's point in its slice, along the ray through the froxel's middle.
    float depth = lerp(sliceDepth(float(id.z)), sliceDepth(float(id.z) + 1.0), lumps.z);
    float3 eye = passView.world_position;
    float3 at = eye + way * (depth / slant);

    float thick = density(at);
    // With no sun to cast a shadow, the sky's whole light reaches every point, as the closed form
    // has it.
    float3 sky = skyAlong(way);
    float3 light = sky;

    int sun = brightest();
    if (sun >= 0)
    {
        float3 ambient = min(skyAmbient(), sky);
        float shadow = passDirectionalShadow(uint(sun), at, -way, passViewDepth(at));
        light = ambient + (sky - ambient) * shadow;
    }

    uint lamps = min(passPointLightCount(), uint(wind.w));
    for (uint i = 0u; i < lamps; i++)
    {
        float3 reaching = passPointLightRadiance(i, at);
        if (dot(reaching, reaching) <= 0.0)
            continue;
        float shadow = passPointShadow(i, at, -way);
        light += reaching * shadow * passView.exposure * (1.0 / (4.0 * 3.14159265));
    }

    float4 now = float4(light * thick, thick);

    // Last frame's light where this froxel's point stood in last frame's view.
    if (lumps.w > 0.5)
    {
        float2 before = passPreviousUvOf(at);
        float was = -passTransform(passPreviousView.view_from_world, float4(at, 1.0)).z;
        float3 coordinate = float3(before, sliceAt(was) / grid.z);
        if (all(coordinate >= 0.0) && all(coordinate <= 1.0) && was > 0.0)
        {
            float4 history = fog_scatter_previous.SampleLevel(fog_sampler, coordinate, 0.0);
            now = lerp(history, now, 0.12);
        }
    }

    fog_scatter.Store(id, now);
}
