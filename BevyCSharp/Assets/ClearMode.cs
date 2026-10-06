namespace Bevy;

/// <summary>What a camera does with the pixels it is about to draw over.</summary>
public enum ClearMode
{
    /// <summary>Clear to the world's clear color.</summary>
    World = 0,

    /// <summary>Clear to this camera's own color.</summary>
    Custom = 1,

    /// <summary>
    /// Clear nothing and draw over what is already there, for a camera layered on another.
    /// </summary>
    Keep = 2,
}
