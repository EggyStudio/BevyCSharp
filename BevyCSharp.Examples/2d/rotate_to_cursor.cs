// Bevy's rotate_to_cursor example, examples/2d/rotate_to_cursor.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.TwoD;

// Demonstrates rotating a ship to face the cursor, turned on the fixed timestep.
internal static class RotateToCursor
{
    private static Entity _camera, _ship;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            _camera = Render2d.SpawnCamera2d();
            _ship = ctx.Ecs.Spawn();
            ctx.Ecs.Add(_ship, Transform.Identity);
            Render2d.SetSprite(ctx.Ecs, _ship, AssetServer.Load(AssetKind.Image, "textures/simplespace/ship_C.png"));
        }, "rotate_to_cursor.Setup");

        app.On(Stage.FixedUpdate, ctx =>
        {
            var (x, y) = ctx.Input.MousePosition;
            if (!Render.TryRay(_camera, x, y, out var cursor, out _)) return;

            // The ship's nose is up, a quarter turn from the angle the direction makes.
            var transform = ctx.Ecs.GetOrDefault<Transform>(_ship);
            var angle = MathF.Atan2(cursor.Y - transform.Translation.Y, cursor.X - transform.Translation.X);
            transform.Rotation = Quat.FromRotationZ(angle - MathF.PI / 2f);
            ctx.Ecs.Set(_ship, transform);
        }, "rotate_to_cursor.PlayerMovement");
    }
}
