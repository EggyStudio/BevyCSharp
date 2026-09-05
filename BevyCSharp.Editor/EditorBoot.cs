using Bevy;
using BevyCSharp.Editor.Behaviors;
using BevyCSharp.Editor.Framework;
using BevyCSharp.Editor.Panels;

namespace BevyCSharp.Editor;

/// <summary>
/// Brings the editor up and drives it, as an ordinary behavior.
/// </summary>
/// <remarks>
/// Nothing here is privileged. The editor is a BevyCSharp app like any other, which is what lets a
/// panel bind straight to engine state rather than through an adapter, and what means anything
/// learned building the editor applies to building a game.
/// </remarks>
[Behavior]
public partial struct EditorBoot
{
    /// <summary>The running app, so the script host can register into it.</summary>
    public static App? Host { get; set; }

    /// <summary>Builds the scene the panels float over, then opens what starts open.</summary>
    [OnStartup]
    public static void Start(BehaviorContext ctx)
    {
        // Before anything else says anything, so the console panel has the startup lines in it.
        EditorLog.Start();

        var camera = Scene(ctx);

        EditorSelection.Camera = camera;
        EditorCommands.Register(camera);

        // Tabs are minimised panels. The asset browser is the one an editor wants at hand and not
        // on screen, which is what a tab is for.
        EditorTabs.Add("Assets", static () => new AssetsPanel());
        EditorTabs.Add("Console", static () => new ConsolePanel());

        // Only the world, the tools, the tabs and the key strip. Everything else appears because
        // something was selected or something was asked for, which is the difference between an
        // editor that starts with work in front of it and one that starts with its own furniture.
        EditorShell.Show(new WorldPanel());
        EditorShell.Show(new LeftBarPanel());
        EditorShell.Show(new CentreBarPanel());
        EditorShell.Show(new RightBarPanel());
        EditorShell.Show(new BottomBarPanel());
        EditorShell.Show(new TabsPanel());
        EditorShell.Show(new KeysPanel());

        EditorProject.RestoreLayout();

        // A right click on nothing offers what can be spawned, which is what a right click on an
        // empty scene means everywhere else.
        EditorShell.ViewportMenu = static (x, y) => EditorShell.ShowMenu("Spawn", x, y, "Spawn");

        Console.WriteLine($"[editor] {EditorShell.Open.Count} panels open");

        if (Host is { } app) EditorScripts.Start(app);
    }

    /// <summary>Puts something in front of the camera, so the viewport is a scene.</summary>
    private static Entity Scene(BehaviorContext ctx)
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

        return camera;
    }

    /// <summary>Rebuilds the scripts when one has changed and settled.</summary>
    [OnUpdate]
    public static void ReloadScripts(BehaviorContext ctx) => EditorScripts.Poll();

    /// <summary>
    /// Opens the entity panel as soon as something is selected.
    /// </summary>
    /// <remarks>
    /// The editor starts with the world and nothing else. Picking something is the moment its
    /// details become worth the room, so that is when the panel appears, and closing it again is a
    /// thing a person does rather than something that happens on the next click.
    /// </remarks>
    [OnUpdate]
    public static void FollowSelection(BehaviorContext ctx)
    {
        // Compared against what was last reacted to rather than against the frame the selection
        // changed on: the shell ticks after this system, so the frame numbers are always one
        // apart and a panel opened by comparing them would never open at all.
        if (EditorSelection.Current == _followed) return;

        _followed = EditorSelection.Current;

        if (!EditorSelection.Any) return;
        if (EditorShell.Find<DataPanel>() is not null) return;

        EditorShell.Show(new DataPanel());
    }

    /// <summary>What was selected when the entity panel was last opened for it.</summary>
    private static Entity _followed = Entity.None;

    /// <summary>
    /// Hands the interface's reports to the panels, once a frame.
    /// </summary>
    /// <remarks>
    /// After the update rather than during it, because the bridge notices a widget's value changing
    /// from a system in the update and the two would otherwise have no order between them. Draining
    /// first and writing back second only works if what the person did this frame has already been
    /// reported.
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
