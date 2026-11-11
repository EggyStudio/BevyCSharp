namespace Bevy;

/// <summary>
/// Everything a method's attributes asked for.
/// </summary>
/// <param name="Label">What the button says, or nothing for the method's own name.</param>
/// <param name="Tooltip">A sentence about what pressing it does.</param>
/// <param name="Hidden">Whether it is offered at all.</param>
/// <param name="Line">Whether the button shares a line with its neighbors, and where in it.</param>
/// <param name="Weight">How much of that line it takes against the others on it.</param>
/// <param name="Space">Whether a blank row goes above it.</param>
/// <param name="Separator">Whether a line goes above it.</param>
/// <param name="Header">A heading to draw above it.</param>
/// <param name="Foldout">The fold it sits in, with slashes between the levels.</param>
/// <param name="FoldoutOpen">Whether that fold starts open.</param>
/// <param name="Note">A sentence to draw above it, in the panel.</param>
/// <param name="NoteKind">How loudly that sentence is said.</param>
/// <param name="Order">Where it sits among the other buttons.</param>
public sealed record MethodHints(
    string? Label = null,
    string? Tooltip = null,
    bool Hidden = false,
    ButtonLine Line = ButtonLine.Alone,
    double Weight = 1d,
    bool Space = false,
    bool Separator = false,
    string? Header = null,
    string? Foldout = null,
    bool FoldoutOpen = true,
    string? Note = null,
    NoteKind NoteKind = NoteKind.Info,
    int Order = 0)
{
    /// <summary>What a method with no attributes on it asked for.</summary>
    public static readonly MethodHints None = new();
}
