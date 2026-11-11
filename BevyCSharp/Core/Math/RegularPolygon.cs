namespace Bevy;

/// <summary>A polygon with equal sides and angles, centered on the origin, Bevy's <c>RegularPolygon</c>.</summary>
/// <param name="Circumradius">How far its corners are from its center.</param>
/// <param name="Sides">How many sides, three or more.</param>
/// <remarks>A corner points up before it is turned, as Bevy's does.</remarks>
public readonly record struct RegularPolygon(float Circumradius, int Sides) : IBounded2d
{
    /// <summary>Its corners, turned by an angle, the first up before turning.</summary>
    public IEnumerable<Vec2> Vertices(float rotation)
    {
        var step = MathF.Tau / Sides;
        for (var i = 0; i < Sides; i++)
        {
            var theta = rotation + MathF.PI / 2f + i * step;
            yield return new Vec2(MathF.Cos(theta), MathF.Sin(theta)) * Circumradius;
        }
    }

    /// <inheritdoc/>
    /// <remarks>Bevy's, its turned corners and its center held, which a polygon about the origin always holds.</remarks>
    public Aabb2d AabbAt(Isometry2d isometry)
    {
        var (min, max) = (Vec2.Zero, Vec2.Zero);
        foreach (var corner in Vertices(isometry.Rotation.AsRadians)) (min, max) = (Vec2.Min(min, corner), Vec2.Max(max, corner));
        return new Aabb2d(min + isometry.Translation, max + isometry.Translation);
    }

    /// <inheritdoc/>
    public BoundingCircle BoundingCircleAt(Isometry2d isometry) => new(isometry.Translation, Circumradius);
}
