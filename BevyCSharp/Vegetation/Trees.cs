namespace Bevy;

/// <summary>
/// Trees grown from a species and a seed, their branches grown within Weber and Penn's controls by
/// space colonization and built into meshes through the modeling, with no files to load.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Grow(TreeSpecies, uint)"/> grows a tree's skeleton, the points its stems pass through with their
/// thickness, and <see cref="Build"/> makes the skeleton into an <see cref="EditMesh"/> of tapered
/// tubes, which <see cref="Modeling.Meshes"/> draws as it draws any other. The two are apart so a
/// skeleton can be kept, in a level or a save, and built again without growing it.
/// </para>
/// <para>
/// The growth is after Runions, Lane and Prusinkiewicz (2007), with buds as Palubicki and others
/// (2009) give them. Attractors are scattered through the crown's <see cref="TreeSpecies.Shape"/>,
/// the trunk grows up through them, and every bud that perceives one ahead of it grows toward it
/// an internode at a time, taking the attractors it comes near, until none is left that a bud
/// perceives. The paper's angles, bends, forks and lengths are the buds' tendencies, so a species
/// reads as the paper's does, and the attractors decide which buds grow, so the crown fills its
/// outline and its inside stays open.
/// </para>
/// <para>
/// A seed grows the same skeleton every time, point for point, since the growth draws its random
/// numbers from a generator of its own and does everything in a fixed order. Another machine's
/// floating point may round a sine differently and grow a slightly different tree, which keeping
/// the skeleton rather than the seed avoids.
/// </para>
/// </remarks>
public static partial class Trees
{
    /// <summary>The material slot of the bark, which every face <see cref="Build"/> makes is in.</summary>
    public const string BarkSlot = "bark";

    /// <summary>Grows a tree of a species from a seed.</summary>
    /// <param name="species">The species.</param>
    /// <param name="seed">The seed, any number, each growing another tree of the species.</param>
    /// <returns>The tree's skeleton, standing up the Y axis from the origin.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A control of the species is outside what it may be, which the message names.</exception>
    /// <remarks>
    /// It grows on the thread that calls it and touches nothing of the app's, so it runs on a
    /// worker while a level loads. Its cost grows with the crown's points.
    /// </remarks>
    public static TreeSkeleton Grow(TreeSpecies species, uint seed) => Grow(species, seed, []);

    /// <summary>Grows a tree of a species from a seed, round boxes it does not grow into and that shade it.</summary>
    /// <param name="species">The species.</param>
    /// <param name="seed">The seed, any number, each growing another tree of the species.</param>
    /// <param name="blockers">
    /// Boxes in the tree's space, the trunk's foot at the origin, from their lowest corner to their
    /// highest, a wall, a building or a neighbor's crown.
    /// </param>
    /// <returns>The tree's skeleton, standing up the Y axis from the origin.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A control of the species is outside what it may be, which the message names.</exception>
    /// <remarks>
    /// No stem grows into a box, and a box shades the space under it, so a tree beside a wall or
    /// under a neighbor's crown grows its branches away from it toward the light, as a tree at a
    /// forest's edge leans out of it. The same species, seed and boxes grow the same tree.
    /// </remarks>
    public static TreeSkeleton Grow(TreeSpecies species, uint seed, IReadOnlyList<(Vec3 Low, Vec3 High)> blockers)
    {
        ArgumentNullException.ThrowIfNull(species);
        ArgumentNullException.ThrowIfNull(blockers);
        Check(species);
        foreach (var (low, high) in blockers)
        {
            if (!(low.X <= high.X && low.Y <= high.Y && low.Z <= high.Z)) throw new ArgumentOutOfRangeException(nameof(blockers), $"A box runs from its lowest corner to its highest, and one runs from {low} to {high}.");
        }

        return new TreeGrowth(species, seed, blockers).Grow();
    }

