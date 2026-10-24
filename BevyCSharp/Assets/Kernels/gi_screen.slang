// Global illumination's screen probes, a probe on the surface the camera's depth holds at the middle
// of each tile of pixels, a thread a probe, which send a ray along each texel of a hemisphere of
// directions around the surface's normal as far as twice the world's probes are apart. A ray is
// stepped through the depth while it is on the picture and through the scene's distance field from
// where it leaves it, and one that meets a surface brings back the light the surface sends along it.
// One that meets nothing brings back the light from beyond, the world's first cascade merged,
// blended between the world's probes around where it ended. The rays are summed into the light
// arriving at the probe's surface, which gi_screen_filter.slang blends with the probes around and
// the frame before's, and the pass blends between the probes around each pixel that stand on a
// surface like the pixel's (gi_light.slang). A probe on no surface, or outside the world's first
// two cascades, holds nothing, and the pass reads the world's probes there.
//
// Each probe also says how much of each glow light its surface sees, a byte a light, four lights a
// code and four codes a texel of gi_glow_seen0 to gi_glow_seen3, and the light of those too faint to
// be marched to by their faces in gi_glow_faint, which gi_screen_glow.slang takes the picture's glow
// light from the probes around each point by. Where the program left the screen's probes out and a
// glow light is lit, the probes stand for the glow lights alone, their surfaces and what they see of
// each light written and no ray traced.
//
// Taken from 3DEngine's gi_screen.slang. 3DEngine reads a depth of half the window's size, and here
// the camera's depth is read whole, a probe on the texel under its tile's middle; 3DEngine keeps the
// probe's view in a buffer, and here the camera's view comes through bcs_pass. Its log of one
// probe's rays for its gi.screenprobe command is not taken. The march through the depth is the
// pass's (gi_light.slang) with a face's way found from the depth, since a dispatch reads no G-buffer.
import bcs_pass;
import bcs_field;
import bcs_gi;
import bcs_glow;

Texture3D<float> scene_field;
SamplerState scene_field_sampler;
StructuredBuffer<FieldInfo> scene_field_info;
Texture3D<float4> scene_albedo;
Texture3D<uint> scene_faces;
Texture3D<float4> scene_glow;
StructuredBuffer<GiLights> gi_lights;
StructuredBuffer<GlowLight> glow_lights;

// Each world probe's six faces, every cascade, as gi_ambient.slang left them this frame, which a ray's
// hit takes the light reaching it from and which say where each probe stands, and each probe's reach,
// which weighs the light from beyond a ray by whether a probe sees where the ray ended.
Texture3D<float4> probe_cubes;
Texture3D<float> probe_reach;

// The world's first cascade merged, a tile of octahedron texels a probe, and its second, or the first
// again where it has one, which a probe on a surface past the first cascade takes the light from
// beyond its rays from.
Texture3D<float4> merged_first;
Texture3D<float4> merged_second;

// rgb: the light arriving at each probe's surface, irradiance. a: 1 for a probe that holds it.
[format("rgba16f")] WTexture2D<float4> gi_screen_light;

// xyz: each probe's surface's normal. w: how far it is from the eye, or -1 for a probe on no surface.
[format("rgba16f")] WTexture2D<float4> gi_screen_surfaces;

// How much of each glow light each probe's surface sees, a byte a light from none to all of it, four
// lights a code, four codes a texel, and the light of the lights too faint to be marched to by their
// faces, irradiance.
[format("rgba32ui")] WTexture2D<uint4> gi_glow_seen0;
[format("rgba32ui")] WTexture2D<uint4> gi_glow_seen1;
[format("rgba32ui")] WTexture2D<uint4> gi_glow_seen2;
[format("rgba32ui")] WTexture2D<uint4> gi_glow_seen3;
[format("rgba32f")] WTexture2D<float4> gi_glow_faint;

