using Bevy.Interop;

namespace Bevy;

/// <summary>How the window fills the screen.</summary>
public enum WindowMode
{
    /// <summary>An ordinary window.</summary>
    Windowed = 0,

    /// <summary>Fills the monitor the window is on, with no decoration and no mode switch.</summary>
    BorderlessFullscreen = 1,

    /// <summary>
    /// Takes the monitor exclusively, at its current video mode.
    /// </summary>
    /// <remarks>
    /// Lets the display driver hand the window the screen outright, which can be worth a frame of
    /// latency. It also makes alt-tabbing heavier, because the compositor has to take the screen
    /// back. Most desktop games use <see cref="BorderlessFullscreen"/>.
    /// </remarks>
    Fullscreen = 2,
}
