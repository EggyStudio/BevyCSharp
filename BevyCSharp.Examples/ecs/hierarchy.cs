using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Creates a hierarchy of parent and child entities, a logo with two children turning with it and
// about themselves, one child despawned after two seconds and everything after four.
internal static class Hierarchy
{
    private static Entity _parent;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Render2d.SpawnCamera2d();
            var texture = AssetServer.Load(AssetKind.Image, "branding/icon.png");

            _parent = Sprite(ecs, texture, new Transform(Vec3.Zero, Quat.Identity, new Vec3(0.75f)), (1f, 1f, 1f, 1f));
            ecs.SetParent(Sprite(ecs, texture, new Transform(new Vec3(250f, 0f, 0f), Quat.Identity, new Vec3(0.75f)), Scene.Srgb(0f, 0f, 1f)), _parent);

            // Spawned on its own and given its parent afterward, the other way Bevy shows.
            var child = Sprite(ecs, texture, new Transform(new Vec3(0f, 250f, 0f), Quat.Identity, new Vec3(0.75f)), Scene.Srgb(0f, 1f, 0f));
            ecs.SetParent(child, _parent);
        }, "hierarchy.Setup");

        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            if (!ecs.IsAlive(_parent)) return;

            // The parent turns one way, carrying its children, and each child turns twice as fast
            // the other way about itself.
            Turn(ecs, _parent, -MathF.PI / 2f * ctx.Time.Delta);
            var children = ecs.ChildrenOf(_parent);
            foreach (var child in children) Turn(ecs, child, MathF.PI * ctx.Time.Delta);

            if (ctx.Time.Elapsed >= 2f && children.Length == 2) ecs.Despawn(children[^1]);
            if (ctx.Time.Elapsed >= 4f) ecs.Despawn(_parent);
        }, "hierarchy.Rotate");
    }

    private static Entity Sprite(EcsWorld ecs, AssetHandle texture, Transform at, (float R, float G, float B, float A) color)
    {
        var entity = ecs.Spawn();
        ecs.Add(entity, at);
        Render2d.SetSprite(ecs, entity, texture, new SpriteSettings { Color = color });
        return entity;
    }

    private static void Turn(EcsWorld ecs, Entity entity, float radians)
    {
        var transform = ecs.GetOrDefault<Transform>(entity);
        transform.Rotation = Quat.FromRotationZ(radians) * transform.Rotation;
        ecs.Set(entity, transform);
    }
}