// x: a tile's pixels along each side. y: how far a ray goes before the light beyond it is read. z: 1
// where the probes stand for the glow lights alone. w: one more than the probe whose light is written
// as a value that is not a number, as a test feeds it, or 0 for none.
uniform float4 screen;

// x: the world's probes along each side of a cascade. y: their spacing in cells. z: how many cascades.
// w: unused.
uniform float4 probes;

// x: the world's first cascade's octahedron's texels along each side. y: its second's, or 0 where it
// has none. zw: unused.
uniform uint4 texels;

// xy: how far from its tile's middle each probe marches to the glow lights from this frame, in
// pixels, the same for every probe and moved from frame to frame so a probe's points cover its tile.
// zw: unused.
uniform float4 jitter;

// The hemisphere's texels along each side, sixteen rays a probe.
static const uint Rays = 4;

// The depth at a pixel, clamped to the picture, 0 where nothing was drawn, Bevy's depth running
// backwards.
float depthAt(int2 pixel)
{
    int2 size = int2(passPictureSize());
    return passLoadDepth(clamp(pixel, int2(0), size - 1));
}

// The point the depth holds at a pixel.
float3 pointAt(int2 pixel)
{
    return passWorldPosition(passUv(pixel), depthAt(pixel));
}

// The way the surface at p, at `pixel`, runs along an axis of the depth, a texel `step` long, toward
// the side whose next two texels go on in a line from p, so a probe at the foot of a wall takes its
// floor's way and not the way up the wall, and away from the depth's edge and from the sky. Toward
// the nearer texel, a floor seen at a slant took the wall's in 3DEngine, and its probe, its normal
// tilted, saw a lamp past the wall that the floor does not.
float3 wayAlong(int2 pixel, float3 p, int2 step)
{
    int2 size = int2(passPictureSize());
    bool aheadIn = all(pixel + step * 2 < size) && depthAt(pixel + step) > 0.0 && depthAt(pixel + step * 2) > 0.0;
    bool behindIn = all(pixel - step * 2 >= int2(0)) && depthAt(pixel - step) > 0.0 && depthAt(pixel - step * 2) > 0.0;
    float3 ahead = pointAt(pixel + step), further = pointAt(pixel + step * 2);
    float3 behind = pointAt(pixel - step), beyond = pointAt(pixel - step * 2);
    float3 aheadMiss = 2.0 * ahead - further - p, behindMiss = 2.0 * behind - beyond - p;
    return aheadIn && (!behindIn || dot(aheadMiss, aheadMiss) <= dot(behindMiss, behindMiss)) ? ahead - p : p - behind;
}

// The step toward a pixel's neighbor a texel on or back along an axis, the one whose point lies
// nearer p, so a texel at a thing's edge takes its neighbor on its own surface.
float3 nearerStep(int2 pixel, int2 step, float3 p)
{
    float3 ahead = depthAt(pixel + step) > 0.0 ? pointAt(pixel + step) - p : float3(1e10);
    float3 behind = depthAt(pixel - step) > 0.0 ? p - pointAt(pixel - step) : float3(1e10);
    return dot(ahead, ahead) < dot(behind, behind) ? ahead : behind;
}

// The way out from the surface the depth holds at a pixel, where its point is p, turned toward the
// eye, from its nearer neighbors along each axis.
float3 pictureNormal(int2 pixel, float3 p)
{
    float3 toEye = passView.world_position - p;
    float3 n = surfaceNormal(cross(nearerStep(pixel, int2(1, 0), p), nearerStep(pixel, int2(0, 1), p)), toEye);
    return dot(n, toEye) < 0.0 ? -n : n;
}

