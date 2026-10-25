namespace Bevy;

// The species the library carries, the paper's four from its appendix with the paper's values, and
// a bush and a fern of the engine's own on the same controls. The paper counts splits a segment
// and these a stem, so each of its split counts is multiplied by its curve resolution for that
// level, as TreeLevel.Splits says.
public sealed partial record TreeSpecies
{
    /// <summary>The quaking aspen of Weber and Penn's appendix, a tall slender tree of 13 meters, three either way, with a narrow crown over a bare trunk.</summary>
    public static TreeSpecies QuakingAspen { get; } = new()
    {
        Shape = TreeShape.TendFlame,
        Scale = 13f,
        ScaleVariation = 3f,
        BaseSize = 0.4f,
        Ratio = 0.015f,
        Flare = 0.6f,
        AttractionUp = 0.5f,
        BarkSet = "silver-birch-bark",
        LeafSet = "silver-birch-leaves",
        BarkColor = Color.FromSrgb(0.80f, 0.79f, 0.74f),
        Leaves = 25,
        LeafScale = 0.17f,
        LeafLevel = new TreeLevel { DownAngle = 45f, DownAngleVariation = 10f, Rotate = 77f },
        LeafColor = Color.FromSrgb(0.33f, 0.48f, 0.18f),
        FallColor = Color.FromSrgb(0.95f, 0.78f, 0.20f),
        Levels =
        [
            new TreeLevel { CurveVariation = 20f },
            new TreeLevel { DownAngle = 60f, DownAngleVariation = -50f, Rotate = 140f, Branches = 50, Length = 0.3f, Curve = -40f, CurveVariation = 50f },
            new TreeLevel { DownAngle = 45f, DownAngleVariation = 10f, Rotate = 140f, Branches = 30, Length = 0.6f, Curve = -40f, CurveVariation = 75f },
        ],
    };

    /// <summary>The black tupelo of Weber and Penn's appendix, 23 meters, five either way, a straight trunk to the top with level branches all the way up.</summary>
    public static TreeSpecies BlackTupelo { get; } = new()
    {
        Shape = TreeShape.TaperedCylindrical,
        Scale = 23f,
        ScaleVariation = 5f,
        BaseSize = 0.2f,
        Ratio = 0.015f,
        Flare = 1f,
        AttractionUp = 0.5f,
        BarkSet = "broadleaf-bark",
        LeafSet = "red-alder-leaves",
        BarkColor = Color.FromSrgb(0.36f, 0.33f, 0.30f),
        Leaves = 6,
        LeafScale = 0.3f,
        LeafScaleX = 0.5f,
        LeafLevel = new TreeLevel { DownAngle = 45f, DownAngleVariation = 10f, Rotate = 140f },
        LeafColor = Color.FromSrgb(0.22f, 0.38f, 0.12f),
        FallColor = Color.FromSrgb(0.75f, 0.15f, 0.08f),
        Levels =
        [
            new TreeLevel { Taper = 1.1f, CurveVariation = 40f },
            new TreeLevel { DownAngle = 60f, DownAngleVariation = -40f, Rotate = 140f, Branches = 50, Length = 0.3f, LengthVariation = 0.05f, CurveVariation = 90f },
            new TreeLevel { DownAngle = 30f, DownAngleVariation = 10f, Rotate = 140f, Branches = 25, Length = 0.6f, LengthVariation = 0.1f, Curve = -10f, CurveVariation = 150f },
            new TreeLevel { DownAngle = 45f, DownAngleVariation = 10f, Rotate = 140f, Branches = 12, Length = 0.4f },
        ],
    };

    /// <summary>
    /// The weeping willow of Weber and Penn's appendix, 12 meters, four either way, its trunk parting
    /// low into three and its branches arching out and hanging in curtains to near the ground, held to
    /// the paper's pruning envelope.
    /// </summary>
    public static TreeSpecies WeepingWillow { get; } = new()
    {
        Shape = TreeShape.Cylindrical,
        Scale = 15f,
        ScaleVariation = 5f,
        BaseSize = 0.05f,
        BaseSplits = 2,
        Ratio = 0.03f,
        Flare = 0.75f,
        AttractionUp = -3f,
        PruneRatio = 1f,
        PruneWidth = 0.4f,
        PruneWidthPeak = 0.6f,
        PrunePowerLow = 0.001f,
        PrunePowerHigh = 0.5f,
        BarkSet = "weeping-willow-bark",
        LeafSet = "weeping-willow-leaves",
        BarkColor = Color.FromSrgb(0.40f, 0.35f, 0.29f),
        Leaves = 15,
        LeafScale = 0.12f,
        LeafScaleX = 0.2f,
        LeafLevel = new TreeLevel { DownAngle = 20f, DownAngleVariation = 10f, Rotate = 140f },
        LeafColor = Color.FromSrgb(0.45f, 0.55f, 0.25f),
        FallColor = Color.FromSrgb(0.85f, 0.78f, 0.30f),
        Levels =
        [
            new TreeLevel { Length = 0.8f, Splits = 0.8f, SplitAngle = 3f, CurveBack = 20f, CurveVariation = 120f },
            new TreeLevel { DownAngle = 20f, DownAngleVariation = 10f, Rotate = -120f, RotateVariation = 30f, Branches = 25, Length = 0.5f, LengthVariation = 0.1f, Splits = 3.2f, SplitAngle = 30f, SplitAngleVariation = 10f, Curve = 40f, CurveBack = 80f, CurveVariation = 90f },
            new TreeLevel { DownAngle = 30f, DownAngleVariation = 10f, Rotate = -120f, RotateVariation = 30f, Branches = 10, Length = 1.5f, Splits = 2.4f, SplitAngle = 45f, SplitAngleVariation = 20f },
            new TreeLevel { DownAngle = 20f, DownAngleVariation = 10f, Rotate = 140f, Branches = 300, Length = 0.1f },
        ],
    };

