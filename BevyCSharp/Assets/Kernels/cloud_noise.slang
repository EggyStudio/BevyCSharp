// The noise the clouds are shaped from, made once an app (Render.SetClouds), every volume tiling so
// the sky can be filled with it without a seam.
//
// The shape volume holds in red Perlin noise made billowy by Worley noise, the low frequency a
// cloud's body is cut from, and in green, blue and alpha Worley noise at three rising frequencies,
// which round the body into heaps, as Schneider's clouds for Horizon Zero Dawn (Nubis) shape theirs.
// The detail volume holds Worley noise at three higher frequencies, which wears a cloud's edges
// into wisps. The weather map holds where the sky has cloud in red and what kind in green, from
// the flat heaps of fair weather at none to towering ones at one, the same in both its slices.
//
// Worley noise is the distance to the nearest of points scattered one a cell, inverted so it is
// bright about each point. Both kinds wrap their lattice at the volume's edge, which is what makes
// them tile.

// x: the texels along each side. y: which to make, the shape volume at 0, the detail at 1 and the
// weather map at 2. z: the texels deep. w: the seed.
uniform uint4 size;

[format("rgba8")] WTexture3D<float4> cloud_noise;

// Three numbers from 0 to 1 a lattice point, pcg3d's.
float3 hash3(uint3 v)
{
    v = v * 1664525u + 1013904223u + size.w;
    v.x += v.y * v.z;
    v.y += v.z * v.x;
    v.z += v.x * v.y;
    v ^= v >> 16u;
    v.x += v.y * v.z;
    v.y += v.z * v.x;
    v.z += v.x * v.y;
    return float3(v) / 4294967295.0;
}

// The lattice point wrapped into a period, so the noise repeats every period cells.
uint3 wrap(float3 cell, float period)
{
    return uint3(fmod(fmod(cell, period) + period, period));
}

// Worley noise at a point measured in cells, the lattice repeating every period, bright about each
// cell's point and dark between.
float worley(float3 p, float period)
{
    float3 cell = floor(p);
    float nearest = 1e9;
    for (int z = -1; z <= 1; z++)
        for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
            {
                float3 neighbor = cell + float3(x, y, z);
                float3 at = neighbor + hash3(wrap(neighbor, period));
                float3 apart = at - p;
                nearest = min(nearest, dot(apart, apart));
            }
    return 1.0 - saturate(sqrt(nearest));
}

// Gradient noise at a point measured in cells, the lattice repeating every period, from about -1
// to 1.
float perlin(float3 p, float period)
{
    float3 cell = floor(p);
    float3 f = p - cell;
    float3 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
    float corners[8];
    for (int i = 0; i < 8; i++)
    {
        float3 corner = float3(i & 1, (i >> 1) & 1, (i >> 2) & 1);
        float3 gradient = normalize(hash3(wrap(cell + corner, period)) * 2.0 - 1.0 + 1e-4);
        corners[i] = dot(gradient, f - corner);
    }
    float x0 = lerp(lerp(corners[0], corners[1], u.x), lerp(corners[2], corners[3], u.x), u.y);
    float x1 = lerp(lerp(corners[4], corners[5], u.x), lerp(corners[6], corners[7], u.x), u.y);
    return lerp(x0, x1, u.z) * 1.4;
}

// Octaves of Worley noise from a base period, each twice the last and half as strong.
float worleyOctaves(float3 uv, float period)
{
    return worley(uv * period, period) * 0.625
        + worley(uv * period * 2.0, period * 2.0) * 0.25
        + worley(uv * period * 4.0, period * 4.0) * 0.125;
}

// Octaves of gradient noise from a base period, from 0 to about 1.
float perlinOctaves(float3 uv, float period, int octaves)
{
    float sum = 0.0;
    float weight = 0.5;
    float total = 0.0;
    for (int i = 0; i < octaves; i++)
    {
        float scale = period * float(1 << i);
        sum += perlin(uv * scale, scale) * weight;
        total += weight;
        weight *= 0.5;
    }
    return saturate(sum / total * 0.5 + 0.5);
}

float remap(float value, float low, float high, float toLow, float toHigh)
{
    return toLow + (value - low) / max(high - low, 1e-4) * (toHigh - toLow);
}

[shader("compute")]
[numthreads(4, 4, 4)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint3 extent = uint3(size.x, size.x, size.z);
    if (any(id >= extent))
        return;
    float3 uv = (float3(id) + 0.5) / float3(extent);

    float4 texel;
    if (size.y == 0u)
    {
        // Perlin-Worley, the gradient noise lifted where the Worley is bright, then three Worleys.
        float billows = worleyOctaves(uv, 4.0);
        float body = saturate(remap(perlinOctaves(uv, 4.0, 4), billows - 1.0, 1.0, 0.0, 1.0));
        texel = float4(body, worleyOctaves(uv, 4.0), worleyOctaves(uv, 8.0), worleyOctaves(uv, 16.0));
    }
    else if (size.y == 1u)
    {
        texel = float4(worleyOctaves(uv, 4.0), worleyOctaves(uv, 8.0), worleyOctaves(uv, 16.0), 1.0);
    }
    else
    {
        // The weather on a plane, where the clouds are in red, coarse patches with ragged edges,
        // and what kind in green, varying over a longer way.
        float3 flat = float3(uv.xy, 0.0);
        // Stretched, since octaves of noise bunch about the middle, so a coverage covers about as
        // much of the sky as it says.
        float where = saturate((perlinOctaves(flat, 6.0, 5) - 0.5) * 3.0 + 0.5);
        float kind = perlinOctaves(flat + 0.37, 2.0, 3);
        texel = float4(where, kind, 0.0, 1.0);
    }
    cloud_noise.Store(id, texel);
}
