using System.Numerics;
using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>
/// The small emissive surfaces, which global illumination carries as lights of their own rather
/// than by the rays that meet them, as <c>bcs_glow</c>'s <c>GlowLight</c> reads them, 3DEngine's
/// GlowLights.
/// </summary>
/// <remarks>
/// <para>
/// A probe's rays meet a small emitter seldom and take it for a whole texel of the sky where they
/// do, and the world's probes stand above a floor at their spacing, so the faces a floor reads see
/// none of a lamp lying on it. Carried as a light, its light reaches a surface in closed form from
/// the faces of its box, shadowed by a march through the field, at each pixel the pass draws and at
/// each probe's ray's hit, which takes the two brightest. The field marks the cells such an emitter
/// paints, so a probe's ray that meets it does not take its light a second time.
/// </para>
/// <para>
/// An emitter is carried where its box's longest side in the world is no more than twice the first
/// cascade's probe spacing (<see cref="SceneField.Carried"/>). Emitters square to the axes that touch
/// and emit the same light are one light, so a stack of emissive blocks is not four lights each
/// hidden by the others. The <see cref="Most"/> carried emitters nearest the eye are lights each
/// frame. Its light reaches as far as it lights a surface by a fiftieth.
/// </para>
/// <para>
/// 3DEngine takes a mesh's surface over its box's from its vertices and cuts a mesh of many
/// emitters into its parts, which the field's gather here does not hand over, so each emitter is
/// its box whole, every face emitting, as a block is. A ball emits from about half its box's
/// surface, so a round emitter is carried as about twice as bright as it is.
/// </para>
/// </remarks>
internal static class GlowLights
{
    /// <summary>How many carried emitters are lights at most, the nearest the eye, as <c>MostGlowLights</c> in <c>bcs_glow</c>.</summary>
    public const int Most = 64;

    /// <summary>The bytes of a light, its middle and reach, its three half sides and its light.</summary>
    public const int Bytes = 80;

    // The light a surface at the reach takes from one, a fiftieth of its own, past which the light is
    // let go, over a quarter of the box's surface as the box's mean face seen from afar.
    private const float Faint = 0.02f;

    /// <summary>A light as it is written, the middle of its box, half its side along each of the box's axes, its light and its surface over its box's.</summary>
    internal readonly record struct Light(Vector3 Middle, Vector3 X, Vector3 Y, Vector3 Z, Vector3 Glow, float Share);

    /// <summary>
    /// Writes the lights the carried emitters among <paramref name="drawn"/> make, those nearest
    /// <paramref name="eye"/>, into <paramref name="bytes"/>, and gives how many were written.
    /// </summary>
    /// <param name="bytes"><see cref="Most"/> lights of <see cref="Bytes"/> each.</param>
    /// <param name="drawn">The meshes the field gathered this frame.</param>
    /// <param name="eye">The camera's eye, the nearest to which are written.</param>
    /// <param name="firstCell">The width of a cell of the field's first cascade, which says which emitters are carried.</param>
    public static int Write(Span<byte> bytes, IReadOnlyList<(SceneFieldPlan.Instance Instance, bool Skinned)> drawn, Vector3 eye, float firstCell)
    {
        var lights = Gather(drawn, firstCell);
        lights.Sort((a, b) => Vector3.DistanceSquared(a.Middle, eye).CompareTo(Vector3.DistanceSquared(b.Middle, eye)));
        var floats = MemoryMarshal.Cast<byte, float>(bytes);
        var count = Math.Min(lights.Count, Most);
        for (var n = 0; n < count; n++)
        {
            var (middle, x, y, z, glow, share) = lights[n];
            var surface = 4 * FacesSurface(new Vector3(x.Length(), y.Length(), z.Length())) * share;
            var brightest = MathF.Max(glow.X, MathF.Max(glow.Y, glow.Z));
            var reach = MathF.Sqrt(brightest * surface / (4 * Faint));
            var light = floats.Slice(n * Bytes / 4, Bytes / 4);
            (light[0], light[1], light[2], light[3]) = (middle.X, middle.Y, middle.Z, reach);
            (light[4], light[5], light[6], light[7]) = (x.X, x.Y, x.Z, x.Length());
            (light[8], light[9], light[10], light[11]) = (y.X, y.Y, y.Z, y.Length());
            (light[12], light[13], light[14], light[15]) = (z.X, z.Y, z.Z, z.Length());
            // The share below 2 and twice the faces above it, every face here, as glowShare and
            // glowFaces in bcs_glow read them.
            (light[16], light[17], light[18], light[19]) = (glow.X, glow.Y, glow.Z, share);
        }
        return count;
    }

