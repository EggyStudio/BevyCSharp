namespace Bevy;

/// <summary>A 2D shape whose bounds can be taken where an isometry places it, Bevy's <c>Bounded2d</c>.</summary>
/// <remarks>
/// <see cref="Rectangle"/>, <see cref="Circle"/>, <see cref="Triangle2d"/>, <see cref="Segment2d"/>,
/// <see cref="Capsule2d"/> and <see cref="RegularPolygon"/>, each centered on the origin as Bevy's are,
/// so a game holds a mix of them and asks each for its bounds alike.
/// </remarks>
public interface IBounded2d
{
    /// <summary>The box about the shape where it is placed, Bevy's <c>aabb_2d</c>.</summary>
    Aabb2d AabbAt(Isometry2d isometry);

    /// <summary>A circle about the shape where it is placed, Bevy's <c>bounding_circle</c>.</summary>
    BoundingCircle BoundingCircleAt(Isometry2d isometry);
}