    /// <summary>
    /// The California black oak of Weber and Penn's appendix, 10 meters, ten either way, its trunk
    /// parting low and forking on into a broad rounded crown of crooked limbs.
    /// </summary>
    public static TreeSpecies CaliforniaBlackOak { get; } = new()
    {
        Shape = TreeShape.Hemispherical,
        Scale = 10f,
        ScaleVariation = 10f,
        BaseSize = 0.05f,
        BaseSplits = 2,
        Ratio = 0.018f,
        Flare = 1.2f,
        AttractionUp = 0.8f,
        BarkSet = "broadleaf-bark",
        LeafSet = "shingle-oak-leaves",
        BarkColor = Color.FromSrgb(0.30f, 0.27f, 0.24f),
        Leaves = 25,
        LeafScale = 0.12f,
        LeafScaleX = 0.66f,
        LeafLevel = new TreeLevel { DownAngle = 45f, DownAngleVariation = 10f, Rotate = 140f },
        LeafColor = Color.FromSrgb(0.28f, 0.40f, 0.14f),
        FallColor = Color.FromSrgb(0.65f, 0.38f, 0.12f),
        Levels =
        [
            new TreeLevel { Taper = 0.95f, Splits = 3.2f, SplitAngle = 10f, CurveVariation = 90f },
            new TreeLevel { DownAngle = 30f, DownAngleVariation = -30f, Rotate = 80f, Branches = 40, Length = 0.8f, LengthVariation = 0.1f, Splits = 2f, SplitAngle = 10f, SplitAngleVariation = 10f, Curve = 40f, CurveBack = -70f, CurveVariation = 150f },
            new TreeLevel { DownAngle = 45f, DownAngleVariation = 10f, Rotate = 140f, Branches = 120, Length = 0.2f, LengthVariation = 0.05f, Splits = 0.3f, SplitAngle = 10f, SplitAngleVariation = 10f, CurveVariation = -30f },
        ],
    };

    /// <summary>A round shrub of a meter and a half, its stems parting at the ground and branching into a dense ball.</summary>
    public static TreeSpecies Bush { get; } = new()
    {
        Shape = TreeShape.Spherical,
        Scale = 1.5f,
        ScaleVariation = 0.4f,
        BaseSize = 0f,
        BaseSplits = 6,
        Ratio = 0.02f,
        AttractionUp = 0.4f,
        BarkSet = "hornbeam-bark",
        LeafSet = "hornbeam-leaves",
        BarkColor = Color.FromSrgb(0.42f, 0.40f, 0.33f),
        Leaves = 12,
        LeafScale = 0.07f,
        LeafScaleX = 0.6f,
        LeafLevel = new TreeLevel { DownAngle = 50f, DownAngleVariation = 15f, Rotate = 137.5f },
        LeafColor = Color.FromSrgb(0.30f, 0.45f, 0.14f),
        FallColor = Color.FromSrgb(0.80f, 0.62f, 0.20f),
        Levels =
        [
            new TreeLevel { SplitAngle = 18f, SplitAngleVariation = 6f, CurveVariation = 30f },
            new TreeLevel { DownAngle = 40f, DownAngleVariation = 15f, Rotate = 140f, Branches = 20, Length = 0.45f, LengthVariation = 0.1f, Curve = -15f, CurveVariation = 50f },
            new TreeLevel { DownAngle = 45f, DownAngleVariation = 15f, Rotate = 140f, Branches = 10, Length = 0.5f, CurveVariation = 60f },
        ],
    };

    /// <summary>A fern, a short stock with fronds of a meter arching out from it all round, each drawn as a frond along its stem.</summary>
    public static TreeSpecies Fern { get; } = new()
    {
        Shape = TreeShape.Cylindrical,
        Scale = 1f,
        ScaleVariation = 0.2f,
        BaseSize = 0f,
        Ratio = 0.1f,
        LeafSet = "fern-leaves",
        BarkColor = Color.FromSrgb(0.32f, 0.40f, 0.20f),
        Leaves = -1,
        LeafScale = 0.3f,
        LeafColor = Color.FromSrgb(0.25f, 0.42f, 0.15f),
        FallColor = Color.FromSrgb(0.55f, 0.40f, 0.20f),
        Levels =
        [
            new TreeLevel { Length = 0.1f },
            new TreeLevel { DownAngle = 25f, DownAngleVariation = -15f, Rotate = 137.5f, Branches = 14, Length = 10f, LengthVariation = 1.5f, Curve = 60f, CurveVariation = 15f },
        ],
    };
}
