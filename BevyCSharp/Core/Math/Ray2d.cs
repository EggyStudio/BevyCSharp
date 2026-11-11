namespace Bevy;

/// <summary>A ray in the plane, Bevy's <c>Ray2d</c>, from a point toward a direction of length one.</summary>
/// <param name="Origin">Where it starts.</param>
/// <param name="Direction">Which way it goes, of length one, which <see cref="Toward"/> makes it.</param>
public readonly record struct Ray2d(Vec2 Origin, Vec2 Direction)
{
    /// <summary>A ray from a point toward a direction of any length but zero, which is made length one.</summary>
    /// <exception cref="ArgumentException">The direction has no length.</exception>
    public static Ray2d Toward(Vec2 origin, Vec2 direction)
    {
        if (direction.LengthSquared == 0f) throw new ArgumentException("A ray needs a direction.", nameof(direction));
        return new Ray2d(origin, direction.Normalized);
    }

    /// <summary>The point a distance along it.</summary>
    public Vec2 At(float distance) => Origin + Direction * distance;
}
