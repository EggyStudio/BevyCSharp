using Bevy;
using BevyCSharp.Editor.Behaviors;
using BevyCSharp.Editor.Framework;
using BevyCSharp.Editor.Panels;

namespace BevyCSharp.Editor;

/// <summary>
/// Brings the editor up and drives it, as an ordinary behavior.
/// </summary>
/// <remarks>
/// Nothing here is privileged. The editor is a BevyCSharp app like any other, which is what lets
/// a panel bind straight to engine state rather than through an adapter, and what means anything
/// learned building the editor applies to building a game.
/// </remarks>
[Behavior]
public partial struct EditorBoot
{
    /// <summary>Builds the scene the panels float over, then opens them.</summary>
    [OnStartup]
    public static void Start(BehaviorContext ctx)
    {
        var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 50f });
        ctx.Ecs.Add(camera, Transform.LookingAt(new Vec3(4f, 3f, 7f), Vec3.Zero, Vec3.UnitY));
        Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });

        // The camera is steered the way a scene view is, so what the editor shows is something a
        // person can move around in rather than a fixed picture with panels over it.
        ctx.Ecs.Add(camera, FlyCamera.LookingAt(new Vec3(4f, 3f, 7f), Vec3.Zero));
        ctx.Ecs.SetName(camera, "Scene camera");

        var sun = Render.SpawnLight(new LightSettings
        {
            Kind = LightKind.Directional,
            Intensity = 11_000f,
        });
        ctx.Ecs.Add(sun, Transform.LookingAt(new Vec3(5f, 4f, 3f), Vec3.Zero, Vec3.UnitY));
        ctx.Ecs.SetName(sun, "Sun");

        // Something to look at behind the panels, so the viewport reads as a scene rather than a
        // background colour. The scene fills the window; the panels sit over it.
        var cube = ctx.Ecs.Spawn();
        Render.SetMesh(ctx.Ecs, cube, Render.CreateMesh(MeshShape.Cuboid, 2f, 2f, 2f));
        Render.SetMaterial(ctx.Ecs, cube,
            Render.CreateMaterial(0.3f, 0.5f, 0.9f, metallic: 0.1f, roughness: 0.4f));
        ctx.Ecs.Add(cube, Transform.Identity);
        ctx.Ecs.SetName(cube, "Cube");

        var ground = ctx.Ecs.Spawn();
        Render.SetMesh(ctx.Ecs, ground, Render.CreateMesh(MeshShape.Plane, 30f, 30f));
        Render.SetMaterial(ctx.Ecs, ground, Render.CreateMaterial(0.12f, 0.13f, 0.16f));
        ctx.Ecs.Add(ground, Transform.At(0f, -1.2f, 0f));
        ctx.Ecs.SetName(ground, "Ground");

        // What the editor opens with. Every one of these is closed and reopened from the toolbar,
        // and the arrangement of them is the layout rather than anything built into the shell.
        EditorKeys.Camera = camera;

        EditorShell.Show(new ToolbarPanel(camera));
        EditorShell.Show(new HierarchyPanel());
        EditorShell.Show(new InspectorPanel());
        EditorShell.Show(new StatsPanel());
        EditorShell.Show(new PostPanel(camera));

        // A saved arrangement, if there is one. Restored after the panels are shown rather than
        // before, because a placement is looked up by panel and a panel that is not open has
        // nowhere for one to land.
        if (File.Exists(EditorPaths.Layout))
        {
            EditorShell.Layout.Restore(File.ReadAllText(EditorPaths.Layout));
            Console.WriteLine($"[editor] layout restored from {EditorPaths.Layout}");
        }

        Console.WriteLine($"[editor] {EditorShell.Open.Count} panels open");

        StartScripts();
    }

    /// <summary>The running app, so the script host can register into it.</summary>
    public static App? Host { get; set; }

    private static ScriptHost? _scripts;
    private static ScriptWatcher? _watcher;

    /// <summary>Compiles the scripts directory and starts watching it.</summary>
    private static void StartScripts()
    {
        if (Host is null) return;

        var directory = Path.Combine(AppContext.BaseDirectory, "assets", "scripts");

        _scripts = new ScriptHost(Host, directory);
        _watcher = new ScriptWatcher(directory);

        Build(first: true);
    }

    /// <summary>Compiles the scripts and swaps the result in.</summary>
    private static void Build(bool first)
    {
        if (_scripts is null) return;

        if (!_scripts.Reload())
        {
            Console.WriteLine($"[editor] scripts not loaded: {_scripts.LastError}");
            return;
        }

        Console.WriteLine(
            $"[editor] scripts {(first ? "loaded" : "reloaded")}: "
            + $"{_scripts.Registered} registration(s)");
    }

    /// <summary>Rebuilds when a script file has changed and settled.</summary>
    /// <remarks>
    /// A generation's startup runs as it is registered, which is inside this system, so a script
    /// spawns what it needs and cleans up what the last one left.
    /// </remarks>
    [OnUpdate]
    public static void ReloadScripts(BehaviorContext ctx)
    {
        if (_watcher?.TakeChange() != true) return;

        Build(first: false);
    }

    /// <summary>
    /// Hands the interface's reports to the panels, once a frame.
    /// </summary>
    /// <remarks>
    /// After the update rather than during it, because the bridge notices a widget's value
    /// changing from a system in the update and the two would otherwise have no order between
    /// them. Draining first and writing back second only works if what the person did this frame
    /// has already been reported: the other way round, a panel writes its own value over the edit
    /// before anything has read it, and the control appears to ignore the press.
    /// </remarks>
    [OnPostUpdate]
    public static void Drive(BehaviorContext ctx) => EditorShell.Tick(ctx);

    /// <summary>
    /// Writes the window to a PNG when <c>BCS_SHOT</c> names one, then keeps running.
    /// </summary>
    /// <remarks>
    /// For checking that the editor draws what it should without a person watching it. Whether a
    /// panel is laid out correctly, or whether the scene behind it is there at all, is not
    /// something a test can assert and not something a log line shows.
    /// </remarks>
    [OnUpdate]
    public static void Capture(BehaviorContext ctx)
    {
        var path = Environment.GetEnvironmentVariable("BCS_SHOT");
        if (string.IsNullOrEmpty(path)) return;

        // Which frame, so a capture can be taken after something was changed on disk rather than
        // only at the start. Hot reload is the one thing no still picture of frame 180 can show.
        var chosen = Environment.GetEnvironmentVariable("BCS_SHOT_FRAME");
        var wanted = int.TryParse(chosen, out var frame) ? frame : 180;

        if (ctx.Time.FrameCount != (ulong)wanted) return;

        Render.Screenshot(path);
        Console.WriteLine($"[editor] captured the window to {path}");
    }

    /// <summary>
    /// Closes on Escape, unless something is being typed into.
    /// </summary>
    /// <remarks>
    /// A person clearing a value field and changing their mind reaches for Escape, and an editor
    /// that quits at that point has thrown away more than the edit.
    /// </remarks>
    [OnUpdate]
    public static void QuitOnEscape(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Escape)) return;
        if (!PanelBinding.Focused.IsNone) return;

        ctx.Exit();
    }
}
