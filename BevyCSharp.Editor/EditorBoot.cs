using Bevy;
using BevyCSharp.Editor.Behaviors;
using BevyCSharp.Editor.Framework;

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
        ConsoleLog.Start();

        var camera = Scene(ctx);

        EditorSelection.Camera = camera;
        EditorCommands.Register(camera);

        // The interface: one ImGui context, the editor's style, and the panels that make it.
        EditorShell.Load(EditorPaths.Assets);

        EditorShell.Tabs.Add(("Console", ConsoleTab.Draw));
        EditorShell.Tabs.Add(("Style", StyleTab.Draw));

        EditorProject.RestoreLayout();

        Console.WriteLine("[editor] the interface is up");

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
    /// Hands the interface's reports to the panels, once a frame.
    /// </summary>
    /// <remarks>
    /// After the update rather than during it, because the bridge notices a widget's value changing
    /// from a system in the update and the two would otherwise have no order between them. Draining
    /// first and writing back second only works if what the person did this frame has already been
    /// reported.
    /// </remarks>
    [OnPostUpdate]
    public static void Drive(BehaviorContext ctx)
    {
        // Told the frame before anything writes a line, so the log can say when something was said
        // without every writer having to ask the engine what time it is.
        ConsoleLog.Frame = ctx.Time.FrameCount;

        // Everything the interface is happens between these two: the shell lays the panels out
        // and they draw themselves, and what came of it goes to the renderer.
        EditorShell.Tick(ctx);
        EditorShell.Draw();
    }

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
    /// Drops the console into the window, or puts it away.
    /// </summary>
    /// <remarks>
    /// The key under Escape, which is where every game has put this since Quake. It works while
    /// something is being typed into as well, because what is usually being typed into when
    /// somebody reaches for it is the console itself.
    /// </remarks>
    [OnUpdate]
    public static void ConsoleOnBackquote(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Backquote)) return;

        // The console tab, raised or put away. Where every game has put this since Quake.
        var console = EditorShell.Tabs.FindIndex(tab => tab.Name == "Console");
        if (console >= 0) EditorShell.OpenTab = EditorShell.OpenTab == console ? -1 : console;
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

        // Whatever is being typed into keeps Escape: it is what somebody reaches for when they
        // have changed their mind about a value, and quitting instead throws away more than that.
        if (ImGuiRuntime.WantsKeyboard) return;

        // What is open closes before the program does. A tab is up, and then it is not.
        if (EditorShell.OpenTab >= 0)
        {
            EditorShell.OpenTab = -1;
            return;
        }

        ctx.Exit();
    }
}
