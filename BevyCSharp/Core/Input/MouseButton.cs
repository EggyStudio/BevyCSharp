namespace Bevy;

/// <summary>A mouse button.</summary>
/// <remarks>Values are bit indices, matching the mapping in the native bridge.</remarks>
public enum MouseButton
{
    /// <summary>The primary, usually left, button.</summary>
    Left = 0,

    /// <summary>The secondary, usually right, button.</summary>
    Right = 1,

    /// <summary>The middle button, usually the scroll wheel.</summary>
    Middle = 2,

    /// <summary>The "back" thumb button.</summary>
    Back = 3,

    /// <summary>The "forward" thumb button.</summary>
    Forward = 4,
}
