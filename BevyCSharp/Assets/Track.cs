namespace Bevy;

using System.Runtime.InteropServices;
using Bevy.Interop;

/// <summary>
/// How one row or column of a grid is sized.
/// </summary>
/// <remarks>
/// The same shape as <see cref="Length"/> and a different set of choices, because a track can be
/// sized by what it holds in ways a distance cannot.
/// </remarks>
public readonly record struct Track
{
    private Track(int kind, float value, int repeat)
    {
        Kind = kind;
        Value = value;
        Repeat = repeat;
    }

    /// <summary>Which of the kinds below this is.</summary>
    internal int Kind { get; }

    /// <summary>The number the kind reads, where it reads one.</summary>
    internal float Value { get; }

    /// <summary>How many times it repeats, or a negative asking for a fill.</summary>
    internal int Repeat { get; }

    /// <summary>As large as what it holds.</summary>
    public static Track Auto => new(0, 0f, 1);

    /// <summary>A fixed number of logical pixels.</summary>
    public static Track Px(float value) => new(1, value, 1);

    /// <summary>A percentage of the grid across that axis.</summary>
    public static Track Percent(float value) => new(2, value, 1);

    /// <summary>
    /// A share of whatever room is left after the fixed tracks have taken theirs.
    /// </summary>
    /// <remarks>
    /// What makes a column stretch. Two tracks of <c>Fr(1f)</c> and <c>Fr(3f)</c> split the
    /// leftover room one part to three, so the shares are relative to each other rather than to
    /// the grid.
    /// </remarks>
    public static Track Fr(float share) => new(3, share, 1);

    /// <summary>
    /// A share of the room left over, as <see cref="Fr"/> is, that may also be narrower than what
    /// it holds.
    /// </summary>
    /// <remarks>
    /// Bevy's <c>GridTrack::flex</c>, a stylesheet's <c>minmax(0, 1fr)</c>. An <see cref="Fr"/>
    /// track grows to fit the smallest its contents can be, so one holding something wide or a
    /// square sized by its height takes more than its share and pushes the tracks after it along.
    /// This one keeps its share and lets the contents overflow it, as equal columns and a row that
    /// fits the window need.
    /// </remarks>
    public static Track Flex(float share) => new(6, share, 1);

    /// <summary>The smallest its contents can be squeezed to.</summary>
    public static Track MinContent => new(4, 0f, 1);

    /// <summary>The largest its contents would like to be.</summary>
    public static Track MaxContent => new(5, 0f, 1);

    /// <summary>The same track, stated <paramref name="count"/> times over.</summary>
    /// <remarks>
    /// One entry rather than a list of identical ones, like a stylesheet's <c>repeat()</c>, for a
    /// grid of twelve equal columns.
    /// </remarks>
    public Track Repeated(int count) => new(Kind, Value, Math.Max(1, count));

    /// <summary>
    /// The same track, repeated as many times as the grid has room for.
    /// </summary>
    /// <remarks>
    /// What a gallery that reflows with its window is. Offered only for a track sized in pixels or
    /// in percent, because the rest have no size to divide the room by, and asked for on one of
    /// those it is read as once.
    /// </remarks>
    /// <param name="collapse">
    /// Whether to drop the tracks nothing landed in, so an empty row takes no space rather than
    /// standing there. The difference between a stylesheet's <c>auto-fit</c> and its
    /// <c>auto-fill</c>.
    /// </param>
    public Track Filling(bool collapse = false) => new(Kind, Value, collapse ? -2 : -1);

    /// <inheritdoc/>
    public override string ToString()
    {
        var track = Kind switch
        {
            1 => $"{Value}px",
            2 => $"{Value}%",
            3 => $"{Value}fr",
            6 => $"minmax(0, {Value}fr)",
            4 => "min",
            5 => "max",
            _ => "auto",
        };

        return Repeat switch
        {
            -1 => $"fill({track})",
            -2 => $"fit({track})",
            1 => track,
            _ => $"{Repeat} x {track}",
        };
    }
}