    /// <summary>
    /// The lights the carried emitters among <paramref name="drawn"/> make, one for each, or, for
    /// each group of boxes square to the world's axes that touch and emit the same light, one for the
    /// group, its box around them all and its share of that box's surface what of theirs no other
    /// covers.
    /// </summary>
    /// <remarks>
    /// Four emissive blocks stacked two by two were four lights in 3DEngine, each marched to through
    /// the others and each emitting from the faces the others cover, so the stack threw a ring of
    /// light that came and went around it. As one light of a box two blocks wide its light is the
    /// stack's.
    /// </remarks>
    internal static List<Light> Gather(IReadOnlyList<(SceneFieldPlan.Instance Instance, bool Skinned)> drawn, float firstCell)
    {
        var lights = new List<Light>();
        var square = new List<(SceneFieldPlan.Box Box, Vector3 Glow, float Surface)>();
        foreach (var (instance, _) in drawn)
        {
            if (!SceneField.Carried(instance, firstCell)) continue;
            var (w, glow) = (instance.World, instance.Emission);
            var box = instance.Bounds;
            // A flat emitter is a box a hundredth of its size thick about its face, which lights each
            // side by the face's whole surface.
            var size = box.Max - box.Min;
            var largest = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
            var least = 0.01f * largest;
            if (largest > 0 && MathF.Min(size.X, MathF.Min(size.Y, size.Z)) < least)
            {
                var grow = Vector3.Max(Vector3.Zero, new Vector3(least) - size) / 2;
                box = new SceneFieldPlan.Box(box.Min - grow, box.Max + grow);
            }
            if (Square(w))
            {
                var world = box.Transformed(w);
                square.Add((world, glow, FacesSurface(world.Max - world.Min)));
                continue;
            }
            var half = (box.Max - box.Min) / 2;
            lights.Add(new Light(Vector3.Transform((box.Min + box.Max) / 2, w), new Vector3(w.M11, w.M12, w.M13) * half.X,
                new Vector3(w.M21, w.M22, w.M23) * half.Y, new Vector3(w.M31, w.M32, w.M33) * half.Z, glow, 1));
        }

        // The boxes that touch, found through a grid of the largest box's size, and joined.
        var parent = Enumerable.Range(0, square.Count).ToArray();
        int Root(int i)
        {
            while (parent[i] != i) i = parent[i] = parent[parent[i]];
            return i;
        }
        var touching = new List<(int A, int B)>();
        if (square.Count > 1)
        {
            var cell = MathF.Max(square.Max(s => MathF.Max(s.Box.Max.X - s.Box.Min.X, MathF.Max(s.Box.Max.Y - s.Box.Min.Y, s.Box.Max.Z - s.Box.Min.Z))), 1e-4f);
            var grid = new Dictionary<(int, int, int), List<int>>();
            (int, int, int) Key(Vector3 p) => ((int)MathF.Floor(p.X / cell), (int)MathF.Floor(p.Y / cell), (int)MathF.Floor(p.Z / cell));
            for (var i = 0; i < square.Count; i++)
            {
                var key = Key(square[i].Box.Min);
                if (!grid.TryGetValue(key, out var list)) grid[key] = list = [];
                list.Add(i);
            }
            for (var i = 0; i < square.Count; i++)
            {
                var (kx, ky, kz) = Key(square[i].Box.Min);
                for (var dx = -1; dx <= 1; dx++)
                for (var dy = -1; dy <= 1; dy++)
                for (var dz = -1; dz <= 1; dz++)
                    if (grid.TryGetValue((kx + dx, ky + dy, kz + dz), out var near))
                        foreach (var j in near)
                            if (j > i && Touch(square[i].Box, square[j].Box) && Vector3.DistanceSquared(square[i].Glow, square[j].Glow) < 1e-8f)
                            {
                                touching.Add((i, j));
                                parent[Root(i)] = Root(j);
                            }
            }
        }
        var covered = new Dictionary<int, float>();
        foreach (var (a, b) in touching)
            covered[Root(a)] = covered.GetValueOrDefault(Root(a)) + 2 * Contact(square[a].Box, square[b].Box);
        foreach (var group in Enumerable.Range(0, square.Count).GroupBy(Root))
        {
            var (min, max) = (new Vector3(float.MaxValue), new Vector3(float.MinValue));
            float surface = 0;
            foreach (var i in group)
            {
                (min, max) = (Vector3.Min(min, square[i].Box.Min), Vector3.Max(max, square[i].Box.Max));
                surface += square[i].Surface;
            }
            var share = Math.Clamp((surface - covered.GetValueOrDefault(group.Key)) / Math.Max(FacesSurface(max - min), 1e-8f), 0.05f, 1);
            var half = (max - min) / 2;
            lights.Add(new Light((min + max) / 2, new Vector3(half.X, 0, 0), new Vector3(0, half.Y, 0), new Vector3(0, 0, half.Z), square[group.Key].Glow, share));
        }
        return lights;
    }

