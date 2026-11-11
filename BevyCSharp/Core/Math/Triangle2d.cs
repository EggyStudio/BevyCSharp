namespace Bevy;

/// <summary>A triangle, Bevy's <c>Triangle2d</c>.</summary>
/// <param name="A">A corner.</param>
/// <param name="B">The next corner.</param>
/// <param name="C">The last corner.</param>
public readonly record struct Triangle2d(Vec2 A, Vec2 B, Vec2 C) : IBounded2d
{
    /// <inheritdoc/>
    public Aabb2d AabbAt(Isometry2d isometry) => Aabb2d.FromPointCloud(isometry, [A, B, C]);

    /// <inheritdoc/>
    /// <remarks>
    /// Bevy's. A triangle with an angle of a right angle or more is held by the circle on the side
    /// across from that angle, which reaches its third corner too, and any other by the circle
    /// through its three corners, the smallest circle in both.
    /// </remarks>
    public BoundingCircle BoundingCircleAt(Isometry2d isometry)
    {
        var across = Vec2.Dot(B - A, C - A) <= 0f ? (B, C)
            : Vec2.Dot(C - B, A - B) <= 0f ? (C, A)
            : Vec2.Dot(A - C, B - C) <= 0f ? (A, B)
            : ((Vec2, Vec2)?)null;
        if (across is var (first, second)) return new Segment2d(first, second).BoundingCircleAt(isometry);

        var (center, radius) = Circumcircle();
        return new BoundingCircle(isometry * center, radius);
    }

    /// <summary>The circle through its three corners, its center and its radius.</summary>
    public (Vec2 Center, float Radius) Circumcircle()
    {
        var (b, c) = (B - A, C - A);
        var (bLength, cLength) = (b.LengthSquared, c.LengthSquared);
        var inverse = 1f / (2f * (b.X * c.Y - b.Y * c.X));
        var u = new Vec2(inverse * (c.Y * bLength - b.Y * cLength), inverse * (b.X * cLength - c.X * bLength));
        return (u + A, u.Length);
    }
}
