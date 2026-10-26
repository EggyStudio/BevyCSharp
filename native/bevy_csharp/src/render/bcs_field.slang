// The scene's distance field as a shader that reads it sees it, which any shader can reach with
// `import bcs_field;`.
//
// Cascades of cells around the camera, each twice as coarse and as wide as the one before, and after
// them the far cascade where the program asks for one, its cells as wide as its reach takes, hold how
// far each cell's middle is from the nearest surface of the meshes that cast shadows, below zero
// inside a mesh with an inside, exact within a few cells of a surface and held at that many beyond.
// They lie one after another along z in one 3D image of half floats, so a shader binds the field
// once, with a buffer holding one `FieldInfo` that says where each cascade lies, and a cascade not
// yet built has a cell width of zero and is passed over (see `Render.SetSceneField`).
//
// Taken from 3DEngine's `scenefield.slang`, its names written as this bridge's modules write
// theirs.

module bcs_field;


/// A cascade, where it lies.
public struct FieldCascade
{
    /// xyz: the world position of the cascade's corner, its first cell's least corner. w: the
    /// width of a cell, or zero for a cascade not yet built.
    public float4 origin_and_cell;
};

/// Where the field's cascades lie this frame.
public struct FieldInfo
{
    /// x: how many cascades the image holds, zero where the field is off. y: the cells along each
    /// side of a cascade. z: how many cells from a surface the distances are exact. w: the far
    /// cascade's place, the last, or zero where there is none.
    public float4 counts;
    public FieldCascade cascades[9];
    /// xyz: the eye the cascades lie around this frame, which their outer bands keep clear of
    /// (`bandHold`). w: unused.
    public float4 eye;
};

/// Whether the field holds anything to read.
public bool fieldOn(FieldInfo info)
{
    return info.counts.x > 0.5;
}

/// The field's cascades the world's `probes` cascades of light probes stand in, the field's own in
/// order but for the last, which stands in the far cascade where the field has one, so a cascade of
/// probes lies where its cascade does and the probes holding a point are found as the field's
/// cascades holding it are. The probes are read through it and the field's distances through the
/// field's own info, since past the cascades of probes it holds neither the field's count nor its
/// places in the image.
public FieldInfo probesOf(FieldInfo info, int probes)
{
    FieldInfo probed = info;
    int far = int(info.counts.w);
    if (far > 0 && probes > 0)
        probed.cascades[probes - 1] = info.cascades[far];
    probed.counts.x = float(min(probes, int(info.counts.x)));
    probed.counts.w = 0.0;
    return probed;
}

/// The cells of the field's last cascade's outer band, across which a shader that reads the
/// field's distances fades to what it gives where the field holds nothing (`fieldHold`).
public static const float FieldBand = 8.0;

/// The cells an outer band ends short of the eye on a side the eye stands near (`bandHold`).
public static const float EyeClear = 6.0;

/// How far into cascade `c`'s outer band a point `cell` lies, in its cells, as a share of the band,
/// one at the band's inner edge and beyond and zero at the side, `margin` cells off each side. A
/// cascade lies 0.35 of its width ahead of the eye, so the eye stands 1.6 to 9.6 cells from the
/// side behind it, inside a band of eight; on a side the eye is that near the band ends `EyeClear`
/// cells short of it, or is none and the value steps at the side, behind the eye where no surface
/// it sees lies.
public float bandHold(FieldInfo info, int c, float3 cell, float band, float margin)
{
    float4 oc = info.cascades[c].origin_and_cell;
    float3 eye = (info.eye.xyz - oc.xyz) / oc.w;
    float3 low = clamp(eye - margin - EyeClear, 0.0, band);
    float3 high = clamp(info.counts.y - eye - margin - EyeClear, 0.0, band);
    float3 held = saturate(min((cell - margin) / max(low, 1e-3), (info.counts.y - cell - margin) / max(high, 1e-3)));
    return min(min(held.x, held.y), held.z);
}

