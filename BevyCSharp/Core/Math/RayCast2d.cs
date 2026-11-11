namespace Bevy;

/// <summary>A ray tested against bounding volumes as far as a distance, Bevy's <c>RayCast2d</c>.</summary>
/// <param name="Ray">The ray.</param>
/// <param name="Max">How far along it a volume is found.</param>
/// <remarks>
/// Each test answers how far along the ray it first meets the volume, zero where the ray starts
/// inside it, or null where it misses or meets it past <paramref name="Max"/>.
/// </remarks>
public readonly record struct RayCast2d(Ray2d Ray, float Max)
{
    /// <summary>How far along the ray it meets a box, or null.</summary>
    /// <remarks>
    /// The slab test Bevy's is, the distances to each axis's two sides taken from the ray's
    /// reciprocal direction, the ray meeting the box where it is between both pairs at once. An
    /// axis the ray runs along gives infinities or not-a-number, which the comparisons pass over,
    /// as Bevy's do.
    /// </remarks>
    public float? AabbIntersectionAt(Aabb2d box)
    {
        var (o, d) = (Ray.Origin, Ray.Direction);
        var (recipX, recipY) = (1f / d.X, 1f / d.Y);
        var (minX, maxX) = float.IsNegative(d.X) ? (box.Max.X, box.Min.X) : (box.Min.X, box.Max.X);
        var (minY, maxY) = float.IsNegative(d.Y) ? (box.Max.Y, box.Min.Y) : (box.Min.Y, box.Max.Y);

        var tMin = MaxOf(MaxOf((minX - o.X) * recipX, (minY - o.Y) * recipY), 0f);
        var tMax = MinOf(MinOf((maxY - o.Y) * recipY, (maxX - o.X) * recipX), Max);
        return tMin <= tMax ? tMin : null;
    }

    /// <summary>How far along the ray it meets a circle, or null.</summary>
    public float? CircleIntersectionAt(BoundingCircle circle)
    {
        var offset = Ray.Origin - circle.Center;
        var projected = Vec2.Dot(offset, Ray.Direction);
        var cross = Vec2.PerpDot(offset, Ray.Direction);
        var distanceSquared = circle.Radius * circle.Radius - cross * cross;
        if (distanceSquared < 0f || MathF.CopySign(projected * projected, -projected) < -distanceSquared) return null;

        var toi = -projected - MathF.Sqrt(distanceSquared);
        return toi > Max ? null : MathF.Max(toi, 0f);
    }

    // Rust's min and max, which take the other where one is not a number, where MathF's spread it.
    private static float MinOf(float a, float b) => float.IsNaN(a) ? b : float.IsNaN(b) ? a : MathF.Min(a, b);

    private static float MaxOf(float a, float b) => float.IsNaN(a) ? b : float.IsNaN(b) ? a : MathF.Max(a, b);
}
