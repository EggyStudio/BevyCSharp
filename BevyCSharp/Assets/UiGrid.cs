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

    /// <summary>The smallest its contents can be squeezed to.</summary>
    public static Track MinContent => new(4, 0f, 1);

    /// <summary>The largest its contents would like to be.</summary>
    public static Track MaxContent => new(5, 0f, 1);

    /// <summary>The same track, stated <paramref name="count"/> times over.</summary>
    /// <remarks>
    /// One entry rather than a list of identical ones, which is what a stylesheet's <c>repeat()</c>
    /// is for and what a grid of twelve equal columns wants to say.
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

/// <summary>Which way a grid puts an item that was given no place of its own.</summary>
public enum GridFlow
{
    /// <summary>Along the row, starting a new one when it runs out.</summary>
    Row = 0,

    /// <summary>Down the column.</summary>
    Column = 1,

    /// <summary>Along the row, backfilling any earlier gap the item fits in.</summary>
    RowDense = 2,

    /// <summary>Down the column, backfilling the same way.</summary>
    ColumnDense = 3,
}

/// <summary>How an item sits across the cell it was placed in.</summary>
public enum CellAlign
{
    /// <summary>Whatever the layout would do, which is to fill the cell.</summary>
    Default = 0,

    /// <summary>Against the start of the cell.</summary>
    Start = 1,

    /// <summary>Against the end of it.</summary>
    End = 2,

    /// <summary>In the middle.</summary>
    Center = 3,

    /// <summary>Lined up with its neighbors' first line of text.</summary>
    Baseline = 4,

    /// <summary>Stretched to fill the cell.</summary>
    Stretch = 5,
}

/// <summary>
/// Where an item sits on its parent's grid.
/// </summary>
/// <remarks>
/// Grid lines are counted from one, and a negative counts back from the far edge, so a column of
/// <c>-1</c> is the last one whatever the grid turned out to be. Zero leaves the item where the
/// flow would have put it, which is what an item that only wants to span two columns says.
/// </remarks>
public sealed class GridPlacement
{
    /// <summary>The row line the item starts at, or zero to let the flow place it.</summary>
    public int Row { get; set; }

    /// <summary>How many rows it covers. Zero and one are both one row.</summary>
    public int RowSpan { get; set; }

    /// <summary>The column line the item starts at, or zero to let the flow place it.</summary>
    public int Column { get; set; }

    /// <summary>How many columns it covers.</summary>
    public int ColumnSpan { get; set; }

    /// <summary>How this one item sits across its cell, overriding the grid's own answer.</summary>
    public CellAlign Align { get; set; } = CellAlign.Default;
}

/// <summary>
/// The tracks a grid is laid out on.
/// </summary>
/// <remarks>
/// <para>
/// A second layout algorithm rather than more fields on the first. Flexbox lays a run of children
/// out along one axis and decides the other from what they are; a grid states both axes up front
/// and drops the children into the cells, which is what makes a column line up with the column
/// above it. An inventory, a calendar and a table of settings are all grids.
/// </para>
/// <para>
/// A list left out keeps whatever the node had, so the rows and the columns can be set in separate
/// calls.
/// </para>
/// </remarks>
public sealed class GridSettings
{
    /// <summary>Which way an item with no place of its own is put next.</summary>
    public GridFlow Flow { get; set; } = GridFlow.Row;

    /// <summary>The rows stated up front.</summary>
    public IReadOnlyList<Track>? Rows { get; set; }

    /// <summary>The columns stated up front.</summary>
    public IReadOnlyList<Track>? Columns { get; set; }

    /// <summary>
    /// The rows made for an item placed past the ones stated.
    /// </summary>
    /// <remarks>
    /// Used in turn and then from the start again, so one entry sizes every row the grid grows.
    /// What a list of unknown length wants, where the columns are stated and the rows are not.
    /// </remarks>
    public IReadOnlyList<Track>? AutoRows { get; set; }

