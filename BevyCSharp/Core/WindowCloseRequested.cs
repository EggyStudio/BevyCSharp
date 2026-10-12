namespace Bevy;

/// <summary>
/// Something asked the window to close.
/// </summary>
/// <remarks>
/// A request rather than a fact, because the window is still open, which is the chance to save, or
/// to ask whether the player meant it. Call <see cref="App.RequestExit()"/> to actually go.
/// </remarks>
public readonly record struct WindowCloseRequested;
