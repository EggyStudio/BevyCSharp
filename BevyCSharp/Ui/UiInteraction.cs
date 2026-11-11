namespace Bevy;

/// <summary>
/// How the pointer stands on a node.
/// </summary>
/// <remarks>
/// Only a node spawned with <see cref="UiSettings.Interactive"/> reports one. Asking a node that
/// was not is refused rather than answered <see cref="None"/>, because a button that quietly
/// never fires is the harder mistake to find.
/// </remarks>
public enum UiInteraction
{
    /// <summary>The pointer is elsewhere.</summary>
    None = 0,

    /// <summary>The pointer is over the node.</summary>
    Hovered = 1,

    /// <summary>The pointer is over the node and its primary button is down.</summary>
    Pressed = 2,
}
