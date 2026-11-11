namespace Bevy;

/// <summary>Which button a pointer pressed, as Bevy's <c>PointerButton</c> names them.</summary>
/// <remarks>The mouse's left button is the primary one, and a touch is always primary.</remarks>
public enum PointerButton
{
    /// <summary>The left mouse button, or a touch.</summary>
    Primary,

    /// <summary>The right mouse button.</summary>
    Secondary,

    /// <summary>The middle mouse button.</summary>
    Middle,
}
