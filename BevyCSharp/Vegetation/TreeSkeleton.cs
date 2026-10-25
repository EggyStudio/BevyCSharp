namespace Bevy;

/// <summary>
/// A grown tree's stems as points, each with the point it grew from, its place and its radius,
/// which <see cref="Trees.Build(TreeSkeleton, TreeDetail)"/> makes into a mesh.
/// </summary>
/// <remarks>
/// <para>
/// The first point is the trunk's foot and has no parent, and every other point comes after the
/// point it grew from, so the points read in order go from the trunk out to the twigs and a
/// stem is followed by following parents. A point with more than one child is a fork or a place a
/// branch leaves, the thickest child carrying the stem on.
/// </para>
/// <para>
/// The skeleton is plain data, a parent, a place and a radius a point and nothing worked out from
/// them, so a level may keep it beside the species and the seed and draw the same tree on a machine
/// whose floating point would grow it a little differently.
/// </para>
/// </remarks>
public sealed record TreeSkeleton
{
    /// <summary>A skeleton of the points given.</summary>
    /// <param name="nodes">The points, the trunk's foot first and each after its parent.</param>
    /// <exception cref="ArgumentException">
    /// There are no points, the first has a parent, another's parent does not come before it, or a
    /// place or a radius is not a finite number, a radius above zero.
    /// </exception>
    public TreeSkeleton(IReadOnlyList<TreeNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        if (nodes.Count == 0) throw new ArgumentException("A tree's skeleton has a point at the trunk's foot at least.", nameof(nodes));

        for (var i = 0; i < nodes.Count; i++)
        {
            var (parent, place, radius) = nodes[i];
            if (i == 0 ? parent != -1 : parent < 0 || parent >= i)
            {
                throw new ArgumentException(i == 0
                    ? $"The trunk's foot, the first point, has no parent, and this one's is {parent}."
                    : $"Point {i} grew from point {parent}, which does not come before it.", nameof(nodes));
            }

            if (!float.IsFinite(place.X) || !float.IsFinite(place.Y) || !float.IsFinite(place.Z) || !(radius > 0f) || !float.IsFinite(radius))
            {
                throw new ArgumentException($"Point {i} is at {place} with a radius of {radius}, and a place is finite and a radius finite and above zero.", nameof(nodes));
            }
        }

        Nodes = nodes;
    }

    /// <summary>The points, the trunk's foot first and each after the point it grew from.</summary>
    public IReadOnlyList<TreeNode> Nodes { get; }

    /// <summary>Whether another skeleton has the same points in the same order, compared one by one.</summary>
    /// <param name="other">The other skeleton.</param>
    public bool Equals(TreeSkeleton? other) => other is not null && Nodes.SequenceEqual(other.Nodes);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Nodes.Count, Nodes[0], Nodes[^1]);

    /// <summary>The smallest box holding every point, not counting the stems' thickness.</summary>
    public (Vec3 Low, Vec3 High) Bounds
    {
        get
        {
            var low = Nodes[0].Place;
            var high = low;
            foreach (var node in Nodes)
            {
                var place = node.Place;
                low = new Vec3(MathF.Min(low.X, place.X), MathF.Min(low.Y, place.Y), MathF.Min(low.Z, place.Z));
                high = new Vec3(MathF.Max(high.X, place.X), MathF.Max(high.Y, place.Y), MathF.Max(high.Z, place.Z));
            }

            return (low, high);
        }
    }
}
