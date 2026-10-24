// The glow lights' light on the camera's surfaces, a thread a texel of an image half the picture's
// size, which the pass reads in place of marching to the lights at each pixel (gi_light.slang). Each
// texel's point takes how much of each glow light it sees from the four screen probes around it, each
// probe's share marched from a point of its tile that moves from frame to frame (gi_screen.slang),
// and the light each sends the point in closed form, and blends that with what the texel held the
// frame before where its surface was. Over a round of the probes' points the blend is their mean,
// each frame counting alike, so a penumbra a probe's tile cuts across reads as the probes standing at
// every place in their tiles would read it, and a turn moves it with the world and not with the
// picture's grid. In 3DEngine the floor across a lamp's penumbra moved with turns of one to eight
// pixels by 0.45 to 0.94% of its light, where a pixel that marches itself moved by 0.69 to 0.91%, and
// read at the tiles' middles by 3.1 to 7.9%.
//
// Taken from 3DEngine's gi_screen_glow.slang, whose texels are those of a depth half the window's
// size, and here of an image half the picture's, each reading the camera's depth under its middle.
import bcs_pass;
import bcs_field;
import bcs_gi;
import bcs_glow;

StructuredBuffer<FieldInfo> scene_field_info;
StructuredBuffer<GiLights> gi_lights;
StructuredBuffer<GlowLight> glow_lights;

// Each screen probe's surface's normal and distance from the eye, as gi_screen.slang left them.
Texture2D<float4> gi_screen_surfaces;

// How much of each glow light each probe sees, and the fainter lights' light, as gi_screen.slang left
// them.
Texture2D<uint4> gi_glow_seen0;
Texture2D<uint4> gi_glow_seen1;
Texture2D<uint4> gi_glow_seen2;
Texture2D<uint4> gi_glow_seen3;
Texture2D<float4> gi_glow_faint;

// rgb: the share each texel's point takes of the rough unhidden light of the glow light near it that
// gives it the most of it (glowLightRough, glowLightNear), its light over it, which the pass scales by
// its rough light at each pixel, so its light falls away from the lamp at each pixel as it does. a:
// the point's distance from the eye, or -1 for a texel on no surface. This frame's, which the pass
// reads, and the frame before's.
[format("rgba16f")] WTexture2D<float4> gi_glow_light;
Texture2D<float4> gi_glow_light_previous;

// x: the furthest the frames' light has lain from the blend, in luminance. y: the frames the blend
// holds. zw: the surface's normal, folded onto the octahedron's square. This frame's and the frame
// before's.
[format("rgba16f")] WTexture2D<float4> gi_glow_state;
Texture2D<float4> gi_glow_state_previous;

// rgb: the light of the other glow lights, irradiance, which changes less from texel to texel and the
// pass takes as it is. This frame's and the frame before's.
[format("rgba16f")] WTexture2D<float4> gi_glow_far;
Texture2D<float4> gi_glow_far_previous;

// xyz: each texel's surface's normal, a half and a half of it. w, in 255ths: none for a texel on no
// surface, 1 for one whose share is of no light, 2 and on for the glow light the share is of, counted
// from 2, GlowOnLight for one on a glow light's own surface, and GlowAsk for one no probe around
// holds. The pass weighs the texels around a pixel by the normal, since at a wall's foot the floor's
// texels lie as far from the eye as the wall's, and in 3DEngine the floor beside a lamp lent the
// lamp's face its light.
[format("rgba8")] WTexture2D<float4> gi_glow_normal;

// x: a tile's pixels along each side. y: 1 where the frame before's images may be read. zw: unused.
uniform float4 screen;

// xy: how far from its tile's middle each probe marched to the glow lights from this frame, in pixels.
// zw: unused.
uniform float4 jitter;

// x, y: the places of glow lights whose light is the one the frame before had there, a bit a place,
// the first 32 in x. z: the field's cascades built this frame as meshes came or went within them or
// for the first time, a bit a cascade. w: unused.
uniform uint4 places;

// xyz: the corners of the box around the meshes that came or went within those cascades.
uniform float4 changed_min;
uniform float4 changed_max;

// The frames of a round of the probes' points (JitterAt), after which the blend counts every frame's
// light alike and may be dropped. The least share of this frame's light in the blend after a round
// where the view holds still, a thirty-second, since at a sixty-fourth what the first frames of an
// app saw before the field held the wall stayed in 3DEngine's blend under its shadow twice as long.
// And where the view moves, when each frame's blend is read between four texels of the frame before's
// and spreads a little further over the surface.
static const float GlowRound = 16.0;
static const float GlowStill = 1.0 / 32.0;
static const float GlowMoving = 1.0 / 8.0;

