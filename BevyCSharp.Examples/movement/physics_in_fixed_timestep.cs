// Bevy's physics_in_fixed_timestep example, examples/movement/physics_in_fixed_timestep.rs at
// v0.19.1, by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Movement;

// A player moved with WASD among spheres, its camera turned with the mouse. Its movement is worked
// out in fixed steps, as a game's physics is, and drawn every frame between the place before the
// last step and the place after it, by how far the fixed clock has run past that step, so it moves
// smoothly whatever the frame rate.
internal static class PhysicsInFixedTimestep
{
    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Player.FixedRan = false;
            Player.Camera = ecs.Camera(Transform.Identity);

            var player = ecs.Spawn();
            ecs.Add(player, new Transform(Vec3.Zero, Quat.Identity, new Vec3(0.3f)));
            ecs.Add(player, new Player());

            // Tailwind's sky at 200, six spheres across, four up and ten deep, three apart.
            var sphere = Render.CreateMesh(MeshShape.Sphere, 0.3f);
            var sky = Scene.Material(Scene.Srgb8(186, 230, 253));
            const int Across = 6, Up = 4, Deep = 10;
            const float Distance = 3f;
            for (var x = 0; x < Across; x++)
            {
                for (var y = 0; y < Up; y++)
                {
                    for (var z = 0; z < Deep; z++)
                    {
                        ecs.Mesh(sphere, sky, Transform.At(
                            x * Distance - (Across - 1) * Distance / 2f,
                            y * Distance - (Up - 1) * Distance / 2f,
                            z * Distance - (Deep - 1) * Distance / 2f));
                    }
                }
            }

            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f });
            ecs.Add(sun, Transform.LookingAt(Vec3.Zero, new Vec3(-1f, -3f, 0.5f), Vec3.UnitY));

            var column = Ui.SpawnNode(new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f), Direction = UiDirection.Column });
            ecs.SetParent(Ui.SpawnText("Move the player with WASD", new UiSettings(), 25f), column);
            ecs.SetParent(Ui.SpawnText("Rotate the camera with the mouse", new UiSettings(), 25f), column);
        }, "physics_in_fixed_timestep.Setup");
    }
}

/// <summary>
/// The player, its input gathered before the fixed steps, moved in them, and drawn after them
/// between its last two places.
/// </summary>
[Behavior]
public partial struct Player
{
    /// <summary>The camera the player looks through, which turns with the mouse and follows the player.</summary>
    public static Entity Camera;

    /// <summary>Whether a fixed step ran this frame, so the input it used can be let go.</summary>
    public static bool FixedRan;

    private const float Speed = 4f;
    private static readonly Vec2 Sensitivity = new(0.003f, 0.002f);

    /// <summary>Which way the keys held this frame ask to go, across and ahead.</summary>
    public Vec2 AccumulatedInput;

    /// <summary>How fast it moves, in world units a second.</summary>
    public Vec3 Velocity;

    /// <summary>Where the fixed steps have it now.</summary>
    public Vec3 PhysicalTranslation;

    /// <summary>Where they had it before the last step.</summary>
    public Vec3 PreviousPhysicalTranslation;

    /// <summary>The camera turned by the mouse, then the keys read as a direction it faces.</summary>
    [OnPreUpdate]
    public void AccumulateInput(BehaviorContext ctx)
    {
        FixedRan = false;
        var ecs = ctx.Ecs;
        var camera = ecs.GetOrDefault<Transform>(Camera);

        var (dx, dy) = ctx.Input.MouseDelta;
        if (dx != 0f || dy != 0f)
        {
            // Yaw about the world's up and pitch about the camera's own right, the pitch kept
            // short of straight up or down.
            var forward = camera.Rotation * -Vec3.UnitZ;
            var yaw = MathF.Atan2(-forward.X, -forward.Z) - dx * Sensitivity.X;
            var pitch = Math.Clamp(MathF.Asin(Math.Clamp(forward.Y, -1f, 1f)) - dy * Sensitivity.Y, -MathF.PI / 2f + 0.01f, MathF.PI / 2f - 0.01f);
            camera.Rotation = Quat.FromRotationY(yaw) * Quat.FromRotationX(pitch);
            ecs.Set(Camera, camera);
        }

        var input = ctx.Input;
        AccumulatedInput = new Vec2(
            (input.KeyDown(Key.D) ? 1f : 0f) - (input.KeyDown(Key.A) ? 1f : 0f),
            (input.KeyDown(Key.W) ? 1f : 0f) - (input.KeyDown(Key.S) ? 1f : 0f));

        var moved = camera.Rotation * new Vec3(AccumulatedInput.X, 0f, -AccumulatedInput.Y);
        Velocity = (moved.Length > 1f ? moved.Normalized : moved) * Speed;
    }

    /// <summary>One fixed step of movement, the place before it kept.</summary>
    [OnFixedUpdate]
    public void AdvancePhysics(BehaviorContext ctx)
    {
        FixedRan = true;
        PreviousPhysicalTranslation = PhysicalTranslation;
        PhysicalTranslation += Velocity * ctx.Time.FixedDelta;
    }

    /// <summary>Drawn between its last two places, and the camera put where it is.</summary>
    [OnUpdate]
    public void InterpolateRenderedTransform(BehaviorContext ctx, ref Transform transform)
    {
        if (FixedRan) AccumulatedInput = default;

        var alpha = ctx.Time.FixedOverstep;
        transform.Translation = PreviousPhysicalTranslation + (PhysicalTranslation - PreviousPhysicalTranslation) * alpha;

        var camera = ctx.Ecs.GetOrDefault<Transform>(Camera);
        ctx.Ecs.Set(Camera, camera with { Translation = transform.Translation });
    }
}
