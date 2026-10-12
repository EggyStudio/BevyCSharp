namespace Bevy;

/// <summary>The input method committed text, which is inserted where the caret is.</summary>
/// <param name="Text">What was chosen.</param>
public readonly record struct ImeCommit(string Text);
