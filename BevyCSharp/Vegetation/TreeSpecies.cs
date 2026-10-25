namespace Bevy;

/// <summary>
/// What a kind of tree is, by Weber and Penn's controls of 1995, from which <see cref="Trees.Grow"/>
/// grows a tree of that kind for each seed.
/// </summary>
/// <remarks>
/// <para>
/// The paper's controls are the ones an artist sets in the tools that took them up, the crown's
/// <see cref="Shape"/>, how big the tree is and how much one varies from the next, how much of the
/// trunk is bare, how thick the trunk is for its height, and for the trunk and each level of
/// branches the angles, bends, forks, counts and lengths of <see cref="TreeLevel"/>. A tree's
/// branches are grown within them rather than drawn from them, so the crown fills the space it is
/// given as a grown tree's does, and two seeds of one species are two trees of that kind.
/// </para>
/// <para>
/// The measures are in meters and degrees, the tree standing up the Y axis from the origin, and a
/// species is a value, so one is made from another with <c>with</c> and a change to it changes
/// every tree grown from it after.
/// </para>
/// </remarks>
public sealed record TreeSpecies
{
    /// <summary>The outline of the crown, <see cref="TreeShape.Conical"/> unless set.</summary>
    public TreeShape Shape { get; init; } = TreeShape.Conical;

    /// <summary>How tall the tree is in meters, before the trunk's <see cref="TreeLevel.Length"/>, 10 unless set.</summary>
    public float Scale { get; init; } = 10f;

    /// <summary>How far in meters <see cref="Scale"/> varies either way from one tree to the next, 0 unless set.</summary>
    public float ScaleVariation { get; init; }

    /// <summary>The share of the trunk's height below the crown, which carries no branches, 0.3 unless set.</summary>
    public float BaseSize { get; init; } = 0.3f;

    /// <summary>The radius of the trunk at the ground against its height, 0.015 unless set.</summary>
    /// <remarks>
    /// The pipe model sets how thick each stem is against the trunk, and this sets how thick the
    /// trunk is, so a tree of ten meters at 0.015 is thirty centimeters across at the ground and
    /// every branch is as much thicker or thinner as the trunk.
    /// </remarks>
    public float Ratio { get; init; } = 0.015f;

    /// <summary>
    /// How strongly the branches turn upward as they grow, below zero turning them downward, 0 unless
    /// set. The trunk does not.
    /// </summary>
    /// <remarks>
    /// A few tenths turns a branch's tip upward over its length, as an aspen's, and a few units below
    /// zero hangs the branches down, as a weeping willow's.
    /// </remarks>
    public float AttractionUp { get; init; }

    /// <summary>
    /// The trunk and the levels of branches beneath it, from one, a trunk alone, to four, a trunk
    /// with three levels of branches. A trunk with two levels unless set.
    /// </summary>
    public IReadOnlyList<TreeLevel> Levels { get; init; } =
    [
        new TreeLevel { CurveVariation = 10f },
        new TreeLevel { DownAngle = 60f, Branches = 40, Length = 0.35f, Curve = -20f, CurveVariation = 30f },
        new TreeLevel { DownAngle = 45f, Branches = 16, Length = 0.5f, CurveVariation = 40f },
    ];

    /// <summary>Whether another species has the same controls, its levels compared one by one.</summary>
    /// <param name="other">The other species.</param>
    public bool Equals(TreeSpecies? other) =>
        other is not null && Shape == other.Shape && Scale.Equals(other.Scale) && ScaleVariation.Equals(other.ScaleVariation)
        && BaseSize.Equals(other.BaseSize) && Ratio.Equals(other.Ratio) && AttractionUp.Equals(other.AttractionUp)
        && (ReferenceEquals(Levels, other.Levels) || (Levels is not null && other.Levels is not null && Levels.SequenceEqual(other.Levels)));

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Shape, Scale, ScaleVariation, BaseSize, Ratio, AttractionUp, Levels?.Count ?? 0);
}
