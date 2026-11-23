using Diagnostics;

namespace Bevy;

/// <summary>
/// The settings file of a run whose window is shown, <c>settings.txt</c> in the player's folder
/// (<see cref="UserData"/>), read as the app starts and written as a saved setting changes and as
/// the app ends, through the Diagnostics module's <see cref="SettingsFile"/>.
/// </summary>
/// <remarks>
/// <para>
/// The file holds the saved settings a player set that differ from their defaults, the keys and a
/// game's own settings added to <see cref="Registry.Process"/>, and every line of a name this run
/// does not know as it stood. The engine's own settings are the run's and are never saved
/// (<see cref="EngineSettings"/>). It sits beside the game's other files in its folder, a save and
/// a <see cref="Persistent{T}"/> among them.
/// </para>
/// <para>
/// A run with no window, a headless one, one drawn offscreen and one with no renderer, reads no
/// file and writes none (<see cref="SettingsPath"/>). The window's settings are read from the file
/// once before, as the app is made, so the window opens with them (<see cref="WindowSettings"/>),
/// and found set here. A file that cannot be read is left as it is, every setting starting at its
/// default, and a line a setting refuses or a write that fails is a warning in the console and the
/// log. The file is a resource of the app's world, so it writes a last time as the world goes.
/// </para>
/// </remarks>
internal sealed class KeptSettings : IDisposable
{
    private readonly SettingsFile _file;
    private readonly List<Setting> _watched = [];

    private KeptSettings(SettingsFile file) => _file = file;

    /// <summary>The path the file was read from.</summary>
    internal string Path => _file.Path;

    /// <summary>
    /// Reads the run's settings file into the registry's settings, with the world lent so each
    /// reaches the app, and keeps it in the world, which writes it from then on as a saved setting
    /// changes and a last time as the world goes.
    /// </summary>
    internal static void Open(World world)
    {
        if (SettingsPath.For(world.Resource<Config>()) is not { } path) return;

        // Read once before the module's file is made, so a file that cannot be read is never
        // written over and the values it holds stay for a run that can.
        try
        {
            if (File.Exists(path)) _ = File.ReadAllText(path);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Warn($"The settings file {path} could not be read, so every setting starts at its default. {error.Message}");
            return;
        }

        var file = new SettingsFile(path);
        IReadOnlyList<string> refused;
        using (ConsoleHost.Lend(world)) refused = file.Read(Registry.Process);
        foreach (var line in refused) Warn($"{line} The setting starts at its default.");
        file.Trouble += Warn;

        var kept = new KeptSettings(file);
        foreach (var setting in Registry.Process.Settings)
        {
            if (!setting.Saved || setting.ReadOnly) continue;
            setting.Changed += kept.Write;
            kept._watched.Add(setting);
        }

        world.InsertResource(kept);
    }

    /// <summary>Writes the file once more where a value is unsaved and stops writing.</summary>
    public void Dispose()
    {
        foreach (var setting in _watched) setting.Changed -= Write;
        _watched.Clear();
        _file.Dispose();
    }

    // Writes the file where its text would change, a failure a warning.
    private void Write(Setting changed)
    {
        try
        {
            _file.Write(Registry.Process);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Warn($"The settings file {_file.Path} could not be written for {changed.Name}. {error.Message}");
        }
    }

    private static void Warn(string text) => ConsoleLog.Write(ConsoleLevel.Warning, text);
}
