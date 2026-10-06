using System.Numerics;

namespace Bevy;

/// <summary>A run of cubic segments joined end to end, Bevy's <c>CubicCurve</c>, which every spline here is made into.</summary>
/// <typeparam name="T">What it passes through, <see cref="Vec2"/> or <see cref="Vec3"/>.</typeparam>
/// <remarks>
/// Sampled at <c>t</c> from zero to its number of segments, each whole number where one segment
/// ends and the next begins, a <c>t</c> outside them taken from the first or the last segment
/// carried on, as Bevy's is.
/// </remarks>
public sealed class CubicCurve<T>
    where T : struct, IAdditionOperators<T, T, T>, IMultiplyOperators<T, float, T>
{
    /// <summary>Makes a curve of segments.</summary>
    /// <exception cref="ArgumentException">There are none.</exception>
    public CubicCurve(IEnumerable<CubicSegment<T>> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);
        Segments = [.. segments];
        if (Segments.Count == 0) throw new ArgumentException("A curve needs a segment.", nameof(segments));
    }

    /// <summary>Its segments, in order.</summary>
    public IReadOnlyList<CubicSegment<T>> Segments { get; }

    /// <summary>The point at <paramref name="t"/>.</summary>
    public T Position(float t) => Segment(t, out var local).Position(local);

    /// <summary>How fast the point moves at <paramref name="t"/>.</summary>
    public T Velocity(float t) => Segment(t, out var local).Velocity(local);

    /// <summary>How fast that changes at <paramref name="t"/>.</summary>
    public T Acceleration(float t) => Segment(t, out var local).Acceleration(local);

    /// <summary>
    /// Points along the whole curve at even steps of <c>t</c>, <paramref name="subdivisions"/> of
    /// them and one more for the end, Bevy's <c>iter_positions</c>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">There are no subdivisions.</exception>
    public IEnumerable<T> IterPositions(int subdivisions)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(subdivisions, 1);
        var step = Segments.Count / (float)subdivisions;
        for (var i = 0; i <= subdivisions; i++) yield return Position(i * step);
    }

    private CubicSegment<T> Segment(float t, out float local)
    {
        if (Segments.Count == 1)
        {
            local = t;
            return Segments[0];
        }

        var index = Math.Clamp((int)MathF.Floor(t), 0, Segments.Count - 1);
        local = t - index;
        return Segments[index];
    }
}
