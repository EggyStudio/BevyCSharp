// What a BevyCSharp shader written in Slang sees of Bevy, and of the material drawing it.
//
// A shader reaches it with `import bcs;`, and everything in it is under the `bcs` namespace. The
// bridge writes this file out before every compile, so it always describes the bridge the shader
// is about to run against rather than whichever one was installed last.
//
// Locations are given as `TEXCOORDn` semantics rather than `vk::location`, because Slang's WGSL
// backend numbers stage inputs and outputs by semantic index and ignores the attribute. The index
// is therefore the location, and the structs below line up with the ones Bevy's own WGSL declares.
//
// Bevy's matrices are declared as arrays of columns and multiplied out by hand. Which way Slang
// lays a matrix out depends on the target and on flags, and an array of `float4` has one layout
// everywhere, so the question never arises.

module bcs;

public namespace bcs
{

// -- The material, group three
//
// What a shader material carries. Every material has all of it, and a binding nobody set holds a
// fallback: zeros for the numbers and the data, and a white or empty picture for each texture.

/// The sixty-four numbers a material carries, as sixteen rows of four.
public struct Parameters
{
    public float4 rows[16];
};

[[vk::binding(0, 3)]] public ConstantBuffer<Parameters> parameters;

[[vk::binding(1, 3)]] public Texture2D texture0;
[[vk::binding(2, 3)]] public SamplerState sampler0;

/// Whatever bytes the material was given, read with `data.Load<T>(offset)`.
///
/// A raw buffer rather than a structured one, because Slang reads a raw buffer with C packing, and
/// C packing is what a C# struct with sequential layout already has. A struct written from C# and
/// a struct declared here with the same fields in the same order therefore agree on every offset.
[[vk::binding(3, 3)]] public ByteAddressBuffer data;

[[vk::binding(4, 3)]] public Texture2D texture1;
[[vk::binding(5, 3)]] public SamplerState sampler1;
[[vk::binding(6, 3)]] public Texture2D texture2;
[[vk::binding(7, 3)]] public SamplerState sampler2;
[[vk::binding(8, 3)]] public Texture2D texture3;
[[vk::binding(9, 3)]] public SamplerState sampler3;
[[vk::binding(10, 3)]] public Texture2D texture4;
[[vk::binding(11, 3)]] public SamplerState sampler4;
[[vk::binding(12, 3)]] public Texture2D texture5;
[[vk::binding(13, 3)]] public SamplerState sampler5;
[[vk::binding(14, 3)]] public Texture2D texture6;
[[vk::binding(15, 3)]] public SamplerState sampler6;
[[vk::binding(16, 3)]] public Texture2D texture7;
[[vk::binding(17, 3)]] public SamplerState sampler7;

// These carry no sampler of their own, because a sampler is the scarcest binding there is on some
// backends. Any of the eight above samples them.
[[vk::binding(18, 3)]] public TextureCube cube0;
[[vk::binding(19, 3)]] public TextureCube cube1;
[[vk::binding(20, 3)]] public Texture2DArray array0;
[[vk::binding(21, 3)]] public Texture2DArray array1;
[[vk::binding(22, 3)]] public Texture3D volume0;
[[vk::binding(23, 3)]] public Texture3D volume1;

/// One row of four numbers, in the order the material was given them.
public float4 row(uint index)
{
    return parameters.rows[index];
}

/// One number, counting across the rows.
public float number(uint index)
{
    return parameters.rows[index / 4][index % 4];
}

/// The element at `index` of the data, read as an array of `T`.
public T element<T>(uint index)
{
    return data.Load<T>(index * sizeof(T));
}

/// How many bytes of data the material holds, rounded up to a multiple of four.
///
/// Never zero, because a buffer has to have a size to be bound, so a material given nothing holds
/// sixteen bytes of zeros.
public uint data_size()
{
    uint size;
    data.GetDimensions(size);
    return size;
}

// -- The view, group zero

/// The part of Bevy's view uniform a shader usually reads, in the order Bevy declares it.
///
/// Only a prefix, because the uniform goes on to hold the color grading, which a shader has no
/// business redoing. A prefix is enough, since binding a buffer larger than what is declared is
/// allowed.
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

[[vk::binding(0, 0)]] public ConstantBuffer<View> view;

/// Time, in the layout Bevy's `Globals` has.
public struct Globals
{
    /// Seconds since startup, wrapping to zero after an hour.
    public float time;
    /// Seconds since the previous frame.
    public float delta_time;
    public uint frame_count;
};

/// Time, as the main pass binds it.
[[vk::binding(11, 0)]] public ConstantBuffer<Globals> globals;

/// Time, as the prepass binds it, which is at a different binding because the prepass binds less.
[[vk::binding(1, 0)]] public ConstantBuffer<Globals> prepass_globals;

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

/// Where the camera is.
public float3 camera_position()
{
    return view.world_position;
}

/// Where a fragment is on the view, from zero to one, given its `SV_Position`.
public float2 screen_uv(float4 frag_position)
{
    return (frag_position.xy - view.viewport.xy) / view.viewport.zw;
}

// -- The mesh, group two

/// One mesh's transforms, in the layout Bevy keeps them in.
///
/// The world transform is an affine matrix stored as its three rows, which is why it is three
/// vectors rather than a matrix. The inverse transpose, for normals, is packed into nine floats.
public struct Mesh
{
    public float4 world_from_local[3];
    public float4 previous_world_from_local[3];
    public float4 local_from_world_transpose_a[2];
    public float local_from_world_transpose_b;
    public uint flags;
    public uint2 lightmap_uv_rect;
    public uint first_vertex_index;
    public uint current_skin_index;
    public uint material_and_lightmap_bind_group_slot;
    public uint tag;
    public uint morph_descriptor_index;
};

[[vk::binding(0, 2)]] public StructuredBuffer<Mesh> meshes;

/// Where a point on the mesh drawn as `instance` is in the world.
public float3 local_to_world(uint instance, float3 position)
{
    let rows = meshes[instance].world_from_local;
    let p = float4(position, 1.0);
    return float3(dot(rows[0], p), dot(rows[1], p), dot(rows[2], p));
}

/// Where the same point was in the world on the previous frame, which is what motion vectors need.
public float3 previous_local_to_world(uint instance, float3 position)
{
    let rows = meshes[instance].previous_world_from_local;
    let p = float4(position, 1.0);
    return float3(dot(rows[0], p), dot(rows[1], p), dot(rows[2], p));
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

    // A zero normal stays zero rather than normalizing into a NaN, which is what Bevy does too.
    let n = c0 * normal.x + c1 * normal.y + c2 * normal.z;
    return any(normal != float3(0.0)) ? normalize(n) : n;
}

/// Where a point on the mesh lands in clip space.
public float4 local_to_clip(uint instance, float3 position)
{
    return world_to_clip(local_to_world(instance, position));
}

/// Which of the mesh's own vertices `vertex_id` is, counting from zero.
///
/// `SV_VertexID` counts from where the mesh begins in the buffer Bevy packs every mesh into, which
/// is not zero, so a shader that numbers its vertices, to find the element of a buffer that places
/// each quad for instance, subtracts where the mesh begins.
public uint vertex_number(uint instance, uint vertex_id)
{
    return vertex_id - meshes[instance].first_vertex_index;
}

// -- What passes between stages in the main pass

/// A vertex of a mesh with positions, normals and one set of UVs, which every mesh Bevy builds
/// has.
public struct Vertex
{
    public float3 position : TEXCOORD0;
    public float3 normal : TEXCOORD1;
    public float2 uv : TEXCOORD2;
    public uint instance_index : SV_InstanceID;
};

/// What Bevy's own vertex shader hands the fragment shader, less what depends on the mesh.
///
/// A fragment shader taking this works with Bevy's vertex shader as well as with one written here,
/// which is why a material may name a fragment shader alone.
public struct VertexOutput
{
    public float4 position : SV_Position;
    public float4 world_position : TEXCOORD0;
    public float3 world_normal : TEXCOORD1;
    public float2 uv : TEXCOORD2;
};

/// Does what Bevy's own vertex shader does, for a shader that wants to start from it.
public VertexOutput standard_vertex(Vertex vertex)
{
    VertexOutput output;
    let world = local_to_world(vertex.instance_index, vertex.position);
    output.world_position = float4(world, 1.0);
    output.position = world_to_clip(world);
    output.world_normal = normal_local_to_world(vertex.instance_index, vertex.normal);
    output.uv = vertex.uv;
    return output;
}

// -- What passes between stages in the prepass
//
// The prepass is what draws depth for shadows, and normals and motion for the effects that read
// them. Its vertex locations differ from the main pass, which is why it has structs of its own.

/// A vertex as the prepass reads it.
///
/// The bridge hands a prepass vertex shader of a material's own every attribute the mesh has at
/// these locations, whichever prepass it is drawing for, so the normal is there for a shader that
/// moves the mesh along it.
public struct PrepassVertex
{
    public float3 position : TEXCOORD0;
    public float2 uv : TEXCOORD1;
    public float3 normal : TEXCOORD3;
    public uint instance_index : SV_InstanceID;
};

/// Everything any prepass fragment shader reads, so one vertex shader serves all of them.
///
/// Bevy's fragment shader for the prepass reads a different subset depending on what the camera
/// asked for, and a vertex shader that leaves out one it reads is a pipeline that fails to build.
/// Writing all of them costs nothing where they are not read.
public struct PrepassVertexOutput
{
    public float4 position : SV_Position;
    public float2 uv : TEXCOORD0;
    public float2 uv_b : TEXCOORD1;
    public float3 world_normal : TEXCOORD2;
    public float4 world_tangent : TEXCOORD3;
    public float4 world_position : TEXCOORD4;
    public float4 previous_world_position : TEXCOORD5;
    public nointerpolation uint instance_index : TEXCOORD7;
};

/// Fills in everything the prepass reads from where a vertex ends up.
///
/// `position` and `normal` are on the mesh, before its transform, so a shader that moves a vertex
/// moves it here and passes the result.
public PrepassVertexOutput prepass_output(uint instance, float3 position, float3 normal, float2 uv)
{
    PrepassVertexOutput output;
    let world = local_to_world(instance, position);
    output.world_position = float4(world, 1.0);
    output.previous_world_position = float4(previous_local_to_world(instance, position), 1.0);
    output.position = world_to_clip(world);
    output.world_normal = normal_local_to_world(instance, normal);
    output.world_tangent = float4(0.0);
    output.uv = uv;
    output.uv_b = uv;
    output.instance_index = instance;
    return output;
}

}
