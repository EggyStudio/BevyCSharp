using System.Numerics;
using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>
/// The small emissive surfaces, which global illumination carries as lights of their own rather than
/// by the rays that meet them, as <c>bcs_glow</c>'s <c>GlowLight</c> reads them.
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
/// frame. Its light reaches as far as it lights a surface by a thousandth of its own light
/// (<see cref="Faint"/>).
/// </para>
/// <para>
/// Each emitter is its box, the faces its mesh's triangles facing out of it lean toward emitting,
/// by the share of those faces the triangles make, as the field's gather hands both over. Taken as
/// its box whole, a ball, whose surface is about half its box's, would be carried twice as bright
/// as it is, and a lantern's upright panels would send from the box's lower face the light they
/// send aside, onto the ground under it. The gather does not cut a mesh of many emitters into its
/// parts, so such a mesh is one light of its box.
/// </para>
/// </remarks>
internal static class GlowLights
{
    /// <summary>How many carried emitters are lights at most, the nearest the eye, as <c>MostGlowLights</c> in <c>bcs_glow</c>.</summary>
    public const int Most = 64;

    /// <summary>The bytes of a light, its middle and reach, its three half sides and its light.</summary>
    public const int Bytes = 80;

    // The light a surface at the reach takes from one, a thousandth of the light it emits, past
    // which the light is let go, over a quarter of the box's surface as the box's mean face seen
    // from afar. A share of its own, so every light keeps the same share of its light whatever its
    // brightness, where let go at a share of the picture's unit a dim light would reach less far
    // than a bright one of its size, and several emitters splitting one's light would each reach
    // less far than the one. A thousandth keeps the reach of one bright emitter as far as a camera
    // exposed for the room it lights sees it.
    private const float Faint = 0.001f;

    /// <summary>
    /// A light as it is written, the middle of its box, half its side along each of the box's axes,
    /// its light, its surface over that of its box's faces that emit, and which of them emit, a bit
    /// a face, two an axis of <see cref="X"/>, <see cref="Y"/> and <see cref="Z"/>, the side the
    /// axis points to the higher, none for every face.
    /// </summary>
    internal readonly record struct Light(Vector3 Middle, Vector3 X, Vector3 Y, Vector3 Z, Vector3 Glow, float Share, int Faces = 0);

    // Every face of a box, a bit a face, as the faces are written.
    private const int All = 63;

    /// <summary>
    /// Writes the lights the carried emitters among <paramref name="drawn"/> make, those nearest
    /// <paramref name="eye"/>, into <paramref name="bytes"/>, and gives how many places they take,
    /// up to the last taken.
    /// </summary>
    /// <param name="bytes"><see cref="Most"/> lights of <see cref="Bytes"/> each.</param>
    /// <param name="drawn">The meshes the field gathered this frame.</param>
    /// <param name="eye">The camera's eye, the nearest to which are written.</param>
    /// <param name="firstCell">The width of a cell of the field's first cascade, which says which emitters are carried.</param>
    /// <param name="places">
    /// The places the lights kept the frame before, which a light still among them is written in
    /// again, a place none takes left a light of no reach, or null to write them in order of their
    /// distance from the eye.
    /// </param>
    public static int Write(Span<byte> bytes, IReadOnlyList<(SceneFieldPlan.Instance Instance, bool Skinned)> drawn, Vector3 eye, float firstCell,
        GlowPlaces? places = null)
    {
        var lights = Gather(drawn, firstCell);
        lights.Sort((a, b) => Vector3.DistanceSquared(a.Middle, eye).CompareTo(Vector3.DistanceSquared(b.Middle, eye)));
        var floats = MemoryMarshal.Cast<byte, float>(bytes);
        var count = Math.Min(lights.Count, Most);
        Span<int> placeOf = stackalloc int[Most];
        var used = places?.Place(lights, count, placeOf) ?? count;
        floats[..(used * Bytes / 4)].Clear();
        for (var n = 0; n < count; n++)
        {
            if (places is null) placeOf[n] = n;
            var (middle, x, y, z, glow, share, faces) = lights[n];
            var surface = 4 * FacesSurface(new Vector3(x.Length(), y.Length(), z.Length()), faces == 0 ? All : faces) * share;
            var reach = MathF.Sqrt(surface / (4 * Faint));
            var light = floats.Slice(placeOf[n] * Bytes / 4, Bytes / 4);
            (light[0], light[1], light[2], light[3]) = (middle.X, middle.Y, middle.Z, reach);
            (light[4], light[5], light[6], light[7]) = (x.X, x.Y, x.Z, x.Length());
            (light[8], light[9], light[10], light[11]) = (y.X, y.Y, y.Z, y.Length());
            (light[12], light[13], light[14], light[15]) = (z.X, z.Y, z.Z, z.Length());
            // The share below 2 and twice the faces above it, as glowShare and glowFaces in
            // bcs_glow read them.
            (light[16], light[17], light[18], light[19]) = (glow.X, glow.Y, glow.Z, share + 2 * faces);
        }
        return used;
    }

