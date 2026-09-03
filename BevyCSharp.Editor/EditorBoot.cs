using Bevy;
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

        var sun = Render.SpawnLight(new LightSettings
        {
            Kind = LightKind.Directional,
            Intensity = 11_000f,
        });
        ctx.Ecs.Add(sun, Transform.LookingAt(new Vec3(5f, 4f, 3f), Vec3.Zero, Vec3.UnitY));

        // Something to look at behind the panels, so the viewport reads as a scene rather than a
        // background colour. The scene fills the window; the panels sit over it.
        var cube = ctx.Ecs.Spawn();
        Render.SetMesh(ctx.Ecs, cube, Render.CreateMesh(MeshShape.Cuboid, 2f, 2f, 2f));
        Render.SetMaterial(ctx.Ecs, cube,
            Render.CreateMaterial(0.3f, 0.5f, 0.9f, metallic: 0.1f, roughness: 0.4f));
        ctx.Ecs.Add(cube, Transform.Identity);

        var ground = ctx.Ecs.Spawn();
        Render.SetMesh(ctx.Ecs, ground, Render.CreateMesh(MeshShape.Plane, 30f, 30f));
        Render.SetMaterial(ctx.Ecs, ground, Render.CreateMaterial(0.12f, 0.13f, 0.16f));
        ctx.Ecs.Add(ground, Transform.At(0f, -1.2f, 0f));

        EditorShell.Show(new PostPanel(camera));
        Console.WriteLine($"[editor] {EditorShell.Open.Count} panel open");

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

    /// <summary>Hands the interface's reports to the panels, once a frame.</summary>
    [OnUpdate]
    public static void Drive(BehaviorContext ctx) => EditorShell.Tick();

    // TEMPORARY PROBE: a reload respawns the widgets, so the entity behind an id changes.
    private static ulong _lastApply;

    [OnUpdate]
    public static void WatchReload(BehaviorContext ctx)
    {
        if (ctx.Time.FrameCount % 30 != 0) return;

        var apply = Xui.Element("apply");
        if (apply.Bits == _lastApply) return;

        Console.WriteLine($"[probe] frame {ctx.Time.FrameCount}: 'apply' is now {apply}");
        _lastApply = apply.Bits;
    }

    // TEMPORARY DIAGNOSTIC: capture the window so the picture can be checked without an eye.
    [OnUpdate]
    public static void Capture(BehaviorContext ctx)
    {
        if (ctx.Time.FrameCount != 180) return;

        var path = Environment.GetEnvironmentVariable("BCS_SHOT");
        if (string.IsNullOrEmpty(path)) return;

        Render.Screenshot(path);
        Console.WriteLine($"[diag] capturing to {path}");
    }

    /// <summary>Closes on Escape.</summary>
    [OnUpdate]
    public static void QuitOnEscape(BehaviorContext ctx)
    {
        if (ctx.Input.KeyPressed(Key.Escape)) ctx.Exit();
    }
}
