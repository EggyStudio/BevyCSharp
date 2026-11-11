namespace Bevy;

/// <summary>A box swept along a ray, Bevy's <c>AabbCast2d</c>, to find how far it moves before it meets another.</summary>
/// <param name="Aabb">The box swept, about the ray's origin.</param>
/// <param name="Ray">The ray it moves along and how far.</param>
public readonly record struct AabbCast2d(Aabb2d Aabb, RayCast2d Ray)
{
    /// <summary>How far along the ray the swept box first touches another, or null.</summary>
    /// <remarks>Bevy's, the other box grown by the swept one and met by the ray alone.</remarks>
    public float? AabbCollisionAt(Aabb2d other) =>
        Ray.AabbIntersectionAt(new Aabb2d(other.Min - Aabb.Max, other.Max - Aabb.Min));
}
