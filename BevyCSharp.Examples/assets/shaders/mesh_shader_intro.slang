// Bevy's mesh_shader_intro shaders in Slang: a task shader that runs a cube of mesh shader
// workgroups growing with time, a mesh shader that writes a cube for each, colored by where it is
// and culled triangle by triangle in a wave, and a fragment shader that colors it.
import bcs_pass;

/// What the task shader hands every mesh shader workgroup it runs.
struct Payload
{
    /// Multiplied into every cube's color, so changing it changes them all.
    float4 color_mask;
    /// How many workgroups run across, down and deep, which each cube's color is a share of.
    uint3 groups;
};

groupshared Payload payload;

[shader("amplification")]
[numthreads(1, 1, 1)]
void task()
{
    payload.color_mask = float4(1.0);

    // How many workgroups run grows with time until it passes what the GPU allows, when every
    // cube disappears, which shows what choosing too many does.
    let count = uint(bcs_pass::globals.time);
    payload.groups = uint3(count);
    DispatchMesh(count, count, count, payload);
}

/// A corner of a cube where a mesh shader writes it, with its color, which Bevy's mesh shader
/// writes once a triangle and this one once a corner, since Slang declares nothing a fragment
/// shader reads per triangle, and every corner of a cube has the cube's color.
struct Out
{
    float4 position : SV_Position;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
    float4 color_mask : COLOR0;
};

/// Whether a triangle is left out.
struct Culled
{
    bool cull : SV_CullPrimitive;
};

/// A corner of the cube as it is laid out below.
struct Corner
{
    float3 position;
    float3 normal;
    float2 uv;
};

/// Half the cube's size.
static const float HALF = 0.1;

/// The cube's corners, four a face, with Y up and the camera looking down Z.
static const Corner CUBE[24] = {
    // Front
    Corner(float3(-HALF, -HALF, HALF), float3(0.0, 0.0, 1.0), float2(0.0, 0.0)),
    Corner(float3(HALF, -HALF, HALF), float3(0.0, 0.0, 1.0), float2(1.0, 0.0)),
    Corner(float3(HALF, HALF, HALF), float3(0.0, 0.0, 1.0), float2(1.0, 1.0)),
    Corner(float3(-HALF, HALF, HALF), float3(0.0, 0.0, 1.0), float2(0.0, 1.0)),
    // Back
    Corner(float3(-HALF, HALF, -HALF), float3(0.0, 0.0, -1.0), float2(1.0, 0.0)),
    Corner(float3(HALF, HALF, -HALF), float3(0.0, 0.0, -1.0), float2(0.0, 0.0)),
    Corner(float3(HALF, -HALF, -HALF), float3(0.0, 0.0, -1.0), float2(0.0, 1.0)),
    Corner(float3(-HALF, -HALF, -HALF), float3(0.0, 0.0, -1.0), float2(1.0, 1.0)),
    // Right
    Corner(float3(HALF, -HALF, -HALF), float3(1.0, 0.0, 0.0), float2(0.0, 0.0)),
    Corner(float3(HALF, HALF, -HALF), float3(1.0, 0.0, 0.0), float2(1.0, 0.0)),
    Corner(float3(HALF, HALF, HALF), float3(1.0, 0.0, 0.0), float2(1.0, 1.0)),
    Corner(float3(HALF, -HALF, HALF), float3(1.0, 0.0, 0.0), float2(0.0, 1.0)),
    // Left
    Corner(float3(-HALF, -HALF, HALF), float3(-1.0, 0.0, 0.0), float2(1.0, 0.0)),
    Corner(float3(-HALF, HALF, HALF), float3(-1.0, 0.0, 0.0), float2(0.0, 0.0)),
    Corner(float3(-HALF, HALF, -HALF), float3(-1.0, 0.0, 0.0), float2(0.0, 1.0)),
    Corner(float3(-HALF, -HALF, -HALF), float3(-1.0, 0.0, 0.0), float2(1.0, 1.0)),
    // Top
    Corner(float3(HALF, HALF, -HALF), float3(0.0, 1.0, 0.0), float2(1.0, 0.0)),
    Corner(float3(-HALF, HALF, -HALF), float3(0.0, 1.0, 0.0), float2(0.0, 0.0)),
    Corner(float3(-HALF, HALF, HALF), float3(0.0, 1.0, 0.0), float2(0.0, 1.0)),
    Corner(float3(HALF, HALF, HALF), float3(0.0, 1.0, 0.0), float2(1.0, 1.0)),
    // Bottom
    Corner(float3(HALF, -HALF, HALF), float3(0.0, -1.0, 0.0), float2(0.0, 0.0)),
    Corner(float3(-HALF, -HALF, HALF), float3(0.0, -1.0, 0.0), float2(1.0, 0.0)),
    Corner(float3(-HALF, -HALF, -HALF), float3(0.0, -1.0, 0.0), float2(1.0, 1.0)),
    Corner(float3(HALF, -HALF, -HALF), float3(0.0, -1.0, 0.0), float2(0.0, 1.0)),
};

/// Its triangles, two a face.
static const uint3 TRIANGLES[12] = {
    uint3(0, 1, 2), uint3(2, 3, 0),       // front
    uint3(4, 5, 6), uint3(6, 7, 4),       // back
    uint3(8, 9, 10), uint3(10, 11, 8),    // right
    uint3(12, 13, 14), uint3(14, 15, 12), // left
    uint3(16, 17, 18), uint3(18, 19, 16), // top
    uint3(20, 21, 22), uint3(22, 23, 20), // bottom
};

[shader("mesh")]
[numthreads(1, 1, 1)]
[outputtopology("triangle")]
void mesh(
    uint3 at : SV_DispatchThreadID,
    in payload Payload given,
    out vertices Out corners[24],
    out indices uint3 triangles[12],
    out primitives Culled culled[12])
{
    SetMeshOutputCounts(24, 12);

    // The cube goes where its workgroup is in the grid, in place of the transform a mesh has.
    let offset = float3(at);

    // A color for where it is among all of them, times what the task shader handed on.
    let color = float4(float3(at) / float3(given.groups), 1.0) * given.color_mask;

    for (uint i = 0; i < 24; i++)
    {
        let corner = CUBE[i];
        corners[i].position = bcs_pass::transform(bcs_pass::view.clip_from_world, float4(corner.position + offset, 1.0));
        corners[i].normal = corner.normal;
        corners[i].uv = corner.uv;
        corners[i].color_mask = color;
    }

    // Culled in a wave that moves with time, offset by where the cube is.
    let culled_now = sin(bcs_pass::globals.time * 10.0 + offset.x + offset.y + offset.z) > 0.8;

    for (uint i = 0; i < 12; i++)
    {
        triangles[i] = TRIANGLES[i];
        culled[i].cull = culled_now;
    }
}

/// The color and the depth a fragment writes.
struct Colored
{
    float4 color : SV_Target;
    float depth : SV_Depth;
};

[shader("fragment")]
Colored fragment(Out corner)
{
    Colored output;
    output.color = corner.color_mask;
    // Its own depth, which Bevy's example writes to show a fragment shader can.
    output.depth = corner.position.z;
    return output;
}
