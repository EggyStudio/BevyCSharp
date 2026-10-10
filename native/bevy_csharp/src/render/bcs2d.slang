// What a BevyCSharp 2D material shader sees of Bevy: the view, time, the mesh's transform, and the
// struct Bevy's own 2D vertex shader hands a fragment shader.
//
// A shader drawing a 2D mesh reaches it with `import bcs2d;`, and everything in it is under the
// `bcs2d` namespace. What the material carries is whatever the shader declares, as for a 3D
// material, and is set from C# by name. Bevy's 2D pipeline binds the view in group zero, the mesh
// in group one and the material in group two, and the bindings below are in spaces the bridge
// moves to the first two after the compile (100 to the view's, 101 to the mesh's), which keeps
// them out of the way of the material's.
//
// Locations are `TEXCOORDn` semantics, as in `bcs`, the index being the location, and the structs
// below line up with the ones Bevy's own 2D WGSL declares, which differ from the 3D ones.

module bcs2d;

public namespace bcs2d
{

// -- The view, in Bevy's group zero

/// The part of Bevy's view uniform a shader usually reads, in the order Bevy declares it.
///
/// Matrices are arrays of four columns, which `transform` multiplies out.
public struct View
{
    public float4 clip_from_world[4];
    public float4 unjittered_clip_from_world[4];
    public float4 world_from_clip[4];
    public float4 world_from_view[4];
    public float4 view_from_world[4];
    public float4 clip_from_view[4];
    public float4 view_from_clip[4];
    public float3 world_position;
    public float exposure;
    /// Where the view is on its target: x, y, width and height, in pixels.
    public float4 viewport;
    public float4 main_pass_viewport;
};

[[vk::binding(0, 100)]] public ConstantBuffer<View> view;

/// Time, in the layout Bevy's `Globals` has.
public struct Globals
{
    /// Seconds since startup, wrapping to zero after an hour.
    public float time;
    /// Seconds since the previous frame.
    public float delta_time;
    public uint frame_count;
};

/// Time, laid out as C lays it out (`ScalarDataLayout`) in the twelve bytes Bevy binds.
[[vk::binding(1, 100)]] public ConstantBuffer<Globals, ScalarDataLayout> globals;

/// Multiplies a column-major matrix, held as four columns, by a vector.
public float4 transform(float4 columns[4], float4 v)
{
    return columns[0] * v.x + columns[1] * v.y + columns[2] * v.z + columns[3] * v.w;
}

/// Where a point in the world lands in clip space.
public float4 world_to_clip(float3 position)
{
    return transform(view.clip_from_world, float4(position, 1.0));
}

/// Where a fragment is on the view, from zero to one, given its `SV_Position`.
public float2 screen_uv(float4 frag_position)
{
    return (frag_position.xy - view.viewport.xy) / view.viewport.zw;
}

// -- The mesh, in Bevy's group one

/// One 2D mesh's transforms, in the layout Bevy keeps them in.
///
/// The world transform is an affine matrix stored as its three rows. The inverse transpose, for
/// normals, is packed into nine floats.
public struct Mesh2d
{
    public float4 world_from_local[3];
    public float4 local_from_world_transpose_a[2];
    public float local_from_world_transpose_b;
    public uint flags;
    public uint material_bind_group_slot;
    public uint tag;
    public uint metadata_index;
};

[[vk::binding(0, 101)]] public StructuredBuffer<Mesh2d> meshes;

/// Where a point on the mesh drawn as `instance` is in the world.
public float3 local_to_world(uint instance, float3 position)
{
    let rows = meshes[instance].world_from_local;
    let p = float4(position, 1.0);
    return float3(dot(rows[0], p), dot(rows[1], p), dot(rows[2], p));
}

/// Where a point on the mesh lands in clip space.
public float4 local_to_clip(uint instance, float3 position)
{
    return world_to_clip(local_to_world(instance, position));
}

/// Which way a normal on the mesh faces in the world.
public float3 normal_local_to_world(uint instance, float3 normal)
{
    let a = meshes[instance].local_from_world_transpose_a;
    let b = meshes[instance].local_from_world_transpose_b;

    // The same unpacking as Bevy's `mat2x4_f32_to_mat3x3_unpack`, as three columns.
    let c0 = a[0].xyz;
    let c1 = float3(a[0].w, a[1].xy);
    let c2 = float3(a[1].zw, b);

    let n = c0 * normal.x + c1 * normal.y + c2 * normal.z;
    return any(normal != float3(0.0)) ? normalize(n) : n;
}

/// The number an entity's `MeshTag` gives the mesh drawn as `instance`, zero without one.
public uint tag(uint instance)
{
    return meshes[instance].tag;
}

// -- What passes between stages

/// A vertex of a 2D mesh with positions, normals and one set of UVs, which every 2D shape Bevy
/// builds has.
public struct Vertex
{
    public float3 position : TEXCOORD0;
    public float3 normal : TEXCOORD1;
    public float2 uv : TEXCOORD2;
    public uint instance_index : SV_InstanceID;
};

/// What Bevy's own 2D vertex shader hands the fragment shader, less what depends on the mesh.
///
/// A fragment shader taking this works with Bevy's vertex shader as well as with one written here,
/// which is why a 2D material may name a fragment shader alone.
public struct VertexOutput
{
    public float4 position : SV_Position;
    public float4 world_position : TEXCOORD0;
    public float3 world_normal : TEXCOORD1;
    public float2 uv : TEXCOORD2;
    /// Which of the meshes drawn together this one is, for `tag`, at the location Bevy's 2D
    /// vertex shader hands it on at.
    public uint instance_index : TEXCOORD5;
};

/// Does what Bevy's own 2D vertex shader does, for a shader that starts from it.
public VertexOutput standard_vertex(Vertex vertex)
{
    VertexOutput output;
    let world = local_to_world(vertex.instance_index, vertex.position);
    output.world_position = float4(world, 1.0);
    output.position = world_to_clip(world);
    output.world_normal = normal_local_to_world(vertex.instance_index, vertex.normal);
    output.uv = vertex.uv;
    output.instance_index = vertex.instance_index;
    return output;
}

}
