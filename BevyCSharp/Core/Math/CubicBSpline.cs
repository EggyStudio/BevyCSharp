using System.Numerics;

namespace Bevy;

/// <summary>A uniform B-spline, Bevy's <c>CubicBSpline</c>, drawn toward its control points without passing through them.</summary>
/// <typeparam name="T">What it passes through, <see cref="Vec2"/> or <see cref="Vec3"/>.</typeparam>
/// <remarks>
/// The smoothest of the splines here, smooth in how fast its direction turns as well as in its
/// direction, at the cost of not reaching its points. Each four points in a row make a segment.
/// </remarks>
public sealed class CubicBSpline<T>
    where T : struct, IAdditionOperators<T, T, T>, IMultiplyOperators<T, float, T>
{
    // Bevy's, from Kaihuai Qin's general matrix representation of B-splines, a sixth of the integers.
    private static readonly float[,] Matrix =
    {
        { 1f / 6f, 4f / 6f, 1f / 6f, 0f }, { -3f / 6f, 0f, 3f / 6f, 0f }, { 3f / 6f, -6f / 6f, 3f / 6f, 0f }, { -1f / 6f, 3f / 6f, -3f / 6f, 1f / 6f },
    };

    /// <summary>Takes the points, in order.</summary>
    public CubicBSpline(IEnumerable<T> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        ControlPoints = [.. points];
    }

    /// <summary>The points it is drawn toward.</summary>
    public IReadOnlyList<T> ControlPoints { get; }

    /// <summary>The curve, a segment for each four points in a row, or null with fewer than four.</summary>
    public CubicCurve<T>? ToCurve() =>
        ControlPoints.Count < 4 ? null : new CubicCurve<T>(Enumerable.Range(0, ControlPoints.Count - 3).Select(i => Segment(i, i + 1, i + 2, i + 3)));

    /// <summary>
    /// The curve round its points and back, a segment starting at each point, or null with none.
    /// Its first segment is the one the open curve starts with, the second point to the third, and
    /// the segments round the ends come after, as Bevy's are.
    /// </summary>
    public CubicCurve<T>? ToCurveCyclic()
    {
        var count = ControlPoints.Count;
        if (count == 0) return null;
        return new CubicCurve<T>(Enumerable.Range(0, count).Select(i => Segment(i % count, (i + 1) % count, (i + 2) % count, (i + 3) % count)));
    }

    private CubicSegment<T> Segment(int a, int b, int c, int d) =>
        CubicSegment<T>.Coefficients(ControlPoints[a], ControlPoints[b], ControlPoints[c], ControlPoints[d], Matrix);
}
