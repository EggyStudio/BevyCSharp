namespace Bevy;

/// <summary>A box in the plane whose sides lie along the axes, Bevy's <c>Aabb2d</c>, a shape's cheapest bounds.</summary>
/// <param name="Min">The corner with the smallest coordinates.</param>
/// <param name="Max">The corner with the largest.</param>
/// <remarks>
/// Tested against another volume to find what may touch, before anything costlier asks whether it
/// does, and swept along a ray with <see cref="AabbCast2d"/>.
/// </remarks>
public readonly record struct Aabb2d(Vec2 Min, Vec2 Max)
{
    /// <summary>The box about a center, reaching half its size each way.</summary>
    public static Aabb2d FromCenter(Vec2 center, Vec2 halfSize) => new(center - halfSize, center + halfSize);

    /// <summary>The box about points placed by an isometry.</summary>
    /// <exception cref="ArgumentException">There are no points.</exception>
    public static Aabb2d FromPointCloud(Isometry2d isometry, ReadOnlySpan<Vec2> points)
    {
        if (points.IsEmpty) throw new ArgumentException("A box needs a point to hold.", nameof(points));
        var (min, max) = (isometry.Rotation * points[0], isometry.Rotation * points[0]);
        foreach (var point in points[1..])
        {
            var turned = isometry.Rotation * point;
            (min, max) = (Vec2.Min(min, turned), Vec2.Max(max, turned));
        }

        return new Aabb2d(min + isometry.Translation, max + isometry.Translation);
    }

    /// <summary>Its middle.</summary>
    public Vec2 Center => (Min + Max) * 0.5f;

    /// <summary>Half its size along each axis.</summary>
    public Vec2 HalfSize => (Max - Min) * 0.5f;

    /// <summary>The point of the box nearest a point, the point itself where the box holds it.</summary>
    public Vec2 ClosestPoint(Vec2 point) => Vec2.Clamp(point, Min, Max);

    /// <summary>Whether it overlaps another box, touching counting.</summary>
    public bool Intersects(Aabb2d other) =>
        Min.X <= other.Max.X && Max.X >= other.Min.X && Min.Y <= other.Max.Y && Max.Y >= other.Min.Y;

    /// <summary>Whether it overlaps a circle, touching counting.</summary>
    public bool Intersects(BoundingCircle circle) =>
        (circle.Center - ClosestPoint(circle.Center)).LengthSquared <= circle.Radius * circle.Radius;
}
