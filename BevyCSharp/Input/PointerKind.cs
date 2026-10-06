namespace Bevy;

/// <summary>What kind of pointer, for <see cref="PointerId"/>.</summary>
public enum PointerKind
{
    /// <summary>The mouse, of which there is one.</summary>
    Mouse,

    /// <summary>A finger on a touch screen.</summary>
    Touch,

    /// <summary>A pointer a game drives itself, as one moving over an interface drawn into a texture.</summary>
    Custom,
}