    /// <summary>
    /// The lights the carried emitters among <paramref name="drawn"/> make, one for each, or, for
    /// each group of boxes square to the world's axes that touch and emit the same light, one for the
    /// group, its box around them all and its share of that box's surface what of theirs no other
    /// covers.
    /// </summary>
    /// <remarks>
    /// Four emissive blocks stacked two by two, taken as four lights, would each be marched to
    /// through the others and each emit from the faces the others cover, so a ring of light around
    /// the stack would come and go from frame to frame. As one light of a box two blocks wide its
    /// light is the stack's.
    /// </remarks>
    internal static List<Light> Gather(IReadOnlyList<(SceneFieldPlan.Instance Instance, bool Skinned)> drawn, float firstCell)
    {
        var lights = new List<Light>();
        var square = new List<(SceneFieldPlan.Box Box, Vector3 Glow, float Surface, int Faces)>();
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
            var flat = largest > 0 && MathF.Min(size.X, MathF.Min(size.Y, size.Z)) < least;
            if (flat)
            {
                var grow = Vector3.Max(Vector3.Zero, new Vector3(least) - size) / 2;
                box = new SceneFieldPlan.Box(box.Min - grow, box.Max + grow);
            }
            // The faces of its box the mesh sends its light from and the share of them its own
            // surface makes, where the bridge worked them out, so a ball, whose surface is about
            // half its box's, emits as a ball does, and a lantern's upright glass from its sides.
            // A flat one emits from every face of the box it is grown to, by its whole surface.
            var faces = flat || instance.Faces is 0 or All ? All : instance.Faces;
            var own = !flat && instance.Surface > 0 ? Math.Clamp(instance.Surface / Math.Max(FacesSurface(size, faces), 1e-8f), 0.05f, 1f) : 1f;
            if (Square(w))
            {
                var world = box.Transformed(w);
                // A side of the box a negative scale turns the other way is the other face.
                for (var k = 0; k < 3; k++)
                    if ((k == 0 ? w.M11 : k == 1 ? w.M22 : w.M33) < 0)
                        faces = (faces & ~(3 << (2 * k))) | (((faces >> (2 * k)) & 1) << (2 * k + 1)) | (((faces >> (2 * k + 1)) & 1) << (2 * k));
                square.Add((world, glow, FacesSurface(world.Max - world.Min, faces) * own, faces));
                continue;
            }
            var half = (box.Max - box.Min) / 2;
            lights.Add(new Light(Vector3.Transform((box.Min + box.Max) / 2, w), new Vector3(w.M11, w.M12, w.M13) * half.X,
                new Vector3(w.M21, w.M22, w.M23) * half.Y, new Vector3(w.M31, w.M32, w.M33) * half.Z, glow, own, faces == All ? 0 : faces));
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
            covered[Root(a)] = covered.GetValueOrDefault(Root(a)) + Covered(square[a].Box, square[a].Faces, square[b].Box, square[b].Faces);
        foreach (var group in Enumerable.Range(0, square.Count).GroupBy(Root))
        {
            var (min, max) = (new Vector3(float.MaxValue), new Vector3(float.MinValue));
            float surface = 0;
            foreach (var i in group)
            {
                (min, max) = (Vector3.Min(min, square[i].Box.Min), Vector3.Max(max, square[i].Box.Max));
                surface += square[i].Surface;
            }
            // The group's faces that emit, each a member's that lies on the group's side there, so
            // a row of panels that send from their upper faces alone sends from the row's upper
            // face and not from the five others of the box around it.
            var near = 1e-4f * MathF.Max((max - min).Length(), 1e-6f);
            var faces = 0;
            foreach (var i in group)
            {
                var (box, own) = (square[i].Box, square[i].Faces);
                for (var k = 0; k < 3; k++)
                {
                    if (((own >> (2 * k)) & 1) != 0 && MathF.Abs(box.Min[k] - min[k]) <= near) faces |= 1 << (2 * k);
                    if (((own >> (2 * k + 1)) & 1) != 0 && MathF.Abs(box.Max[k] - max[k]) <= near) faces |= 1 << (2 * k + 1);
                }
            }
            faces = faces == 0 ? All : faces;
            var share = Math.Clamp((surface - covered.GetValueOrDefault(group.Key)) / Math.Max(FacesSurface(max - min, faces), 1e-8f), 0.05f, 1);
            var half = (max - min) / 2;
            lights.Add(new Light((min + max) / 2, new Vector3(half.X, 0, 0), new Vector3(0, half.Y, 0), new Vector3(0, 0, half.Z), square[group.Key].Glow, share,
                faces == All ? 0 : faces));
        }
        return lights;
    }

