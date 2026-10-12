namespace Bevy;

/// <summary>The window changed size.</summary>
/// <remarks>
/// Sent when the window is resized, which includes going fullscreen and back. The size is in
/// logical pixels, matching <see cref="Window.Size"/>.
/// </remarks>
/// <param name="Width">New width in logical pixels.</param>
/// <param name="Height">New height in logical pixels.</param>
public readonly record struct WindowResized(float Width, float Height);