// Where q falls on the picture, the pixel it falls in, its depth and the depth the picture holds
// there, or false where it lies behind the eye or off the picture.
bool onScreen(float3 q, out int2 pixel, out float depth, out float held)
{
    pixel = int2(0);
    depth = 0.0;
    held = 0.0;
    float4 clip = passTransform(passView.clip_from_world, float4(q, 1.0));
    if (clip.w <= 1e-4)
        return false;
    float3 ndc = clip.xyz / clip.w;
    float2 uv = float2(ndc.x * 0.5 + 0.5, 0.5 - ndc.y * 0.5);
    if (any(uv < 0.0) || any(uv > 1.0))
        return false;
    int2 size = int2(passPictureSize());
    pixel = min(int2(uv * float2(size)), size - 1);
    depth = ndc.z;
    held = passLoadDepth(pixel);
    return true;
}

// A ray from p along way traced through the depth in steps that lengthen as they go, as the pass's
// traceScreen, meeting a surface where it first passes behind the depth within two steps of it, or a
// quarter of a unit, the step halved five times to find where, the ray met only where the depth
// does not leap between the halving's ends and the face there turns toward the ray, the face's way
// found from the depth. A ray that passed behind the depth so, or went off the picture, is left at
// the last point it stood in front of it in `left`, for the field to find what it passed behind.
bool traceScreen(float3 p, float3 way, float length, int steps, out float3 hit, out float left)
{
    hit = p;
    left = length;
    float before = 0.0;
    float behindFrom = -1.0;
    float3 eye = passView.world_position;
    for (int i = 0; i < steps; i++)
    {
        float s = float(i + 1) / float(steps);
        float t = length * s * s;
        float3 q = p + way * t;
        int2 pixel;
        float depth, held;
        if (!onScreen(q, pixel, depth, held))
        {
            left = behindFrom >= 0.0 ? behindFrom : before;
            return false;
        }
        if (held > 0.0 && depth < held)
        {
            float3 surface = pointAt(pixel);
            float behind = distance(eye, q) - distance(eye, surface);
            if (behind < max(length / float(steps) * 2.0, 0.25))
            {
                float near = before, far = t;
                int2 atPixel = pixel;
                [loop]
                for (int k = 0; k < 5; k++)
                {
                    float middle = (near + far) * 0.5;
                    int2 m;
                    float md, mh;
                    if (onScreen(p + way * middle, m, md, mh) && mh > 0.0 && md < mh)
                    {
                        far = middle;
                        atPixel = m;
                    }
                    else
                        near = middle;
                }
                hit = pointAt(atPixel);
                // Across a thing's edge the depth leaps between the halving's ends, and a ray that
                // crossed it passed behind the thing.
                int2 frontPixel;
                float frontDepth, frontHeld;
                bool leaps = false;
                if (onScreen(p + way * near, frontPixel, frontDepth, frontHeld))
                {
                    float frontBy = frontHeld > 0.0 ? distance(eye, p + way * near) - distance(eye, pointAt(frontPixel)) : -1e6;
                    float behindBy = distance(eye, p + way * far) - distance(eye, hit);
                    leaps = behindBy - frontBy > max(0.25, 8.0 * (far - near));
                }
                // A face the ray meets from behind, its way leaving the surface, is not where it
                // meets the thing.
                if (!leaps && dot(pictureNormal(atPixel, hit), way) < 0.0)
                    return true;
            }
            if (behindFrom < 0.0)
                behindFrom = before;
        }
        before = t;
    }
    left = behindFrom >= 0.0 ? behindFrom : length;
    return false;
}

// A direction of the hemisphere around +z for a point of the square, the octahedron's upper half
// unfolded onto it, its length before it was made one in w.
float4 hemisphere(float2 uv)
{
    float2 a = uv * 2.0 - 1.0;
    float x = (a.x + a.y) * 0.5, y = (a.x - a.y) * 0.5;
    float3 d = float3(x, y, 1.0 - abs(x) - abs(y));
    return float4(normalize(d), length(d));
}

