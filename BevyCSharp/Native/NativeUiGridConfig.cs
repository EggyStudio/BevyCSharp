using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>The tracks a grid is laid out on.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeUiGridConfig
{
    /// <summary>0 along the row, 1 down the column, 2 and 3 the same while backfilling.</summary>
    public int AutoFlow;

    /// <summary>The rows stated up front.</summary>
    public NativeGridTrack* Rows;

    /// <summary>How many of them.</summary>
    public int RowCount;

    /// <summary>The columns stated up front.</summary>
    public NativeGridTrack* Columns;

    /// <summary>How many of them.</summary>
    public int ColumnCount;

    /// <summary>The rows made for items placed past the ones stated.</summary>
    public NativeGridTrack* AutoRows;

    /// <summary>How many of them.</summary>
    public int AutoRowCount;

    /// <summary>The columns made the same way.</summary>
    public NativeGridTrack* AutoColumns;

    /// <summary>How many of them.</summary>
    public int AutoColumnCount;

    /// <summary>How an item sits across its cell.</summary>
    public int JustifyItems;
}
