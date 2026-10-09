// Bevy's rotation example, examples/2d/rotation.rs at v0.20.0, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.TwoD;

// Demonstrates rotating entities in 2D, a ship flown with the arrow keys, two enemies snapping to
// face it and two turning toward it at their own speeds.
internal static class Rotation2d
{
    internal static readonly (float X, float Y) Bounds = (1200f, 640f);

    public static void Configure(Config config) => config.FixedHz = 60;

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();
        Ui.SpawnText("Up Arrow: Move Forward\nLeft / Right Arrow: Turn", new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });

        var ship = AssetServer.Load(AssetKind.Image, "textures/simplespace/ship_C.png");
        var enemyA = AssetServer.Load(AssetKind.Image, "textures/simplespace/enemy_A.png");
        var enemyB = AssetServer.Load(AssetKind.Image, "textures/simplespace/enemy_B.png");
        var (across, up) = (Bounds.X / 4f, Bounds.Y / 4f);

        ecs.Add(Sprite(ecs, ship, Vec3.Zero), new Player { MovementSpeed = 500f, RotationSpeed = MathF.PI * 2f });
        ecs.Add(Sprite(ecs, enemyA, new Vec3(-across, 0f, 0f)), new SnapToPlayer());
        ecs.Add(Sprite(ecs, enemyA, new Vec3(0f, -up, 0f)), new SnapToPlayer());
        ecs.Add(Sprite(ecs, enemyB, new Vec3(across, 0f, 0f)), new RotateToPlayer { RotationSpeed = MathF.PI / 4f });
        ecs.Add(Sprite(ecs, enemyB, new Vec3(0f, up, 0f)), new RotateToPlayer { RotationSpeed = MathF.PI / 2f });
    }, "rotation.Setup");

    private static Entity Sprite(EcsWorld ecs, AssetHandle image, Vec3 at)
    {
        var sprite = ecs.Spawn();
        ecs.Add(sprite, new Transform(at));
        Render2d.SetSprite(ecs, sprite, image);
        return sprite;
    }

    // Where the player's ship is, as Bevy's systems take it from the one entity with Player.
    internal static Vec3? PlayerAt(BehaviorContext ctx)
    {
        foreach (var player in ctx.Ecs.Query<Player>(markChanged: false)) return ctx.Ecs.GetOrDefault<Transform>(player.Entity).Translation;
        return null;
    }
}

/// <summary>The player's ship, and how fast it flies and turns.</summary>
[Behavior]
public partial struct Player
{
    /// <summary>How fast it flies, in pixels a second.</summary>
    public float MovementSpeed;

    /// <summary>How fast it turns, in radians a second.</summary>
    public float RotationSpeed;

    /// <summary>
    /// Turned by left and right and flown along its own up by the up arrow, kept inside the bounds,
    /// on the fixed timestep.
    /// </summary>
    [OnFixedUpdate]
    public void PlayerMovementSystem(BehaviorContext ctx, ref Transform transform)
    {
        var input = ctx.Input;
        var dt = ctx.Time.FixedDelta;
        var turn = (input.KeyDown(Key.ArrowLeft) ? 1f : 0f) - (input.KeyDown(Key.ArrowRight) ? 1f : 0f);
        transform.Rotation = Quat.FromRotationZ(turn * RotationSpeed * dt) * transform.Rotation;

        var forward = transform.Rotation * Vec3.UnitY;
        var moved = transform.Translation + forward * ((input.KeyDown(Key.ArrowUp) ? 1f : 0f) * MovementSpeed * dt);
        var (x, y) = (Rotation2d.Bounds.X / 2f, Rotation2d.Bounds.Y / 2f);
        transform.Translation = new Vec3(Math.Clamp(moved.X, -x, x), Math.Clamp(moved.Y, -y, y), 0f);
    }
}

/// <summary>An enemy that faces the player at once.</summary>
[Behavior]
public partial struct SnapToPlayer
{
    /// <summary>Turned to face the player, on the fixed timestep.</summary>
    [OnFixedUpdate]
    public void SnapToPlayerSystem(BehaviorContext ctx, ref Transform transform)
    {
        if (Rotation2d.PlayerAt(ctx) is not { } player) return;
        var to = player - transform.Translation;
        transform.Rotation = Quat.FromRotationZ(MathF.Atan2(to.Y, to.X) - MathF.PI / 2f);
    }
}

/// <summary>An enemy that turns toward the player at its own speed.</summary>
[Behavior]
public partial struct RotateToPlayer
{
    /// <summary>How fast it turns, in radians a second.</summary>
    public float RotationSpeed;

    /// <summary>Turned toward the player by no more than its speed allows or the angle left, on the fixed timestep.</summary>
    [OnFixedUpdate]
    public void RotateToPlayerSystem(BehaviorContext ctx, ref Transform transform)
    {
        if (Rotation2d.PlayerAt(ctx) is not { } player) return;
        var to = (player - transform.Translation).Normalized;
        var front = transform.Rotation * Vec3.UnitY;
        var dot = Math.Clamp(front.X * to.X + front.Y * to.Y, -1f, 1f);
        if (MathF.Abs(dot - 1f) < float.Epsilon) return;

        var right = transform.Rotation * Vec3.UnitX;
        var sign = right.X * to.X + right.Y * to.Y >= 0f ? -1f : 1f;
        transform.Rotation = Quat.FromRotationZ(sign * MathF.Min(RotationSpeed * ctx.Time.FixedDelta, MathF.Acos(dot))) * transform.Rotation;
    }
}
