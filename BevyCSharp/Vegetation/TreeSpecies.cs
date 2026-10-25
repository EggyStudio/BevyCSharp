namespace Bevy;

/// <summary>
/// What a kind of tree is, by Weber and Penn's controls of 1995, from which <see cref="Trees.Grow(TreeSpecies, uint)"/>
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
public sealed partial record TreeSpecies
{
    /// <summary>The outline of the crown, <see cref="TreeShape.Conical"/> unless set.</summary>
    public TreeShape Shape { get; init; } = TreeShape.Conical;

    /// <summary>How tall the tree is in meters, before the trunk's <see cref="TreeLevel.Length"/>, 10 unless set.</summary>
    public float Scale { get; init; } = 10f;

    /// <summary>
    /// How far in meters <see cref="Scale"/> varies either way from one tree to the next, up to the
    /// scale itself, 0 unless set. A tree is a tenth of its scale at the least.
    /// </summary>
    public float ScaleVariation { get; init; }

    /// <summary>The share of the scale below the crown, a bare trunk that carries no branches, 0.3 unless set.</summary>
    public float BaseSize { get; init; } = 0.3f;

    /// <summary>
    /// How many more trunks the trunk splits into at the crown's foot, 0 unless set, spread evenly
    /// round it and leaning out by the trunk's <see cref="TreeLevel.SplitAngle"/>, as an oak's or a
    /// willow's trunk parts low down.
    /// </summary>
    public int BaseSplits { get; init; }

    /// <summary>The radius of the trunk at the ground against its height, 0.015 unless set.</summary>
    /// <remarks>
    /// The pipe model sets how thick each stem is against the trunk, and this sets how thick the
    /// trunk is, so a tree of ten meters at 0.015 is thirty centimeters across at the ground and
    /// every branch is as much thicker or thinner as the trunk.
    /// </remarks>
    public float Ratio { get; init; } = 0.015f;

    /// <summary>How much wider the trunk is at the ground, over its lowest eighth, 0 unless set, so at 1 its foot is twice as wide.</summary>
    public float Flare { get; init; }

    /// <summary>
    /// How strongly the branches turn upward as they grow, below zero turning them downward, 0 unless
    /// set. The trunk and the first level of branches do not, as the paper's do not, their curves
    /// standing for it.
    /// </summary>
    /// <remarks>
    /// At 1 a stem turns by its angle from upright over its length, so its last internode stands
    /// upright, at a few tenths its tip turns part of the way, as an aspen's, and a few units below
    /// zero hang its stems down, as a weeping willow's.
    /// </remarks>
    public float AttractionUp { get; init; }

    /// <summary>
    /// How far branches are held to the pruning envelope, from 0, not at all, to 1, a branch
    /// stopping where it leaves it, 0 unless set.
    /// </summary>
    /// <remarks>
    /// The envelope is a solid of revolution round the trunk, from the crown's foot to the top of the
    /// scale, as wide as <see cref="PruneWidth"/> of the scale at its peak, and narrowing below and
    /// above by its powers. A ratio between the two holds a branch at the envelope by that chance,
    /// and the trunk is never held.
    /// </remarks>
    public float PruneRatio { get; init; }

    /// <summary>
    /// The name of the bark set of <see cref="TreeTextures"/> the tree is drawn with, or nothing for
    /// the bark the generator makes, nothing unless set.
    /// </summary>
    /// <remarks>A name the collection does not carry draws the generated bark too, so a species may name a set a game adds later.</remarks>
    public string? BarkSet { get; init; }

    /// <summary>The name of the leaf set of <see cref="TreeTextures"/> the tree's leaves are cut from, or nothing for the generated leaves, nothing unless set.</summary>
    public string? LeafSet { get; init; }

    /// <summary>The bark's color where the generator makes it, a gray brown unless set.</summary>
    public Color BarkColor { get; init; } = Color.FromSrgb(0.42f, 0.36f, 0.30f);

