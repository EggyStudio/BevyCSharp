using System.Numerics;

namespace Bevy;

/// <summary>A Hermite spline, Bevy's <c>CubicHermite</c>, through each control point with the tangent given there.</summary>
/// <typeparam name="T">What it passes through, <see cref="Vec2"/> or <see cref="Vec3"/>.</typeparam>
/// <remarks>
/// It passes through every point, leaving each along its tangent and as fast as the tangent is
/// long, which gives a game the most say of the splines here at the cost of a tangent a point.
/// </remarks>
public sealed class CubicHermite<T>
    where T : struct, IAdditionOperators<T, T, T>, IMultiplyOperators<T, float, T>
{
    private static readonly float[,] Matrix = { { 1f, 0f, 0f, 0f }, { 0f, 1f, 0f, 0f }, { -3f, -2f, 3f, -1f }, { 2f, 1f, -2f, 1f } };

    /// <summary>Takes the points and the tangent at each, in order.</summary>
    /// <exception cref="ArgumentException">The points and the tangents differ in number.</exception>
    public CubicHermite(IEnumerable<T> points, IEnumerable<T> tangents)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(tangents);
        var (p, v) = (points.ToList(), tangents.ToList());
        if (p.Count != v.Count) throw new ArgumentException($"{p.Count} points and {v.Count} tangents, where each point has one.", nameof(tangents));
        ControlPoints = [.. p.Zip(v)];
    }

    /// <summary>Each point and its tangent.</summary>
    public IReadOnlyList<(T Point, T Tangent)> ControlPoints { get; }

    /// <summary>The curve from the first point to the last, or null with fewer than two.</summary>
    public CubicCurve<T>? ToCurve() =>
        ControlPoints.Count < 2 ? null : new CubicCurve<T>(Enumerable.Range(0, ControlPoints.Count - 1).Select(i => Segment(i, i + 1)));

    /// <summary>The curve from the first point round through the last and back to the first, or null with none.</summary>
    public CubicCurve<T>? ToCurveCyclic() =>
        ControlPoints.Count == 0 ? null : new CubicCurve<T>(Enumerable.Range(0, ControlPoints.Count).Select(i => Segment(i, (i + 1) % ControlPoints.Count)));

    private CubicSegment<T> Segment(int from, int to) =>
        CubicSegment<T>.Coefficients(ControlPoints[from].Point, ControlPoints[from].Tangent, ControlPoints[to].Point, ControlPoints[to].Tangent, Matrix);
}
