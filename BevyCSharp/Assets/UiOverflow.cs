namespace Bevy;

/// <summary>
/// What happens to contents that run past a node's edge.
/// </summary>
public enum UiOverflow
{
    /// <summary>They are drawn anyway, outside the node.</summary>
    Visible = 0,

    /// <summary>They are cut off at the edge.</summary>
    Clip = 1,

    /// <summary>Cut off at the edge, and the layout is told they do not fit.</summary>
    Hidden = 2,

    /// <summary>
    /// Cut off at the edge, and movable inside it with <see cref="Ui.SetScroll"/>.
    /// </summary>
    Scroll = 3,
}