    /// <summary>
    /// How many leaves each stem of the last level carries, as the paper's leaves, 0 unless set, so
    /// a species without them is bare. Below zero, each stem of the last level is itself a leaf
    /// that many times over, a frond drawn along its length, as a fern's or a palm's is.
    /// </summary>
    /// <remarks>
    /// The leaves sit along the twigs, the stems that carry no others, more of them toward each
    /// twig's tip as the paper places them, so a crown is leafy at its outside, and a twig shorter
    /// than most carries fewer.
    /// </remarks>
    public int Leaves { get; init; }

    /// <summary>The outline of the leaves the generator makes where no leaf set is named, <see cref="TreeLeafShape.Oval"/> unless set.</summary>
    public TreeLeafShape LeafShape { get; init; } = TreeLeafShape.Oval;

    /// <summary>How long a leaf is in meters, or how wide a frond is, 0.17 unless set.</summary>
    public float LeafScale { get; init; } = 0.17f;

    /// <summary>How wide a generated leaf is against its length, 1 unless set. A leaf cut from a set takes its picture's proportions.</summary>
    public float LeafScaleX { get; init; } = 1f;

    /// <summary>
    /// The angles the leaves leave their twig at, the down angle from the twig and the turn round it
    /// from one leaf to the next, as the paper gives its leaves the angles of the level past the last
    /// stems. Its other controls are not read.
    /// </summary>
    public TreeLevel LeafLevel { get; init; } = new() { DownAngle = 45f, DownAngleVariation = 10f, Rotate = 137.5f };

    /// <summary>How far each leaf turns to face up and outward from the tree, from 0 to 1, 0.5 unless set, as the paper's leaves turn toward the light.</summary>
    public float LeafBend { get; init; } = 0.5f;

    /// <summary>The leaves' color in summer, which tints the generated leaves, a mid green unless set.</summary>
    public Color LeafColor { get; init; } = Color.FromSrgb(0.30f, 0.45f, 0.15f);

    /// <summary>The leaves' color in the fall, toward which <see cref="Trees.LeafMaterial"/> turns them, a yellow brown unless set.</summary>
    public Color FallColor { get; init; } = Color.FromSrgb(0.80f, 0.55f, 0.15f);

    /// <summary>The pruning envelope's widest radius as a share of the scale, 0.5 unless set.</summary>
    public float PruneWidth { get; init; } = 0.5f;

    /// <summary>How far up the crown the pruning envelope is widest, from above 0, its foot, to 1, its top, 0.5 unless set.</summary>
    public float PruneWidthPeak { get; init; } = 0.5f;

    /// <summary>
    /// The curve of the pruning envelope below its peak, 0.5 unless set, under one rounding it out and
    /// over one in, a value near zero keeping it as wide down to the crown's foot.
    /// </summary>
    public float PrunePowerLow { get; init; } = 0.5f;

    /// <summary>The curve of the pruning envelope above its peak, 0.5 unless set, as <see cref="PrunePowerLow"/> is below it.</summary>
    public float PrunePowerHigh { get; init; } = 0.5f;

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
        && BaseSize.Equals(other.BaseSize) && BaseSplits == other.BaseSplits && Ratio.Equals(other.Ratio) && Flare.Equals(other.Flare)
        && AttractionUp.Equals(other.AttractionUp) && PruneRatio.Equals(other.PruneRatio) && PruneWidth.Equals(other.PruneWidth)
        && PruneWidthPeak.Equals(other.PruneWidthPeak) && PrunePowerLow.Equals(other.PrunePowerLow) && PrunePowerHigh.Equals(other.PrunePowerHigh)
        && BarkSet == other.BarkSet && LeafSet == other.LeafSet && BarkColor.Equals(other.BarkColor) && Leaves == other.Leaves
        && LeafShape == other.LeafShape && LeafScale.Equals(other.LeafScale) && LeafScaleX.Equals(other.LeafScaleX)
        && Equals(LeafLevel, other.LeafLevel) && LeafBend.Equals(other.LeafBend) && LeafColor.Equals(other.LeafColor) && FallColor.Equals(other.FallColor)
        && (ReferenceEquals(Levels, other.Levels) || (Levels is not null && other.Levels is not null && Levels.SequenceEqual(other.Levels)));

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Shape, Scale, ScaleVariation, BaseSize, Ratio, AttractionUp, Levels?.Count ?? 0);
}
