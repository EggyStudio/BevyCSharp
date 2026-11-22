using System.Runtime.CompilerServices;
using Diagnostics;

namespace Bevy;

/// <summary>
/// The engine's settings in the Diagnostics module's registry of the process, every value a console
/// command or an environment variable sets, each with its range or its choices, its default,
/// whether it only reads and the variable that overrides it.
/// </summary>
/// <remarks>
/// <para>
/// A setting reads and sets the running app (<see cref="App.Running"/>) and refuses a set where
/// none runs, with the reason, so its value stays as it reads. The settings panel draws them on a
/// page for each first word of their names, <c>time</c>, <c>gi</c>, <c>field</c>, <c>scenes</c>
/// and <c>run</c>, beside the keys' page, and a console line names one as it names a command,
/// <c>time.speed 2</c> setting it and <c>time.speed</c> alone saying it.
/// </para>
/// <para>
/// The settings file keeps none of them, since each is the run's, how its clock goes, the light a
/// reading leaves out or how a tool started it, and the file keeps what a player chooses for the
/// machine, the keys and a game's own settings (<see cref="KeptSettings"/>). One an environment
/// variable gives takes its value for the whole run, as the app's config does, and a set is then
/// kept for the file alone. The commands that set them stand beside them, <c>app.speed</c> and the
/// rest, since scripts and guides name them.
/// </para>
/// </remarks>
internal static partial class EngineSettings
{
    /// <summary>Every setting of the engine's, by name.</summary>
    internal static IReadOnlyList<Setting> All { get; } = [.. Clock(), .. Illumination(), .. Field(), .. Scenes(), .. Run()];