    // The surface of the faces of a box of this size the bits name, two an axis.
    private static float FacesSurface(Vector3 size, int faces = All)
    {
        float surface = 0;
        for (var k = 0; k < 3; k++)
            surface += (((faces >> (2 * k)) & 1) + ((faces >> (2 * k + 1)) & 1)) * size[(k + 1) % 3] * size[(k + 2) % 3];
        return surface;
    }

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

    // The surface two touching boxes emitting from the faces the bits name press together, where
    // one's side lies on the other's along an axis, the side of each that emits from it, so two
    // panels side by side that send from their upper faces alone lose none of their light, or none
    // where they overlap or meet at an edge.
    private static float Covered(SceneFieldPlan.Box a, int facesA, SceneFieldPlan.Box b, int facesB)
    {
        var slack = 1e-3f * MathF.Min((a.Max - a.Min).Length(), (b.Max - b.Min).Length());
        var overlap = Vector3.Max(Vector3.Zero, Vector3.Min(a.Max, b.Max) - Vector3.Max(a.Min, b.Min));
        for (var axis = 0; axis < 3; axis++)
        {
            var below = MathF.Abs(a.Max[axis] - b.Min[axis]) <= slack;
            if (!below && MathF.Abs(b.Max[axis] - a.Min[axis]) > slack || overlap[axis] > slack) continue;
            // a's face toward b and b's toward a, the higher side of the axis on the box below.
            var (up, down) = (2 * axis + 1, 2 * axis);
            var sides = below ? ((facesA >> up) & 1) + ((facesB >> down) & 1) : ((facesA >> down) & 1) + ((facesB >> up) & 1);
            return sides * overlap[(axis + 1) % 3] * overlap[(axis + 2) % 3];
        }
        return 0;
    }
}

/// <summary>
/// The place each glow light is written in, kept from frame to frame. A light among the frame
/// before's keeps its place and a new one takes the first that is free, so what the screen's probes
/// saw of a light the frame before is found in its place as the lights move nearer the eye and
/// farther in the order they are taken in.
/// </summary>
internal sealed class GlowPlaces
{
    private readonly Dictionary<Vector3, int> _before = [];
    private readonly Dictionary<Vector3, int> _now = [];

    /// <summary>The places whose light is the one the frame before had there, a bit a place, the first 32 in the low code.</summary>
    public (uint Low, uint High) Kept { get; private set; }

    /// <summary>
    /// Places the first <paramref name="count"/> of <paramref name="lights"/>, each known by its
    /// middle, in <paramref name="placeOf"/>, and gives how many places are taken up to the last.
    /// </summary>
    public int Place(List<GlowLights.Light> lights, int count, Span<int> placeOf)
    {
        Span<bool> taken = stackalloc bool[GlowLights.Most];
        uint low = 0, high = 0;
        _now.Clear();
        for (var n = 0; n < count; n++)
        {
            placeOf[n] = -1;
            if (_before.TryGetValue(lights[n].Middle, out var place) && !taken[place])
            {
                (placeOf[n], taken[place]) = (place, true);
                if (place < 32) low |= 1u << place;
                else high |= 1u << (place - 32);
            }
        }
        int free = 0, used = 0;
        for (var n = 0; n < count; n++)
        {
            if (placeOf[n] < 0)
            {
                while (taken[free]) free++;
                (placeOf[n], taken[free]) = (free, true);
            }
            _now[lights[n].Middle] = placeOf[n];
            used = Math.Max(used, placeOf[n] + 1);
        }
        // A place no light took holds nothing new.
        for (var place = 0; place < used; place++)
            if (!taken[place])
            {
                if (place < 32) low |= 1u << place;
                else high |= 1u << (place - 32);
            }
        _before.Clear();
        foreach (var (middle, place) in _now) _before[middle] = place;
        Kept = (low, high);
        return used;
    }

    /// <summary>Lets every place go, as a frame that writes no glow light does.</summary>
    public void Forget()
    {
        _before.Clear();
        Kept = (0, 0);
    }
}
