using Bevy;

namespace BevyCSharp.Sample.Panels;

/// <summary>
/// The game's own interface: a readout, two controls and a button.
/// </summary>
/// <remarks>
/// <para>
/// Three files and no more: <c>assets/ui/hud.html</c> says what is in it,
/// <c>assets/ui/hud.css</c> says what it looks like, and this says what it means. Nothing here
/// builds a widget, walks a tree or lays anything out: an attribute ties a field to an element by
/// its id, and the generator writes the reading and writing.
/// </para>
/// <para>
/// It is the same mechanism the editor's panels use, which is the point: a game's interface is not
/// a lesser thing built on a different system.
/// </para>
/// </remarks>
[UiPanel("ui/hud.html", Root = "#hud")]
public sealed partial class HudPanel
{
    /// <summary>How many frames have gone by.</summary>
    [Bind("#hud-frames", Mode = BindMode.OneWay)]
    public string Frames { get; private set; } = "0";

    /// <summary>How many spinning cubes there are.</summary>
    [Bind("#hud-cubes", Mode = BindMode.OneWay)]
    public string Cubes { get; private set; } = "0";

    /// <summary>Whether the cubes are turning, which the tick box writes back.</summary>
    [Bind("#hud-spin")]
    public bool Spinning = true;

    /// <summary>How fast they turn, from nothing to a hundred.</summary>
    [Bind("#hud-speed")]
    public float Speed = 40f;

    /// <summary>Reads the world before the values are written out.</summary>
    [OnRefresh]
    public void Read()
    {
        if (UiHost.Context is not { } ctx) return;

        Frames = ctx.Time.FrameCount.ToString();

        var cubes = 0;
        foreach (var entity in ctx.Ecs.All())
        {
            if (ctx.Ecs.Has<Behaviors.Spinner>(entity)) cubes++;
        }

        Cubes = cubes.ToString();
    }

    /// <summary>Writes the two controls back onto everything that spins.</summary>
    [OnChange]
    public void Apply()
    {
        if (UiHost.Context is not { } ctx) return;

        foreach (var entity in ctx.Ecs.All())
        {
            if (!ctx.Ecs.TryGet<Behaviors.Spinner>(entity, out var spinner)) continue;

            var wanted = Spinning ? Speed * 0.02f : 0f;
            if (Math.Abs(spinner.Speed - wanted) < 0.0001f) continue;

            spinner.Speed = wanted;
            ctx.Ecs.Set(entity, spinner);
        }
    }

    /// <summary>Puts another cube in the world, a little to the side of the last one.</summary>
    [OnClick("#hud-add")]
    public void AddCube()
    {
        if (UiHost.Context is not { } ctx) return;

        var cube = ctx.Ecs.Spawn();

        Render.SetMesh(ctx.Ecs, cube, Render.CreateMesh(MeshShape.Cuboid, 0.8f, 0.8f, 0.8f));
        Render.SetMaterial(
            ctx.Ecs, cube, Render.CreateMaterial(0.85f, 0.55f, 0.25f, roughness: 0.4f));

        _placed++;
        ctx.Ecs.Add(cube, Transform.At(_placed * 1.4f, 1.5f, -2f));
        ctx.Ecs.Add(cube, new Behaviors.Spinner { Speed = Speed * 0.02f });
    }

    /// <summary>How many cubes this panel has put in the world.</summary>
    private int _placed;
}