/// The coarsest built cascade no coarser than `up_to`, or -1 where none is built, as in an app's
/// first frames, where the field builds its cascades finest first as many a frame as its budget
/// allows.
public int fieldCoarsestBuilt(FieldInfo info, int up_to)
{
    [loop]
    for (int i = min(up_to, int(info.counts.x) - 1); i >= 0; i--)
        if (info.cascades[i].origin_and_cell.w > 0.0)
            return i;
    return -1;
}

/// How much of what the field holds at `p` a shader takes, all of it inside the field's cascades,
/// falling to none across the outer band (`FieldBand`) of the coarsest one built, kept clear of the
/// eye (`bandHold`), and none past it, so a shader that reads the field to its edge fades there
/// rather than stepping.
public float fieldHold(FieldInfo info, float3 p)
{
    int c = fieldCoarsestBuilt(info, int(info.counts.x) - 1);
    if (c < 0)
        return 0.0;
    float4 oc = info.cascades[c].origin_and_cell;
    return bandHold(info, c, (p - oc.xyz) / oc.w, FieldBand, 1.0);
}

/// The finest built cascade `p` lies in, at least `margin` cells inside its sides, or -1 for none.
public int fieldCascade(FieldInfo info, float3 p, float margin)
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
public float fieldDistanceIn(Texture3D<float> field, SamplerState linear, FieldInfo info, int i, float3 p)
{
    float4 oc = info.cascades[i].origin_and_cell;
    float size = info.counts.y;
    float3 cell = clamp((p - oc.xyz) / oc.w, 0.5, size - 0.5);
    float3 uvw = float3(cell.xy / size, (cell.z + float(i) * size) / (size * info.counts.x));
    return field.SampleLevel(linear, uvw, 0.0);
}

/// The distance from `p` to the nearest surface, from the finest cascade around it, or far where
/// none holds it.
public float fieldDistance(Texture3D<float> field, SamplerState linear, FieldInfo info, float3 p)
{
    int i = fieldCascade(info, p, 1.0);
    return i < 0 ? 1e6 : fieldDistanceIn(field, linear, info, i, p);
}

/// The way out from the nearest surface at `p`, the field's slope across half a cell, or up where
/// the field is flat or holds nothing there.
public float3 fieldNormal(Texture3D<float> field, SamplerState linear, FieldInfo info, float3 p)
{
    int i = fieldCascade(info, p, 1.5);
    if (i < 0)
        return float3(0, 1, 0);
    float e = info.cascades[i].origin_and_cell.w * 0.5;
    float3 g = float3(
        fieldDistanceIn(field, linear, info, i, p + float3(e, 0, 0)) - fieldDistanceIn(field, linear, info, i, p - float3(e, 0, 0)),
        fieldDistanceIn(field, linear, info, i, p + float3(0, e, 0)) - fieldDistanceIn(field, linear, info, i, p - float3(0, e, 0)),
        fieldDistanceIn(field, linear, info, i, p + float3(0, 0, e)) - fieldDistanceIn(field, linear, info, i, p - float3(0, 0, e)));
    float length2 = dot(g, g);
    return length2 > 1e-12 ? g * rsqrt(length2) : float3(0, 1, 0);
}

/// The width of a cell of the finest built cascade around `p`, or zero where none holds it.
public float fieldCell(FieldInfo info, float3 p)
{
    int i = fieldCascade(info, p, 1.0);
    return i < 0 ? 0.0 : info.cascades[i].origin_and_cell.w;
}

/// The way a face looks in eight bits, four along each side of the octahedron the sphere is folded
/// onto, as the field's build keeps it in each cell's word, enough to tell faces that look apart
/// from faces that meet square.
public uint faceBits(float3 n)
{
    n /= abs(n.x) + abs(n.y) + abs(n.z);
    float2 o = n.z >= 0.0 ? n.xy : (1.0 - abs(n.yx)) * float2(n.x >= 0.0 ? 1.0 : -1.0, n.y >= 0.0 ? 1.0 : -1.0);
    uint2 q = min(uint2((o * 0.5 + 0.5) * 16.0), uint2(15u));
    return q.x | (q.y << 4);
}

