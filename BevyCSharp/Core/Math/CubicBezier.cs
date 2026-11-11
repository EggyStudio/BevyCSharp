using System.Numerics;

namespace Bevy;

/// <summary>Cubic Bézier curves joined end to end, Bevy's <c>CubicBezier</c>, each of four control points.</summary>
/// <typeparam name="T">What it passes through, <see cref="Vec2"/> or <see cref="Vec3"/>.</typeparam>
/// <remarks>
/// Each segment starts at its first point and ends at its fourth, drawn toward the two between
/// without passing through them, so a smooth join is a segment's third point, its fourth and the
/// next segment's second in a line.
/// </remarks>
public sealed class CubicBezier<T>
    where T : struct, IAdditionOperators<T, T, T>, IMultiplyOperators<T, float, T>
{
    private static readonly float[,] Matrix = { { 1f, 0f, 0f, 0f }, { -3f, 3f, 0f, 0f }, { 3f, -6f, 3f, 0f }, { -1f, 3f, -3f, 1f } };

    /// <summary>Takes the segments' control points, four each.</summary>
    public CubicBezier(IEnumerable<(T P0, T P1, T P2, T P3)> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);
        ControlPoints = [.. segments];
    }

    /// <summary>Each segment's four control points.</summary>
    public IReadOnlyList<(T P0, T P1, T P2, T P3)> ControlPoints { get; }

    /// <summary>The curve, or null where there is no segment.</summary>
    public CubicCurve<T>? ToCurve() =>
        ControlPoints.Count == 0 ? null : new CubicCurve<T>(ControlPoints.Select(p => CubicSegment<T>.Coefficients(p.P0, p.P1, p.P2, p.P3, Matrix)));
}
