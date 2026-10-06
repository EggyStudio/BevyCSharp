namespace Bevy;

/// <summary>A box centered on the origin, Bevy's <c>Cuboid</c>, measured by half its size along each axis.</summary>
/// <param name="HalfSize">Half its size along each axis, so the box reaches from minus this to this.</param>
/// <remarks>
/// A shape as a value, which a game samples points in or on, as Bevy's <c>ShapeSample</c> does, to
/// scatter what it spawns. Drawing one is <see cref="Render.CreateMesh(string, float, float, float)"/>'s.
/// </remarks>
public readonly record struct Cuboid(Vec3 HalfSize)
{
    /// <summary>A cube whose sides are each <paramref name="length"/> long.</summary>
    public static Cuboid FromLength(float length) => new(new Vec3(length / 2f));

    /// <summary>A box of the size given along each axis.</summary>
    public static Cuboid FromSize(Vec3 size) => new(size * 0.5f);

    /// <summary>Its size along each axis.</summary>
    public Vec3 Size => HalfSize * 2f;

    /// <summary>A point inside it, each as likely as any other.</summary>
    /// <remarks>Bevy's <c>sample_interior</c>, each axis drawn on its own across the box.</remarks>
    /// <param name="random">Where the randomness comes from, seeded where the same points are needed again.</param>
    public Vec3 SampleInterior(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return new Vec3(Across(random, HalfSize.X), Across(random, HalfSize.Y), Across(random, HalfSize.Z));
    }

    /// <summary>A point on its surface, each as likely as any other.</summary>
    /// <remarks>
    /// Bevy's <c>sample_boundary</c>. A pair of opposite faces is chosen by its area, so a long
    /// face is landed on as often as its size says, one of the two at even odds, and the point is
    /// drawn across it.
    /// </remarks>
    /// <param name="random">Where the randomness comes from.</param>
    public Vec3 SampleBoundary(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        var (first, second) = (random.NextSingle() * 2f - 1f, random.NextSingle() * 2f - 1f);
        var side = random.Next(2) == 0 ? -1f : 1f;

        var (x, y, z) = (HalfSize.Y * HalfSize.Z, HalfSize.X * HalfSize.Z, HalfSize.X * HalfSize.Y);
        var pick = random.NextSingle() * (x + y + z);
        var unit = pick < x ? new Vec3(side, first, second)
            : pick < x + y ? new Vec3(first, side, second)
            : new Vec3(first, second, side);
        return new Vec3(unit.X * HalfSize.X, unit.Y * HalfSize.Y, unit.Z * HalfSize.Z);
    }

    private static float Across(Random random, float half) => (random.NextSingle() * 2f - 1f) * half;
}