// The eight probes of the world's cascade c around p, the one nearest the field's origin first, and
// how far p lies from it toward the last, in probes, or false where it lies outside the cascade.
bool probesAround(FieldInfo world, float3 p, uint c, out int3 base, out float3 f)
{
    float4 oc = world.cascades[c].origin_and_cell;
    float3 q = (p - oc.xyz) / (oc.w * probes.y) - 0.5;
    base = int3(floor(q));
    f = q - float3(base);
    return oc.w > 0.0 && all(q >= -0.5) && all(q <= probes.x - 0.5);
}

// Where the probe of cascade c at `at` stands, its spot in the grid moved as its faces say.
float3 probeAt(FieldInfo world, int3 at, uint c)
{
    float4 oc = world.cascades[c].origin_and_cell;
    int p = int(probes.x);
    float3 moved = float3(0.0);
    [unroll]
    for (int axis = 0; axis < 3; axis++)
        moved[axis] = faceMove(probe_cubes.Load(int4(axis * 2 * p + at.x, at.y, int(c) * p + at.z, 0)).a);
    return oc.xyz + (float3(at) + 0.5 + moved) * (oc.w * probes.y);
}

// The texel of a cascade's octahedron of `size` along each side at t, which may lie a texel past the
// square's edge. Across an edge the octahedron folds onto the same edge mirrored, and past a corner
// onto the corner across from it, so a ray near the edge is blended with the texels beside it on the
// sphere.
int2 folded(int2 t, int size)
{
    if (t.x < 0)
        t = int2(0, size - 1 - t.y);
    else if (t.x >= size)
        t = int2(size - 1, size - 1 - t.y);
    if (t.y < 0)
        t = int2(size - 1 - t.x, 0);
    else if (t.y >= size)
        t = int2(size - 1 - t.x, size - 1);
    return t;
}

// How much the probe of cascade c at `at`, standing at probe, sees of the point q, 1 where q lies
// nearer it than its rays reached that way the frame before, falling to 0 over a quarter of the
// probes' spacing past it, since q lies where a ray passed and a probe beyond a wall half the spacing
// thick should see none of it.
float probeReaches(float3 q, float3 probe, int3 at, uint c, float apart)
{
    float3 toward = q - probe;
    float d = length(toward);
    if (d < 1e-4)
        return 1.0;
    int2 texel = min(int2(octahedron(toward / d) * float(ReachTexels)), int2(ReachTexels - 1));
    float reached = probe_reach.Load(int4(at.xy * ReachTexels + texel, int(c) * int(probes.x) + at.z, 0));
    float slack = 0.25 * apart;
    return saturate((reached + slack - d) / slack);
}