    /// <summary>Makes a tree's skeleton into a mesh of its stems.</summary>
    /// <param name="skeleton">The skeleton.</param>
    /// <returns>A new mesh, every face in <see cref="BarkSlot"/>, colored as bark.</returns>
    /// <remarks>
    /// <para>
    /// Each stem is one <see cref="EditMesh.Tube"/> through its points, a stem carried on through a
    /// point by the thickest child there and every other child starting a stem of its own at the
    /// point it leaves. A stem's tube starts on its parent's middle, inside its parent, since a
    /// child is never thicker than the stem it leaves, so the joint is closed with no boolean.
    /// </para>
    /// <para>
    /// A stem has more sides the thicker it is against the trunk, from twelve for the trunk down to
    /// three for the thinnest twigs, so the triangles go where they are seen.
    /// </para>
    /// </remarks>
    public static EditMesh Build(TreeSkeleton skeleton)
    {
        ArgumentNullException.ThrowIfNull(skeleton);
        var nodes = skeleton.Nodes;

        // The child that carries each point's stem on, the thickest, the first of equals.
        var carried = new int[nodes.Count];
        Array.Fill(carried, -1);
        for (var i = 1; i < nodes.Count; i++)
        {
            var parent = nodes[i].Parent;
            if (carried[parent] < 0 || nodes[i].Radius > nodes[carried[parent]].Radius) carried[parent] = i;
        }

        var mesh = new EditMesh()
            .SlotRename(EditMesh.DefaultSlot, BarkSlot)
            .SlotColor(BarkSlot, 0.40f, 0.34f, 0.28f)
            .SlotRoughness(BarkSlot, 0.9f);

        var trunk = nodes[0].Radius;
        var points = new List<Vec3>();
        var radii = new List<float>();
        for (var start = 0; start < nodes.Count; start++)
        {
            var parent = nodes[start].Parent;
            if (parent >= 0 && carried[parent] == start) continue;

            points.Clear();
            radii.Clear();
            if (parent >= 0)
            {
                points.Add(nodes[parent].Place);
                radii.Add(nodes[start].Radius);
            }

            for (var node = start; node >= 0; node = carried[node])
            {
                points.Add(nodes[node].Place);
                radii.Add(nodes[node].Radius);
            }

            if (points.Count < 2) continue;
            var sides = Math.Clamp(2 + (int)MathF.Round(10f * MathF.Sqrt(radii[0] / trunk)), 3, 12);
            mesh.Tube(points, radii, sides);
        }

        return mesh.SelectNone();
    }

