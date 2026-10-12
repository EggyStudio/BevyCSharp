namespace Bevy;

/// <summary>
/// The pointer moved off the window.
/// </summary>
/// <remarks>
/// Worth handling for anything that follows the cursor, which would otherwise keep tracking the
/// last position it saw.
/// </remarks>
public readonly record struct CursorLeft;
