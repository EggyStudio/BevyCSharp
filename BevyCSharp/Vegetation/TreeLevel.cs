namespace Bevy;

/// <summary>
/// One level of a species' stems, its trunk or a level of its branches, with Weber and Penn's
/// controls of 1995 for the stems of that level.
/// </summary>
/// <remarks>
/// <para>
/// The paper names each control by its level, <c>1DownAngle</c> for the first level of branches,
/// and here a level is an entry of <see cref="TreeSpecies.Levels"/>, the trunk first. A level's
/// controls are for its own stems, how they leave the stem they grow from, how they bend, fork and
/// taper, how many there are on each stem of the level before and how long they are against it.
/// </para>
/// <para>
/// The paper draws each stem from these numbers alone, and <see cref="Trees.Grow"/> grows them
/// instead, so each control is a tendency the growth follows. The down and rotate angles are the
/// way a bud points as it is set, the curve and the attraction up turn a stem as it lengthens, the
/// splits are the chance its tip forks as it grows, the branches are how many buds a stem sets for
/// the next level, and the length is the most a stem of the level grows. Whether a bud grows and
/// how far depends on the room it finds, so two stems of one level differ as two branches of one
/// tree do.
/// </para>
/// </remarks>
public sealed record TreeLevel
{
    /// <summary>
    /// The angle in degrees between a stem of this level and the stem it leaves, as it leaves it, 45
    /// unless set. The trunk's is not read.
    /// </summary>
    public float DownAngle { get; init; } = 45f;

    /// <summary>
    /// How far in degrees <see cref="DownAngle"/> varies either way at random, 0 unless set.
    /// </summary>
    /// <remarks>
    /// Below zero, it varies instead along the stem the stems leave, as the paper's does, by this
    /// much more at the foot and up to six tenths of it less at the tip, so a stem that leaves its
    /// parent low down leaves it at a wider angle than one that leaves it high up.
    /// </remarks>
    public float DownAngleVariation { get; init; }

    /// <summary>
    /// The angle in degrees round the stem they leave from one stem of this level to the next, 140
    /// unless set, so they leave it on every side in a spiral.
    /// </summary>
    /// <remarks>
    /// Below zero, each leaves half a turn round from the last, this angle more and less by turns,
    /// so they stand on two sides, as the paper's do.
    /// </remarks>
    public float Rotate { get; init; } = 140f;

    /// <summary>How far in degrees <see cref="Rotate"/> varies either way at random, 0 unless set.</summary>
    public float RotateVariation { get; init; }

    /// <summary>
    /// How many degrees a stem of this level bends over its whole length, downward where above zero
    /// and upward below it, 0 unless set. The trunk bends to a side chosen at random.
    /// </summary>
    public float Curve { get; init; }

    /// <summary>How many degrees a stem of this level bends at random over its length, either way, 0 unless set.</summary>
    public float CurveVariation { get; init; }

    /// <summary>How many times a stem of this level forks along its length on average, 0 unless set.</summary>
    /// <remarks>
    /// The paper counts forks a segment, a stem being a few segments, and this counts them a stem,
    /// so the paper's number is multiplied by its curve resolution to give this one. A fork grows
    /// on as two stems of the same level, half the split angle either side of the way the one was
    /// growing, each able to grow the length the one had left.
    /// </remarks>
    public float Splits { get; init; }

    /// <summary>The angle in degrees between the two stems of a fork, 0 unless set.</summary>
    public float SplitAngle { get; init; }

    /// <summary>
    /// How many stems of this level each stem of the level before carries at most, 0 unless set.
    /// The trunk's is not read.
    /// </summary>
    /// <remarks>
    /// The stem before sets this many buds along itself, on the trunk only above
    /// <see cref="TreeSpecies.BaseSize"/>, and a bud grows into a stem only where it finds room, so
    /// the crown's inside, which the stems round it fill first, carries fewer.
    /// </remarks>
    public int Branches { get; init; }

    /// <summary>How long a stem of this level grows at most, against the stem it leaves, 1 unless set.</summary>
    /// <remarks>
    /// The trunk's is against <see cref="TreeSpecies.Scale"/>. The first level of branches is as long
    /// as this share of the trunk where the crown's <see cref="TreeSpecies.Shape"/> is widest, and
    /// shorter where it is narrower, and a level past it is shorter toward the tip of the stem it
    /// leaves, as the paper's are.
    /// </remarks>
    public float Length { get; init; } = 1f;

    /// <summary>How far <see cref="Length"/> varies either way at random, 0 unless set.</summary>
    public float LengthVariation { get; init; }

    /// <summary>
    /// How much a stem of this level narrows along its length, from 0, as thick at its tip as at its
    /// foot, to 1, thinning as its branches leave it, 1 unless set.
    /// </summary>
    /// <remarks>
    /// At 1 a stem is as thick at each point as the pipe model makes it, its cross section the sum
    /// of those of the stems it carries past that point, so it thins at each branch that leaves it
    /// and toward its tip. At 0 it keeps the thickness of its foot to its tip, and between the two
    /// it is between them.
    /// </remarks>
    public float Taper { get; init; } = 1f;
}
