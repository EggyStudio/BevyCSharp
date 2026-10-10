// Bevy's deferred_raymarch shader, assets/shaders/deferred_raymarch.wesl at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in Slang.
//
// Marches a ray from each pixel to a signed distance field and packs the surface it meets into the
// camera's deferred buffers as a standard material is packed, so Bevy's deferred lighting shades it
// like any other geometry. Its shadow stage marches from each pixel of a light's shadow view and
// writes the depth of what it meets, so it casts shadows like any other caster.

import bcs_pass;

static const float FAR = 100.0;
static const float SURFACE_EPSILON = 0.001;
static const uint MAX_STEPS = 160;
static const float STEP_SCALE = 0.5;

// Any distance field would do, since what packs it into the buffers below does not depend on its
// shape.
static const float GYROID_SCALE = 5.0;

float gyroid_field(float3 p)
{
    let q = p * GYROID_SCALE + bcs_pass::globals.time * 0.6;
    return dot(sin(q), cos(q.yzx));
}

float map(float3 p)
{
    let shell = (abs(gyroid_field(p)) - 0.14) / GYROID_SCALE;
    let ball = length(p) - 1.3;
    return max(ball, shell);
}

float3 sdf_normal(float3 p)
{
    let e = float2(1.0, -1.0) * 0.0005;
    return normalize(
        e.xyy * map(p + e.xyy) +
        e.yyx * map(p + e.yyx) +
        e.yxy * map(p + e.yxy) +
        e.xxx * map(p + e.xxx));
}

float raymarch(float3 origin, float3 direction)
{
    var t = 0.0;
    for (uint i = 0; i < MAX_STEPS; i++)
    {
        let d = map(origin + direction * t);
        if (d < SURFACE_EPSILON)
            return t;

        t += d * STEP_SCALE;
        if (t > FAR)
            break;
    }

    return FAR;
}

[shader("vertex")]
bcs_pass::Input vertex(uint index : SV_VertexID)
{
    return bcs_pass::full_screen(index);
}

[shader("fragment")]
bcs_pass::Deferred fragment(bcs_pass::Input input)
{
    let origin = bcs_pass::ray_origin(input.uv);
    let direction = bcs_pass::ray_direction(input.uv);
    let t = raymarch(origin, direction);

    // Discarded where the ray misses, which leaves what Bevy's geometry wrote there.
    if (t >= FAR)
        discard;

    let world = origin + direction * t;

    var surface = bcs_pass::surface(sdf_normal(world));
    let field = gyroid_field(world);
    surface.base_color = 0.5 + 0.5 * cos(6.2831 * (field * 0.4 + bcs_pass::globals.time * 0.05) + float3(0.0, 0.8, 1.6));
    surface.metallic = 0.7;
    surface.perceptual_roughness = 0.25;
    // Bevy's packs an opaque standard material with nothing more set, which fog leaves alone.
    surface.fog = false;
    return bcs_pass::deferred(surface, world);
}

// Into a light's shadow map only the depth is needed.
[shader("fragment")]
bcs_pass::ShadowDepth shadow(bcs_pass::Input input)
{
    let origin = bcs_pass::ray_origin(input.uv);
    let direction = bcs_pass::ray_direction(input.uv);
    let t = raymarch(origin, direction);
    if (t >= FAR)
        discard;

    return bcs_pass::shadow_depth(origin + direction * t);
}
