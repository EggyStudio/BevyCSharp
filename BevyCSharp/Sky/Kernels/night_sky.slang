// The sky behind the atmosphere, the stars, the galaxy's band and the moon's disc, added over a
// camera's sky before tonemapping (Render.SetNightSky), dimmed by as much of their light as the
// atmosphere lets through, so they fade and redden toward the horizon, the stars and the galaxy
// coming out as the sun goes down.
//
// The stars and the galaxy stand in the sky's own coordinates, which the way a pixel looks along is
// turned back into by the latitude and the sidereal time (SkyClock), so they turn about the pole
// star as the hours go. A star is a point a hash gives in a cell of a grid over the faces of a cube
// about the sky, most faint and a few bright, a little blue or a little yellow, spread over a pixel
// so it neither flickers nor grows with the picture's size, and twinkling more near the horizon,
// where the air it shines through is thicker. The galaxy is a band about the galactic equator,
// brightest toward its center, broken by dark lanes of noise.
//
// The moon is a sphere in its direction, lit from the sun's, so its phase is what the sun lights
// of the half that faces the eye, its dark part lit faintly by the earth, its seas darker patches
// of noise. It hides the stars behind it.
import bcs_pass;

// x: the latitude, in radians. y: the sidereal time, in radians. z: the time, in seconds. w: how
// much the stars twinkle.
uniform float4 clock;

// xyz: the way toward the moon. w: its angular radius, in radians.
uniform float4 moon;

// xyz: the way toward the sun, which lights the moon. w: how much of the moon's light the earth
// sends back to its dark part.
uniform float4 sun;

// x: the brightest star's light, in nits. y: the galaxy's. z: the full moon's. w: the cells of the
// stars' grid along each face of the cube.
uniform float4 bright;

static const float Pi = 3.14159265;

[shader("vertex")]
FullscreenOutput vertex(uint index : SV_VertexID)
{
    // At the near plane, which every surface's depth passes against, so the sky is drawn over too.
    var corner = fullscreenTriangle(index);
    corner.position.z = 1.0;
    return corner;
}

// Four numbers from 0 to 1 a cell of a face, pcg4d's.
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

float valueNoise(float3 p)
{
    float3 cell = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float corners[8];
    for (int i = 0; i < 8; i++)
    {
        float3 corner = float3(i & 1, (i >> 1) & 1, (i >> 2) & 1);
        corners[i] = hash4(uint4(int4(int3(cell + corner) + 4096, 7))).x;
    }
    float x0 = lerp(lerp(corners[0], corners[1], f.x), lerp(corners[2], corners[3], f.x), f.y);
    float x1 = lerp(lerp(corners[4], corners[5], f.x), lerp(corners[6], corners[7], f.x), f.y);
    return lerp(x0, x1, f.z);
}

float octaves(float3 p)
{
    return valueNoise(p) * 0.5 + valueNoise(p * 2.03) * 0.3 + valueNoise(p * 4.07) * 0.2;
}

// A face of the cube and the place on it a sky direction falls at, from -1 to 1 across and down.
void onCube(float3 c, out uint face, out float2 at)
{
    float3 a = abs(c);
    if (a.x >= a.y && a.x >= a.z)
    {
        face = c.x > 0.0 ? 0u : 1u;
        at = c.yz / a.x;
    }
    else if (a.y >= a.z)
    {
        face = c.y > 0.0 ? 2u : 3u;
        at = c.xz / a.y;
    }
    else
    {
        face = c.z > 0.0 ? 4u : 5u;
        at = c.xy / a.z;
    }
}

// The sky direction a place on a face stands for.
float3 offCube(uint face, float2 at)
{
    float3 c;
    if (face < 2u)
        c = float3(face == 0u ? 1.0 : -1.0, at.x, at.y);
    else if (face < 4u)
        c = float3(at.x, face == 2u ? 1.0 : -1.0, at.y);
    else
        c = float3(at.x, at.y, face == 4u ? 1.0 : -1.0);
    return normalize(c);
}

