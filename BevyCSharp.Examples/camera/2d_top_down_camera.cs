// Bevy's 2d_top_down_camera example, examples/camera/2d_top_down_camera.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Cameras;

// Shows a 2D camera following a player smoothly from above, the player a bright light moved with
// WASD over a dark field, bloomed so it glows.
internal static class TopDownCamera2d
{
    internal const float PlayerSpeed = 100f;

    // How quickly the camera closes on the player, as the rate of Bevy's smooth_nudge.
    internal const float CameraDecayRate = 2f;

    // The camera, which Bevy finds by its Camera2d.
    internal static Entity Camera;

    public static void Build(App app) => app.Startup(Setup, "2d_top_down_camera.Setup");

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        // The world the player moves over, and the player, brighter than white so it blooms.
        Entity Shape(AssetHandle mesh, (float R, float G, float B, float A) color, float z)
        {
            var entity = ecs.Spawn();
            ecs.Add(entity, Transform.At(0f, 0f, z));
            Render2d.SetMesh(ecs, entity, mesh);
            Render2d.SetMaterial(ecs, entity, Render2d.CreateMaterial(new ColorMaterialSettings { Color = color }));
            return entity;
        }

        Shape(Render.CreateMesh(MeshShape.Rectangle, 1000f, 700f), Color.FromSrgb(0.2f, 0.2f, 0.3f), 0f);
        ecs.Add(Shape(Render.CreateMesh(MeshShape.Circle, 25f), Color.FromSrgb(6.25f, 9.4f, 9.1f), 2f), new Player());

        Ui.SpawnText("Move the light with WASD.\nThe camera will smoothly track the light.",
            new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });

        Camera = Render2d.SpawnCamera2d();
        ecs.Insert<BloomRef>(Camera);
    }
}

/// <summary>The light the player moves, which the camera follows.</summary>
[Behavior]
public partial struct Player
{
    /// <summary>
    /// A rough walk with WASD, enough to have something for the camera to follow, and then the
    /// camera moved part of the way to it, as Bevy chains its <c>move_player</c> and
    /// <c>update_camera</c>.
    /// </summary>
    [OnUpdate]
    public void MovePlayer(BehaviorContext ctx, ref Transform transform)
    {
        var input = ctx.Input;
        var direction = new Vec2(
            (input.KeyDown(Key.D) ? 1f : 0f) - (input.KeyDown(Key.A) ? 1f : 0f),
            (input.KeyDown(Key.W) ? 1f : 0f) - (input.KeyDown(Key.S) ? 1f : 0f));

        // Normalized, so a diagonal is no faster than a straight line.
        if (direction != Vec2.Zero)
        {
            var step = direction * (TopDownCamera2d.PlayerSpeed * ctx.Time.Delta / MathF.Sqrt(direction.X * direction.X + direction.Y * direction.Y));
            transform.Translation += new Vec3(step.X, step.Y, 0f);
        }

        // The part of the way the camera moves decays with time as Bevy's smooth_nudge does, so
        // the chase is the same at any frame rate.
        var camera = ctx.Ecs.GetOrDefault<Transform>(TopDownCamera2d.Camera);
        var target = new Vec3(transform.Translation.X, transform.Translation.Y, camera.Translation.Z);
        var t = 1f - MathF.Exp(-TopDownCamera2d.CameraDecayRate * ctx.Time.Delta);
        ctx.Ecs.Set(TopDownCamera2d.Camera, camera with { Translation = camera.Translation + (target - camera.Translation) * t });
    }
}
