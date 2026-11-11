namespace Bevy;

/// <summary>A circle centered on the origin, Bevy's <c>Circle</c>.</summary>
/// <param name="Radius">How far it reaches.</param>
public readonly record struct Circle(float Radius) : IBounded2d
{
    /// <inheritdoc/>
    public Aabb2d AabbAt(Isometry2d isometry) => Aabb2d.FromCenter(isometry.Translation, new Vec2(Radius));

    /// <inheritdoc/>
    public BoundingCircle BoundingCircleAt(Isometry2d isometry) => new(isometry.Translation, Radius);
}
