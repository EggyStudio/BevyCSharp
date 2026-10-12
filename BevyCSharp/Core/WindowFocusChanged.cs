namespace Bevy;

/// <summary>The window gained or lost focus.</summary>
/// <remarks>What a game pauses on, and what stops it eating a laptop battery in the background.</remarks>
/// <param name="Focused">True when the window has focus.</param>
public readonly record struct WindowFocusChanged(bool Focused);
