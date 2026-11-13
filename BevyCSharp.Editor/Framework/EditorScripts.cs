using Bevy;
using Bevy.Scripting;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The behavior scripts in the asset directory, compiled while the editor runs.
/// </summary>
/// <remarks>
/// <para>
/// The editor's side of <see cref="ScriptsPlugin"/>, which compiles them, says each build in the
/// log and builds again when one is saved, since the editor watches its assets. This adds the
/// plugin as the editor starts and offers its build to the menu and the settings, with a button
/// for the case the watcher cannot see, such as a file written by something that never touches the
/// directory it watches.
/// </para>
/// <para>
/// Added from the editor's startup rather than before the run, so the app has added its states by
/// the time the scripts declare theirs, and the editor does not enter a game's states while the
/// level is edited.
/// </para>
/// </remarks>
public static class EditorScripts
{
    private static Scripts? _scripts;

    /// <summary>The states last said not to be entered, so the same ones are not said again.</summary>
    private static string _unentered = string.Empty;

    /// <summary>Whatever went wrong the last time, or <see langword="null"/>.</summary>
    public static string? LastError => _scripts?.LastError;

    /// <summary>How many behaviors the running scripts registered.</summary>
    public static int Registered => _scripts?.Registered ?? 0;

    /// <summary>Adds the plugin, which compiles the scripts directory and watches it.</summary>
    public static void Start(App app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // The editor never enters a game's states, so it says that once below rather than each of
        // the game's state-scoped systems saying it at every load.
        StateRegistry.ReportUnentered = false;

        app.AddPlugin(new ScriptsPlugin(Path.Combine(EditorPaths.Assets, "scripts")));
        _scripts = app.World.Resource<Scripts>();

        // The first build is made as the plugin is added, before there is anything to be told.
        _scripts.Built += SayUnentered;
        if (_scripts.Builds > 0) SayUnentered(_scripts);
    }

    /// <summary>Compiles them again, whoever asked.</summary>
    public static void Reload() => _scripts?.Reload();

    /// <summary>
    /// Says the states the scripts declare, once for each set of them, which a build that changes
    /// nothing about them does not say again.
    /// </summary>
    private static void SayUnentered(Scripts scripts)
    {
        var unentered = string.Join(", ", StateRegistry.Unentered());
        if (unentered.Length > 0 && unentered != _unentered)
        {
            Log.Info(
                $"The scripts declare the states {unentered}, which the editor does not enter while the level is edited, "
                + "so what is scoped to them runs when the game is played.");
        }

        _unentered = unentered;
    }
}