/// The way back from the eight bits `faceBits` keeps.
public float3 faceWay(uint bits)
{
    float2 o = (float2(float(bits & 15u), float((bits >> 4) & 15u)) + 0.5) / 16.0 * 2.0 - 1.0;
    float3 n = float3(o, 1.0 - abs(o.x) - abs(o.y));
    if (n.z < 0.0)
        n.xy = (1.0 - abs(n.yx)) * float2(n.x >= 0.0 ? 1.0 : -1.0, n.y >= 0.0 ? 1.0 : -1.0);
    return normalize(n);
}

/// A color of the field at `p`, the cells' colors blended and divided by how much of them a surface
/// painted, so a cell no surface painted lends nothing, or gray where none around `p` is painted.
/// The colors lie as the distances do, `albedo` an image of eight bits a channel whose alpha is one
/// where a surface painted the cell.
public float3 surfaceAlbedo(Texture3D<float4> albedo, SamplerState linear, FieldInfo info, float3 p)
{
    int c = fieldCascade(info, p, 1.0);
    if (c < 0)
        return float3(0.5);
    float4 oc = info.cascades[c].origin_and_cell;
    float size = info.counts.y;
    float3 cell = clamp((p - oc.xyz) / oc.w, 0.5, size - 0.5);
    float3 uvw = float3(cell.xy / size, (cell.z + float(c) * size) / (size * info.counts.x));
    float4 painted = albedo.SampleLevel(linear, uvw, 0.0);
    return painted.a > 0.05 ? painted.rgb / painted.a : float3(0.5);
}

/// The light the field's surfaces emit at `p`, blended as their colors are, the glow's light over
/// the share of the cells around `p` a surface painted, or none where no surface painted any. The
/// glow holds a cell's nearest surface's emission, or the light an emitter thinner than a cell lends
/// it where that is more, as field_resolve.slang writes it.
public float3 surfaceGlow(Texture3D<float4> glow, Texture3D<float4> albedo, SamplerState linear, FieldInfo info, float3 p)
{
    int c = fieldCascade(info, p, 1.0);
    if (c < 0)
        return float3(0.0);
    float4 oc = info.cascades[c].origin_and_cell;
    float size = info.counts.y;
    float3 cell = clamp((p - oc.xyz) / oc.w, 0.5, size - 0.5);
    float3 uvw = float3(cell.xy / size, (cell.z + float(c) * size) / (size * info.counts.x));
    float coverage = albedo.SampleLevel(linear, uvw, 0.0).a;
    return coverage > 0.05 ? glow.SampleLevel(linear, uvw, 0.0).rgb / coverage : float3(0.0);
}

/// A color of the field at `p` on a surface facing `n`, the eight cells around `p` blended by hand,
/// each kept only where the face whose color it holds, as `faces` keeps its way (`faceWay`), looks
/// the hit's way, within 60 degrees of `n`, or `surfaceAlbedo`'s blend where none of them does, as
/// at a thin edge, so the hit is not left dark. A cell on a corner's diagonal lies as far from
/// either wall and holds whichever painted it, so in 3DEngine a red wall's hit within a cell of the
/// white wall it meets blended the white in and sent on twice the light a path-traced reference
/// reads.
public float3 surfaceAlbedoFacing(Texture3D<uint> faces, Texture3D<float4> albedo, SamplerState linear, FieldInfo info, float3 p, float3 n)
{
    int c = fieldCascade(info, p, 1.0);
    if (c < 0)
        return float3(0.5);
    float4 oc = info.cascades[c].origin_and_cell;
    int size = int(info.counts.y);
    float3 q = clamp((p - oc.xyz) / oc.w, 0.5, float(size) - 0.5) - 0.5;
    int3 base = int3(floor(q));
    float3 f = q - float3(base);
    float4 sum = float4(0.0);
    for (int corner = 0; corner < 8; corner++)
    {
        int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        int3 at = min(base + offset, int3(size - 1));
        int3 texel = int3(at.xy, at.z + c * size);
        float3 lerps = lerp(1.0 - f, f, float3(offset));
        float facing = dot(faceWay(faces.Load(int4(texel, 0))), n) >= 0.5 ? 1.0 : 0.0;
        sum += albedo.Load(int4(texel, 0)) * (lerps.x * lerps.y * lerps.z * facing);
    }
    return sum.a > 0.01 ? sum.rgb / sum.a : surfaceAlbedo(albedo, linear, info, p);
}