    private static void Check(TreeSpecies species)
    {
        Finite(species.Scale, nameof(species.Scale), above: 0f);
        if (!(species.ScaleVariation >= 0f && species.ScaleVariation <= species.Scale))
        {
            throw new ArgumentOutOfRangeException(nameof(species), species.ScaleVariation, "A species' scale variation is from zero to its scale.");
        }

        if (!Enum.IsDefined(species.Shape)) throw new ArgumentOutOfRangeException(nameof(species), species.Shape, "A species' shape is one of TreeShape's.");
        if (!(species.BaseSize >= 0f && species.BaseSize < 1f)) throw new ArgumentOutOfRangeException(nameof(species), species.BaseSize, "A species' base size is from zero up to one.");
        if (species.BaseSplits < 0) throw new ArgumentOutOfRangeException(nameof(species), species.BaseSplits, "A species' base splits are none or more.");
        Finite(species.Ratio, nameof(species.Ratio), above: 0f);
        Finite(species.Flare, nameof(species.Flare), above: 0f, orEqual: true);
        Finite(species.AttractionUp, nameof(species.AttractionUp));
        if (!(species.PruneRatio is >= 0f and <= 1f)) throw new ArgumentOutOfRangeException(nameof(species), species.PruneRatio, "A species' prune ratio is from zero to one.");
        Finite(species.PruneWidth, nameof(species.PruneWidth), above: 0f);
        if (!(species.PruneWidthPeak is > 0f and <= 1f)) throw new ArgumentOutOfRangeException(nameof(species), species.PruneWidthPeak, "A species' prune width peak is above zero and up to one.");
        Finite(species.PrunePowerLow, nameof(species.PrunePowerLow), above: 0f, orEqual: true);
        Finite(species.PrunePowerHigh, nameof(species.PrunePowerHigh), above: 0f, orEqual: true);

        var levels = species.Levels ?? throw new ArgumentOutOfRangeException(nameof(species), "A species has its levels.");
        if (levels.Count is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(species), levels.Count, "A species has from one level, its trunk, to four.");

        for (var i = 0; i < levels.Count; i++)
        {
            var level = levels[i] ?? throw new ArgumentOutOfRangeException(nameof(species), $"Level {i} of the species is missing.");
            foreach (var (value, name) in (ReadOnlySpan<(float, string)>)[(level.DownAngle, nameof(level.DownAngle)), (level.DownAngleVariation, nameof(level.DownAngleVariation)), (level.Rotate, nameof(level.Rotate)), (level.Curve, nameof(level.Curve)), (level.CurveBack, nameof(level.CurveBack)), (level.CurveVariation, nameof(level.CurveVariation))])
            {
                Finite(value, $"level {i}'s {name}");
            }

            foreach (var (value, name) in (ReadOnlySpan<(float, string)>)[(level.RotateVariation, nameof(level.RotateVariation)), (level.Splits, nameof(level.Splits)), (level.SplitAngle, nameof(level.SplitAngle)), (level.SplitAngleVariation, nameof(level.SplitAngleVariation)), (level.Length, nameof(level.Length)), (level.LengthVariation, nameof(level.LengthVariation))])
            {
                Finite(value, $"level {i}'s {name}", above: 0f, orEqual: true);
            }

            if (level.SplitAngle > 180f) throw new ArgumentOutOfRangeException(nameof(species), level.SplitAngle, $"Level {i}'s split angle is up to 180 degrees.");
            if (level.Branches < 0) throw new ArgumentOutOfRangeException(nameof(species), level.Branches, $"Level {i}'s branches are none or more.");
            if (!(level.Taper is >= 0f and <= 3f)) throw new ArgumentOutOfRangeException(nameof(species), level.Taper, $"Level {i}'s taper is from zero to three.");
        }

        if (!(levels[0].LengthVariation < levels[0].Length)) throw new ArgumentOutOfRangeException(nameof(species), levels[0].LengthVariation, "The trunk's length is above zero and its variation below it.");

        if (!Enum.IsDefined(species.LeafShape)) throw new ArgumentOutOfRangeException(nameof(species), species.LeafShape, "A species' leaf shape is one of TreeLeafShape's.");
        Finite(species.LeafScale, nameof(species.LeafScale), above: 0f);
        Finite(species.LeafScaleX, nameof(species.LeafScaleX), above: 0f);
        if (!(species.LeafBend is >= 0f and <= 1f)) throw new ArgumentOutOfRangeException(nameof(species), species.LeafBend, "A species' leaf bend is from zero to one.");
        var leaf = species.LeafLevel ?? throw new ArgumentOutOfRangeException(nameof(species), "A species has its leaf level.");
        foreach (var (value, name) in (ReadOnlySpan<(float, string)>)[(leaf.DownAngle, nameof(leaf.DownAngle)), (leaf.DownAngleVariation, nameof(leaf.DownAngleVariation)), (leaf.Rotate, nameof(leaf.Rotate)), (leaf.RotateVariation, nameof(leaf.RotateVariation))])
        {
            Finite(value, $"leaf level's {name}");
        }
    }

    private static void Finite(float value, string name, float? above = null, bool orEqual = false)
    {
        var fits = float.IsFinite(value) && (above is not { } least || value > least || (orEqual && value == least));
        if (!fits)
        {
            var range = above is { } bound ? (orEqual ? $" and {bound} or more" : $" and above {bound}") : "";
            throw new ArgumentOutOfRangeException("species", value, $"A species' {name} is a finite number{range}.");
        }
    }
}