// The light from beyond a ray's reach along way, the world's merged light blended between the eight
// probes of the finest cascade around the point the ray reached, each weighed by how near it is and
// by whether its rays reached the point the ray passed half their spacing before its end the frame
// before (probeReaches), a probe inside a mesh passed over and one that does not reach the point kept
// only where none does, or blended around the point the ray left from where its end lies past both
// cascades. Read around the point on the surface the ray left from, in 3DEngine the probes there saw
// along the ray's way what the ray itself passed by, and a Cornell box's block's front read 97% over
// its reference across the ways its rays stood for, where read here 3% over. Each probe's light along
// way is blended between the four texels of its octahedron around it, as near as way lies to each,
// since read from the texel way falls in, a ray on the border between two flipped from one to the
// other as the normal from the depth wavered by a degree or two, and a probe's light changed as the
// camera moved.
float3 beyond(FieldInfo world, float3 start, float3 normal, float3 way, float reached, uint around)
{
    float3 end = start + way * reached;
    uint c = 0u;
    int3 base;
    float3 f;
    if (!probesAround(world, end, 0u, base, f))
    {
        c = 1u;
        if (texels.y == 0u || !probesAround(world, end, 1u, base, f))
        {
            end = start;
            reached = 0.0;
            c = around;
            probesAround(world, end, c, base, f);
        }
    }
    float apart = world.cascades[c].origin_and_cell.w * probes.y;
    // Where the ray passed half the probes' spacing before its end, or, read around its start, a
    // point before the surface.
    float3 passed = reached > 0.0 ? end - way * min(0.5 * apart, reached * 0.5) : end + normal * (0.3 * apart);
    uint size = c == 0u ? texels.x : texels.y;
    float2 o = octahedron(way) * float(size) - 0.5;
    int2 first = int2(floor(o));
    float2 between = o - float2(first);
    float3 sum = float3(0.0), seenSum = float3(0.0);
    float weights = 0.0, seenWeights = 0.0;
    [loop]
    for (int corner = 0; corner < 8; corner++)
    {
        int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        int3 neighbor = clamp(base + offset, int3(0), int3(int(probes.x) - 1));
        float3 lerps = lerp(1.0 - f, f, float3(offset));
        float weight = lerps.x * lerps.y * lerps.z;
        if (weight <= 0.0)
            continue;
        float sees = probeReaches(passed, probeAt(world, neighbor, c), neighbor, c, apart);
        [unroll]
        for (int k = 0; k < 4; k++)
        {
            int2 side = int2(k & 1, k >> 1);
            int2 texel = folded(first + side, int(size));
            float2 shares = lerp(1.0 - between, between, float2(side));
            int4 at = int4(neighbor.xy * int(size) + texel, neighbor.z, 0);
            float4 light = c == 0u ? merged_first.Load(at) : merged_second.Load(at);
            float held = weight * shares.x * shares.y * (light.a > 0.25 ? 1.0 : 0.0);
            sum += light.rgb * held;
            weights += held;
            seenSum += light.rgb * held * sees;
            seenWeights += held * sees;
        }
    }
    if (seenWeights > 1e-3 * weights)
        return seenSum / seenWeights;
    return weights > 1e-4 ? sum / weights : float3(0.0);
}

