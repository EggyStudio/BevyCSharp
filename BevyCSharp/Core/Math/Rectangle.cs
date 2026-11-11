namespace Bevy;

/// <summary>A rectangle centered on the origin, Bevy's <c>Rectangle</c>, measured by half its size.</summary>
/// <param name="HalfSize">Half its width and half its height.</param>
public readonly record struct Rectangle(Vec2 HalfSize) : IBounded2d
{
    /// <summary>A rectangle of a width and a height.</summary>
    public static Rectangle FromSize(float width, float height) => new(new Vec2(width / 2f, height / 2f));

    /// <inheritdoc/>
    /// <remarks>Bevy's, the half size turned by the absolute rotation, which is the turned rectangle's reach along each axis.</remarks>
    public Aabb2d AabbAt(Isometry2d isometry)
    {
        var (cos, sin) = (MathF.Abs(isometry.Rotation.Cos), MathF.Abs(isometry.Rotation.Sin));
        var reach = new Vec2(cos * HalfSize.X + sin * HalfSize.Y, sin * HalfSize.X + cos * HalfSize.Y);
        return Aabb2d.FromCenter(isometry.Translation, reach);
    }

    /// <inheritdoc/>
    public BoundingCircle BoundingCircleAt(Isometry2d isometry) => new(isometry.Translation, HalfSize.Length);
}
