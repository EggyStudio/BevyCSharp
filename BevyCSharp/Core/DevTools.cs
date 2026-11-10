namespace Bevy;

/// <summary>
/// Bevy's dev tools an app adds, through <see cref="Config.DevTools"/>.
/// </summary>
/// <remarks>
/// Each is a plugin Bevy's default plugins leave out, so an app asks for those it uses. A bridge
/// with the renderer is needed.
/// </remarks>
[Flags]
public enum DevTools
{
    /// <summary>None of them.</summary>
    None = 0,

    /// <summary>
    /// The FPS overlay at the window's top left with its frame time graph, set through the
    /// reflected <c>FpsOverlayConfigRef</c> resource (<c>FpsOverlayPlugin</c>).
    /// </summary>
    /// <remarks>
    /// It adds Bevy's frame time diagnostics where the app has not. How often its text changes is
    /// set as the app is built, so the resource's interval changes nothing after.
    /// </remarks>
    FpsOverlay = 1,

    /// <summary>
    /// The infinite grid, a ground plane of lines fading with distance for a tool's view, drawn on
    /// an entity given the reflected <c>InfiniteGridRef</c> and set through its
    /// <c>InfiniteGridSettingsRef</c> (<c>InfiniteGridPlugin</c>).
    /// </summary>
    InfiniteGrid = 2,

    /// <summary>
    /// The overlay at each pointer of what picking finds under it, with a log of picking's events,
    /// shown while <see cref="Picking.DebugMode"/> is other than disabled
    /// (<c>DebugPickingPlugin</c>).
    /// </summary>
    /// <remarks>
    /// The events are logged at Bevy's debug and trace levels, which its log leaves out unless
    /// <c>RUST_LOG</c> lets them through, as <c>RUST_LOG=bevy_dev_tools=trace</c> does.
    /// </remarks>
    PickingDebug = 4,
}
