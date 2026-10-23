// The scene's distance field as a shader that reads it sees it, which any shader can reach with
// `import bcs_field;`.
//
// Cascades of cells around the camera, each twice as coarse and as wide as the one before, hold how
// far each cell's middle is from the nearest surface of the meshes that cast shadows, below zero
// inside a mesh with an inside, exact within a few cells of a surface and held at that many beyond.
// They lie one after another along z in one 3D image of half floats, so a shader binds the field
// once, with a buffer holding one `Info` that says where each cascade lies, and a cascade not yet
// built has a cell width of zero and is passed over (see `Render.SetSceneField`).
//
// Taken from 3DEngine's `scenefield.slang`, its names written as this bridge's modules write theirs.

module bcs_field;

public namespace bcs_field
{

/// A cascade, where it lies.
public struct Cascade
{
    /// xyz: the world position of the cascade's corner, its first cell's least corner. w: the
    /// width of a cell, or zero for a cascade not yet built.
    public float4 origin_and_cell;
};

/// Where the field's cascades lie this frame.
public struct Info
{
    /// x: how many cascades the image holds, zero where the field is off. y: the cells along each
    /// side of a cascade. z: how many cells from a surface the distances are exact. w: unused.
    public float4 counts;
    public Cascade cascades[8];
    /// xyz: the eye the cascades lie around this frame, which their outer bands keep clear of
    /// (`band_hold`). w: unused.
    public float4 eye;
};

/// Whether the field holds anything to read.
public bool on(Info info)
{
    return info.counts.x > 0.5;
}

/// The cells of the field's last cascade's outer band, across which a shader that reads the
/// field's distances fades to what it gives where the field holds nothing (`hold`).
public static const float BAND = 8.0;

/// The cells an outer band ends short of the eye on a side the eye stands near (`band_hold`).
public static const float EYE_CLEAR = 6.0;

/// How far into cascade `c`'s outer band a point `cell` lies, in its cells, as a share of the band,
/// one at the band's inner edge and beyond and zero at the side, `margin` cells off each side. A
/// cascade lies 0.35 of its width ahead of the eye, so the eye stands 1.6 to 9.6 cells from the side
/// behind it, inside a band of eight; on a side the eye is that near the band ends `EYE_CLEAR` cells
/// short of it, or is none and the value steps at the side, behind the eye where no surface it
/// sees lies.
public float band_hold(Info info, int c, float3 cell, float band, float margin)
{
    float4 oc = info.cascades[c].origin_and_cell;
    float3 eye = (info.eye.xyz - oc.xyz) / oc.w;
    float3 low = clamp(eye - margin - EYE_CLEAR, 0.0, band);
    float3 high = clamp(info.counts.y - eye - margin - EYE_CLEAR, 0.0, band);
    float3 held = saturate(min((cell - margin) / max(low, 1e-3), (info.counts.y - cell - margin) / max(high, 1e-3)));
    return min(min(held.x, held.y), held.z);
}

/// The coarsest built cascade no coarser than `up_to`, or -1 where none is built, as in an app's
/// first frames, where the field builds its cascades finest first as many a frame as its budget
/// allows.
public int coarsest_built(Info info, int up_to)
{
    [loop]
    for (int i = min(up_to, int(info.counts.x) - 1); i >= 0; i--)
        if (info.cascades[i].origin_and_cell.w > 0.0)
            return i;
    return -1;
}

/// How much of what the field holds at `p` a shader takes, all of it inside the field's cascades,
/// falling to none across the outer band (`BAND`) of the coarsest one built, kept clear of the eye
/// (`band_hold`), and none past it, so a shader that reads the field to its edge fades there rather
/// than stepping.
public float hold(Info info, float3 p)
{
    int c = coarsest_built(info, int(info.counts.x) - 1);
    if (c < 0)
        return 0.0;
    float4 oc = info.cascades[c].origin_and_cell;
    return band_hold(info, c, (p - oc.xyz) / oc.w, BAND, 1.0);
}

/// The finest built cascade `p` lies in, at least `margin` cells inside its sides, or -1 for none.
public int cascade_of(Info info, float3 p, float margin)
{
    int count = int(info.counts.x);
    float size = info.counts.y;
    for (int i = 0; i < count; i++)
    {
        float4 oc = info.cascades[i].origin_and_cell;
        if (oc.w <= 0.0)
            continue;
        float3 cell = (p - oc.xyz) / oc.w;
        if (all(cell >= margin) && all(cell <= size - margin))
            return i;
    }
    return -1;
}

/// The distance from `p` to the nearest surface in cascade `i`, in world units, blended between
/// the eight cells around it by a linear sampler, each read no nearer a side than half a cell, so
/// cascades stacked in the image do not bleed into each other.
public float distance_in(Texture3D<float> field, SamplerState linear, Info info, int i, float3 p)
{
    float4 oc = info.cascades[i].origin_and_cell;
    float size = info.counts.y;
    float3 cell = clamp((p - oc.xyz) / oc.w, 0.5, size - 0.5);
    float3 uvw = float3(cell.xy / size, (cell.z + float(i) * size) / (size * info.counts.x));
    return field.SampleLevel(linear, uvw, 0.0);
}

/// The distance from `p` to the nearest surface, from the finest cascade around it, or far where
/// none holds it.
public float distance(Texture3D<float> field, SamplerState linear, Info info, float3 p)
{
    int i = cascade_of(info, p, 1.0);
    return i < 0 ? 1e6 : distance_in(field, linear, info, i, p);
}

/// The way out from the nearest surface at `p`, the field's slope across half a cell, or up where
/// the field is flat or holds nothing there.
public float3 normal(Texture3D<float> field, SamplerState linear, Info info, float3 p)
{
    int i = cascade_of(info, p, 1.5);
    if (i < 0)
        return float3(0, 1, 0);
    float e = info.cascades[i].origin_and_cell.w * 0.5;
    float3 g = float3(
        distance_in(field, linear, info, i, p + float3(e, 0, 0)) - distance_in(field, linear, info, i, p - float3(e, 0, 0)),
        distance_in(field, linear, info, i, p + float3(0, e, 0)) - distance_in(field, linear, info, i, p - float3(0, e, 0)),
        distance_in(field, linear, info, i, p + float3(0, 0, e)) - distance_in(field, linear, info, i, p - float3(0, 0, e)));
    float length2 = dot(g, g);
    return length2 > 1e-12 ? g * rsqrt(length2) : float3(0, 1, 0);
}

/// The width of a cell of the finest built cascade around `p`, or zero where none holds it.
public float cell(Info info, float3 p)
{
    int i = cascade_of(info, p, 1.0);
    return i < 0 ? 0.0 : info.cascades[i].origin_and_cell.w;
}

/// The way a face looks in eight bits, four along each side of the octahedron the sphere is folded
/// onto, as the field's build keeps it in each cell's word, enough to tell faces that look apart
/// from faces that meet square.
public uint face_bits(float3 n)
{
    n /= abs(n.x) + abs(n.y) + abs(n.z);
    float2 o = n.z >= 0.0 ? n.xy : (1.0 - abs(n.yx)) * float2(n.x >= 0.0 ? 1.0 : -1.0, n.y >= 0.0 ? 1.0 : -1.0);
    uint2 q = min(uint2((o * 0.5 + 0.5) * 16.0), uint2(15u));
    return q.x | (q.y << 4);
}

/// The way back from the eight bits `face_bits` keeps.
public float3 face_way(uint bits)
{
    float2 o = (float2(float(bits & 15u), float((bits >> 4) & 15u)) + 0.5) / 16.0 * 2.0 - 1.0;
    float3 n = float3(o, 1.0 - abs(o.x) - abs(o.y));
    if (n.z < 0.0)
        n.xy = (1.0 - abs(n.yx)) * float2(n.x >= 0.0 ? 1.0 : -1.0, n.y >= 0.0 ? 1.0 : -1.0);
    return normalize(n);
}

}
