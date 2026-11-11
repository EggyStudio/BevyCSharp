namespace Bevy;

/// <summary>A line segment between two points, Bevy's <c>Segment2d</c>.</summary>
/// <param name="Point1">One end.</param>
/// <param name="Point2">The other.</param>
public readonly record struct Segment2d(Vec2 Point1, Vec2 Point2) : IBounded2d
{
    /// <summary>A segment centered on the origin along a direction, a length long.</summary>
    public static Segment2d FromDirectionAndLength(Vec2 direction, float length)
    {
        var half = direction.Normalized * (length / 2f);
        return new Segment2d(-half, half);
    }

    /// <summary>Its middle.</summary>
    public Vec2 Center => (Point1 + Point2) * 0.5f;

    /// <inheritdoc/>
    public Aabb2d AabbAt(Isometry2d isometry) => Aabb2d.FromPointCloud(isometry, [Point1, Point2]);

    /// <inheritdoc/>
    public BoundingCircle BoundingCircleAt(Isometry2d isometry) =>
        new(isometry * Center, (Point1 - Center).Length);
}
