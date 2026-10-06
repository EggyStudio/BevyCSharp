namespace Bevy;

/// <summary>
/// Which way a node stacks its children.
/// </summary>
/// <remarks>
/// A node lays its children out along one axis. This is the axis, and everything else in the
/// layout is described relative to it: <see cref="UiSettings.Justify"/> spreads them along it,
/// <see cref="UiSettings.Align"/> places them across it.
/// </remarks>
public enum UiDirection
{
    /// <summary>Left to right, which is the default.</summary>
    Row = 0,

    /// <summary>Top to bottom, for a menu or a list.</summary>
    Column = 1,

    /// <summary>Right to left.</summary>
    RowReverse = 2,

    /// <summary>Bottom to top.</summary>
    ColumnReverse = 3,
}
