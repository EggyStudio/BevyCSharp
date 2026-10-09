// Bevy's water_material shader in Slang, ripples made by laying four octaves of a normal map over
// each other as they drift, for its ssr example. Bevy's draws only deferred, into the buffers its
// screen-space reflections read, and this does too, through its deferred stage.

import bcs;

Texture2D water_normals;
SamplerState water_sampler;

/// How far each octave drifts each second, in u and v, two octaves to a vector.
uniform float4 octave_vectors[2];
/// How wide the waves of each octave are.
uniform float4 octave_scales;
/// How tall the waves of each octave are.
uniform float4 octave_strengths;

/// One octave's normal, which is not a slerp toward it but near enough.
float3 octave(float2 uv, float strength)
{
    let n = water_normals.Sample(water_sampler, uv).rbg * 2.0 - 1.0;
    return normalize(lerp(float3(0.0, 1.0, 0.0), n, strength));
}

/// The four octaves together, drifting with the time.
float3 ripples(float2 uv, float time)
{
    let uv0 = uv * octave_scales[0] + octave_vectors[0].xy * time;
    let uv1 = uv * octave_scales[1] + octave_vectors[0].zw * time;
    let uv2 = uv * octave_scales[2] + octave_vectors[1].xy * time;
    let uv3 = uv * octave_scales[3] + octave_vectors[1].zw * time;
    return normalize(
        octave(uv0, octave_strengths[0]) + octave(uv1, octave_strengths[1]) +
        octave(uv2, octave_strengths[2]) + octave(uv3, octave_strengths[3]));
}

/// Black and nearly as smooth as glass, as the example's standard material base is.
bcs::Surface water(bcs::Surface surface, float2 uv, float time)
{
    surface.base_color = float4(0.0, 0.0, 0.0, 1.0);
    surface.roughness = 0.09;
    surface.normal = ripples(uv, time);
    return surface;
}

[shader("vertex")]
bcs::PrepassVertexOutput prepass_vertex(bcs::PrepassVertex vertex)
{
    return bcs::prepass_output(vertex.instance_index, vertex.position, vertex.normal, vertex.uv);
}

[shader("fragment")]
bcs::Deferred deferred(bcs::PrepassVertexOutput mesh)
{
    return bcs::deferred(water(bcs::surface(mesh), mesh.uv, bcs::prepass_globals.time), mesh);
}
