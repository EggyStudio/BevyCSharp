namespace Bevy;

/// <summary>
/// Where a run of text may be broken when it does not fit on one line.
/// </summary>
/// <remarks>
/// The width it is broken against comes from the layout, so a node free to grow sideways never
/// wraps whatever this says. Give the text or an ancestor a <see cref="UiSettings.Width"/> or
/// <see cref="UiSettings.MaxWidth"/> and the wrapping has something to happen at.
/// </remarks>
public enum TextWrap
{
    /// <summary>At spaces and the like, keeping words whole. Suits prose.</summary>
    WordBoundary = 0,

    /// <summary>Anywhere at all, which is how a terminal breaks lines.</summary>
    AnyCharacter = 1,

    /// <summary>At words, falling back to characters for a word too long to fit alone.</summary>
    WordOrCharacter = 2,

    /// <summary>
    /// Never, so a long line runs past the edge unless something clips it.
    /// </summary>
    /// <remarks>
    /// An explicit newline in the string still breaks. Pair with
    /// <see cref="UiOverflow.Clip"/> for a single line that is cut off rather than wrapped.
    /// </remarks>
    NoWrap = 3,
}
