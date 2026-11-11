namespace Bevy;

/// <summary>Which of Bevy's text edits, in the order of its <c>TextEdit</c>.</summary>
/// <remarks>
/// The input method's composition is left out, since it reaches a field through the input method's
/// own messages, as <see cref="SyntheticInput.Compose"/> sends them.
/// </remarks>
public enum TextEditKind
{
    /// <summary>Copies the selection to the clipboard.</summary>
    Copy = 0,

    /// <summary>Copies the selection to the clipboard and deletes it.</summary>
    Cut = 1,

    /// <summary>Puts the clipboard's text at the cursor, in place of the selection where there is one.</summary>
    Paste = 2,

    /// <summary>Puts <see cref="TextEdit.Text"/> at the cursor, in place of the selection where there is one.</summary>
    Insert = 3,

    /// <summary>Deletes the character behind the cursor, or the selection.</summary>
    Backspace = 4,

    /// <summary>Deletes the word behind the cursor, or the selection.</summary>
    BackspaceWord = 5,

    /// <summary>Deletes the character at the cursor, or the selection.</summary>
    Delete = 6,

    /// <summary>Deletes the word at the cursor, or the selection.</summary>
    DeleteWord = 7,

    /// <summary>Moves the cursor a character left.</summary>
    Left = 8,

    /// <summary>Moves the cursor a character right.</summary>
    Right = 9,

    /// <summary>Moves the cursor a word left.</summary>
    WordLeft = 10,

    /// <summary>Moves the cursor a word right.</summary>
    WordRight = 11,

    /// <summary>Moves the cursor up a line as laid out.</summary>
    Up = 12,

    /// <summary>Moves the cursor down a line as laid out.</summary>
    Down = 13,

    /// <summary>Moves the cursor to the start of the text.</summary>
    TextStart = 14,

    /// <summary>Moves the cursor to the end of the text.</summary>
    TextEnd = 15,

    /// <summary>Moves the cursor to the start of the line the text breaks at a line break.</summary>
    HardLineStart = 16,

    /// <summary>Moves the cursor to the end of the line the text breaks at a line break.</summary>
    HardLineEnd = 17,

    /// <summary>Moves the cursor to the start of its line as laid out.</summary>
    LineStart = 18,

    /// <summary>Moves the cursor to the end of its line as laid out.</summary>
    LineEnd = 19,

    /// <summary>Takes the selection away, leaving the cursor where it was.</summary>
    CollapseSelection = 20,

    /// <summary>Selects the whole text.</summary>
    SelectAll = 21,

    /// <summary>Selects the whole text where nothing is selected.</summary>
    SelectAllIfCollapsed = 22,

    /// <summary>Moves the cursor to <see cref="TextEdit.Point"/>.</summary>
    MoveToPoint = 23,

    /// <summary>Selects the word at <see cref="TextEdit.Point"/>.</summary>
    SelectWordAtPoint = 24,

    /// <summary>Selects the line as laid out at <see cref="TextEdit.Point"/>.</summary>
    SelectLineAtPoint = 25,

    /// <summary>Selects the line between line breaks at <see cref="TextEdit.Point"/>.</summary>
    SelectHardLineAtPoint = 26,

    /// <summary>Extends the selection to <see cref="TextEdit.Point"/>.</summary>
    ExtendSelectionToPoint = 27,

    /// <summary>Extends the selection from where it began to <see cref="TextEdit.Point"/>, as a click with Shift held does.</summary>
    ShiftClickExtension = 28,

    /// <summary>Scrolls by the lines <see cref="TextEdit.Point"/>'s X gives, down where it is above zero.</summary>
    ScrollByLines = 29,

    /// <summary>Scrolls as little as brings <see cref="TextEdit.Point"/> into view.</summary>
    ScrollTo = 30,

    /// <summary>Scrolls by <see cref="TextEdit.Point"/>, in pixels.</summary>
    ScrollBy = 31,
}
