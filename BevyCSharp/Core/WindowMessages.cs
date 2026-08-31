namespace Bevy;

/// <summary>The window changed size.</summary>
/// <remarks>
/// Sent when the window is resized, which includes going fullscreen and back. The size is in
/// logical pixels, matching <see cref="Window.Size"/>.
/// </remarks>
/// <param name="Width">New width in logical pixels.</param>
/// <param name="Height">New height in logical pixels.</param>
public readonly record struct WindowResized(float Width, float Height);

/// <summary>The window gained or lost focus.</summary>
/// <remarks>What a game pauses on, and what stops it eating a laptop battery in the background.</remarks>
/// <param name="Focused">True when the window has focus.</param>
public readonly record struct WindowFocusChanged(bool Focused);

/// <summary>
/// Something asked the window to close.
/// </summary>
/// <remarks>
/// A request rather than a fact: the window is still open, which is the chance to save, or to ask
/// whether the player meant it. Call <see cref="App.RequestExit"/> to actually go.
/// </remarks>
public readonly record struct WindowCloseRequested;

/// <summary>
/// The display's scale factor changed.
/// </summary>
/// <remarks>
/// Sent when the window moves to a monitor with different scaling, or the setting changes. UI
/// sized in logical pixels follows on its own; anything measured in physical pixels does not.
/// </remarks>
/// <param name="ScaleFactor">Physical pixels per logical pixel.</param>
public readonly record struct WindowScaleFactorChanged(float ScaleFactor);

/// <summary>The pointer moved onto the window.</summary>
public readonly record struct CursorEntered;

/// <summary>
/// The pointer moved off the window.
/// </summary>
/// <remarks>
/// Worth handling for anything that follows the cursor, which would otherwise keep tracking the
/// last position it saw.
/// </remarks>
public readonly record struct CursorLeft;
