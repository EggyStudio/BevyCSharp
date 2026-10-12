namespace Bevy;

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
