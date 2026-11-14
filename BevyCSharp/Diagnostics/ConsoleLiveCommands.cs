using Bevy.Interop;

namespace Bevy;

/// <summary>
/// What the command line asks of the live path, whether a running app reads its files again as
/// they change and what it read again lately.
/// </summary>
/// <remarks>
/// A session editing a running game asks this to know its save was seen, without reading the log,
/// and asks it first to know whether a save will be seen at all, since watching is the program's,
/// the environment's or a Debug build's to decide.
/// </remarks>
internal static class ConsoleLiveCommands
{
    /// <summary>
    /// Says whether the app watches its assets and what decided it, whether Bevy's watcher reads
    /// its files, and the files read again lately, the latest last.
    /// </summary>
    [Command("live.status", "Says whether the app reads its files again as they change, what decided that, and the files read again lately: live.status")]
    internal static string Status()
    {
        var world = ConsoleHost.World ?? throw new InvalidOperationException("live.status reads the app, so it can only run inside a system.");
        var config = world.Resource<Config>();

        var watching = !config.WatchAssetsAskedOfAnything
            ? "not watching the assets, since nothing asked"
            : $"{(config.WatchAssets ? "watching" : "not watching")} the assets, as {config.WatchAssetsSource} asks";

        // Bevy's watcher reads textures, models and the rest of Bevy's assets. Only a bridge of the
        // render profile or one over it carries it, and the system can refuse it a folder, which
        // the log says as the app starts.
        var bevy = NativeAssets.bcs_assets_watching() == 1 ? "Bevy's watcher reads its assets"
            : !config.WatchAssets ? "Bevy's watcher is off"
            : App.HasRenderer ? "Bevy's assets are not read again, since the asset folder could not be watched, as the log says"
            : "Bevy's assets are not read again, since this bridge carries no watcher";

        var reloads = world.TryResource<LiveReloads>();
        var latest = reloads is null || reloads.Count == 0
            ? "nothing read again yet"
            : $"{(reloads.Count == 1 ? "one file" : $"{reloads.Count} files")} read again, the latest "
              + string.Join(", ", reloads.Latest.Select(each => $"{each.Reload.Path} ({each.Reload.Kind}) at frame {each.Frame}"));

        return $"{watching}, {bevy}, {latest}";
    }
}
