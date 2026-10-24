// Blends each screen probe's light with the probes around it that stand on a surface like its own, a
// thread a probe, five by five of them, each weighed by how near it is and how alike its normal and
// its plane are to the probe's, so the few rays each probe sends average over many and the tiles the
// picture is cut into do not show. Then with what the probes of the frame before held where the
// probe's surface was, a fifth of this frame's to four fifths of theirs, so the light does not crawl
// as the probes slide over the surfaces when the camera moves. The probe's point is found from the
// camera's depth, its place in the frame before through that frame's view, and the four probes of the
// frame before around it blended where each stood on a like surface. A probe whose surface the frame
// before did not show takes this frame's light alone. The frame before's light is held first within
// the spread of this frame's light among the like probes around, so where the light changed, a lamp
// carried or lit, the blend follows it within a frame or two, and where it holds still the frame
// before's light lies inside that spread and keeps its calm.
//
// Taken from 3DEngine's gi_screen_filter.slang, the camera's views through bcs_pass and the frame
// before's images the camera keeps with their history.
import bcs_pass;
import bcs_gi;

// The light each probe's rays summed, and each probe's surface, as gi_screen.slang left them, and the
// frame before's surfaces.
Texture2D<float4> gi_screen_light;
Texture2D<float4> gi_screen_surfaces;
Texture2D<float4> gi_screen_surfaces_previous;

// The light blended, this frame's, which the pass reads, and the frame before's.
[format("rgba16f")] WTexture2D<float4> gi_screen_blended;
Texture2D<float4> gi_screen_blended_previous;

// x: a tile's pixels along each side. y: 1 where the frame before's probes may be read, as they may
// not be on the first frame the images are the camera's. zw: unused.
uniform float4 screen;

// How much of a probe's light this frame's rays give where the frame before's probes held it.
static const float Fresh = 0.2;

// How many of this frame's spreads among the like probes around the frame before's light may lie from
// their mean.
static const float Spread = 2.0;

// Whether a light is one a float holds, neither a value that is not a number nor one past the largest.
// A light that is not is taken as none, by the blend of the probes around and by the frame before's,
// and written as none, so a fault anywhere that gives one darkens a probe for a frame where the blends
// had carried it to every probe around and every frame after, as a non-number at the glow lights'
// light turned 215,459 pixels of 3DEngine's desert village black.
bool lightHeld(float3 light)
{
    return all(abs(light) < 1e30);
}

// The pixel a probe stands on, the middle of the depth's texel under its tile's middle, as
// gi_screen.slang places it.
float2 probePixel(int2 id)
{
    float2 size = float2(passPictureSize());
    return floor(min((float2(id) + 0.5) * screen.x, size - 0.5)) + 0.5;
}

// The point a probe stands on, as gi_screen.slang placed it.
float3 standsOn(int2 id)
{
    int2 pixel = int2(probePixel(id));
    return passWorldPosition(passUv(pixel), passLoadDepth(pixel));
}

// Where the frame before's eye stood, from its view, which turns the world about the eye and moves it
// so the eye lies at the middle.
float3 eyeBefore()
{
    float3 t = passPreviousView.view_from_world[3].xyz;
    return -float3(dot(passPreviousView.view_from_world[0].xyz, t), dot(passPreviousView.view_from_world[1].xyz, t),
        dot(passPreviousView.view_from_world[2].xyz, t));
}

