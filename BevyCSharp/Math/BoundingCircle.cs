namespace Bevy;

/// <summary>A circle about a shape, Bevy's <c>BoundingCircle</c>, bounds that turning the shape leaves alone.</summary>
/// <param name="Center">Its middle.</param>
/// <param name="Radius">How far it reaches.</param>
/// <remarks>Swept along a ray with <see cref="BoundingCircleCast"/>.</remarks>
public readonly record struct BoundingCircle(Vec2 Center, float Radius)
{
    /// <summary>The point of the circle nearest a point, the point itself where the circle holds it.</summary>
    public Vec2 ClosestPoint(Vec2 point)
    {
        var offset = point - Center;
        return offset.LengthSquared <= Radius * Radius ? point : Center + offset.Normalized * Radius;
    }

    /// <summary>Whether it overlaps another circle, touching counting.</summary>
    public bool Intersects(BoundingCircle other) =>
        (other.Center - Center).LengthSquared <= (Radius + other.Radius) * (Radius + other.Radius);

    /// <summary>Whether it overlaps a box, touching counting.</summary>
    public bool Intersects(Aabb2d box) => box.Intersects(this);
}
