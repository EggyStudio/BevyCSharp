namespace Bevy;

/// <summary>
/// What a panel declared about itself.
/// </summary>
/// <remarks>
/// <para>
/// Generated from the attributes on the panel class, so a panel author writes a declaration and
/// not a constructor call. Whatever is showing the panel reads it; nothing writes it.
/// </para>
/// <para>
/// The three numbers that mean something to a layout rather than to a document are kept as plain
/// numbers here: which dock a panel belongs to, what dismisses it, and which panels it draws in
/// front of. What those numbers mean is the business of whatever arranges the panels, which for
/// this editor is its shell and for a game is whatever the game wrote. A library that named them
/// would be a library with an opinion about how a game's interface is laid out.
/// </para>
/// </remarks>
/// <param name="Document">The document's path under the asset root.</param>
/// <param name="Root">The CSS id of the panel's outermost element, or nothing.</param>
/// <param name="Dock">Which dock it belongs to, as whatever arranges panels numbers them.</param>
/// <param name="X">Its offset within that dock, or its left edge when floating.</param>
/// <param name="Y">The same, vertically.</param>
/// <param name="Width">How wide, or <see cref="float.NaN"/> to leave it to the stylesheet.</param>
/// <param name="Height">The same, and <see cref="float.NaN"/> for as tall as its contents.</param>
/// <param name="Order">Where it sits among its dock's other panels.</param>
/// <param name="Dismiss">What makes it go away, as whatever is arranging panels numbers it.</param>
/// <param name="Layer">Which panels it draws in front of.</param>
public sealed record PanelChrome(
    string Document = "",
    string? Root = null,
    int Dock = 0,
    float X = float.NaN,
    float Y = float.NaN,
    float Width = float.NaN,
    float Height = float.NaN,
    int Order = 0,
    int Dismiss = 0,
    int Layer = 0);
