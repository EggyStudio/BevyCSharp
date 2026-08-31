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

/// <summary>
/// A file was dropped on the window.
/// </summary>
/// <remarks>
/// One message per file, so dropping a selection of three sends three. The path is what the
/// platform reported, which is absolute and outside the asset directory, so it is read with
/// ordinary file APIs rather than through the asset server.
/// </remarks>
/// <param name="Path">The absolute path of the dropped file.</param>
public readonly record struct FileDropped(string Path);

/// <summary>
/// A file is being dragged over the window, and has not been let go.
/// </summary>
/// <remarks>
/// The chance to show what the drop would do, before it happens. Every hover ends in either a
/// <see cref="FileDropped"/> or a <see cref="FileHoverCancelled"/>.
/// </remarks>
/// <param name="Path">The absolute path of the file being dragged.</param>
public readonly record struct FileHovered(string Path);

/// <summary>The drag left the window without dropping, so any hover feedback should be cleared.</summary>
public readonly record struct FileHoverCancelled;
