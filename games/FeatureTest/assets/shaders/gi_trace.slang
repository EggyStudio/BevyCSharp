// Screen-space global illumination, the first step, for each pixel of a half-sized image, one ray
// bounced off the surface there, marched through the depth buffer, picking up the light of what it
// hits from last frame's lit picture, or the sky's where it leaves the screen.
//
// A deliberately small version of the technique, to show the pieces a package builds it from, the
// G-buffer for the surface's normal, the depth buffer to march through, blue noise to choose the
// ray, last frame's picture for the light it finds, and the camera's environment for what it does
// not. Everything it declares is set by name from `ScreenSpaceLight.cs`.

import bcs_pass;

Texture2D<uint4> gbuffer;
Texture2D lit_previous;
[format("rgba16f")] WTexture2D<float4> gathered;

uniform uint steps;
uniform float reach;
uniform float thickness;

/// A direction around `normal`, more of them close to it than far from it, which is how much each
/// direction's light counts on a surface that scatters it evenly.
float3 cosine_direction(float3 normal, float2 random)
{
    let angle = 6.2831853 * random.x;
    let radius = sqrt(random.y);
    let local = float3(radius * cos(angle), radius * sin(angle), sqrt(max(0.0, 1.0 - random.y)));

    let helper = abs(normal.y) < 0.999 ? float3(0.0, 1.0, 0.0) : float3(1.0, 0.0, 0.0);
    let tangent = normalize(cross(helper, normal));
    let bitangent = cross(normal, tangent);
    return normalize(tangent * local.x + bitangent * local.y + normal * local.z);
}

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint width, height;
    gathered.GetDimensions(width, height);

    if (id.x >= width || id.y >= height)
        return;

    let picture = passPictureSize();
    let pixel = int2(id.xy * 2);
    let depth = passLoadDepth(pixel);

    if (depth <= 0.0)
    {
        gathered.Store(id.xy, float4(0.0));
        return;
    }

    let surface = passSurfaceOf(gbuffer.Load(int3(pixel, 0)));
    let world = passWorldPosition(passUv(pixel), depth);
    let noise = passBlueNoise(int2(id.xy));
    let direction = cosine_direction(surface.normal, noise.xy);

    // The sky's light, in the picture's own units, unless something on screen is met first.
    var light = passEnvironmentDiffuse(direction) * passView.exposure;
    let start = world + surface.normal * 0.02;

    for (uint index = 0; index < steps; index++)
    {
        let along = reach * (float(index) + noise.z) / float(steps);
        let probe = start + direction * along;
        let clip = passTransform(passView.clip_from_world, float4(probe, 1.0));

        if (clip.w <= 0.0)
            break;

        let ndc = clip.xy / clip.w;
        let uv = float2(ndc.x * 0.5 + 0.5, 0.5 - ndc.y * 0.5);

        if (any(uv < 0.0) || any(uv > 1.0))
            break;

        let hit_pixel = int2(uv * float2(picture));
        let scene = passLoadDepth(hit_pixel);

        if (scene <= 0.0)
            continue;

        // Behind what the picture shows there, by less than a surface is thick, is a hit.
        let behind = passViewDepth(probe) - passDistanceFromDepth(scene);

        if (behind > 0.0 && behind < thickness)
        {
            let hit = passWorldPosition(uv, scene);
            let before = passPreviousUvOf(hit);
            light = lit_previous.SampleLevel(passPictureSampler, before, 0.0).rgb;
            break;
        }
    }

    gathered.Store(id.xy, float4(light, 1.0));
}