    /// <summary>The columns made the same way.</summary>
    public IReadOnlyList<Track>? AutoColumns { get; set; }

    /// <summary>How an item sits across its cell, unless it says otherwise itself.</summary>
    public CellAlign Align { get; set; } = CellAlign.Default;
}

/// <summary>Laying a node's children out on a grid.</summary>
public static unsafe class UiGrid
{
    /// <summary>
    /// Lays a node's children out on a grid.
    /// </summary>
    /// <remarks>
    /// Applied to a node that already exists rather than passed with one, because the tracks are
    /// lists and a list has no room in the flat settings every other field arrives in. The node is
    /// told to lay out as a grid here, so nothing has to set <see cref="UiSettings.Display"/> as
    /// well.
    /// </remarks>
    /// <param name="node">The node whose children are laid out.</param>
    /// <param name="settings">The tracks and how they are filled.</param>
    /// <exception cref="BevyNativeException">The node is gone, or this build has no renderer.</exception>
    /// <example>
    /// <code>
    /// UiGrid.Set(panel, new GridSettings
    /// {
    ///     Columns = [Track.Px(96f).Filling(), ],
    ///     AutoRows = [Track.Px(96f)],
    /// });
    /// </code>
    /// </example>
    public static void Set(Entity node, GridSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var rows = Described(settings.Rows);
        var columns = Described(settings.Columns);
        var autoRows = Described(settings.AutoRows);
        var autoColumns = Described(settings.AutoColumns);

        // Pinned rather than copied to the stack, because the lists are as long as the caller made
        // them and a grid of a hundred columns is a reasonable thing to ask for.
        fixed (NativeGridTrack* rowsAt = rows)
        fixed (NativeGridTrack* columnsAt = columns)
        fixed (NativeGridTrack* autoRowsAt = autoRows)
        fixed (NativeGridTrack* autoColumnsAt = autoColumns)
        {
            var config = new NativeUiGridConfig
            {
                AutoFlow = (int)settings.Flow,
                Rows = rowsAt,
                RowCount = rows?.Length ?? 0,
                Columns = columnsAt,
                ColumnCount = columns?.Length ?? 0,
                AutoRows = autoRowsAt,
                AutoRowCount = autoRows?.Length ?? 0,
                AutoColumns = autoColumnsAt,
                AutoColumnCount = autoColumns?.Length ?? 0,
                JustifyItems = (int)settings.Align,
            };

            Interop.Native.Check(
                Interop.Native.bcs_ui_set_grid(node.Bits, &config),
                $"laying {node} out as a grid");
        }
    }

    /// <summary>
    /// Places one child on its parent's grid.
    /// </summary>
    /// <remarks>
    /// Only a child of a node that <see cref="Set"/> was called on has a grid to be placed on. A
    /// child with no placement of its own is put wherever the flow reaches next, which is what most
    /// of them want.
    /// </remarks>
    /// <param name="node">The child being placed.</param>
    /// <param name="placement">Where it sits.</param>
    /// <exception cref="BevyNativeException">The node is gone, or this build has no renderer.</exception>
    public static void Place(Entity node, GridPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(placement);

        Interop.Native.Check(
            Interop.Native.bcs_ui_set_grid_placement(
                node.Bits,
                placement.Row,
                placement.RowSpan,
                placement.Column,
                placement.ColumnSpan,
                (int)placement.Align),
            $"placing {node} on a grid");
    }

    /// <summary>One list of tracks as the bridge takes it, or nothing where none was given.</summary>
    private static NativeGridTrack[]? Described(IReadOnlyList<Track>? tracks)
    {
        if (tracks is null || tracks.Count == 0) return null;

        var native = new NativeGridTrack[tracks.Count];

        for (var i = 0; i < tracks.Count; i++)
        {
            native[i] = new NativeGridTrack
            {
                Kind = tracks[i].Kind,
                Value = tracks[i].Value,
                Repeat = tracks[i].Repeat,
            };
        }

        return native;
    }
}
