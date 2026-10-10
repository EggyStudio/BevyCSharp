// Mesh shaders that each draw one half of the view in clip space, workgroup 0 the left half and 1
// the right, in the color the task shader hands on, or without a task shader green on the left and
// blue on the right.

uniform float4 tint;

struct Payload
{
    float4 color;
};

groupshared Payload payload;

struct Corner
{
    float4 position : SV_Position;
    float4 color : COLOR0;
};

static const float2 CORNERS[4] = { float2(0.0, -1.0), float2(1.0, -1.0), float2(1.0, 1.0), float2(0.0, 1.0) };

[shader("amplification")]
[numthreads(1, 1, 1)]
void task()
{
    payload.color = tint;
    DispatchMesh(2, 1, 1, payload);
}

[shader("mesh")]
[numthreads(1, 1, 1)]
[outputtopology("triangle")]
void mesh(uint3 group : SV_GroupID, in payload Payload given, out vertices Corner corners[4], out indices uint3 triangles[2])
{
    SetMeshOutputCounts(4, 2);
    let left = group.x == 0 ? -1.0 : 0.0;

    for (uint i = 0; i < 4; i++)
    {
        corners[i].position = float4(left + CORNERS[i].x, CORNERS[i].y, 0.5, 1.0);
        corners[i].color = given.color;
    }

    triangles[0] = uint3(0, 1, 2);
    triangles[1] = uint3(0, 2, 3);
}

[shader("mesh")]
[numthreads(1, 1, 1)]
[outputtopology("triangle")]
void mesh_alone(uint3 group : SV_GroupID, out vertices Corner corners[4], out indices uint3 triangles[2])
{
    SetMeshOutputCounts(4, 2);
    let left = group.x == 0 ? -1.0 : 0.0;

    for (uint i = 0; i < 4; i++)
    {
        corners[i].position = float4(left + CORNERS[i].x, CORNERS[i].y, 0.5, 1.0);
        corners[i].color = group.x == 0 ? float4(0.0, 1.0, 0.0, 1.0) : float4(0.0, 0.0, 1.0, 1.0);
    }

    triangles[0] = uint3(0, 1, 2);
    triangles[1] = uint3(0, 2, 3);
}

[shader("fragment")]
float4 fragment(Corner corner) : SV_Target
{
    return corner.color;
}