/// The alpha a cell of the field's colors is painted with by a surface that lets `through` of the
/// light pass a unit of its depth (`MaterialSettings.LightThrough`), its color painted times it, 1
/// for a solid surface down to a half for one that lets all of it through, so a reader that divides
/// a blend of the colors by their alphas takes the colors back and weighs such a cell less, and one
/// that reads a cell as it is takes the share back (`lightThroughOf`), as 3DEngine's field paints it.
public float lightThroughPaint(float through)
{
    return 1.0 - 0.5 * saturate(through);
}

/// The share of light a cell painted with `alpha` lets pass a unit of its surface's depth, 0 for a
/// solid surface's cell or one no surface painted.
public float lightThroughOf(float alpha)
{
    return alpha > 0.25 ? saturate(2.0 * (1.0 - alpha)) : 0.0;
}

/// The share of light the surface met at `p` lets pass a unit of its depth, read from the cell `p`
/// lies in as it is, 0 for a solid surface's cell or one no surface painted.
public float lightThroughAt(Texture3D<float4> albedo, FieldInfo info, float3 p)
{
    int c = fieldCascade(info, p, 1.0);
    if (c < 0)
        return 0.0;
    float4 oc = info.cascades[c].origin_and_cell;
    int size = int(info.counts.y);
    int3 cell = clamp(int3(floor((p - oc.xyz) / oc.w)), int3(0), int3(size - 1));
    return lightThroughOf(albedo.Load(int4(cell.xy, cell.z + c * size, 0)).a);
}

/// How far a ray along `way` from `origin` that met such a surface at `t` runs before it has left
/// it, half a cell a step while the field holds the point within half a cell of a surface or inside
/// one, and in `inside` whether it is still within at `t` plus `most`, its interval's end, or after
/// 64 steps, as a ray grazing a canopy is, where it ends that far in. A crown's leaves, cards the
/// field holds half a cell either side, run together into one region where they lie closer than a
/// cell, which a ray crosses whole.
public float lightThroughDepth(Texture3D<float> field, SamplerState linear, FieldInfo info, float3 origin, float3 way, float t, float most, out bool inside)
{
    float depth = 0.0;
    inside = true;
    [loop]
    for (int step = 0; step < 64; step++)
    {
        float3 p = origin + way * (t + depth);
        float cell = fieldCell(info, p);
        if (cell <= 0.0 || depth > 0.0 && fieldDistance(field, linear, info, p) > 0.5 * cell)
        {
            inside = false;
            return depth;
        }
        depth += 0.5 * cell;
        if (depth >= most)
            return most;
    }
    return depth;
}

/// The share of light a ray keeps crossing a surface that lets `passing` through a unit of its
/// depth, run through for `depth` from a hit with cells `cell` wide, less the three quarters of a
/// cell the hit lies before the surface and the march's last step leaves past it, where it left it.
public float lightThroughKept(float passing, float depth, float cell, bool inside)
{
    return pow(passing, inside ? depth : max(depth - 0.75 * cell, 0.0));
}
