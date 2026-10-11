namespace Bevy;

/// <summary>
/// How often the app updates while its window has focus and while it does not, as Bevy's
/// <c>WinitSettings</c>, set with <see cref="Window.SetUpdateSettings"/>.
/// </summary>
/// <remarks>
/// Bevy's presets are here by its names. An app runs at <see cref="Game"/> unless it sets another,
/// and <see cref="DesktopApp"/> suits an editor or a tool, which has nothing to show until
/// something happens.
/// </remarks>
/// <param name="Focused">The mode while the window has focus.</param>
/// <param name="Unfocused">The mode while it does not.</param>
public readonly record struct UpdateSettings(UpdateMode Focused, UpdateMode Unfocused)
{
    /// <summary>Continuous while focused, and reactive at sixty updates a second without focus.</summary>
    public static UpdateSettings Game => new(UpdateMode.Continuous, UpdateMode.ReactiveLowPower(TimeSpan.FromSeconds(1.0 / 60.0)));

    /// <summary>Reactive while focused, waiting five seconds at most, and a minute without focus.</summary>
    public static UpdateSettings DesktopApp => new(UpdateMode.Reactive(TimeSpan.FromSeconds(5)), UpdateMode.ReactiveLowPower(TimeSpan.FromMinutes(1)));

    /// <summary>Reactive at sixty updates a second while focused, and once a second without focus.</summary>
    public static UpdateSettings Mobile => new(UpdateMode.Reactive(TimeSpan.FromSeconds(1.0 / 60.0)), UpdateMode.ReactiveLowPower(TimeSpan.FromSeconds(1)));

    /// <summary>Continuous whether the window has focus or not.</summary>
    public static UpdateSettings Continuous => new(UpdateMode.Continuous, UpdateMode.Continuous);
}
