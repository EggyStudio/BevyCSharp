// Ray-traced ambient occlusion: from every surface on screen, a few short rays across the half of
// the sky it faces, through a ray scene of stand-in meshes, and the share that met nothing written
// where Bevy's own lighting reads its occlusion.
//
// Compiled to SPIR-V, since ray queries exist nowhere else. The rays leave from the depth buffer's
// surfaces and point where the prepass's normals say, and blue noise picks their directions so the
// grain a few rays leave is fine and changes every frame.

import bcs_pass;
import bcs_ray;

RaytracingAccelerationStructure scene;
[format("r16f")] RWTexture2D<float> ambient_occlusion;

/// How far a ray looks for something in the way, in world units.
uniform float radius;
/// How dark full occlusion is, from nothing at zero to black at one.
uniform float strength;

static const uint RAYS = 4;

/// A direction in the hemisphere about `normal`, weighted toward it the way light falling on a
/// surface is, from two numbers between zero and one.
float3 cosine_direction(float3 normal, float2 u)
{
    let tangent = normalize(abs(normal.y) < 0.99 ? cross(normal, float3(0.0, 1.0, 0.0)) : cross(normal, float3(1.0, 0.0, 0.0)));
    let bitangent = cross(normal, tangent);

    let r = sqrt(u.x);
    let angle = 6.2831853 * u.y;
    return normalize(tangent * (r * cos(angle)) + bitangent * (r * sin(angle)) + normal * sqrt(max(0.0, 1.0 - u.x)));
}

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint width;
    uint height;
    ambient_occlusion.GetDimensions(width, height);

    if (id.x >= width || id.y >= height)
        return;

    // The occlusion image can be smaller than the picture, so each texel reads the depth under it.
    let picture = bcs_pass::picture_size();
    let pixel = int2(float2(id.xy) * float2(picture) / float2(width, height));
    let depth = bcs_pass::load_depth(pixel);

    if (depth <= 0.0)
    {
        ambient_occlusion[id.xy] = 1.0;
        return;
    }

    let uv = (float2(pixel) + 0.5) / float2(picture);
    let normal = normalize(bcs_pass::load_normal(pixel));
    let from = bcs_pass::world_position(uv, depth) + normal * 0.02;
    let noise = bcs_pass::blue_noise(pixel);

    float occlusion = 0.0;

    for (uint ray = 0; ray < RAYS; ray++)
    {
        // Each ray takes the noise turned a different way, so four rays are four directions.
        let u = frac(noise.xy + float2(ray * 0.618034, ray * 0.414214));
        let hit = bcs_ray::trace_in(scene, from, cosine_direction(normal, u), 0.001, radius);

        if (hit.hit)
        {
            // Something close by hides more of the sky than something at the edge of the radius.
            occlusion += 1.0 - hit.t / radius;
        }
    }

    ambient_occlusion[id.xy] = 1.0 - strength * occlusion / RAYS;
}
