namespace Bevy;

/// <summary>
/// How the lines of a run of text sit against each other.
/// </summary>
/// <remarks>
/// Alignment inside the text's own box, not where that box sits in its parent, which is
/// <see cref="UiSettings.Align"/> and <see cref="UiSettings.Justify"/>. It shows only once there
/// is more than one line, or the box is wider than the text.
/// </remarks>
public enum TextJustify
{
    /// <summary>Lines start at the left edge.</summary>
    Left = 0,

    /// <summary>Lines are centered on each other.</summary>
    Center = 1,

    /// <summary>Lines end at the right edge.</summary>
    Right = 2,

    /// <summary>Words are spaced so both edges line up, except on the last line.</summary>
    Justified = 3,

    /// <summary>The left in left-to-right text, the right in right-to-left.</summary>
    Start = 4,

    /// <summary>The right in left-to-right text, the left in right-to-left.</summary>
    End = 5,
}
