// Bends grass blades and tree crowns in a wind, and lights them as Bevy lights its own materials.
//
// Every blade of the field is one entity with the same mesh and material, which Bevy draws as one
// instanced batch, so the shader tells the blades apart by where each stands in the world, its
// origin setting the phase of the gusts, and the field ripples rather than swaying as one. A vertex
// bends by how high it stands on its mesh, its root not at all and its top the most, along the
// wind's direction in the world, whichever way the blade is turned. The prepass bends the same
// way, so the shadows and the depth the effects read follow the blades.

import bcs;

/// How far the top of the mesh is blown, in world units, at the strongest gust.
uniform float sway;

/// Where on the mesh, in its own units up, the bending starts.
uniform float root;

/// How far above the root the bending is whole.
uniform float reach;

/// How fast the gusts come.
uniform float speed;

/// Which way the wind blows, across the ground.
uniform float2 wind;

/// How far the normal a surface is lit by leans toward straight up, from none at zero, for a
/// sheet such as a blade that light passes through.
uniform float lean;

/// The color at the root and at the top, the top catching the light.
uniform float4 low;
uniform float4 high;

/// How much of the way up the mesh a point is, from nothing at its root to one at its reach.
float height(float3 position)
{
    return saturate((position.y - root) / reach);
}

/// Where a point of the mesh is blown to in the world, at a time in seconds, which the main pass
/// and the prepass each read from their own binding.
float3 blown(uint instance, float3 position, float time)
{
    let world = bcs::local_to_world(instance, position);
    let origin = bcs::local_to_world(instance, float3(0.0));
    let t = time * speed;

    // Two waves crossing the field at different speeds, so the gusts do not repeat as one.
    let along = dot(origin.xz, normalize(wind));
    let gust = 0.6 + 0.4 * sin(t - along * 0.45) + 0.25 * sin(t * 2.7 + origin.x * 1.3 + origin.z * 0.7);

    // Bent more toward the top, as a stalk is, and dipped a little as it leans.
    let h = height(position);
    let bend = h * h * sway * gust;
    return world + float3(wind.x * bend, -0.3 * bend * h, wind.y * bend);
}

[shader("vertex")]
bcs::VertexOutput vertex(bcs::Vertex v)
{
    bcs::VertexOutput output;
    let world = blown(v.instance_index, v.position, bcs::globals.time);
    output.world_position = float4(world, 1.0);
    output.position = bcs::world_to_clip(world);
    output.world_normal = bcs::normal_local_to_world(v.instance_index, v.normal);
    output.uv = float2(v.uv.x, height(v.position));
    output.instance_index = v.instance_index;
    return output;
}

/// A direction in the world taken into the mesh's own space, by the inverse of its transform,
/// which Bevy keeps transposed for the normals, so its columns are the rows wanted here.
float3 world_to_local(uint instance, float3 direction)
{
    let a = bcs::meshes[instance].local_from_world_transpose_a;
    let b = bcs::meshes[instance].local_from_world_transpose_b;
    let c0 = a[0].xyz;
    let c1 = float3(a[0].w, a[1].xy);
    let c2 = float3(a[1].zw, b);
    return float3(dot(c0, direction), dot(c1, direction), dot(c2, direction));
}

[shader("vertex")]
bcs::PrepassVertexOutput prepass_vertex(bcs::PrepassVertex v)
{
    // The prepass places a point by the mesh's transform itself, so the wind's push is taken
    // into the mesh's own space and added there.
    let push = blown(v.instance_index, v.position, bcs::prepass_globals.time) - bcs::local_to_world(v.instance_index, v.position);
    return bcs::prepass_output(v.instance_index, v.position + world_to_local(v.instance_index, push), v.normal, v.uv);
}

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh, bool front : SV_IsFrontFace) : SV_Target
{
    var surface = bcs::surface(mesh);
    surface.base_color = lerp(low, high, mesh.uv.y);
    surface.roughness = 0.75;

    // A blade is one thin sheet that light passes through, so it is lit by a normal bent most of
    // the way up, as the ground under it is, rather than going dark on the side the sun is behind.
    let facing = front ? surface.normal : -surface.normal;
    surface.normal = normalize(lerp(facing, float3(0.0, 1.0, 0.0), lean));

    return bcs::lit(surface, mesh);
}
