// Bevy's 2d_top_down_camera example, examples/camera/2d_top_down_camera.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Cameras;

// Shows a 2D camera following a player smoothly from above, the player a bright light moved with
// WASD over a dark field, bloomed so it glows.
internal static class TopDownCamera2d
{
    private const float PlayerSpeed = 100f;

    // How quickly the camera closes on the player, as the rate of Bevy's smooth_nudge.
    private const float CameraDecayRate = 2f;

    private static Entity _camera, _player;

    public static void Build(App app)
    {
        app.Startup(Setup, "2d_top_down_camera.Setup");
        app.Update(MovePlayer, "2d_top_down_camera.MovePlayer");
        app.Update(UpdateCamera, "2d_top_down_camera.UpdateCamera");
    }

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
        _player = Shape(Render.CreateMesh(MeshShape.Circle, 25f), Color.FromSrgb(6.25f, 9.4f, 9.1f), 2f);

        Ui.SpawnText("Move the light with WASD.\nThe camera will smoothly track the light.",
            new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });

        _camera = Render2d.SpawnCamera2d();
        ecs.Insert<BloomRef>(_camera);
    }

    // A rough walk, enough to have something for the camera to follow.
    private static void MovePlayer(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var direction = new Vec2(
            (input.KeyDown(Key.D) ? 1f : 0f) - (input.KeyDown(Key.A) ? 1f : 0f),
            (input.KeyDown(Key.W) ? 1f : 0f) - (input.KeyDown(Key.S) ? 1f : 0f));
        if (direction == Vec2.Zero) return;

        // Normalized, so a diagonal is no faster than a straight line.
        var step = direction * (PlayerSpeed * ctx.Time.Delta / MathF.Sqrt(direction.X * direction.X + direction.Y * direction.Y));
        var at = ctx.Ecs.GetOrDefault<Transform>(_player);
        ctx.Ecs.Set(_player, at with { Translation = at.Translation + new Vec3(step.X, step.Y, 0f) });
    }

    // The camera moves part of the way to the player each frame, the part decaying with time as
    // Bevy's smooth_nudge does, so the chase is the same at any frame rate.
    private static void UpdateCamera(BehaviorContext ctx)
    {
        var player = ctx.Ecs.GetOrDefault<Transform>(_player).Translation;
        var camera = ctx.Ecs.GetOrDefault<Transform>(_camera);
        var target = new Vec3(player.X, player.Y, camera.Translation.Z);
        var t = 1f - MathF.Exp(-CameraDecayRate * ctx.Time.Delta);
        ctx.Ecs.Set(_camera, camera with { Translation = camera.Translation + (target - camera.Translation) * t });
    }
}
