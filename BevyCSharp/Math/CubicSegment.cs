using System.Numerics;

namespace Bevy;

/// <summary>One cubic piece of a <see cref="CubicCurve{T}"/>, Bevy's <c>CubicSegment</c>, as its polynomial's coefficients.</summary>
/// <typeparam name="T">What it passes through, <see cref="Vec2"/> or <see cref="Vec3"/>.</typeparam>
/// <param name="A">The constant term, where it starts.</param>
/// <param name="B">The linear term, how fast it starts.</param>
/// <param name="C">The square's term.</param>
/// <param name="D">The cube's term.</param>
/// <remarks>
/// A point along it at <c>t</c> from zero to one is <c>A + B t + C t² + D t³</c>. Every spline here
/// is made into these by multiplying its control points by its own characteristic matrix, as Bevy's
/// <c>CubicSegment::coefficients</c> does, so all of them are sampled alike.
/// </remarks>
public readonly record struct CubicSegment<T>(T A, T B, T C, T D)
    where T : struct, IAdditionOperators<T, T, T>, IMultiplyOperators<T, float, T>
{
    /// <summary>The segment four control points make under a spline's characteristic matrix, row by row.</summary>
    /// <param name="p0">The first control point.</param>
    /// <param name="p1">The second.</param>
    /// <param name="p2">The third.</param>
    /// <param name="p3">The fourth.</param>
    /// <param name="matrix">The spline's characteristic matrix, four rows of four.</param>
    public static CubicSegment<T> Coefficients(T p0, T p1, T p2, T p3, float[,] matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        T Row(int r) => p0 * matrix[r, 0] + p1 * matrix[r, 1] + p2 * matrix[r, 2] + p3 * matrix[r, 3];
        return new CubicSegment<T>(Row(0), Row(1), Row(2), Row(3));
    }

    /// <summary>The point at <paramref name="t"/>, from zero at its start to one at its end.</summary>
    public T Position(float t) => A + (B + (C + D * t) * t) * t;

    /// <summary>How fast the point moves at <paramref name="t"/>, the first derivative.</summary>
    public T Velocity(float t) => B + (C * 2f + D * (3f * t)) * t;

    /// <summary>How fast that changes at <paramref name="t"/>, the second derivative.</summary>
    public T Acceleration(float t) => C * 2f + D * (6f * t);
}