// How far this frame's light may lie from the blend before the blend is dropped, a share past the
// furthest the frames' light has lain from it, and a share of the light past that. The furthest is
// let go by GlowLetGo a frame, three quarters of it held over a round, so the furthest a round's
// points lie, met once a round, stays within GlowDeviations of it.
static const float GlowDeviations = 1.5;
static const float GlowMargin = 0.03;
static const float GlowLetGo = 1.0 / 64.0;

// The least cosine between a point's normal and the normal of a texel of the frame before that the
// point takes the texel's light from, 60 degrees, so the two sides of a right-angled corner part.
static const float GlowSameSurface = 0.5;

// The depth at a pixel, clamped to the picture, 0 where nothing was drawn.
float depthAt(int2 pixel)
{
    return passLoadDepth(clamp(pixel, int2(0), int2(passPictureSize()) - 1));
}

// The point the depth holds at a pixel.
float3 pointAt(int2 pixel)
{
    return passWorldPosition(passUv(pixel), depthAt(pixel));
}

// The pixel of the picture under the middle of a texel of the half-sized images.
int2 pixelOf(int2 texel, int2 halfSize)
{
    int2 size = int2(passPictureSize());
    return min(int2((float2(texel) + 0.5) / float2(halfSize) * float2(size)), size - 1);
}

// The way the surface at p, at `pixel`, runs along an axis of the depth, `step` long, toward the side
// whose next two steps go on in a line from p, so a texel at the foot of a wall takes its floor's way
// and not the way up the wall, and away from the depth's edge and from the sky, as gi_screen.slang
// finds a probe's.
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

// Where a probe marched to the glow lights from this frame, in the picture's pixels, the middle of the
// depth's texel under its tile's middle moved by the frame's jitter, as gi_screen.slang found it.
float2 marchedFrom(int2 probe)
{
    float2 size = float2(passPictureSize());
    float2 middle = min((float2(probe) + 0.5) * screen.x, size - 0.5);
    return floor(clamp(middle + jitter.xy, 0.5, size - 0.5)) + 0.5;
}

// Code `word` of a probe's glow lights, four lights of a byte each.
uint seenAt(int2 probe, int word)
{
    int3 at = int3(probe, 0);
    uint4 codes = word < 4 ? gi_glow_seen0.Load(at) : word < 8 ? gi_glow_seen1.Load(at) : word < 12 ? gi_glow_seen2.Load(at) : gi_glow_seen3.Load(at);
    return codes[word & 3];
}

// How alike a probe's surface is to the point p's at `at` facing n, from 0 to 1: how alike its normal
// is, how near its distance from the eye is to the distance p's plane lies at along the way through
// the probe's pixel, within `alike`, and how near p's plane the depth lies a third and two thirds of
// the way to where it marched from, so a probe on the floor past a wall, on the floor's plane and
// seen over the wall's top, does not lend the floor before the wall the light it sees. None for a
// probe off the grid or on no surface.
float probeLike(int2 probe, int2 grid, float2 at, float3 p, float3 n, float alike)
{
    if (any(probe < int2(0)) || any(probe >= grid))
        return 0.0;
    float4 surface = gi_screen_surfaces.Load(int3(probe, 0));
    if (surface.w < 0.0)
        return 0.0;
    float2 size = float2(passPictureSize());
    float2 middle = floor(min((float2(probe) + 0.5) * screen.x, size - 0.5)) + 0.5;
    float3 way = passRayDirection(middle / size);
    float3 eye = passView.world_position;
    float facing = dot(way, n);
    float expected = facing < -1e-4 ? dot(p - eye, n) / facing : 1e20;
    float off = (surface.w - expected) / alike;
    float2 marched = marchedFrom(probe);
    float between = max(abs(dot(pointAt(int2(lerp(at, marched, 1.0 / 3.0))) - p, n)),
                        abs(dot(pointAt(int2(lerp(at, marched, 2.0 / 3.0))) - p, n))) / alike;
    return pow(saturate(dot(n, surface.xyz)), 8.0) * exp(-off * off - between * between);
}

