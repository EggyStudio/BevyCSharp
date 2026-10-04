using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Reacts to a component being removed. A sprite carries MyComponent, a system removes it after two
// seconds, and an observer of the removal recolors the sprite.
internal static class RemovalDetection
{
    // Only here to be removed.
    internal struct MyComponent;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            Render2d.SpawnCamera2d();
            var icon = ctx.Ecs.Spawn();
            ctx.Ecs.Add(icon, Transform.Identity);
            Render2d.SetSprite(ctx.Ecs, icon, AssetServer.Load(AssetKind.Image, "branding/icon.png"));
            ctx.Ecs.Add(icon, new MyComponent());

            // Told of the removal once it has happened, and of the entity it left.
            ctx.Ecs.Observe<Remove<MyComponent>>(on =>
                on.Ecs.SetReflectedColor(on.Event.Entity, "bevy_sprite::sprite::Sprite", ".color", Color.FromSrgb(0.5f, 1f, 1f)));
        }, "removal_detection.Setup");

        app.Update(ctx =>
        {
            if (ctx.Time.Elapsed <= 2f) return;
            foreach (var entity in ctx.Ecs.EntitiesWith<MyComponent>())
            {
                ctx.Ecs.Remove<MyComponent>(entity);
                break;
            }
        }, "removal_detection.RemoveComponent");
    }
}
