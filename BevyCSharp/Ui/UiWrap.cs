namespace Bevy;

/// <summary>
/// Whether a node's children run onto more than one line.
/// </summary>
public enum UiWrap
{
    /// <summary>One line, however far past the edge it runs.</summary>
    NoWrap = 0,

    /// <summary>As many lines as the children need.</summary>
    Wrap = 1,

    /// <summary>The same, with each new line before the last rather than after it.</summary>
    WrapReverse = 2,
}