// The light the glow lights send p facing n this frame, irradiance, each by as much of it as the
// probes around see, blended by weight, the dominant light's as near p as it is (glowLightNear) given
// back and the rest in far with the fainter lights' light the probes summed. Each probe's code of four
// lights is read once for its four, rather than kept in an array indexed by the light, which lavapipe
// keeps in memory of its own and crashed on in 3DEngine.
float3 glowFromProbes(GiLights lights, FieldInfo info, float3 p, float3 n, int2 at[4], float4 weight, float total, int dominant, out float3 far)
{
    int count = min(int(lights.counts.w), MostGlowLights);
    float margin = max(fieldCell(info, p), 0.0) * 0.5;
    float3 sum = float3(0.0);
    far = float3(0.0);
    [unroll]
    for (int corner = 0; corner < 4; corner++)
        if (weight[corner] > 0.0)
        {
            float3 fainter = gi_glow_faint.Load(int3(at[corner], 0)).rgb;
            if (all(abs(fainter) < 1e30))
                far += fainter * (weight[corner] / total);
        }
    [loop]
    for (int word = 0; word * 4 < count; word++)
    {
        uint4 levels = uint4(seenAt(at[0], word), seenAt(at[1], word), seenAt(at[2], word), seenAt(at[3], word));
        if (all(levels == uint4(0u)))
            continue;
        [loop]
        for (int j = 0; j < 4 && word * 4 + j < count; j++)
        {
            float shown = dot(weight, float4((levels >> uint(j * 8)) & 255u));
            if (shown <= 0.0)
                continue;
            GlowLight light = glow_lights[min(word * 4 + j, count - 1)];
            float faded = glowLightFade(light, p, n);
            if (faded <= 0.0)
                continue;
            float3 arrived = glowLightIrradiance(light, p, n, margin) * (faded * shown / (255.0 * total));
            float near = word * 4 + j == dominant ? glowLightNear(light, p) : 0.0;
            sum += arrived * near;
            far += arrived * (1.0 - near);
        }
    }
    return sum;
}

// The rough light the glow light near p facing n that gives it the most of it sends it, unhidden
// (glowLightRough, glowLightNear), that light in dominant, -1 where none is near, and in on whether p
// lies on one's own surface.
float3 glowRough(GiLights lights, FieldInfo info, float3 p, float3 n, out bool on, out int dominant)
{
    int count = min(int(lights.counts.w), MostGlowLights);
    float margin = max(fieldCell(info, p), 0.0) * 0.5;
    float3 most = float3(0.0);
    on = false;
    dominant = -1;
    [loop]
    for (int i = 0; i < count; i++)
    {
        GlowLight light = glow_lights[i];
        float near = glowLightNear(light, p);
        float faded = near > 0.0 ? glowLightFade(light, p, n) : 0.0;
        if (faded <= 0.0)
            continue;
        if (glowLightOn(light, p, n, margin))
        {
            on = true;
            continue;
        }
        float3 rough = glowLightRough(light, p, n, margin) * (faded * near);
        if (luminance(rough) > luminance(most))
        {
            most = rough;
            dominant = i;
        }
    }
    return most;
}

