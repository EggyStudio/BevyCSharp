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

    /// <summary>Lines are centred on each other.</summary>
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
    /// <summary>At spaces and the like, keeping words whole. What prose wants.</summary>
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

/// <summary>
/// How a run of text is set: its font and size, and what happens to it at the edges of its node.
/// </summary>
public sealed class UiTextSettings
{
    /// <summary>
    /// The font to set the text in, or none for the one built into Bevy.
    /// </summary>
    /// <remarks>
    /// Loaded with <see cref="AssetKind.Font"/> from a TrueType or OpenType file. A handle that
    /// names nothing is refused rather than quietly falling back, since a game that ships a font
    /// and then does not use it looks like a font that failed to load, which it is. Asking for a
    /// font by family name instead is not offered: that needs a Bevy feature which links against
    /// fontconfig on Linux, and this bridge builds with nothing but a C compiler.
    /// </remarks>
    public AssetHandle Font { get; set; } = AssetHandle.None;

    /// <summary>Glyph height in logical pixels.</summary>
    public float FontSize { get; set; } = 20f;

    /// <summary>How the lines sit against each other.</summary>
    public TextJustify Justify { get; set; } = TextJustify.Left;

    /// <summary>Where a line may be broken.</summary>
    public TextWrap Wrap { get; set; } = TextWrap.WordBoundary;
}
