namespace Bevy;

/// <summary>A text field's cursor and selection, for <see cref="Ui.SetTextCursor"/>.</summary>
/// <remarks>Bevy's <c>TextCursorStyle</c>, each default Bevy's own.</remarks>
public sealed class UiTextCursorSettings
{
    /// <summary>The cursor, Tailwind's slate 700 by default.</summary>
    public Color Color { get; set; } = Color.FromHex("#334155");

    /// <summary>
    /// Behind the selected text while the field has the focus, Tailwind's sky 300 by default.
    /// </summary>
    public Color Selection { get; set; } = Color.FromHex("#7dd3fc");

    /// <summary>
    /// Behind the selected text while the field has not the focus, Tailwind's sky 400 by default,
    /// which a page of many fields often makes clear so only the focused one shows a selection.
    /// </summary>
    public Color UnfocusedSelection { get; set; } = Color.FromHex("#38bdf8");

    /// <summary>
    /// The selected text's own color, or null to leave it as the rest of the text is.
    /// </summary>
    public Color? SelectedText { get; set; }

    /// <summary>
    /// How round the selection's corners are, a fraction of a line's height from square at zero to
    /// a half, which rounds a line's selection into a capsule.
    /// </summary>
    public float SelectionRadius { get; set; }
}
