namespace Bevy.Scripting;

/// <summary>
/// The console's commands over an app's behavior scripts, where <see cref="ScriptsPlugin"/>
/// compiles them, so a session driving the app builds them again and reads how the last build went
/// without a window.
/// </summary>
/// <remarks>
/// Registered from this assembly, so they are offered where the app carries the script host, in the
/// editor, the Player and a game that references the project, and nowhere else. Each reads the
/// running app's own <see cref="Scripts"/>, and an app that added no plugin says so.
/// </remarks>
internal static class ScriptCommands
{
    /// <summary>Compiles the scripts again and puts them in place of the running generation.</summary>
    [Command("scripts.reload", "Compiles the behavior scripts again and puts them in place of the running ones: scripts.reload")]
    internal static string Reload()
    {
        if (Find() is not { } scripts) return None();

        if (scripts.Reload())
            return $"compiled {Scripts.Count(scripts.Files, "file")} into {Scripts.Count(scripts.Registered, "behavior")}, "
                   + $"{Scripts.Count(scripts.Carried, "component")} carried onto the new types";

        var why = scripts.LastError ?? $"there are no scripts in {scripts.Directory}";
        ConsoleHost.Fail("NOT_COMPILED", $"The scripts did not compile, so the ones running stay: {why}");
        return $"not compiled, the ones running stay: {why}";
    }

    /// <summary>Says where the scripts are, what of them runs, whether they are watched and how the last build went.</summary>
    [Command("scripts.status", "Says the scripts' folder, what runs from it, whether a change is compiled, and the last build's errors: scripts.status")]
    internal static string Status()
    {
        if (Find() is not { } scripts) return None();

        var running = scripts.Builds == 0
            ? "nothing compiled yet"
            : $"{Scripts.Count(scripts.Registered, "behavior")} from {Scripts.Count(scripts.Files, "file")} running, "
              + $"built {Scripts.Count(scripts.Builds, "time")}";
        var watched = scripts.Watching ? "a change compiled as it is saved" : "not watched, so scripts.reload builds a change";
        var last = scripts.LastError is { } error ? $"the last build failed: {error}" : "the last build compiled";

        return $"{scripts.Directory}, {running}, {watched}, {last}";
    }

    /// <summary>The running app's scripts, or nothing where it compiles none.</summary>
    private static Scripts? Find() => ConsoleHost.World?.TryResource<Scripts>();

    /// <summary>The answer of an app that compiles no scripts, said as a failure.</summary>
    private static string None()
    {
        ConsoleHost.Fail("NO_SCRIPTS", "This app compiles no scripts, since it added no ScriptsPlugin.");
        return "this app compiles no scripts, since it added no ScriptsPlugin";
    }
}
