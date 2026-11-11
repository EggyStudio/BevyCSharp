using System.Numerics;

namespace Bevy;

/// <summary>A cardinal spline, Bevy's <c>CubicCardinalSpline</c>, through each control point, its tangents taken from the points beside it.</summary>
/// <typeparam name="T">What it passes through, <see cref="Vec2"/> or <see cref="Vec3"/>.</typeparam>
/// <remarks>
/// It passes through every point, leaving each toward the next as the line from the point before
/// to the point after leans, scaled by the tension. A tension of one half is the Catmull-Rom spline,
/// <see cref="CatmullRom"/>. Not looping, the ends are given points beyond them mirrored from the
/// points within, so the curve leaves its first point toward its second, as Bevy's does.
/// </remarks>
public sealed class CubicCardinalSpline<T>
    where T : struct, IAdditionOperators<T, T, T>, IMultiplyOperators<T, float, T>
{
    /// <summary>Takes the tension and the points, in order.</summary>
    public CubicCardinalSpline(float tension, IEnumerable<T> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        Tension = tension;
        ControlPoints = [.. points];
    }

    /// <summary>A Catmull-Rom spline, a cardinal spline of tension one half.</summary>
    public static CubicCardinalSpline<T> CatmullRom(IEnumerable<T> points) => new(0.5f, points);

    /// <summary>How tightly it turns at each point, one half for Catmull-Rom.</summary>
    public float Tension { get; }

    /// <summary>The points it passes through.</summary>
    public IReadOnlyList<T> ControlPoints { get; }

    private float[,] Matrix()
    {
        var s = Tension;
        return new[,] { { 0f, 1f, 0f, 0f }, { -s, 0f, s, 0f }, { 2f * s, s - 3f, 3f - 2f * s, -s }, { -s, 2f - s, s - 2f, s } };
    }

    /// <summary>The curve from the first point to the last, or null with fewer than two.</summary>
    public CubicCurve<T>? ToCurve()
    {
        var count = ControlPoints.Count;
        if (count < 2) return null;

        // The points beyond each end, the second and the second-to-last mirrored through the ends,
        // which leaves the curve's ends along the lines to the points beside them.
        var first = ControlPoints[0] * 2f + ControlPoints[1] * -1f;
        var last = ControlPoints[count - 1] * 2f + ControlPoints[count - 2] * -1f;
        T[] all = [first, .. ControlPoints, last];

        var matrix = Matrix();
        return new CubicCurve<T>(Enumerable.Range(0, count - 1).Select(i => CubicSegment<T>.Coefficients(all[i], all[i + 1], all[i + 2], all[i + 3], matrix)));
    }

    /// <summary>
    /// The curve from the first point round through the last and back to the first, or null with
    /// fewer than two, its first segment the one from the first point to the second, as Bevy's is.
    /// </summary>
    public CubicCurve<T>? ToCurveCyclic()
    {
        var count = ControlPoints.Count;
        if (count < 2) return null;

        var matrix = Matrix();
        T At(int i) => ControlPoints[i % count];
        return new CubicCurve<T>(Enumerable.Range(0, count).Select(i => CubicSegment<T>.Coefficients(At(i + count - 1), At(i), At(i + 1), At(i + 2), matrix)));
    }
}
