// The glow lights whose reach meets each block of the field's cells, a cube of cells of one of the
// field's cascades widened by a cell on every side, listed by their index in rising order, so a
// held surface asks only the lights that can reach its block (gi_hold.slang, glowLightsIn). A light
// left out reaches no point of the block, and gives a surface there none of its light, so the light
// a surface takes from the listed lights is the light it takes from every light. A thread a block,
// the blocks of the field's cascades one after another.
import bcs_field;
import bcs_gi;
import bcs_glow;

StructuredBuffer<FieldInfo> info;
StructuredBuffer<GiLights> lights;
StructuredBuffer<GlowLight> glow_lights;

// Each block's count of lights and then their indices, MostGlowLights places a block.
RWStructuredBuffer<uint> glow_blocks;

// x: the cells along each side of a block.
uniform uint4 glow_block;

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint side = uint(info[0].counts.y) / glow_block.x;
    uint each = side * side * side;
    if (id.x >= each * uint(info[0].counts.x))
        return;
    uint c = id.x / each, b = id.x % each;
    uint3 block = uint3(b % side, (b / side) % side, b / (side * side));
    float4 oc = info[0].cascades[c].origin_and_cell;
    float3 low = oc.xyz + (float3(block * glow_block.x) - 1.0) * oc.w;
    float3 high = oc.xyz + (float3((block + 1u) * glow_block.x) + 1.0) * oc.w;
    uint first = id.x * (1u + uint(MostGlowLights));
    uint listed = 0u;
    uint count = min(uint(lights[0].counts.w), uint(MostGlowLights));
    for (uint i = 0u; i < count; i++)
    {
        float4 m = glow_lights[i].middleAndReach;
        float3 off = clamp(m.xyz, low, high) - m.xyz;
        if (dot(off, off) < m.w * m.w)
        {
            glow_blocks[first + 1u + listed] = i;
            listed++;
        }
    }
    glow_blocks[first] = listed;
}
