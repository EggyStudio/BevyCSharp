using Bevy.Interop;

namespace Bevy;

/// <summary>How a text field behaves, for <see cref="Ui.SetEditableText"/>.</summary>
public sealed class UiEditableTextSettings
{
    /// <summary>What it holds to begin with.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>The most characters it holds, or zero for no limit.</summary>
    public int MaxCharacters { get; set; }

    /// <summary>
    /// How many glyphs wide it is, measured by the width of a zero in its font, or zero to be as
    /// wide as its node is laid out.
    /// </summary>
    public float VisibleWidth { get; set; }

    /// <summary>How many lines tall it is. One by default, a single line.</summary>
    public float VisibleLines { get; set; } = 1f;

    /// <summary>Whether Enter starts a new line rather than being left to the game.</summary>
    public bool AllowNewlines { get; set; }

    /// <summary>
    /// Whether it is typed into, read only or shown alone. Typed into by default.
    /// </summary>
    public TextReadWriteMode Mode { get; set; }

    /// <summary>
    /// The only characters it takes, or null for any. A character typed or pasted that is not
    /// among them is refused, and a paste holding one is refused whole.
    /// </summary>
    /// <remarks>
    /// Bevy's filter is a function of the game's own, which cannot cross from C#, so a field here
    /// names the characters it takes, which covers the numbers, the hex digits and the names a
    /// field is usually limited to. What is already in the field is not checked.
    /// </remarks>
    public string? Allowed { get; set; }
}
