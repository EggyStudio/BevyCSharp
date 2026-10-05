// Bevy's compute_mesh shader in Slang, which writes a cube's vertices and indices into buffers,
// each vertex its position, normal and UV, eight numbers in all. What it builds is not the point,
// which is that the mesh is made on the GPU.
import bcs_compute;

RWStructuredBuffer<float> vertex_data;
RWStructuredBuffer<uint> index_data;

// A cube three units across, seen from +Z, each face its own four corners.
static const float vertices[192] = {
    -1.5, -1.5, 1.5, 0.0, 0.0, 1.0, 0.0, 0.0,
    1.5, -1.5, 1.5, 0.0, 0.0, 1.0, 1.0, 0.0,
    1.5, 1.5, 1.5, 0.0, 0.0, 1.0, 1.0, 1.0,
    -1.5, 1.5, 1.5, 0.0, 0.0, 1.0, 0.0, 1.0,
    -1.5, 1.5, -1.5, 0.0, 0.0, -1.0, 1.0, 0.0,
    1.5, 1.5, -1.5, 0.0, 0.0, -1.0, 0.0, 0.0,
    1.5, -1.5, -1.5, 0.0, 0.0, -1.0, 0.0, 1.0,
    -1.5, -1.5, -1.5, 0.0, 0.0, -1.0, 1.0, 1.0,
    1.5, -1.5, -1.5, 1.0, 0.0, 0.0, 0.0, 0.0,
    1.5, 1.5, -1.5, 1.0, 0.0, 0.0, 1.0, 0.0,
    1.5, 1.5, 1.5, 1.0, 0.0, 0.0, 1.0, 1.0,
    1.5, -1.5, 1.5, 1.0, 0.0, 0.0, 0.0, 1.0,
    -1.5, -1.5, 1.5, -1.0, 0.0, 0.0, 1.0, 0.0,
    -1.5, 1.5, 1.5, -1.0, 0.0, 0.0, 0.0, 0.0,
    -1.5, 1.5, -1.5, -1.0, 0.0, 0.0, 0.0, 1.0,
    -1.5, -1.5, -1.5, -1.0, 0.0, 0.0, 1.0, 1.0,
    1.5, 1.5, -1.5, 0.0, 1.0, 0.0, 1.0, 0.0,
    -1.5, 1.5, -1.5, 0.0, 1.0, 0.0, 0.0, 0.0,
    -1.5, 1.5, 1.5, 0.0, 1.0, 0.0, 0.0, 1.0,
    1.5, 1.5, 1.5, 0.0, 1.0, 0.0, 1.0, 1.0,
    1.5, -1.5, 1.5, 0.0, -1.0, 0.0, 0.0, 0.0,
    -1.5, -1.5, 1.5, 0.0, -1.0, 0.0, 1.0, 0.0,
    -1.5, -1.5, -1.5, 0.0, -1.0, 0.0, 1.0, 1.0,
    1.5, -1.5, -1.5, 0.0, -1.0, 0.0, 0.0, 1.0,
};

static const uint indices[36] = {
    0, 1, 2, 2, 3, 0,
    4, 5, 6, 6, 7, 4,
    8, 9, 10, 10, 11, 8,
    12, 13, 14, 14, 15, 12,
    16, 17, 18, 18, 19, 16,
    20, 21, 22, 22, 23, 20,
};

[shader("compute")]
[numthreads(1, 1, 1)]
void main(uint3 global_id : SV_DispatchThreadID)
{
    for (uint i = 0; i < 192; i++)
        vertex_data[i] = vertices[i];
    for (uint i = 0; i < 36; i++)
        index_data[i] = indices[i];
}
