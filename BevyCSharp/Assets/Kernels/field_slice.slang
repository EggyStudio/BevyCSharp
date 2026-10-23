// Paints a slice of one cascade of the scene's distance field into an image the field's window
// shows, four pixels a cell along each side. Each pixel reads the field between the cells around
// it, as a shader sampling the field does, so a surface lying on a boundary between cells, as an
// upright box's face does, draws where it is. A point inside a mesh is red, the deeper the darker,
// one outside is gray, lighter the farther it is, in bands a cell apart, and one within a sixth of
// a cell of a surface is black, so the meshes' outlines read as lines. A cascade not built yet is a
// dark blue.
import bcs_field;

Texture3D<float> field;
SamplerState field_sampler;

StructuredBuffer<FieldInfo> info;

[format("rgba8")] RWTexture2D<float4> slice;

// x: the cascade. y: the axis the slice is across, 0 for x, 1 for y, 2 for z. z: the slice's cell
// along that axis. w: the pixels a cell takes along each side of the image.
uniform uint4 choice;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    FieldInfo field_info = info[0];
    uint size = uint(field_info.counts.y);
    uint2 pixels;
    slice.GetDimensions(pixels.x, pixels.y);
    if (any(id.xy >= pixels))
        return;

    float4 oc = field_info.cascades[min(choice.x, 7u)].origin_and_cell;
    if (choice.x >= uint(field_info.counts.x) || oc.w <= 0.0)
    {
        slice[id.xy] = float4(0.05, 0.07, 0.14, 1.0);
        return;
    }

    // Across y the slice is seen from above, x to the right and z down, so north, minus z, is up.
    // Across x or z it is seen from the side, up being up. The slice lies through its cell's middle.
    float2 cell = (float2(id.xy) + 0.5) / float(max(choice.w, 1u));
    float along = float(min(choice.z, size - 1u)) + 0.5;
    float up = float(size) - cell.y;
    float3 at = choice.y == 1u ? float3(cell.x, along, cell.y)
        : choice.y == 0u ? float3(along, up, cell.x)
        : float3(cell.x, up, along);
    float d = fieldDistanceIn(field, field_sampler, field_info, int(choice.x), oc.xyz + at * oc.w) / oc.w;

    float3 color;
    if (abs(d) < 0.17)
        color = float3(0.0, 0.0, 0.0);
    else if (d < 0.0)
        color = float3(0.9 - 0.15 * min(-d, 4.0), 0.12, 0.1);
    else
    {
        float band = frac(d) < 0.12 ? 0.82 : 1.0;
        color = float3(0.35 + 0.15 * min(d, 4.0)) * band;
    }
    slice[id.xy] = float4(color, 1.0);
}
