namespace Bevy;

/// <summary>
/// The tracks a grid is laid out on.
/// </summary>
/// <remarks>
/// <para>
/// A second layout algorithm rather than more fields on the first. Flexbox lays a run of children
/// out along one axis and decides the other from what they are; a grid states both axes up front
/// and drops the children into the cells, which makes a column line up with the column above it. An
/// inventory, a calendar and a table of settings are all grids.
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
    /// Suits a list of unknown length, where the columns are stated and the rows are not.
    /// </remarks>
    public IReadOnlyList<Track>? AutoRows { get; set; }

    /// <summary>The columns made the same way.</summary>
    public IReadOnlyList<Track>? AutoColumns { get; set; }

    /// <summary>How an item sits across its cell, unless it says otherwise itself.</summary>
    public CellAlign Align { get; set; } = CellAlign.Default;
}
