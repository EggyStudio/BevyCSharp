// Bevy's removal_detection example, examples/ecs/removal_detection.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Ecs;

// Reacts to a component being removed. A sprite carries MyComponent, a system removes it after two
// seconds, and an observer of the removal recolors the sprite.
internal static class RemovalDetection
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        Render2d.SpawnCamera2d();
        var icon = ctx.Ecs.Spawn();
        ctx.Ecs.Add(icon, Transform.Identity);
        Render2d.SetSprite(ctx.Ecs, icon, AssetServer.Load(AssetKind.Image, "branding/icon.png"));
        ctx.Ecs.Add(icon, new MyComponent());

        // Told of the removal once it has happened, and of the entity it left.
        ctx.Ecs.Observe<Remove<MyComponent>>(on =>
            on.Ecs.Wrap<SpriteRef>(on.Event.Entity).Color = Color.FromSrgb(0.5f, 1f, 1f));
    }, "removal_detection.Setup");
}

/// <summary>A component only here to be removed.</summary>
[Behavior]
public partial struct MyComponent
{
    /// <summary>
    /// Taken off its entity once two seconds have gone, by a command as Bevy's is. Bevy's takes it
    /// off the first entity its query finds, and there is the one.
    /// </summary>
    [OnUpdate]
    public void RemoveComponent(BehaviorContext ctx)
    {
        if (ctx.Time.Elapsed > 2f) ctx.Cmd.Remove<MyComponent>(ctx.Entity);
    }
}