// Writes code `word` of a probe's glow lights, four lights of a byte each.
void writeSeen(uint2 id, uint4 codes, int texel)
{
    if (texel == 0)
        gi_glow_seen0.Store(id, codes);
    else if (texel == 1)
        gi_glow_seen1.Store(id, codes);
    else if (texel == 2)
        gi_glow_seen2.Store(id, codes);
    else
        gi_glow_seen3.Store(id, codes);
}

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint across, down;
    gi_screen_light.GetDimensions(across, down);
    if (id.x >= across || id.y >= down)
        return;
    FieldInfo info = scene_field_info[0];
    GiLights lights = gi_lights[0];
    // The texel of the depth under the middle of the tile.
    float2 size = float2(passPictureSize());
    float2 middle = min((float2(id.xy) + 0.5) * screen.x, size - 0.5);
    int2 pixel = int2(middle);
    float d = depthAt(pixel);
    uint probe = id.y * across + id.x;
    int glows = min(int(lights.counts.w), MostGlowLights);
    if (d <= 0.0 || !fieldOn(info))
    {
        for (int texel = 0; texel < 4; texel++)
            writeSeen(id.xy, uint4(0u), texel);
        gi_glow_faint.Store(id.xy, float4(0.0));
        gi_screen_light.Store(id.xy, float4(0.0));
        gi_screen_surfaces.Store(id.xy, float4(0.0, 0.0, 0.0, -1.0));
        return;
    }

    // The surface's normal from the nearer of the texels on either side along each axis, toward the eye.
    float3 p = pointAt(pixel);
    float3 eye = passView.world_position;
    float3 n = pictureNormal(pixel, p);
    gi_screen_surfaces.Store(id.xy, float4(n, distance(eye, p)));

    // How much of each glow light in reach the surface sees. A light that gives the surface an eighth
    // of the glow lights' light or more is marched to by the halves of its faces, and its byte says
    // how much of it the surface sees. A fainter one is marched to at its middle alone, as a ray's hit
    // is, its light added to the fainter ones' at the probe and its byte left none, so a pixel reads
    // the light of at most eight at a probe. Every light marched to by its faces and read at each
    // pixel, a room of 64 lamps at 1280 by 720 cost 3DEngine's global illumination 5.3 ms and its
    // model pass 2.3 more than none, where so they cost 1.8 and 0.3. The glow lights take the
    // surface's normal from its way along each axis of the depth, which holds at a wall's foot where
    // the nearer texel's does not; tilted up the wall, a probe's normal on the floor beside it saw a
    // lamp past the wall that the floor does not.
    float3 gn = surfaceNormal(cross(wayAlong(pixel, p, int2(1, 0)), wayAlong(pixel, p, int2(0, 1))), eye - p);
    if (dot(gn, eye - p) < 0.0)
        gn = -gn;
    float glowCell = fieldCell(info, p);
    float glowMargin = glowCell * 0.5;
    float glowTotal = 0.0;
    [loop]
    for (int i = 0; i < glows; i++)
    {
        float faded = glowLightFade(glow_lights[i], p, gn);
        if (faded > 0.0)
            glowTotal += luminance(glowLightIrradiance(glow_lights[i], p, gn, glowMargin)) * faded;
    }
    // Which share of each light the surface sees is found from a point of it moved about the tile from
    // frame to frame, the light's tier from the probe's own, so the picture's light that
    // gi_screen_glow.slang takes from the probes around a point, held over the frames, is the same
    // wherever the grid lies over the surface. Marched to from the tile's middle alone, a turn of a
    // few pixels moved 3DEngine's floor's light across a glow light's penumbra by up to nine tenths of
    // it, as the grid moved over the floor. The point is kept on the probe's plane with the depth
    // halfway to it, so a point on the floor past a wall the probe stands before is not taken.
    float2 moved = clamp(middle + jitter.xy, 0.5, size - 0.5);
    int2 movedPixel = int2(moved);
    float3 q = depthAt(movedPixel) > 0.0 ? pointAt(movedPixel) : p;
    int2 halfway = int2((middle + moved) * 0.5);
    float3 between = depthAt(halfway) > 0.0 ? pointAt(halfway) : q;
    float plane = 0.02 * distance(eye, p) + 0.05;
    if (abs(dot(q - p, gn)) > plane || abs(dot(between - p, gn)) > plane)
        q = p;
    float3 faint = float3(0.0);
    uint4 codes = uint4(0u);
    [loop]
    for (int word = 0; word < 16; word++)
    {
        uint levels = 0u;
        [loop]
        for (int j = 0; j < 4 && word * 4 + j < glows; j++)
        {
            GlowLight light = glow_lights[word * 4 + j];
            float faded = glowLightFade(light, p, gn);
            if (faded <= 0.0)
                continue;
            float3 arrived = glowLightIrradiance(light, p, gn, glowMargin) * faded;
            if (luminance(arrived) * 8.0 >= glowTotal)
                levels |= uint(glowLightShown(scene_field, scene_field_sampler, info, light, q, gn, glowCell) * 255.0 + 0.5) << uint(j * 8);
            else
                faint += arrived * glowLightSeen(scene_field, scene_field_sampler, info, light, q, gn, glowCell);
        }
        codes[word & 3] = levels;
        if ((word & 3) == 3)
        {
            writeSeen(id.xy, codes, word >> 2);
            codes = uint4(0u);
        }
    }
    // A probe a test feeds a value that is not a number writes one as its fainter lights' light and as
    // its own.
    bool fed = screen.w > 0.5 && uint(screen.w + 0.5) == probe + 1u;
    float notANumber = asfloat(0x7FC00000u);
    gi_glow_faint.Store(id.xy, float4(fed ? float3(notANumber) : faint, 0.0));
    if (screen.z > 0.5)
    {
        gi_screen_light.Store(id.xy, float4(0.0));
        return;
    }

    // The world's first cascade's probes give the light from beyond the rays, or past it its second's,
    // so the screen's probes reach as far as the second cascade does. Ending at the first, a surface
    // past it took the world's probes alone in 3DEngine, and a step showed along that edge.
    FieldInfo world = probesOf(info, int(probes.z));
    int3 base;
    float3 f;
    uint around = 0u;
    if (!probesAround(world, p, 0u, base, f))
    {
        around = 1u;
        if (texels.y == 0u || !probesAround(world, p, 1u, base, f))
        {
            gi_screen_light.Store(id.xy, float4(0.0));
            return;
        }
    }

    float3 side = abs(n.y) < 0.99 ? normalize(cross(n, float3(0.0, 1.0, 0.0))) : float3(1.0, 0.0, 0.0);
    float3 along = cross(n, side);
    float cell = max(fieldCell(info, p), 1e-3);
    float3 start = p + n * (cell * 0.5);
    float length = screen.y;
    float3 sum = float3(0.0);
    float weights = 0.0;
    [loop]
    for (uint v = 0u; v < Rays; v++)
        [loop]
        for (uint u = 0u; u < Rays; u++)
        {
            // A tile's rays leave from one of sixteen places in their texels of the hemisphere's
            // square, every four tiles by four holding each place once, so the filter blends the
            // ways of every place across the tiles around a pixel. Sent from their texels' middles
            // alone, every probe on a surface sent the same sixteen ways, which read the hemisphere
            // 15% under in 3DEngine's room and 41% under on its voxel game's birch trunk.
            float4 local = hemisphere((float2(u, v) + (float2(id.xy & 3u) + 0.5) / 4.0) / float(Rays));
            float3 way = normalize(side * local.x + along * local.y + n * local.z);
            // How squarely the ray falls on the surface times the share of the hemisphere its texel
            // spans, one over the cube of its octahedron point's distance, as the gather of the
            // world's probes weighs its texels.
            float weight = local.z / (local.w * local.w * local.w);
            // Where the ray met a surface, on the picture or past it in the field, shaded once below,
            // since each shading inlines the field's reads and the glow lights' marches, and the
            // kernel shading at each of the three places a ray may meet one compiled to three times
            // the code.
            float3 light = float3(0.0);
            float3 hit;
            float leftAt;
            bool met = traceScreen(start, way, length, 12, hit, leftAt);
            if (!met)
            {
                float t = leftAt < length ? traceField(scene_field, scene_field_sampler, info, start, way, leftAt, length) : -1.0;
                met = t >= 0.0;
                hit = start + way * t;
                if (!met)
                {
                    light = beyond(world, start, n, way, length, around);
                    // A ray past its reach whose world probes hold nothing along its way goes on
                    // through the field, as far as four times its reach and within the cascade it
                    // starts in and the next coarser one, and takes the light of what it meets, since
                    // a corridor narrower than the coarser probes' spacing holds none of them, and its
                    // walls read 78% under their reference in 3DEngine where 67% so. Into the coarser
                    // cascades the march passed walls thinner than their cells, and a closed room took
                    // the sky's light.
                    if (luminance(light) <= 1e-6)
                    {
                        float far = traceFieldWithin(scene_field, scene_field_sampler, info, start, way, length, 4.0 * length, fieldCascade(info, start, 1.0) + 1);
                        met = far >= 0.0;
                        hit = start + way * far;
                    }
                }
            }
            if (met)
            {
                float3 own;
                light = shadeProbeHit(scene_field, scene_field_sampler, info, info, scene_albedo, scene_faces, scene_glow, lights, glow_lights,
                    probe_cubes, hit, way, probes.x, probes.y, probes.z, float3(1.0), own);
            }
            sum += light * weight;
            weights += weight;
        }
    // The weights scaled to sum to pi, the light reaching the surface under a sky of 1. The glow
    // lights' light straight to the surface, which the rays that meet one leave out, the pass takes at
    // each pixel.
    gi_screen_light.Store(id.xy, float4(fed ? float3(notANumber) : sum * (Pi / max(weights, 1e-6)), 1.0));
}
