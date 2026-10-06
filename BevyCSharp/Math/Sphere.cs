namespace Bevy;

/// <summary>A ball centered on the origin, Bevy's <c>Sphere</c>.</summary>
/// <param name="Radius">How far its surface is from its center.</param>
/// <remarks>A shape as a value, which a game samples points in or on, as <see cref="Cuboid"/> is.</remarks>
public readonly record struct Sphere(float Radius)
{
    /// <summary>A point inside it, each as likely as any other.</summary>
    /// <remarks>
    /// Bevy's <c>sample_interior</c>. The distance from the center is the cube root of a cube drawn
    /// evenly, since a shell further out holds more of the ball, and the direction is a point on its
    /// surface.
    /// </remarks>
    /// <param name="random">Where the randomness comes from, seeded where the same points are needed again.</param>
    public Vec3 SampleInterior(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        var distance = MathF.Cbrt(random.NextSingle() * Radius * Radius * Radius);
        return UnitBoundary(random) * distance;
    }

    /// <summary>A point on its surface, each as likely as any other.</summary>
    /// <remarks>
    /// Bevy's <c>sample_boundary</c>, the height drawn evenly from bottom to top and the angle round
    /// evenly, which covers a sphere's surface evenly, as Archimedes found of the cylinder about it.
    /// </remarks>
    /// <param name="random">Where the randomness comes from.</param>
    public Vec3 SampleBoundary(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return UnitBoundary(random) * Radius;
    }

    private static Vec3 UnitBoundary(Random random)
    {
        var z = random.NextSingle() * 2f - 1f;
        var angle = (random.NextSingle() * 2f - 1f) * MathF.PI;
        var ring = MathF.Sqrt(1f - z * z);
        return new Vec3(MathF.Cos(angle) * ring, MathF.Sin(angle) * ring, z);
    }
}
