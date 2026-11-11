namespace Bevy;

/// <summary>
/// Where an item sits on its parent's grid.
/// </summary>
/// <remarks>
/// Grid lines are counted from one, and a negative counts back from the far edge, so a column of
/// <c>-1</c> is the last one whatever the grid turned out to be. Zero leaves the item where the
/// flow would have put it, as an item that only spans two columns says.
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
