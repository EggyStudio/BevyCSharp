using Bevy;

namespace BevyCSharp.Examples.TwoD;

// Renders a sprite moving back and forth, turning at two hundred pixels either side.
internal static class MoveSprite
{
    private static Entity _logo;
    private static bool _right;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            Render2d.SpawnCamera2d();
            _right = true;
            _logo = ctx.Ecs.Spawn();
            ctx.Ecs.Add(_logo, Transform.Identity);
            Render2d.SetSprite(ctx.Ecs, _logo, AssetServer.Load(AssetKind.Image, "branding/icon.png"));
        }, "move_sprite.Setup");

        app.Update(ctx =>
        {
            var transform = ctx.Ecs.GetOrDefault<Transform>(_logo);
            transform.Translation += new Vec3((_right ? 150f : -150f) * ctx.Time.Delta, 0f, 0f);
            if (transform.Translation.X > 200f) _right = false;
            else if (transform.Translation.X < -200f) _right = true;
            ctx.Ecs.Set(_logo, transform);
        }, "move_sprite.SpriteMovement");
    }
}
