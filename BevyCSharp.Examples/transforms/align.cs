// Bevy's align example, examples/transforms/align.rs at v0.19.1, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Transforms;

// Demonstrates aligning a ship by two of its axes to two directions, its nose to the white arrow
// exactly and its right wing as near the gray one as it can be.
internal static class Align
{
    private const string InstructionsText =
        "The bright red axis is the primary alignment axis, and it will always be\n"
        + "made to coincide with the primary target direction (white) exactly.\n"
        + "The fainter red axis is the secondary alignment axis, and it is made to\n"
        + "line up with the secondary target direction (gray) as closely as possible.\n"
        + "Press 'R' to generate random target directions.\n"
        + "Press 'T' to align the ship to those directions.\n"
        + "Click and drag the mouse to rotate the camera.\n"
        + "Press 'H' to hide/show these instructions.";

    // Bevy's SeededRng resource, seeded so the example runs the same each time.
    private static Random _seededRng = new(19878367);

    private static Entity _camera;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            _seededRng = new Random(19878367);

            _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(3f, 2.5f, 4f), Vec3.Zero, Vec3.UnitY));
            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 100f, 100f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.5f, 0.3f)), Transform.At(0f, -2f, 0f));
            ecs.SpawnPointLight(new Vec3(4f, 7f, -4f), shadows: true);

            ecs.Add(ecs.Spawn(), new RandomAxes { First = RandomDirection(), Second = RandomDirection() });
            ecs.Add(Ui.SpawnText(InstructionsText, new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) }), new Instructions());
        }, "align.Setup");

        // The ship, given its first target once its scene is in the world, from the axes made above.
        app.SpawnGltf("models/ship/craft_speederD.gltf", (ctx, ship) =>
        {
            foreach (var axes in ctx.Ecs.Query<RandomAxes>(markChanged: false))
                ctx.Ecs.Add(ship, new Ship { TargetRotation = axes.Component.TargetAlignment() });
        });

        app.Update(HandleMouse, "align.HandleMouse");
    }

    // A drag turns the camera about the middle, a pixel being a seventy-fifth of a radian.
    private static void HandleMouse(BehaviorContext ctx)
    {
        var input = ctx.Input;
        if (!input.MouseDown(MouseButton.Left) || input.MouseDeltaX == 0f) return;

        var camera = ctx.Ecs.GetOrDefault<Transform>(_camera);
        var turn = Quat.FromRotationY(-input.MouseDeltaX / 75f);
        ctx.Ecs.Set(_camera, new Transform(turn * camera.Translation, turn * camera.Rotation, camera.Scale));
    }

    // A direction picked evenly over the sphere, from three normally spread numbers.
    internal static Vec3 RandomDirection()
    {
        float Normal() => MathF.Sqrt(-2f * MathF.Log(1f - _seededRng.NextSingle())) * MathF.Cos(MathF.Tau * _seededRng.NextSingle());
        return new Vec3(Normal(), Normal(), Normal()).Normalized;
    }
}

/// <summary>A ship turning from where it faces now to its target.</summary>
[Behavior]
public partial struct Ship
{
    /// <summary>Where the ship ends its turn.</summary>
    public Quat TargetRotation;

    /// <summary>Whether it is turning, which the turn can be paused by.</summary>
    public bool InMotion;

    /// <summary>The ship's nose, its -Z, and its right wing, its X, drawn as arrows from where it stands.</summary>
    [OnUpdate]
    public void DrawShipAxes(BehaviorContext ctx, in Transform transform)
    {
        Gizmos.Arrow(transform.Translation, transform.Translation + transform.Rotation * -Vec3.UnitZ * 1.5f, (1f, 0f, 0f, 1f));
        Gizmos.Arrow(transform.Translation, transform.Translation + transform.Rotation * Vec3.UnitX * 1.5f, Color.FromSrgb(0.65f, 0f, 0f));
    }

    /// <summary>
    /// R picks new directions and stops the ship to turn from where it faces to them, T starts or
    /// pauses the turn, and H hides or shows the instructions.
    /// </summary>
    [OnUpdate]
    public void HandleKeypress(BehaviorContext ctx)
    {
        var input = ctx.Input;
        if (input.KeyPressed(Key.R))
        {
            foreach (var row in ctx.Ecs.Query<RandomAxes>())
            {
                ref var axes = ref row.Component;
                (axes.First, axes.Second) = (Align.RandomDirection(), Align.RandomDirection());
                (InMotion, TargetRotation) = (false, axes.TargetAlignment());
            }
        }

        if (input.KeyPressed(Key.T)) InMotion = !InMotion;

        if (input.KeyPressed(Key.H))
        {
            foreach (var instructions in ctx.Ecs.Query<Instructions>(markChanged: false))
            {
                ref var visibility = ref ctx.Ecs.GetRef<Visibility>(instructions.Entity);
                visibility.Mode = visibility.Mode == VisibilityMode.Hidden ? VisibilityMode.Visible : VisibilityMode.Hidden;
            }
        }
    }

    /// <summary>
    /// Turned toward its target by Bevy's <c>smooth_nudge</c>, which closes the same share of the
    /// gap each second whatever the frame rate, and stopped once it is there.
    /// </summary>
    [OnUpdate]
    [After("Ship.HandleKeypress")]
    public void RotateShip(BehaviorContext ctx, ref Transform transform)
    {
        if (!InMotion) return;

        transform.Rotation = Quat.Slerp(transform.Rotation, TargetRotation, 1f - MathF.Exp(-3f * ctx.Time.Delta));
        var (now, target) = (transform.Rotation, TargetRotation);
        if (MathF.Abs(now.X * target.X + now.Y * target.Y + now.Z * target.Z + now.W * target.W) >= 1f - 1e-7f) InMotion = false;
    }
}

/// <summary>The two directions the ship is aligned to, its nose to the first and its right wing toward the second.</summary>
[Behavior]
public partial struct RandomAxes
{
    /// <summary>The direction the nose is turned to exactly.</summary>
    public Vec3 First;

    /// <summary>The direction the right wing is turned as near as it can be.</summary>
    public Vec3 Second;

    /// <summary>The first drawn in white and the second in gray, from the middle.</summary>
    [OnUpdate]
    public void DrawRandomAxes(BehaviorContext ctx)
    {
        Gizmos.Arrow(Vec3.Zero, First * 1.5f, (1f, 1f, 1f, 1f));
        Gizmos.Arrow(Vec3.Zero, Second * 1.5f, Color.FromSrgb(0.5f, 0.5f, 0.5f));
    }

    /// <summary>
    /// Bevy's <c>Transform::aligned_by</c>, the rotation taking the ship's nose, -Z, to the first
    /// exactly and its right, X, as near the second as can be, by building the turned axes and
    /// reading the rotation off them.
    /// </summary>
    public readonly Quat TargetAlignment()
    {
        var back = -First;
        var right = (Second - First * Vec3.Dot(Second, First)).Normalized;
        return Quat.FromBasis(right, Vec3.Cross(back, right), back);
    }
}

/// <summary>The instructions, which H hides and shows.</summary>
[Behavior]
public partial struct Instructions;
