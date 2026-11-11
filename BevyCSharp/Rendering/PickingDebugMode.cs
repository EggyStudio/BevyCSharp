namespace Bevy;

/// <summary>
/// What picking's debug tool shows, Bevy's <c>DebugPickingMode</c>, which
/// <see cref="Picking.DebugMode"/> sets.
/// </summary>
public enum PickingDebugMode
{
    /// <summary>Neither the overlay nor the log, Bevy's default.</summary>
    Disabled = 0,

    /// <summary>The overlay, and a log of the events but the moves and drags.</summary>
    Normal = 1,

    /// <summary>The overlay, and a log of every event, the moves and drags among them.</summary>
    Noisy = 2,
}
