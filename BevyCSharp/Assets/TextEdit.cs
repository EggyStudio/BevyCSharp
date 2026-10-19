namespace Bevy;

/// <summary>
/// An edit queued on a text field, Bevy's <c>TextEdit</c>, applied with those the keyboard makes in
/// the frame, for <see cref="Ui.EditText"/>.
/// </summary>
/// <param name="Kind">Which edit.</param>
/// <param name="Text">What <see cref="TextEditKind.Insert"/> puts at the cursor.</param>
/// <param name="Select">For a move of the cursor, whether it extends the selection.</param>
/// <param name="Point">
/// For an edit at a point, the point in the field, and for a scroll, how far, in pixels.
/// </param>
/// <remarks>
/// The constructors below make each edit with what it reads, as a virtual keyboard's keys or a
/// toolbar's buttons type into a field.
/// </remarks>
public readonly record struct TextEdit(TextEditKind Kind, string Text = "", bool Select = false, Vec2 Point = default)
{
    /// <summary>Puts <paramref name="text"/> at the cursor, in place of the selection where there is one.</summary>
    public static TextEdit Insert(string text) => new(TextEditKind.Insert, text);

    /// <summary>Deletes the character behind the cursor, or the selection.</summary>
    public static TextEdit Backspace => new(TextEditKind.Backspace);

    /// <summary>Deletes the character at the cursor, or the selection.</summary>
    public static TextEdit Delete => new(TextEditKind.Delete);

    /// <summary>Moves the cursor a character left, extending the selection where asked.</summary>
    public static TextEdit Left(bool select = false) => new(TextEditKind.Left, Select: select);

    /// <summary>Moves the cursor a character right, extending the selection where asked.</summary>
    public static TextEdit Right(bool select = false) => new(TextEditKind.Right, Select: select);

    /// <summary>Moves the cursor up a line as laid out, extending the selection where asked.</summary>
    public static TextEdit Up(bool select = false) => new(TextEditKind.Up, Select: select);

    /// <summary>Moves the cursor down a line as laid out, extending the selection where asked.</summary>
    public static TextEdit Down(bool select = false) => new(TextEditKind.Down, Select: select);

    /// <summary>Moves the cursor to the start of its line as laid out.</summary>
    public static TextEdit LineStart(bool select = false) => new(TextEditKind.LineStart, Select: select);

    /// <summary>Moves the cursor to the end of its line as laid out.</summary>
    public static TextEdit LineEnd(bool select = false) => new(TextEditKind.LineEnd, Select: select);

    /// <summary>Selects the whole text.</summary>
    public static TextEdit SelectAll => new(TextEditKind.SelectAll);

    /// <summary>Scrolls by <paramref name="lines"/> lines as laid out, down where it is above zero.</summary>
    public static TextEdit ScrollByLines(float lines) => new(TextEditKind.ScrollByLines, Point: new Vec2(lines, 0f));
}
