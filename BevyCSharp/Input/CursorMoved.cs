namespace Bevy;

/// <summary>The cursor moving over a window, as Bevy's <c>CursorMoved</c> message carries it.</summary>
/// <param name="Window">The window.</param>
/// <param name="Position">Where it is, in logical pixels from the window's top left corner.</param>
/// <param name="Delta">How far it moved since the last such message, or null for the first over a window.</param>
/// <remarks>Read with <c>ctx.Read&lt;CursorMoved&gt;()</c>.</remarks>
public readonly record struct CursorMoved(Entity Window, Vec2 Position, Vec2? Delta);
