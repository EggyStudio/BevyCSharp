namespace Bevy;

/// <summary>A point of a tree's skeleton, where a stem passes, how thick it is there and which point it grew from.</summary>
/// <param name="Parent">The index of the point this one grew from, which comes before it, or -1 for the trunk's foot.</param>
/// <param name="Place">Where it is, in meters, the trunk's foot at the origin.</param>
/// <param name="Radius">The stem's radius there, in meters.</param>
/// <remarks>
/// A point names its parent by index rather than holding it, so a skeleton is a flat list of points,
/// a parent, a place and a radius each, that a level file keeps as it is, and the stems are found
/// again by following the parents, the thickest child of a point carrying its stem on.
/// </remarks>
public readonly record struct TreeNode(int Parent, Vec3 Place, float Radius);
