using Bevy.Interop;

namespace Bevy;

/// <summary>What the window does with the mouse cursor.</summary>
public enum CursorGrab
{
    /// <summary>The cursor moves freely and can leave the window.</summary>
    None = 0,

    /// <summary>The cursor stays inside the window but still moves within it.</summary>
    Confined = 1,

    /// <summary>The cursor is pinned in place and only its movement is reported.</summary>
    Locked = 2,
}
