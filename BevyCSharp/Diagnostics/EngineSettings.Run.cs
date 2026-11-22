using Diagnostics;

namespace Bevy;

internal static partial class EngineSettings
{
    // How a tool started the run, from the command line, the program's config or the environment.
    // Each only reads, since no start reads a value set while the program runs, so the file never
    // keeps one and its variable gives it at the start alone, which each names for the record.
    // Those the app's config holds read the running app's, and the rest what their variable gives
    // the process.
    private static IEnumerable<Setting> Run()
    {
        static Config? Started() => App.Running?.Config;
        static string Named(string variable) => Environment.GetEnvironmentVariable(variable) ?? "";

        yield return new Setting<bool>("run.offscreen", $"Whether the run draws into images of its own with no window, as the config or {Config.OffscreenVariable} asks.", false,
            () => Started()?.Offscreen ?? false)
        { ReadOnly = true, Saved = false, EnvironmentVariable = Config.OffscreenVariable };
        yield return new Setting<uint>("run.frames", $"The frames a run with no window stops after, or 0 to run on, as the config or {Config.FramesVariable} gives it.", 0u,
            () => Started()?.HeadlessFrames ?? 0u)
        { ReadOnly = true, Saved = false, EnvironmentVariable = Config.FramesVariable };
        yield return new Setting<bool>("run.serve", "Whether the run answers bcs on a local socket, as the config, --serve or BCS_SERVE asks.", false,
            () => Started()?.Serve ?? false)
        { ReadOnly = true, Saved = false, EnvironmentVariable = "BCS_SERVE" };
        yield return new Setting<bool>("run.watch", $"Whether the run reads its asset files again as they change, as the config or {Config.WatchAssetsVariable} decides, which live.status explains.", false,
            () => Started()?.WatchAssets ?? false)
        { ReadOnly = true, Saved = false, EnvironmentVariable = Config.WatchAssetsVariable };
        yield return new Setting<double>("run.memory_cap", $"The gigabytes the process may hold before it is stopped, or 0 for no cap, as the config or {MemoryGuard.Variable} gives it.", 0.0,
            () => MemoryGuard.Cap / (double)(1L << 30))
        { ReadOnly = true, Saved = false, EnvironmentVariable = MemoryGuard.Variable };
        yield return new Setting<string>("run.sessions", "The folder the session files of served runs are written to, which bcs reads, a folder in the user's local application data where empty, as BCS_SESSIONS gives it.", "",
            () => Named("BCS_SESSIONS"))
        { ReadOnly = true, Saved = false, EnvironmentVariable = "BCS_SESSIONS" };
        yield return new Setting<string>("run.slangc", "The Slang compiler the run compiles its shaders with, found on the PATH or in the checkout where empty, as BCS_SLANGC gives it.", "",
            () => Named("BCS_SLANGC"))
        { ReadOnly = true, Saved = false, EnvironmentVariable = "BCS_SLANGC" };
        yield return new Setting<string>("run.shuffle", "The seed each schedule's systems are shuffled by as the app starts, so a run finds a system leaning on an order nothing asks for, or empty for none, as BCS_SCHEDULE_SHUFFLE_SEED gives it.", "",
            () => Named("BCS_SCHEDULE_SHUFFLE_SEED"))
        { ReadOnly = true, Saved = false, EnvironmentVariable = "BCS_SCHEDULE_SHUFFLE_SEED" };
    }
}
