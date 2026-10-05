// Bevy's rotation example, examples/2d/rotation.rs at v0.19.1, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.TwoD;

// Demonstrates rotating entities in 2D, a ship flown with the arrow keys, two enemies snapping to
// face it and two turning toward it at their own speeds.
internal static class Rotation2d
{
    private static readonly (float X, float Y) Bounds = (1200f, 640f);
    private const float MovementSpeed = 500f;
    private const float RotationSpeed = MathF.PI * 2f;

    private static Entity _player;
    private static readonly List<Entity> Snapping = [];
    private static readonly List<(Entity Enemy, float Speed)> Turning = [];

    public static void Configure(Config config) => config.FixedHz = 60;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Snapping.Clear();
            Turning.Clear();
            Render2d.SpawnCamera2d();
            Ui.SpawnText("Up Arrow: Move Forward\nLeft / Right Arrow: Turn", new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });

            var ship = AssetServer.Load(AssetKind.Image, "textures/simplespace/ship_C.png");
            var enemyA = AssetServer.Load(AssetKind.Image, "textures/simplespace/enemy_A.png");
            var enemyB = AssetServer.Load(AssetKind.Image, "textures/simplespace/enemy_B.png");
            var (across, up) = (Bounds.X / 4f, Bounds.Y / 4f);

            _player = Sprite(ecs, ship, Vec3.Zero);
            Snapping.Add(Sprite(ecs, enemyA, new Vec3(-across, 0f, 0f)));
            Snapping.Add(Sprite(ecs, enemyA, new Vec3(0f, -up, 0f)));
            Turning.Add((Sprite(ecs, enemyB, new Vec3(across, 0f, 0f)), MathF.PI / 4f));
            Turning.Add((Sprite(ecs, enemyB, new Vec3(0f, up, 0f)), MathF.PI / 2f));
        }, "rotation.Setup");

        app.On(Stage.FixedUpdate, ctx =>
        {
            var ecs = ctx.Ecs;
            var input = ctx.Input;
            var dt = ctx.Time.FixedDelta;

            // The ship turns on left and right and flies along its own up on the up arrow, kept
            // inside the bounds.
            var player = ecs.GetOrDefault<Transform>(_player);
            var turn = (input.KeyDown(Key.ArrowLeft) ? 1f : 0f) - (input.KeyDown(Key.ArrowRight) ? 1f : 0f);
            player.Rotation = Quat.FromRotationZ(turn * RotationSpeed * dt) * player.Rotation;
            var forward = player.Rotation * Vec3.UnitY;
            var moved = player.Translation + forward * ((input.KeyDown(Key.ArrowUp) ? 1f : 0f) * MovementSpeed * dt);
            player.Translation = new Vec3(Math.Clamp(moved.X, -Bounds.X / 2f, Bounds.X / 2f), Math.Clamp(moved.Y, -Bounds.Y / 2f, Bounds.Y / 2f), 0f);
            ecs.Set(_player, player);

            // These face the ship at once.
            foreach (var enemy in Snapping)
            {
                var transform = ecs.GetOrDefault<Transform>(enemy);
                var to = player.Translation - transform.Translation;
                transform.Rotation = Quat.FromRotationZ(MathF.Atan2(to.Y, to.X) - MathF.PI / 2f);
                ecs.Set(enemy, transform);
            }

            // These turn toward it at their speed, by no more than the angle left.
            foreach (var (enemy, speed) in Turning)
            {
                var transform = ecs.GetOrDefault<Transform>(enemy);
                var to = (player.Translation - transform.Translation).Normalized;
                var front = transform.Rotation * Vec3.UnitY;
                var dot = Math.Clamp(front.X * to.X + front.Y * to.Y, -1f, 1f);
                if (MathF.Abs(dot - 1f) < float.Epsilon) continue;

                var right = transform.Rotation * Vec3.UnitX;
                var sign = right.X * to.X + right.Y * to.Y >= 0f ? -1f : 1f;
                transform.Rotation = Quat.FromRotationZ(sign * MathF.Min(speed * dt, MathF.Acos(dot))) * transform.Rotation;
                ecs.Set(enemy, transform);
            }
        }, "rotation.Systems");
    }

    private static Entity Sprite(EcsWorld ecs, AssetHandle image, Vec3 at)
    {
        var sprite = ecs.Spawn();
        ecs.Add(sprite, new Transform(at));
        Render2d.SetSprite(ecs, sprite, image);
        return sprite;
    }
}