    // The surface of the six faces of a box of this size.
    private static float FacesSurface(Vector3 size) => 2 * (size.X * size.Y + size.Y * size.Z + size.Z * size.X);

    // Whether a world matrix turns nothing, so a box it moves stays square to the axes.
    private static bool Square(Matrix4x4 w)
    {
        var scale = MathF.Max(MathF.Abs(w.M11), MathF.Max(MathF.Abs(w.M22), MathF.Abs(w.M33)));
        var turned = MathF.Abs(w.M12) + MathF.Abs(w.M13) + MathF.Abs(w.M21) + MathF.Abs(w.M23) + MathF.Abs(w.M31) + MathF.Abs(w.M32);
        return turned <= 1e-5f * MathF.Max(scale, 1e-6f);
    }

    // Whether two boxes touch or overlap, within a thousandth of the smaller one's size.
    private static bool Touch(SceneFieldPlan.Box a, SceneFieldPlan.Box b)
    {
        var slack = 1e-3f * MathF.Min((a.Max - a.Min).Length(), (b.Max - b.Min).Length());
        return a.Min.X <= b.Max.X + slack && b.Min.X <= a.Max.X + slack && a.Min.Y <= b.Max.Y + slack && b.Min.Y <= a.Max.Y + slack
            && a.Min.Z <= b.Max.Z + slack && b.Min.Z <= a.Max.Z + slack;
    }

    // The area of the faces two touching boxes press together, where one's side lies on the other's
    // along an axis, or none where they overlap or meet at an edge.
    private static float Contact(SceneFieldPlan.Box a, SceneFieldPlan.Box b)
    {
        var slack = 1e-3f * MathF.Min((a.Max - a.Min).Length(), (b.Max - b.Min).Length());
        var overlap = Vector3.Max(Vector3.Zero, Vector3.Min(a.Max, b.Max) - Vector3.Max(a.Min, b.Min));
        for (var axis = 0; axis < 3; axis++)
        {
            var meet = MathF.Abs(a.Max[axis] - b.Min[axis]) <= slack || MathF.Abs(b.Max[axis] - a.Min[axis]) <= slack;
            if (meet && overlap[axis] <= slack) return overlap[(axis + 1) % 3] * overlap[(axis + 2) % 3];
        }
        return 0;
    }
}
