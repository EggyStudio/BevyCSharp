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

    /// <summary>Closes on Escape.</summary>
    [OnUpdate]
    public static void QuitOnEscape(BehaviorContext ctx)
    {
        if (ctx.Input.KeyPressed(Key.Escape)) ctx.Exit();
    }
}
