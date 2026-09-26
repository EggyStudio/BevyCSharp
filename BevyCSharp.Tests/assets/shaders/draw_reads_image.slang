// A triangle over the whole picture painting red wherever the camera's `level` image holds more
// than a half, so a test can tell a draw read the image a dispatch wrote.

import bcs_pass;

Texture2D<float> level;

struct Corner
{
    float4 position : SV_Position;
};

[shader("vertex")]
Corner vertex(uint vertex_id : SV_VertexID)
{
    let corner = float2(float((vertex_id << 1) & 2), float(vertex_id & 2));

    Corner output;
    output.position = float4(corner * 2.0 - 1.0, 1.0, 1.0);
    return output;
}

[shader("fragment")]
float4 fragment(Corner corner) : SV_Target
{
    let value = level.Load(int3(int2(corner.position.xy), 0));
    return value > 0.5 ? float4(1.0, 0.0, 0.0, 1.0) : float4(0.0, 0.0, 1.0, 1.0);
}
