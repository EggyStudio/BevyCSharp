// Bevy's hierarchy example, examples/ecs/hierarchy.rs at v0.20.0, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Creates a hierarchy of parent and child entities, a logo with two children turning with it and
// about themselves, one child despawned two seconds in and everything at four. Space moves on
// through the six ways Bevy shows of building it, each under its title. C# parents an entity one
// way, so each builds the same tree, and the titles name Bevy's forms.
internal static class Hierarchy
{
    internal enum Showcase { WithChildren, ChildrenSpawn, ChildrenMacro, ChildrenIter, Related, Bsn }

    private static readonly (Showcase Stage, string Title)[] Titles =
    [
        (Showcase.WithChildren, "with_children()"),
        (Showcase.ChildrenSpawn, "Children::spawn() "),
        (Showcase.ChildrenMacro, "children!() "),
        (Showcase.ChildrenIter, "SpawnIter() "),
        (Showcase.Related, "related!() "),
        (Showcase.Bsn, "BSN"),
    ];

    private static Entity _parent;
    private static float _entered;

    public static void Build(App app)
    {
        app.AddState(Showcase.WithChildren);
        app.Startup(_ => Render2d.SpawnCamera2d(), "hierarchy.Setup");
        foreach (var (stage, title) in Titles)
            app.AddStateSystem(stage, entering: true, new SystemDescriptor(world => SetupStage(new BehaviorContext(world), stage, title), $"hierarchy.Setup{stage}"));

        app.Update(Rotate, "hierarchy.Rotate");
        app.Update(ctx =>
        {
            if (!ctx.Input.KeyPressed(Key.Space)) return;
            Console.WriteLine("Switching scene");
            var now = App.TryState<Showcase>(out var current) ? current : Showcase.WithChildren;
            ctx.SetState((Showcase)(((int)now + 1) % Titles.Length));
        }, "hierarchy.SwitchScene");
    }

    // Bevy's setup_common and the stage's own setup, the title and the logo with its two children,
    // all gone as the stage is left.
    private static void SetupStage(BehaviorContext ctx, Showcase stage, string title)
    {
        var ecs = ctx.Ecs;
        _entered = ctx.Time.Elapsed;
        ecs.DespawnOnExit(Ui.SpawnText($"{title}\nPress Space to continue", new UiSettings(), 36f), stage);

        var texture = AssetServer.Load(AssetKind.Image, "branding/icon.png");
        _parent = Sprite(ecs, texture, new Transform(Vec3.Zero, Quat.Identity, new Vec3(0.75f)), (1f, 1f, 1f, 1f));
        ecs.DespawnOnExit(_parent, stage);
        ecs.SetParent(Sprite(ecs, texture, new Transform(new Vec3(250f, 0f, 0f), Quat.Identity, new Vec3(0.75f)), Color.FromSrgb(0f, 0f, 1f)), _parent);
        ecs.SetParent(Sprite(ecs, texture, new Transform(new Vec3(0f, 250f, 0f), Quat.Identity, new Vec3(0.75f)), Color.FromSrgb(0f, 1f, 0f)), _parent);
    }

    // Bevy's rotate. The parent turns one way, carrying its children, and each child turns twice
    // as fast the other way about itself, the last child despawned two seconds after the stage
    // began and the parent with what is left at four.
    private static void Rotate(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        if (!ecs.IsAlive(_parent)) return;

        Turn(ecs, _parent, -MathF.PI / 2f * ctx.Time.Delta);
        var children = ecs.ChildrenOf(_parent);
        foreach (var child in children) Turn(ecs, child, MathF.PI * ctx.Time.Delta);

        var elapsed = ctx.Time.Elapsed - _entered;
        if (elapsed >= 2f && children.Length == 2) ecs.Despawn(children[^1]);
        if (elapsed >= 4f) ecs.Despawn(_parent);
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
