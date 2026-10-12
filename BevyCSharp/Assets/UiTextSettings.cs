namespace Bevy;

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
    /// font by family name instead is not offered, because that needs a Bevy feature which links
    /// against fontconfig on Linux, and this bridge builds with nothing but a C compiler.
    /// </remarks>
    public AssetHandle Font { get; set; } = AssetHandle.None;

    /// <summary>Glyph height in logical pixels.</summary>
    public float FontSize { get; set; } = 20f;

    /// <summary>How the lines sit against each other.</summary>
    public TextJustify Justify { get; set; } = TextJustify.Left;

    /// <summary>Where a line may be broken.</summary>
    public TextWrap Wrap { get; set; } = TextWrap.WordBoundary;

    /// <summary>
    /// How far apart the lines sit, or zero for the spacing the font asks for.
    /// </summary>
    /// <remarks>
    /// Read as a multiple of the font size unless <see cref="LineHeightInPixels"/> says otherwise,
    /// so <c>1.2f</c> is the usual body spacing and <c>1f</c> packs the lines against each other.
    /// </remarks>
    public float LineHeight { get; set; }

    /// <summary>Whether <see cref="LineHeight"/> is in logical pixels rather than font sizes.</summary>
    public bool LineHeightInPixels { get; set; }

    /// <summary>
    /// How much room is added between the letters, or zero for the fit the font asks for.
    /// </summary>
    /// <remarks>
    /// Suits a heading tracked out, and keeps a line of small capitals legible. Read as a multiple
    /// of the font size unless <see cref="LetterSpacingInPixels"/> says otherwise, so the useful
    /// numbers are small. Negative pulls the letters together, which a display face set large can
    /// take and body text cannot.
    /// </remarks>
    public float LetterSpacing { get; set; }

    /// <summary>Whether <see cref="LetterSpacing"/> is in logical pixels rather than font sizes.</summary>
    public bool LetterSpacingInPixels { get; set; }

    /// <summary>
    /// Whether the glyphs are smoothed at their edges.
    /// </summary>
    /// <remarks>
    /// Turn it off for a pixel font, because smoothing a font drawn to land on whole pixels makes
    /// it look blurred rather than sharp.
    /// </remarks>
    public bool Smooth { get; set; } = true;

    /// <summary>
    /// A shadow cast behind the text, in logical pixels across and down.
    /// </summary>
    /// <remarks>
    /// What keeps light text readable over a picture that might be light too. Drawn in
    /// <see cref="ShadowColor"/>, which is transparent until it is set, so an offset alone draws
    /// nothing.
    /// </remarks>
    public (float X, float Y) ShadowOffset { get; set; }

    /// <summary>The shadow's color, linear RGBA. Transparent draws no shadow.</summary>
    public (float R, float G, float B, float A) ShadowColor { get; set; } = (0f, 0f, 0f, 0f);
}
