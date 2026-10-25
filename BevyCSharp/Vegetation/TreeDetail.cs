namespace Bevy;

/// <summary>
/// How much of a tree a mesh draws, the whole of it near the camera or less of it further off.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Trees.Build(TreeSkeleton, TreeDetail)"/> and
/// <see cref="Trees.BuildLeaves(TreeSkeleton, TreeSpecies, uint, TreeDetail)"/> make each level, and
/// a planted <see cref="Tree"/> is drawn in both, the reduced one taking over as the tree stands
/// further off, the two dithered between so neither pops.
/// </para>
/// <para>
/// The reduced level draws what a tree a few of its heights away still shows. Its stems have half
/// the sides, and the stems thinner than a twenty-fifth of the trunk are left out, which keeps a
/// fifth to two fifths of the presets' triangles of stem, the twigs among the leaves being most of
/// a tree's stems and hidden by them there. It carries every other leaf of the full level at the
/// same place, each drawn the square root of two larger about its stalk, so the crown covers as
/// much with half the cards. A frond is the whole of a fern's leaf and is kept.
/// </para>
/// </remarks>
public enum TreeDetail
{
    /// <summary>Every stem with all its sides and every leaf.</summary>
    Full,

    /// <summary>Half the sides, the thinnest stems left out, and half the leaves, each larger.</summary>
    Reduced,
}