    // Added as the assembly loads, as its commands are, so a line names them before an app draws.
    [ModuleInitializer]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255",
        Justification = "The area's commands are added from its assembly's initializer too, and the settings join them in the same registry.")]
    internal static void Register()
    {
        foreach (var setting in All) Registry.Process.Add(setting);
    }

    // Why a setting of the running app is refused where none runs.
    private const string NoApp = "No app is running to take it.";

    // The running app's clock, or none where no app runs.
    private static Time? RunningClock() => App.Running?.World.TryGetResource<Time>(out var time) == true ? time : null;

    // A setter of the running app's clock, which refuses with the reason where no app runs.
    private static Func<T, string?> OnClock<T>(Action<Time, T> set) => value =>
    {
        if (RunningClock() is not { } time) return NoApp;
        set(time, value);
        return null;
    };

    // A setter of what the running app asked of its light, which refuses where no app runs.
    private static Func<T, string?> OnApp<T>(Action<T> set) => value =>
    {
        if (App.Running is null) return NoApp;
        set(value);
        return null;
    };

    // The game's clock, the seconds a frame moves it by, what holds it, how fast it goes and
    // whether it stops, which app.frametime, app.hold, app.speed and app.pause set as commands.
    private static IEnumerable<Setting> Clock()
    {
        yield return new Setting<double>("time.frame",
            "The seconds each frame moves the game's clock by, reading no clock, or 0 for the machine's clock, as app.frametime sets it.", 0,
            () => RunningClock()?.FrameSeconds ?? 0, apply: OnClock<double>((time, seconds) => time.FrameSeconds = seconds))
        { Low = 0, Step = 1.0 / 60, EnvironmentVariable = Config.FrameTimeVariable, Saved = false };

        yield return new Setting<string>("time.hold",
            "What holds a set clock still, loads while an asset loads, commands between a session's commands, both with a comma between, or off, as app.hold sets it.",
            "off", () => ClockHoldNames.Name(RunningClock()?.Holds ?? ClockHolds.None), apply: Hold)
        { EnvironmentVariable = Config.HoldClockVariable, Saved = false };

        yield return new Setting<float>("time.speed", "How fast the game's clock runs, 1 being the machine's own pace, as app.speed sets it.", 1f,
            () => RunningClock()?.Speed ?? 1f, apply: OnClock<float>((time, speed) => time.SetSpeed(speed)))
        { Low = 0, Step = 0.25, Saved = false };

        yield return new Setting<bool>("time.paused", "Whether the game's clock is stopped, the window and the interface going on, as app.pause sets it.", false,
            () => RunningClock()?.Paused ?? false, apply: OnClock<bool>((time, paused) =>
            {
                if (paused) time.Pause();
                else time.Resume();
            }))
        { Saved = false };

        // The holds' words as app.hold reads them, which an enum's single names could not hold
        // both of.
        static string? Hold(string words)
        {
            if (!ClockHoldNames.TryParse(words, out var holds))
                return $"time.hold takes loads, commands, both with a comma between, or off, not {CommandLine.Quote(words)}.";
            if (RunningClock() is not { } time) return NoApp;
            time.Holds = holds;
            return null;
        }
    }

    // Global illumination's quality, its screen's probes and the bounces its probes are held at
    // for a reading, which gi.set, gi.screen and gi.bounces set as commands.
    private static IEnumerable<Setting> Illumination()
    {
        yield return new Setting<GlobalIllumination>("gi.quality",
            "The quality global illumination is traced through the scene's distance field at, or off, as gi.set sets it.", GlobalIllumination.Off,
            () => WorldProbes.Asked, apply: OnApp<GlobalIllumination>(Render.SetGlobalIllumination))
        { Saved = false };

        yield return new Setting<bool>("gi.screen_probes",
            "Whether global illumination traces its screen's probes, a probe every few pixels of the picture beside the world's, as gi.screen sets it.", true,
            () => WorldProbes.ScreenAsked, apply: OnApp<bool>(Render.SetGlobalIlluminationScreenProbes))
        { Saved = false };

        yield return new Setting<int>("gi.held_bounces",
            "The bounces past the first global illumination's probes are held at for a reading, or -1 for every one as they run, as gi.bounces sets it.", -1,
            () => WorldProbes.HeldBounces ?? -1, apply: HoldBounces)
        { Low = -1, Saved = false };

        static string? HoldBounces(int bounces)
        {
            if (WorldProbes.TracedFrames == 0) return "Global illumination traces no probes.";
            WorldProbes.HoldBounces(bounces < 0 ? null : bounces);
            return null;
        }
    }

    // The scene's distance field, its cascades, the finest's cells, the cascades built a frame,
    // the far cascade's reach and the triangles splatted a frame, which field.set sets together
    // and each of these alone, the others as they were asked.
    private static IEnumerable<Setting> Field()
    {
        static Func<T, string?> With<T>(Func<(int Cascades, float CellSize, int Budget, float FarCascade, int Triangles), T, (int, float, int, float, int)> change) =>
            OnApp<T>(value =>
            {
                var (cascades, cell, budget, far, triangles) = change(SceneField.Asked, value);
                Render.SetSceneField(cascades, cell, budget, far, triangles);
            });

        yield return new Setting<int>("field.cascades", "The cascades the scene's distance field is built in, or 0 for none, as field.set sets it.", 0,
            () => SceneField.Asked.Cascades, apply: With<int>((a, v) => (v, a.CellSize, a.Budget, a.FarCascade, a.Triangles)))
        { Low = 0, High = 8, Saved = false };

        yield return new Setting<float>("field.cell", "How wide the finest cascade's cells are, in meters, as field.set sets it.", 0.25f,
            () => SceneField.Asked.CellSize, apply: With<float>((a, v) => (a.Cascades, v, a.Budget, a.FarCascade, a.Triangles)))
        { Low = 0.001, Step = 0.05, Saved = false };

        yield return new Setting<int>("field.budget", "The most cascades built a frame, as field.set sets it.", 1,
            () => SceneField.Asked.Budget, apply: With<int>((a, v) => (a.Cascades, a.CellSize, v, a.FarCascade, a.Triangles)))
        { Low = 1, Saved = false };

        yield return new Setting<float>("field.far", "How far ahead a far cascade reaches past the others, in meters, or 0 for none, as field.set sets it.", 0f,
            () => SceneField.Asked.FarCascade, apply: With<float>((a, v) => (a.Cascades, a.CellSize, a.Budget, v, a.Triangles)))
        { Low = 0, Step = 8, Saved = false };

        yield return new Setting<int>("field.triangles", "The most triangles splatted into the field a frame, as field.set sets it.", SceneFieldPlan.DefaultTriangleBudget,
            () => SceneField.Asked.Triangles, apply: With<int>((a, v) => (a.Cascades, a.CellSize, a.Budget, a.FarCascade, v)))
        { Low = 1, Step = 4096, Saved = false };
    }

    // The folder the scene packs are fetched into, which BCS_SCENE_PACKS names for a workflow that
    // caches them.
    private static IEnumerable<Setting> Scenes()
    {
        var shared = Path.Combine(UserData.PlatformDirectory(), "BevyCSharp", "scene-packs");
        yield return new Setting<string>("scenes.folder", "The folder the scene packs are fetched into and read from, the shared one where empty.", shared,
            // The folder's setter takes null for the shared one, as its documentation says.
            () => ScenePacks.Folder, folder => ScenePacks.Folder = folder.Length > 0 ? folder : null!)
        { EnvironmentVariable = "BCS_SCENE_PACKS", Saved = false };
    }
}
