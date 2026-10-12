using Bevy.Interop;

namespace Bevy;

/// <summary>Where the window is, how large, and whether it is maximized.</summary>
/// <remarks>
/// What a game keeps to reopen where it was closed (<see cref="Config.RememberWindow"/>), read
/// with <see cref="Window.Place"/>. Wayland never tells an app where its window is, so there
/// <see cref="HasPosition"/> is false and the place is the size alone.
/// </remarks>
/// <param name="HasPosition">Whether <see cref="X"/> and <see cref="Y"/> say anything.</param>
/// <param name="X">The window's left edge, in physical pixels in the desktop's coordinate space.</param>
/// <param name="Y">Its top edge, the same way.</param>
/// <param name="Width">Width in logical pixels.</param>
/// <param name="Height">Height in logical pixels.</param>
/// <param name="Maximized">Whether it fills the screen less the taskbar.</param>
public readonly record struct WindowPlace(bool HasPosition, int X, int Y, uint Width, uint Height, bool Maximized);
