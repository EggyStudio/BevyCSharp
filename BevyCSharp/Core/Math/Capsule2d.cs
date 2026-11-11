namespace Bevy;

/// <summary>A rectangle with half circles at its ends, upright and centered on the origin, Bevy's <c>Capsule2d</c>.</summary>
/// <param name="Radius">The half circles' radius, half its width.</param>
/// <param name="HalfLength">Half the length of its straight middle, from the center to where a half circle starts.</param>
public readonly record struct Capsule2d(float Radius, float HalfLength) : IBounded2d
{
    /// <summary>A capsule of a radius whose straight middle is a length long, as Bevy's <c>Capsule2d::new</c> takes it.</summary>
    public static Capsule2d FromLength(float radius, float length) => new(radius, length / 2f);

    /// <inheritdoc/>
    /// <remarks>Bevy's, the box about its straight middle turned, grown by the radius.</remarks>
    public Aabb2d AabbAt(Isometry2d isometry)
    {
        var up = isometry.Rotation * new Vec2(0f, HalfLength);
        var (a, b) = (-up, up);
        var reach = new Vec2(Radius);
        return new Aabb2d(Vec2.Min(a, b) - reach + isometry.Translation, Vec2.Max(a, b) + reach + isometry.Translation);
    }

    /// <inheritdoc/>
    public BoundingCircle BoundingCircleAt(Isometry2d isometry) => new(isometry.Translation, Radius + HalfLength);
}
