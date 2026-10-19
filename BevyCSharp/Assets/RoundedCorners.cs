namespace Bevy;

/// <summary>
/// Which corners of a Feathers control are rounded, Bevy's <c>RoundedCorners</c>, so buttons side
/// by side join into one group with its outer corners rounded.
/// </summary>
public enum RoundedCorners
{
    /// <summary>No corner.</summary>
    None = 0,

    /// <summary>Every corner, the usual control.</summary>
    All = 1,

    /// <summary>The top left corner.</summary>
    TopLeft = 2,

    /// <summary>The top right corner.</summary>
    TopRight = 3,

    /// <summary>The bottom right corner.</summary>
    BottomRight = 4,

    /// <summary>The bottom left corner.</summary>
    BottomLeft = 5,

    /// <summary>The two top corners.</summary>
    Top = 6,

    /// <summary>The two right corners, the last of a row of buttons.</summary>
    Right = 7,

    /// <summary>The two bottom corners.</summary>
    Bottom = 8,

    /// <summary>The two left corners, the first of a row of buttons.</summary>
    Left = 9,
}
