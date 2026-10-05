using System.Runtime.InteropServices;
using Bevy.Interop;

namespace Bevy;

public static unsafe partial class Gizmos
{
    /// <summary>
    /// Draws a whole run of lines in one crossing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What a wireframe, a path or a grid is. Every other call here crosses the boundary on its
    /// own, which is fine at the few hundred shapes a frame an editor overlay asks for and stops
    /// being fine well before a mesh's worth. This costs the array rather than the number of lines
    /// in it.
    /// </para>
    /// <para>
    /// The lines are all drawn the same way, since <paramref name="inFront"/> is one answer for the
    /// run. A path drawn in two colors is two calls, which is still two rather than one per
    /// segment.
    /// </para>
    /// </remarks>
    /// <param name="lines">The segments, each with its own two ends and color.</param>
    /// <param name="inFront">
    /// Whether the scene can hide them. False by default here where it is true elsewhere, because
    /// what is drawn in runs is usually drawn *in* the scene rather than about it.
    /// </param>
    /// <exception cref="BevyNativeException">There is nothing to draw on.</exception>
    /// <example>
    /// <code>
    /// Span&lt;GizmoSegment&gt; path = stackalloc GizmoSegment[points.Length - 1];
    ///
    /// for (var i = 0; i &lt; path.Length; i++)
    ///     path[i] = new GizmoSegment(points[i], points[i + 1], (0f, 1f, 0f, 1f));
    ///
    /// Gizmos.Lines(path);
    /// </code>
    /// </example>
    public static void Lines(ReadOnlySpan<GizmoSegment> lines, bool inFront = false)
    {
        if (lines.IsEmpty) return;

        // Built here rather than by the caller, so the wire format stays this file's business the
        // way it is for every single-shape call above.
        var configs = lines.Length <= 64
            ? stackalloc NativeGizmoConfig[lines.Length]
            : new NativeGizmoConfig[lines.Length];

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            configs[i] = Shape(
                line.Fades ? 3 : 0,
                line.Start,
                Quat.Identity,
                line.Color,
                inFront,
                end: line.End);

            configs[i].EndColorR = line.EndColor.R;
            configs[i].EndColorG = line.EndColor.G;
            configs[i].EndColorB = line.EndColor.B;
            configs[i].EndColorA = line.EndColor.A;
        }

        // Inside a batch the run joins it, so the whole frame's shapes still cross once.
        if (_batch is { } gathering)
        {
            foreach (var config in configs) gathering.Add(config);
            return;
        }

        DrawMany(configs, "drawing a run of gizmo lines");
    }

    /// <summary>
    /// Draws a line through a run of points, joining the last back to the first where
    /// <paramref name="closed"/> is set.
    /// </summary>
    /// <remarks>
    /// A path, an outline or a polygon, handed over as one run through <see cref="Lines"/>. A
    /// triangle is three points, closed, which <see cref="Triangle"/> says outright.
    /// </remarks>
    /// <param name="points">The points in order, at least two.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="closed">Whether the last point joins the first.</param>
    /// <param name="inFront">Whether the scene can hide it, as for <see cref="Lines"/>.</param>
    public static void Polyline(
        ReadOnlySpan<Vec3> points,
        (float R, float G, float B, float A) color,
        bool closed = false,
        bool inFront = false)
    {
        if (points.Length < 2) return;

        var count = closed && points.Length > 2 ? points.Length : points.Length - 1;
        var segments = count <= 64 ? stackalloc GizmoSegment[count] : new GizmoSegment[count];

        for (var i = 0; i < count; i++)
        {
            segments[i] = new GizmoSegment(points[i], points[(i + 1) % points.Length], color);
        }

        Lines(segments, inFront);
    }

    /// <summary>Draws the outline of a triangle through three points.</summary>
    public static void Triangle(
        Vec3 a,
        Vec3 b,
        Vec3 c,
        (float R, float G, float B, float A) color,
        bool inFront = false) =>
        Polyline([a, b, c], color, closed: true, inFront);

    /// <summary>Draws the six edges of a tetrahedron through four points.</summary>
    /// <remarks>
    /// The simplest solid, and the shape of a <see cref="MeshShape.Tetrahedron"/>, a trigger volume
    /// with four corners or a debug marker that reads as a solid from any side. Six segments in one
    /// call, each edge drawn once rather than twice as four triangles would draw it.
    /// </remarks>
    public static void Tetrahedron(
        Vec3 a,
        Vec3 b,
        Vec3 c,
        Vec3 d,
        (float R, float G, float B, float A) color,
        bool inFront = false)
    {
        ReadOnlySpan<GizmoSegment> edges =
        [
            new(a, b, color), new(b, c, color), new(c, a, color),
            new(a, d, color), new(b, d, color), new(c, d, color),
        ];

        Lines(edges, inFront);
    }

    /// <summary>Draws a regular tetrahedron standing on its base, centered on a point.</summary>
    /// <param name="center">Its middle, which is a quarter of the way up from its base.</param>
    /// <param name="size">How long each edge is.</param>
    /// <param name="color">Its color.</param>
    /// <param name="inFront">Whether it draws over what is in front of it.</param>
    public static void Tetrahedron(Vec3 center, float size, (float R, float G, float B, float A) color, bool inFront = false)
    {
        // A regular tetrahedron's corners are a quarter of its height below the middle for the
        // base and three quarters above for the apex, with the base's corners round a circle.
        var height = size * MathF.Sqrt(2f / 3f);
        var radius = size / MathF.Sqrt(3f);
        var floor = center.Y - (height / 4f);

        Vec3 Corner(float turn) => new(center.X + (radius * MathF.Cos(turn)), floor, center.Z + (radius * MathF.Sin(turn)));

        Tetrahedron(
            Corner(0f),
            Corner(MathF.Tau / 3f),
            Corner(MathF.Tau * 2f / 3f),
            new Vec3(center.X, floor + height, center.Z),
            color,
            inFront);
    }
}
