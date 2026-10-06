using System.Runtime.InteropServices;
using Bevy.Interop;

namespace Bevy;

public static unsafe partial class Gizmos
{
    /// <summary>
    /// The last long run of lines in the bridge's layout, one array a thread, which
    /// <see cref="Lines"/> builds each run into rather than making a new one.
    /// </summary>
    [ThreadStatic]
    private static NativeGizmoSegment[]? _run;

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

        // Inside a batch the run joins it as shapes, so the whole frame's shapes still cross once.
        if (_batch is { } gathering)
        {
            foreach (var line in lines)
            {
                var config = Shape(line.Fades ? 3 : 0, line.Start, Quat.Identity, line.Color, inFront, end: line.End);
                (config.EndColorR, config.EndColorG, config.EndColorB, config.EndColorA) = line.EndColor;
                gathering.Add(config);
            }

            return;
        }

        // Otherwise it crosses as lines, each its ends and its colors and nothing a shape reads
        // besides. A short run is built on the stack and a longer one in an array kept between
        // calls, grown when a run is longer than any before, since one made anew each call is
        // large enough to land on .NET's large object heap.
        if (lines.Length > 64 && (_run is null || _run.Length < lines.Length))
            _run = new NativeGizmoSegment[Math.Max(lines.Length, (_run?.Length ?? 0) * 2)];

        var segments = lines.Length <= 64
            ? stackalloc NativeGizmoSegment[lines.Length]
            : _run.AsSpan(0, lines.Length);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            segments[i] = new NativeGizmoSegment
            {
                StartX = line.Start.X,
                StartY = line.Start.Y,
                StartZ = line.Start.Z,
                EndX = line.End.X,
                EndY = line.End.Y,
                EndZ = line.End.Z,
                ColorR = line.Color.R,
                ColorG = line.Color.G,
                ColorB = line.Color.B,
                ColorA = line.Color.A,
                EndColorR = line.EndColor.R,
                EndColorG = line.EndColor.G,
                EndColorB = line.EndColor.B,
                EndColorA = line.EndColor.A,
                Fades = line.Fades ? 1 : 0,
            };
        }

        fixed (NativeGizmoSegment* at = segments)
        {
            var status = Native.bcs_gizmo_lines(at, segments.Length, inFront ? 1 : 0);
            if (status == NativeStatus.Unsupported)
                throw new BevyNativeException(
                    NativeStatus.Unsupported,
                    "Drawing gizmos failed, because gizmos are drawn by a plugin that comes with "
                    + "the window, so a windowless run has nothing to draw on. Guard with "
                    + "App.HasRenderer and Config.Headless.");

            Native.Check(status, "drawing a run of gizmo lines");
        }
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
