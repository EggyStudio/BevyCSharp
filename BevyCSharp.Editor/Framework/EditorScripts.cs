using Bevy;
using Bevy.Scripting;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The behavior scripts in the asset directory, compiled while the editor runs.
/// </summary>
/// <remarks>
/// A wrapper around the host and the watcher so that anything can ask for a reload. The watcher
/// does it when a file changes, and the asset browser offers a button for the case the watcher
/// cannot see, such as a file written by something that never touches the directory it watches.
/// </remarks>
public static class EditorScripts
{
    private static ScriptHost? _host;

    /// <summary>The states last said not to be entered, so the same ones are not said again.</summary>
    private static string _unentered = string.Empty;
    private static ScriptWatcher? _watcher;

    /// <summary>Whatever went wrong the last time, or <see langword="null"/>.</summary>
    public static string? LastError => _host?.LastError;

    /// <summary>How many registrations the last successful build produced.</summary>
    public static int Registered => _host?.Registered ?? 0;

    /// <summary>Compiles the scripts directory and starts watching it.</summary>
    public static void Start(App app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var directory = Path.Combine(EditorPaths.Assets, "scripts");

        _host = new ScriptHost(app, directory);
        _watcher = new ScriptWatcher(directory);

        // The editor never enters a game's states, so it says that once below rather than each of
        // the game's state-scoped systems saying it at every load.
        StateRegistry.ReportUnentered = false;

        Build("loaded");
    }

    /// <summary>Compiles them again, whoever asked.</summary>
    public static void Reload() => Build("reloaded");

    /// <summary>Rebuilds when a script file has changed and settled.</summary>
    /// <remarks>
    /// A generation's startup runs as it is registered, which is inside whichever system calls
    /// this, so a script spawns what it needs and clears out what the last one left.
    /// </remarks>
    public static void Poll()
    {
        if (_watcher?.TakeChange() != true) return;

        Build("reloaded");
    }

    /// <summary>Compiles the scripts and swaps the result in.</summary>
    private static void Build(string what)
    {
        if (_host is null) return;

        if (!_host.Reload())
        {
            Console.WriteLine($"[editor] scripts not loaded: {_host.LastError}");
            return;
        }

        Console.WriteLine(_host.Carried > 0
            ? $"[editor] scripts {what}: {_host.Registered} registration(s), {_host.Carried} component(s) carried over"
            : $"[editor] scripts {what}: {_host.Registered} registration(s)");

        // Once for each set of states the scripts declare, which a reload that changes nothing
        // about them does not say again.
        var unentered = string.Join(", ", StateRegistry.Unentered());
        if (unentered.Length > 0 && unentered != _unentered)
        {
            Console.WriteLine(
                $"[editor] the scripts declare the states {unentered}, which the editor does not enter while the level is edited, "
                + "so what is scoped to them runs when the game is played");
        }

        _unentered = unentered;
    }
}