// The spatial blend of a probe's light with its like neighbors', or its own where it holds none, and in
// spread how far the light of those neighbors lies from that blend, each channel's standard deviation
// by the same weights. A neighbor counts by how near the plane of the probe's surface its own point
// lies, so a floor seen at a slant blends along its slope; weighed by how alike their distances from
// the eye were, in 3DEngine the probes in rows apart on such a floor lay further apart than the 2%
// allowed, each row kept its own noise, and the floor showed bands.
float4 spatial(int2 id, int2 grid, float4 surface, out float3 spread)
{
    float4 sum = float4(0.0);
    float3 squares = float3(0.0);
    float3 here = standsOn(id);
    for (int y = -2; y <= 2; y++)
        for (int x = -2; x <= 2; x++)
        {
            int2 at = clamp(id + int2(x, y), int2(0), grid - 1);
            float4 light = gi_screen_light.Load(int3(at, 0));
            float4 other = gi_screen_surfaces.Load(int3(at, 0));
            if (light.a < 0.5 || other.w < 0.0 || !lightHeld(light.rgb))
                continue;
            float off = abs(dot(standsOn(at) - here, surface.xyz));
            float weight = exp(-float(x * x + y * y) / 4.0) * pow(saturate(dot(surface.xyz, other.xyz)), 16.0)
                         * exp(-off / (0.02 * surface.w + 0.05));
            sum += float4(light.rgb, 1.0) * weight;
            squares += light.rgb * light.rgb * weight;
        }
    float3 mean = sum.rgb / max(sum.w, 1e-5);
    spread = sqrt(max(squares / max(sum.w, 1e-5) - mean * mean, 0.0));
    return float4(mean, 1.0);
}

// The frame before's light at the probe's surface, with in a how much of the four probes around its
// place then stood on a surface like it, or 0 where the frame before showed none of it. The four are
// blended by where they stand rather than by their tiles' middles, since read by the middles a still
// view's blend took the next probe along each axis by a pixel's share every frame.
float4 earlier(int2 id, int2 grid, float4 surface)
{
    if (screen.y < 0.5)
        return float4(0.0);
    float3 p = standsOn(id);
    float4 clip = passTransform(passPreviousView.clip_from_world, float4(p, 1.0));
    if (clip.w <= 1e-5)
        return float4(0.0);
    float2 ndc = clip.xy / clip.w;
    float2 then = float2(ndc.x * 0.5 + 0.5, 0.5 - ndc.y * 0.5) * float2(passPictureSize());
    int2 base = int2(floor(then / screen.x - 0.5));
    base -= select(then < probePixel(base), int2(1), int2(0));
    float2 low = probePixel(base);
    float2 f = saturate((then - low) / max(probePixel(base + 1) - low, float2(1e-3)));
    float expected = distance(p, eyeBefore());
    float3 sum = float3(0.0);
    float weights = 0.0;
    for (int corner = 0; corner < 4; corner++)
    {
        int2 at = base + int2(corner & 1, corner >> 1);
        if (any(at < int2(0)) || any(at >= grid))
            continue;
        float4 before = gi_screen_surfaces_previous.Load(int3(at, 0));
        if (before.w < 0.0 || dot(before.xyz, surface.xyz) < 0.9 || abs(before.w - expected) > 0.03 * expected + 0.05)
            continue;
        float3 was = gi_screen_blended_previous.Load(int3(at, 0)).rgb;
        if (!lightHeld(was))
            continue;
        float2 lerps = lerp(1.0 - f, f, float2(corner & 1, corner >> 1));
        float weight = lerps.x * lerps.y;
        sum += was * weight;
        weights += weight;
    }
    return weights > 1e-4 ? float4(sum / weights, weights) : float4(0.0);
}

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint across, down;
    gi_screen_light.GetDimensions(across, down);
    if (id.x >= across || id.y >= down)
        return;
    int2 grid = int2(int(across), int(down));
    float4 own = gi_screen_light.Load(int3(id.xy, 0));
    float4 surface = gi_screen_surfaces.Load(int3(id.xy, 0));
    if (own.a < 0.5 || surface.w < 0.0)
    {
        gi_screen_blended.Store(id.xy, lightHeld(own.rgb) ? own : float4(0.0));
        return;
    }
    float3 spread;
    float4 now = spatial(int2(id.xy), grid, surface, spread);
    // Where the frame before's probes cover the place in part, as at the edge of what it showed, this
    // frame's light counts for the rest.
    float4 before = earlier(int2(id.xy), grid, surface);
    float3 held = clamp(before.rgb, now.rgb - Spread * spread, now.rgb + Spread * spread);
    float fresh = lerp(1.0, Fresh, saturate(before.a));
    // Its alpha one more than the share of its light the frame before's gave.
    float3 result = lerp(held, now.rgb, fresh);
    gi_screen_blended.Store(id.xy, float4(lightHeld(result) ? result : float3(0.0), 2.0 - fresh));
}