// Whether a glow light that reaches p facing n has a place whose light the frame before did not have
// there, as one placed or moved has.
bool glowPlaceNew(GiLights lights, float3 p, float3 n)
{
    int count = min(int(lights.counts.w), MostGlowLights);
    uint2 kept = places.xy;
    uint2 used = uint2(count >= 32 ? 0xffffffffu : (1u << uint(count)) - 1u, count <= 32 ? 0u : count >= 64 ? 0xffffffffu : (1u << uint(count - 32)) - 1u);
    if ((kept.x & used.x) == used.x && (kept.y & used.y) == used.y)
        return false;
    [loop]
    for (int i = 0; i < count; i++)
    {
        uint place = uint(i);
        bool same = ((place < 32u ? kept.x >> place : kept.y >> (place - 32u)) & 1u) != 0u;
        if (!same && glowLightFade(glow_lights[i], p, n) > 0.0)
            return true;
    }
    return false;
}

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint width, height;
    gi_glow_light.GetDimensions(width, height);
    if (id.x >= width || id.y >= height)
        return;
    int2 halfSize = int2(int(width), int(height));
    int2 pixel = pixelOf(int2(id.xy), halfSize);
    float d = depthAt(pixel);
    FieldInfo info = scene_field_info[0];
    GiLights lights = gi_lights[0];
    if (d <= 0.0)
    {
        gi_glow_light.Store(id.xy, float4(0.0, 0.0, 0.0, -1.0));
        gi_glow_far.Store(id.xy, float4(0.0));
        gi_glow_state.Store(id.xy, float4(0.0));
        gi_glow_normal.Store(id.xy, float4(0.0));
        return;
    }
    // The surface's normal from its way along each axis, toward the eye, a texel of the half-sized
    // images apart.
    float3 p = pointAt(pixel);
    float3 eye = passView.world_position;
    int2 step = max(int2(passPictureSize()) / halfSize, int2(1));
    float3 n = surfaceNormal(cross(wayAlong(pixel, p, int2(step.x, 0)), wayAlong(pixel, p, int2(0, step.y))), eye - p);
    if (dot(n, eye - p) < 0.0)
        n = -n;
    float range = length(eye - p);
    float alike = 0.02 * range + 0.05;

    // The four probes around the point, where they marched from this frame, each weighed by how near
    // it is and how alike its surface is to the point's (probeLike). Where none of the four by four
    // around stands on a surface like the point's, the point is marked for the pass's pixels on it to
    // march to the lights' faces' halves themselves, as a probe does. Taken from the probes on any
    // surface by how near each is, the floor along a wall's foot in 3DEngine took the light its top
    // saw.
    uint across, down;
    gi_screen_surfaces.GetDimensions(across, down);
    int2 grid = int2(int(across), int(down));
    float2 at = float2(pixel) + 0.5;
    float2 g = (at - marchedFrom(int2(0))) / screen.x;
    int2 base = int2(floor(g));
    float2 f = g - float2(base);
    float4 weight = float4(0.0);
    int2 probe[4];
    [unroll]
    for (int corner = 0; corner < 4; corner++)
    {
        int2 offset = int2(corner & 1, corner >> 1);
        float2 lerps = lerp(1.0 - f, f, float2(offset));
        probe[corner] = clamp(base + offset, int2(0), grid - 1);
        weight[corner] = lerps.x * lerps.y * probeLike(base + offset, grid, at, p, n, alike);
    }
    float total = weight.x + weight.y + weight.z + weight.w;
    // Where none of the four is, as on the floor behind a lamp whose probes stand on the lamp, the
    // four of the four by four around that count most by how near each is, two tiles off counting
    // nothing, each kept in place of the least of the four.
    if (total <= 1e-3)
    {
        [loop]
        for (int k = 0; k < 16; k++)
        {
            int2 other = base + int2(k % 4 - 1, k / 4 - 1);
            float2 apart = abs(g - float2(other));
            float near = max(2.0 - apart.x, 0.0) * max(2.0 - apart.y, 0.0) * 0.25;
            float w = near > 0.0 ? near * probeLike(other, grid, at, p, n, alike) : 0.0;
            float least = min(min(weight.x, weight.y), min(weight.z, weight.w));
            if (w <= least)
                continue;
            int2 kept = clamp(other, int2(0), grid - 1);
            if (weight.x == least) { weight.x = w; probe[0] = kept; }
            else if (weight.y == least) { weight.y = w; probe[1] = kept; }
            else if (weight.z == least) { weight.z = w; probe[2] = kept; }
            else { weight.w = w; probe[3] = kept; }
        }
        total = weight.x + weight.y + weight.z + weight.w;
    }
    bool onLight;
    int dominant;
    float3 rough = glowRough(lights, info, p, n, onLight, dominant);
    if (onLight || total <= 1e-3)
    {
        gi_glow_light.Store(id.xy, float4(0.0, 0.0, 0.0, range));
        gi_glow_far.Store(id.xy, float4(0.0));
        gi_glow_state.Store(id.xy, float4(0.0, 0.0, octahedron(n)));
        gi_glow_normal.Store(id.xy, float4(n * 0.5 + 0.5, float(onLight ? GlowOnLight : GlowAsk) / 255.0));
        return;
    }
    float3 far;
    float3 light = glowFromProbes(lights, info, p, n, probe, weight, total, dominant, far);
    gi_glow_normal.Store(id.xy, float4(n * 0.5 + 0.5, float(dominant + 2) / 255.0));
    float3 now = float3(rough.r > 1e-8 ? light.r / rough.r : 0.0, rough.g > 1e-8 ? light.g / rough.g : 0.0, rough.b > 1e-8 ? light.b / rough.b : 0.0);

    // What the texels of the frame before around the point's place then held, each where its point lay
    // as far from the frame before's eye as this one does on a surface facing as this one's does. A
    // texel holds its light as a share of its own surface's rough light, and at a corner the other
    // surface's texels lie as far from the eye, so taken by their distance alone the floor along the
    // walls of a room in 3DEngine took the walls' shares times its own rough light, a line of light a
    // texel apart along each corner.
    float3 before = float3(0.0), farBefore = float3(0.0);
    float furthest = 0.0, held = 0.0, still = 0.0, frames = 1.0 / GlowStill;
    if (screen.y > 0.5)
    {
        float4 clip = passTransform(passPreviousView.clip_from_world, float4(p, 1.0));
        if (clip.w > 1e-5)
        {
            float2 ndc = clip.xy / clip.w;
            float2 then = float2(ndc.x * 0.5 + 0.5, 0.5 - ndc.y * 0.5) * float2(halfSize) - 0.5;
            int2 from = int2(floor(then));
            float2 part = then - float2(from);
            float3 t = passPreviousView.view_from_world[3].xyz;
            float3 eyeThen = -float3(dot(passPreviousView.view_from_world[0].xyz, t), dot(passPreviousView.view_from_world[1].xyz, t),
                dot(passPreviousView.view_from_world[2].xyz, t));
            float expected = distance(p, eyeThen);
            float most = 0.0;
            [unroll]
            for (int corner = 0; corner < 4; corner++)
            {
                int2 offset = int2(corner & 1, corner >> 1);
                int2 texel = from + offset;
                if (any(texel < int2(0)) || any(texel >= halfSize))
                    continue;
                float4 was = gi_glow_light_previous.Load(int3(texel, 0));
                float3 wasFar = gi_glow_far_previous.Load(int3(texel, 0)).rgb;
                // A texel whose light a float does not hold is taken as none.
                if (was.a <= 0.0 || abs(was.a - expected) > 0.02 * expected + 0.05 || !all(abs(was.rgb) < 1e30) || !all(abs(wasFar) < 1e30))
                    continue;
                float4 state = gi_glow_state_previous.Load(int3(texel, 0));
                if (dot(fromOctahedron(state.zw), n) < GlowSameSurface)
                    continue;
                float2 lerps = lerp(1.0 - part, part, float2(offset));
                float share = lerps.x * lerps.y;
                before += was.rgb * share;
                farBefore += wasFar * share;
                furthest += state.x * share;
                frames = min(frames, state.y);
                held += share;
                most = max(most, share);
            }
            if (held > 1e-4)
            {
                before /= held;
                farBefore /= held;
                furthest /= held;
                still = most / held;
            }
        }
    }
    // Dropped where no texel held the point, where a light that reaches it is new to its place, or,
    // after a round, where the light this frame's share and far light give the point lies further from
    // the blend's than the frames' has. A blend still in its first round is dropped too where the
    // field's cascade under the point was built this frame as meshes came or went around the point, or
    // for the first time, since what the probes saw before the field held what stands between is no
    // part of what they see; held whole, the frames of an app before the field built its meshes in
    // place of their boxes lay in 3DEngine's blend under a wall's shadow, and the floor across a
    // lamp's penumbra strayed from where it settles by 13% a round after the app began.
    float lit = luminance(now * rough + far), litBefore = luminance(before * rough + farBefore);
    float apart = abs(lit - litBefore);
    int under = fieldCascade(info, p, 1.0);
    bool rebuilt = under >= 0 && ((places.z >> uint(under)) & 1u) != 0u && all(p >= changed_min.xyz) && all(p <= changed_max.xyz);
    bool dropped = held <= 1e-4 || glowPlaceNew(lights, p, n) || (rebuilt && frames < GlowRound)
        || (frames >= GlowRound && apart > GlowDeviations * furthest + GlowMargin * max(lit, litBefore) + 1e-6);
    if (dropped)
        frames = 0.0;
    // steady is 1 where the view held still and one texel of the frame before gives the whole, less as
    // the view moves and four share it; and where they cover the place in part, as at the edge of what
    // the frame before showed, this frame's light counts for the rest. A moving view's blend counts as
    // one frame's light, so where the view stops the frames after it make the mean anew and reach a
    // round's within a round.
    float steady = saturate(2.0 * still - 1.0);
    float least = lerp(GlowMoving, GlowStill, steady);
    float fresh = dropped ? 1.0 : lerp(1.0, max(1.0 / (frames + 1.0), least), saturate(held));
    float3 kept = lerp(before, now, fresh), keptFar = lerp(farBefore, far, fresh);
    gi_glow_light.Store(id.xy, float4(all(abs(kept) < 1e30) ? kept : float3(0.0), range));
    gi_glow_far.Store(id.xy, float4(all(abs(keptFar) < 1e30) ? keptFar : float3(0.0), 0.0));
    gi_glow_state.Store(id.xy, float4(dropped ? 0.0 : max(furthest * (1.0 - GlowLetGo), apart), min(frames * steady + 1.0, 1.0 / GlowStill), octahedron(n)));
}
