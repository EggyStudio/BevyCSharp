namespace Bevy;

/// <summary>A circle swept along a ray, Bevy's <c>BoundingCircleCast</c>, to find how far it moves before it meets another.</summary>
/// <param name="Circle">The circle swept, about the ray's origin.</param>
/// <param name="Ray">The ray it moves along and how far.</param>
public readonly record struct BoundingCircleCast(BoundingCircle Circle, RayCast2d Ray)
{
    /// <summary>How far along the ray the swept circle first touches another, or null.</summary>
    /// <remarks>Bevy's, the other circle grown by the swept one's radius and met by the ray alone.</remarks>
    public float? CircleCollisionAt(BoundingCircle other) =>
        Ray.CircleIntersectionAt(new BoundingCircle(other.Center - Circle.Center, other.Radius + Circle.Radius));
}
