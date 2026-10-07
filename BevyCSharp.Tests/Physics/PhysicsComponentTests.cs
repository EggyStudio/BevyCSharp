using Bevy;
using Bevy.Physics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers bodies a level holds as <see cref="RigidBody"/> and <see cref="Collider"/> components,
/// which the plugin makes, makes again and takes away.
/// </summary>
/// <remarks>
/// Headless, so a collider sized to zero takes a unit cube scaled by its entity, there being no mesh
/// to fit. Paced as the other physics tests are, since a fixed step is taken as real time passes.
/// </remarks>
[Collection("engine")]
public sealed class PhysicsComponentTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-bodies-");

    public void Dispose() => _folder.Dispose();

    /// <summary>A floor and a crate as components, the floor a cube stretched flat and fitted to it.</summary>
    private static (Entity Floor, Entity Crate) Level(EcsWorld world)
    {
        var floor = world.Spawn();
        world.SetName(floor, "Floor");
        world.Add(floor, new Transform(new Vec3(0f, -0.5f, 0f), Quat.Identity, new Vec3(20f, 1f, 20f)));
        world.Add(floor, new RigidBody { Kind = BodyKind.Static });
        world.Add(floor, new Collider());

        var crate = world.Spawn();
        world.SetName(crate, "Crate");
        world.Add(crate, Transform.At(0f, 3f, 0f));
        world.Add(crate, new RigidBody { Kind = BodyKind.Dynamic, Mass = 2f });
        world.Add(crate, new Collider { Shape = ColliderShape.Box, Size = new Vec3(1f) });

        return (floor, crate);
    }

    [Fact]
    public void ABodyHeldAsComponentsFallsOntoAFloorHeldTheSameWay()
    {
        using var harness = new EngineHarness(frames: 480, fps: 240, fixedHz: 120, frameSeconds: 1.0 / 240);
        harness.App.AddPlugin(new PhysicsPlugin());

        var crate = Entity.None;
        var final = Vec3.Zero;

        harness.OnContext(Stage.Startup, ctx => crate = Level(ctx.Ecs).Crate);
        harness.OnContext(Stage.Update, ctx => final = ctx.Ecs.GetOrDefault<Transform>(crate).Translation);

        harness.Run();

        // On the floor's top, which the fitted collider puts at zero, with the crate's middle half a
        // unit above it.
        Assert.InRange(final.Y, 0.45f, 0.55f);
    }

    [Fact]
    public void AnEditedColliderIsMadeAgainAndARemovedOneTakesItsBody()
    {
        using var harness = new EngineHarness(frames: 120, fps: 240, fixedHz: 120, frameSeconds: 1.0 / 240);
        harness.App.AddPlugin(new PhysicsPlugin());

        var floor = Entity.None;
        var frame = 0;
        float? hitBefore = null, hitAfter = null;
        var hadBody = false;
        var hasBodyAfterRemoving = true;
        float? hitMoved = null;

        harness.OnContext(Stage.Startup, ctx => floor = Level(ctx.Ecs).Floor);

        harness.OnContext(Stage.Update, ctx =>
        {
            var physics = ctx.Res<PhysicsWorld>();
            frame++;

            switch (frame)
            {
                case 20:
                    // Straight down from above, meeting the floor's top.
                    hitBefore = physics.Raycast(new Vec3(5f, 10f, 5f), new Vec3(0f, -1f, 0f), 50f)?.Point.Y;
                    hadBody = physics.Has(floor);

                    // Twice as thick, which a static body only shows once it is made again.
                    ctx.Ecs.GetRef<Transform>(floor).Scale = new Vec3(20f, 2f, 20f);
                    break;

                case 40:
                    hitAfter = physics.Raycast(new Vec3(5f, 10f, 5f), new Vec3(0f, -1f, 0f), 50f)?.Point.Y;

                    // Moved up by one, which a static body only shows once it is made again.
                    ctx.Ecs.GetRef<Transform>(floor).Translation = new Vec3(0f, 0.5f, 0f);
                    break;

                case 50:
                    hitMoved = physics.Raycast(new Vec3(5f, 10f, 5f), new Vec3(0f, -1f, 0f), 50f)?.Point.Y;
                    ctx.Ecs.Remove<Collider>(floor);
                    break;

                case 70:
                    hasBodyAfterRemoving = physics.Has(floor);
                    break;
            }
        });

        harness.Run();

        Assert.True(hadBody);
        Assert.NotNull(hitBefore);
        Assert.InRange(hitBefore!.Value, -0.01f, 0.01f);
        Assert.NotNull(hitAfter);
        Assert.InRange(hitAfter!.Value, 0.49f, 0.51f);
        Assert.NotNull(hitMoved);
        Assert.InRange(hitMoved!.Value, 1.49f, 1.51f);
        Assert.False(hasBodyAfterRemoving);
    }

    /// <summary>
    /// A static body on an entity under another is where its parent puts it, moved, turned and
    /// scaled, as its collider is drawn, rather than at its own transform taken as the world's.
    /// </summary>
    /// <remarks>
    /// The feature test's Sponza is a scene whose walls are children of the entity that places it,
    /// and the player walked through them, the bodies standing near the hub.
    /// </remarks>
    [Fact]
    public void AStaticBodyUnderAParentIsWhereItsParentPutsIt()
    {
        using var harness = new EngineHarness(frames: 40, fps: 240, fixedHz: 120, frameSeconds: 1.0 / 240);
        harness.App.AddPlugin(new PhysicsPlugin());

        var frame = 0;
        float? placed = null, own = null;

        harness.OnContext(Stage.Startup, ctx =>
        {
            // Ten along X, turned a quarter so its Z is the world's X, and twice the size.
            var parent = ctx.Ecs.Spawn();
            ctx.Ecs.Add(parent, new Transform(new Vec3(10f, 0f, 0f), Quat.FromRotationY(MathF.PI / 2f), new Vec3(2f)));

            // Two along the parent's Z, which is four along the world's X, so at fourteen.
            var wall = ctx.Ecs.Spawn();
            ctx.Ecs.Add(wall, Transform.At(0f, 0f, 2f));
            ctx.Ecs.Add(wall, new RigidBody { Kind = BodyKind.Static });
            ctx.Ecs.Add(wall, new Collider { Shape = ColliderShape.Box, Size = new Vec3(1f) });
            ctx.Ecs.SetParent(wall, parent);
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            if (++frame != 20) return;

            var physics = ctx.Res<PhysicsWorld>();
            placed = physics.Raycast(new Vec3(14f, 10f, 0f), new Vec3(0f, -1f, 0f), 50f)?.Point.Y;
            own = physics.Raycast(new Vec3(0f, 10f, 2f), new Vec3(0f, -1f, 0f), 50f)?.Point.Y;
        });

        harness.Run();

        // The top of a box two across, its middle at the parent's height.
        Assert.NotNull(placed);
        Assert.InRange(placed!.Value, 0.99f, 1.01f);
        Assert.Null(own);
    }

    [Fact]
    public void ASceneCarriesItsBodies()
    {
        var level = _folder.File("bodies.scene.json");

        using (var making = new EngineHarness(frames: 2))
        {
            making.OnContext(Stage.Startup, ctx =>
            {
                Level(ctx.Ecs);
                SceneFile.Save(ctx.Ecs, level);
            });

            making.Run();
        }

        var text = File.ReadAllText(level);
        Assert.Contains("Bevy.Physics.RigidBody", text);
        Assert.Contains("Bevy.Physics.Collider", text);

        using var playing = new EngineHarness(frames: 480, fps: 240, fixedHz: 120, frameSeconds: 1.0 / 240);
        playing.App.AddPlugin(new PhysicsPlugin());

        var crate = Entity.None;
        var final = Vec3.Zero;

        playing.OnContext(Stage.Startup, ctx =>
            crate = SceneFile.Load(ctx.Ecs, level).Entities.Single(entity => ctx.Ecs.NameOf(entity) == "Crate"));
        playing.OnContext(Stage.Update, ctx => final = ctx.Ecs.GetOrDefault<Transform>(crate).Translation);

        playing.Run();

        Assert.InRange(final.Y, 0.45f, 0.55f);
    }
}
