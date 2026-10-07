using Bevy;
using Bevy.Scripting;

namespace BevyCSharp.Player;

/// <summary>
/// Compiles the asset folder's scripts, spawns the scene, and gives it a camera when it has none.
/// </summary>
/// <remarks>
/// <para>
/// The scripts come first, because a scene written in the editor holds the components its scripts
/// declare, and a component whose type is not registered yet is kept as text rather than spawned.
/// Compiled by the same <see cref="ScriptHost"/> the editor uses, so a script plays as it ran in
/// the editor, and watched, so a script saved while the scene plays is swapped in as it is there.
/// </para>
/// <para>
/// A scene made in the editor is looked at through the editor's own camera, which is the editor's
/// and is not saved with it. So a scene with no camera of its own is given one where the editor's
/// was when Play was pressed (<see cref="View"/>), and the scene opens on what was being looked at.
/// </para>
/// </remarks>
[Behavior]
public partial struct PlayerBoot
{
    /// <summary>The running app, which the scripts register into.</summary>
    public static App? Host { get; set; }

    /// <summary>The scene file to play.</summary>
    public static string Scene { get; set; } = string.Empty;

    /// <summary>Where a camera goes when the scene has none, or nothing for a view of the origin.</summary>
    public static Transform? View { get; set; }

    private static ScriptHost? _scripts;
    private static ScriptWatcher? _watcher;

    /// <summary>
    /// Compiles the asset folder's scripts into an app that has not started to run.
    /// </summary>
    /// <remarks>
    /// Before the run rather than at startup, so a state a script declares on its enum
    /// (<see cref="InitialStateAttribute"/>) is one its systems claimed by the time the app adds
    /// the declared states, as it does in the game the scripts are compiled into.
    /// </remarks>
    public static void Compile(App app, string assets)
    {
        var folder = Path.Combine(assets, "scripts");
        if (!Directory.Exists(folder)) return;

        _scripts = new ScriptHost(app, folder);
        _watcher = new ScriptWatcher(folder);

        Console.WriteLine(_scripts.Reload()
            ? $"[player] scripts loaded: {_scripts.Registered} registration(s)"
            : $"[player] scripts not loaded: {_scripts.LastError}");
    }

    /// <summary>Spawns the scene in front of a camera.</summary>
    [OnStartup]
    public static void Start(BehaviorContext ctx)
    {
        // What did not come back is said by name, since a component a script stopped declaring is
        // the usual reason a played scene looks different from the edited one.
        var load = SceneFile.Load(ctx.Ecs, Scene);
        if (load.Problem is { } problem) Console.WriteLine($"[player] {problem}");
        Console.WriteLine($"[player] playing {Path.GetFileName(Scene)}, {load.Entities.Count} entities");

        foreach (var unknown in load.Unknown) Console.WriteLine($"[player] no type this run knows is called {unknown}");
        foreach (var refused in load.Refused) Console.WriteLine($"[player] not spawned: {refused}");

        // Started from, as a game started from it would be, so a save made while it plays lays
        // itself over this scene.
        SaveGame.Begin(ctx.Ecs, Scene);
    }

    /// <summary>Whether the scene was looked at for a camera of its own yet.</summary>
    private static bool _looked;

    /// <summary>
    /// Gives the scene the editor's view as a camera when nothing in it has one, a frame after it
    /// starts.
    /// </summary>
    /// <remarks>
    /// On the first update rather than at startup, so a camera the game's scripts spawn as they
    /// start is there to be found, and the scene is not given a second one drawing over it.
    /// </remarks>
    [OnUpdate]
    public static void Look(BehaviorContext ctx)
    {
        if (_looked) return;
        _looked = true;

        if (HasCamera(ctx.Ecs)) return;

        var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 50f });
        ctx.Ecs.Add(camera, View ?? Transform.LookingAt(new Vec3(4f, 3f, 7f), Vec3.Zero, Vec3.UnitY));
    }

    /// <summary>Swaps in scripts saved while the scene plays.</summary>
    [OnUpdate]
    public static void Watch(BehaviorContext ctx)
    {
        if (_watcher?.TakeChange() != true || _scripts is null) return;

        Console.WriteLine(_scripts.Reload()
            ? $"[player] scripts reloaded: {_scripts.Registered} registration(s), {_scripts.Carried} component(s) carried over"
            : $"[player] scripts not reloaded: {_scripts.LastError}");
    }

    /// <summary>Whether anything in the world is a camera, by Bevy's own camera component.</summary>
    private static bool HasCamera(EcsWorld world) =>
        world.All().Any(entity => world.ComponentsOf(entity).Any(id =>
            world.ComponentName(id).Contains("bevy_camera::components::Camera", StringComparison.Ordinal)));
}
