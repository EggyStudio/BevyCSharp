namespace Bevy;

/// <summary>
/// The platform's input method is composing text that has not been committed yet.
/// </summary>
/// <remarks>
/// <para>
/// An input method (IME) is how Japanese, Chinese or Korean is typed. Keys build a candidate that
/// is shown, usually underlined, and chosen from before it becomes text. A text field shows
/// <see cref="Text"/> at its caret while composing, replaces it with each new one, and drops it
/// when an <see cref="ImeCommit"/> arrives with what was chosen. An empty one ends the composition without
/// committing anything.
/// </para>
/// <para>
/// Only sent while the input method is on, which <see cref="Window.SetIme(bool, float, float)"/> does.
/// </para>
/// </remarks>
/// <param name="Text">What is being composed.</param>
/// <param name="CaretStart">
/// Where the caret's selection starts in <paramref name="Text"/>, as an index into the string, or
/// -1 where the caret is hidden.
/// </param>
/// <param name="CaretEnd">Where it ends, or -1 where the caret is hidden.</param>
public readonly record struct ImeComposing(string Text, int CaretStart, int CaretEnd);
