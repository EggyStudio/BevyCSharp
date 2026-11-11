namespace Bevy;

/// <summary>
/// Which parts of a sliced picture tile rather than stretch.
/// </summary>
/// <remarks>
/// A nine-slice stretches by default, which is wrong for anything with a pattern in it, because a
/// border of dots drawn twice as wide becomes a border of ovals. Tiling repeats the slice instead,
/// which keeps a drawn edge looking drawn at every size. The repeat is measured by the same
/// <c>TileStretch</c> a whole tiled picture uses.
/// </remarks>
[Flags]
public enum SliceTiling
{
    /// <summary>Every part stretches, which is Bevy's own answer.</summary>
    None = 0,

    /// <summary>The four edges between the corners tile.</summary>
    Sides = 1,

    /// <summary>The middle tiles.</summary>
    Center = 2,

    /// <summary>Both, for a patterned panel.</summary>
    All = Sides | Center,
}
