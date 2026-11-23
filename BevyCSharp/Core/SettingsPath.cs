namespace Bevy;

/// <summary>
/// Where a run's settings file is, <c>settings.txt</c> in the player's folder (<see
/// cref="UserData"/>) where the run shows a window, and none for the rest.
/// </summary>
/// <remarks>
/// Read in two places, as an app is made, for the window's settings, which the window opens with
/// (<see cref="WindowSettings.Open"/>), and as it starts, for every other setting, which the
/// diagnostics' settings file reads into the registry. A run with no window, a headless one, one
/// drawn offscreen and one with no renderer, has none, since a test or a tool starts those and the
/// player's choices stay as the player left them. A test points <see cref="For"/> at a folder of
/// its own.
/// </remarks>
internal static class SettingsPath
{
    /// <summary>The file's name in the player's folder.</summary>
    internal const string FileName = "settings.txt";

    /// <summary>The path of a run's settings file, or none where the run keeps none.</summary>
    internal static Func<Config, string?> For { get; set; } = Shown;

    private static string? Shown(Config config) =>
        App.HasRenderer && !config.Headless && !config.Offscreen ? UserData.Resolve(FileName) : null;
}
