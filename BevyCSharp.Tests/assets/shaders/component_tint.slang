// A 2D material whose color is the entry of a component array at the mesh's tag.
import bcs2d;

struct Tint
{
    float4 color;
};

StructuredBuffer<Tint> tints;

[shader("fragment")]
float4 fragment(bcs2d::VertexOutput mesh) : SV_Target
{
    return tints[bcs2d::tag(mesh.instance_index)].color;
}