// The stars' light along a sky direction, each spread over under a pixel's width, so it is as
// sharp as the picture lets it be and neither flickers nor grows with the picture's size.
float3 stars(float3 c, float pixel, float low)
{
    uint face;
    float2 at;
    onCube(c, face, at);
    float cells = bright.w;
    float2 cell = floor((at * 0.5 + 0.5) * cells);
    float3 light = 0.0;
    for (int y = -1; y <= 1; y++)
        for (int x = -1; x <= 1; x++)
        {
            float2 here = cell + float2(x, y);
            float4 h = hash4(uint4(uint2(int2(here) + 65536), face, 11u));
            // One cell in five holds a star.
            if (h.x > 0.2)
                continue;
            float3 star = offCube(face, (here + h.yz) / cells * 2.0 - 1.0);
            float apart = length(c - star);
            float spread = max(pixel * 0.45, 1e-5);
            // Most stars faint and a few bright, as the sky's are, the brightness a high power of a
            // number from 0 to 1.
            float shine = pow(h.w, 20.0);

            // Twinkling as the air between moves, more near the horizon.
            float flicker = 1.0 + clock.w * low * sin(clock.z * (4.0 + h.y * 9.0) + h.z * 60.0);
            float3 tint = lerp(float3(0.75, 0.85, 1.0), float3(1.0, 0.85, 0.65), frac(h.w * 37.0));
            light += tint * shine * flicker * exp(-(apart * apart) / (2.0 * spread * spread));
        }
    return light * bright.x;
}

// The galaxy's band about its equator, brightest toward its middle, broken by dark lanes.
float3 galaxy(float3 c)
{
    // The galactic pole and middle in the sky's coordinates, at their right ascension and
    // declination.
    static const float3 Pole = float3(-0.846, -0.193, 0.456);
    static const float3 Middle = float3(-0.055, -0.873, -0.483);
    float across = dot(c, Pole);
    float band = exp(-across * across / 0.012) + 0.35 * exp(-across * across / 0.08);
    float bulge = exp(-(1.0 - dot(c, Middle)) / 0.08);
    float clumps = 0.55 + 0.45 * octaves(c * 9.0);
    float lanes = 1.0 - 0.6 * smoothstep(0.5, 0.75, octaves(c * 14.0 + 3.1)) * exp(-across * across / 0.004);
    return float3(1.0, 0.94, 0.86) * band * (1.0 + 2.5 * bulge) * clumps * lanes * bright.y;
}

[shader("fragment")]
float4 fragment(FullscreenOutput input) : SV_Target
{
    if (!passHasDepth() || passLoadDepth(int2(input.position.xy)) > 0.0)
        discard;
    float3 way = normalize(passRayDirection(input.uv));
    if (way.y < (passHasAtmosphere() ? -0.1 : 0.0))
        discard;
    float pixel = length(ddx(way)) + length(ddy(way));

    // The way turned back into the sky's own coordinates, its hour angle and declination, then its
    // right ascension from the sidereal time.
    float phi = clock.x;
    float north = -way.z;
    float declination = asin(clamp(way.y * sin(phi) + north * cos(phi), -1.0, 1.0));
    float hourAngle = atan2(-way.x, way.y * cos(phi) - north * sin(phi));
    float ascension = clock.y - hourAngle;
    float3 c = float3(cos(declination) * cos(ascension), cos(declination) * sin(ascension), sin(declination));

    // The stars and the galaxy come out as the sun goes down, whole once it is some nine degrees
    // under the horizon and gone by three over it, as the day's sky outshines them in a picture
    // whose exposure holds still. The moon stays, as it shows pale by day.
    float low = 1.0 - saturate(way.y * 2.0);
    float night = smoothstep(0.05, -0.15, sun.y);
    float3 light = (stars(c, pixel, low) + galaxy(c)) * night;

    // The moon, a sphere seen from far off, lit from the sun's way.
    float facing = dot(way, moon.xyz);
    float offset = acos(clamp(facing, -1.0, 1.0)) / max(moon.w, 1e-5);
    if (offset < 1.5)
    {
        float edge = max(pixel / max(moon.w, 1e-5), 1e-3);
        float inside = 1.0 - smoothstep(1.0 - edge, 1.0 + edge, offset);
        float3 across = (way - moon.xyz * facing) / max(moon.w, 1e-5);
        float3 normal = normalize(across - moon.xyz * sqrt(max(1.0 - dot(across, across), 0.0)));
        float lit = pow(saturate(dot(normal, sun.xyz)), 0.8);
        float seas = 0.72 + 0.28 * smoothstep(0.35, 0.7, octaves(normal * 3.0 + 1.7));
        float3 face = float3(1.0, 0.97, 0.92) * bright.z * (seas * lit + sun.w);
        light = lerp(light, face, inside);
    }

    float3 shown = light * passAtmosphereTransmittance(passView.world_position, way) * passView.exposure;
    if (all(shown <= 0.0))
        discard;
    return float4(shown, 1.0);
}
