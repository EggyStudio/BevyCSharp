// Bevy's rotate_to_cursor example, examples/2d/rotate_to_cursor.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.TwoD;

// Demonstrates rotating a ship to face the cursor, turned on the fixed timestep.
internal static class RotateToCursor
{
    internal static Entity Camera;

    public static void Build(App app) => app.Startup(ctx =>
    {
        Camera = Render2d.SpawnCamera2d();
        var ship = ctx.Ecs.Spawn();
        ctx.Ecs.Add(ship, Transform.Identity);
        Render2d.SetSprite(ctx.Ecs, ship, AssetServer.Load(AssetKind.Image, "textures/simplespace/ship_C.png"));
        ctx.Ecs.Add(ship, new CursorPlayer());
    }, "rotate_to_cursor.Setup");
}

/// <summary>The ship, Bevy's <c>Player</c> under another name since rotation's shares the namespace.</summary>
[Behavior]
public partial struct CursorPlayer
{
    /// <summary>Turned on the fixed timestep to face the cursor, its nose up, a quarter turn from the angle the direction makes.</summary>
    [OnFixedUpdate]
    public void PlayerMovementSystem(BehaviorContext ctx, ref Transform transform)
    {
        var (x, y) = ctx.Input.MousePosition;
        if (!Render.TryRay(RotateToCursor.Camera, x, y, out var cursor, out _)) return;

        var angle = MathF.Atan2(cursor.Y - transform.Translation.Y, cursor.X - transform.Translation.X);
        transform.Rotation = Quat.FromRotationZ(angle - MathF.PI / 2f);
    }
}
