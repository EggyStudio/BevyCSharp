using Bevy;
using Bevy.Physics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers rigid bodies simulated on the managed side and written back through each entity's
/// <see cref="Transform"/>.
/// </summary>
/// <remarks>
/// The simulation needs no renderer, so these run on the headless bridge the test job builds. Each
/// runs whole fixed steps, which keeps the numbers the same on every machine, and paces its frames,
/// because a fixed step is taken as real time passes and an unpaced run passes little.
/// </remarks>
[Collection("engine")]
public sealed class PhysicsTests
{
    /// <summary>
    /// A box let go above a static floor falls under gravity and comes to rest on top of it, and
    /// the only thing that moved it was the transform written each step.
    /// </summary>
    [Fact]
    public void ABoxFallsOntoTheFloorAndRestsThere()
    {
        using var harness = new EngineHarness(frames: 480, fps: 240, fixedHz: 120);
        harness.App.AddPlugin(new PhysicsPlugin());

        var box = Entity.None;
        var lowest = float.MaxValue;
        var final = Vec3.Zero;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var physics = ctx.Res<PhysicsWorld>();

            var floor = ctx.Ecs.Spawn();
            ctx.Ecs.Add(floor, Transform.At(0f, -0.5f, 0f));
            physics.Add(floor, PhysicsShape.Box(new Vec3(20f, 1f, 20f)), BodyKind.Static, Transform.At(0f, -0.5f, 0f));

            box = ctx.Ecs.Spawn();
            ctx.Ecs.Add(box, Transform.At(0f, 3f, 0f));
            physics.Add(box, PhysicsShape.Box(new Vec3(1f)), BodyKind.Dynamic, Transform.At(0f, 3f, 0f), mass: 2f);
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            var at = ctx.Ecs.GetOrDefault<Transform>(box).Translation;
            lowest = Math.Min(lowest, at.Y);
            final = at;
        });

        harness.Run();

        // Resting with its bottom on the floor's top, which puts its middle half a unit up, and
        // never having sunk through on the way down.
        Assert.InRange(final.Y, 0.45f, 0.55f);
        Assert.True(lowest > 0.3f, $"the box went down to {lowest}, into the floor");
        Assert.InRange(final.X, -0.05f, 0.05f);
    }

    /// <summary>
    /// A ray down from above meets the floor where it is, and names the floor's entity.
    /// </summary>
    [Fact]
    public void ARayMeetsTheFloorAndSaysWhichEntityItIs()
    {
        using var harness = new EngineHarness(frames: 3);
        harness.App.AddPlugin(new PhysicsPlugin());

        PhysicsHit? hit = null;
        PhysicsHit? miss = new PhysicsHit();
        var floor = Entity.None;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var physics = ctx.Res<PhysicsWorld>();

            floor = ctx.Ecs.Spawn();
            ctx.Ecs.Add(floor, Transform.At(0f, -0.5f, 0f));
            physics.Add(floor, PhysicsShape.Box(new Vec3(4f, 1f, 4f)), BodyKind.Static, Transform.At(0f, -0.5f, 0f));

            hit = physics.Raycast(new Vec3(1f, 10f, 1f), new Vec3(0f, -2f, 0f), 100f);
            miss = physics.Raycast(new Vec3(10f, 10f, 10f), new Vec3(0f, -1f, 0f), 100f);
        });

        harness.Run();

        Assert.NotNull(hit);
        Assert.Equal(floor, hit.Value.Entity);
        Assert.Equal(10f, hit.Value.Distance, 3);
        Assert.Equal(0f, hit.Value.Point.Y, 3);
        Assert.Equal(1f, hit.Value.Normal.Y, 3);
        Assert.Null(miss);
    }

    /// <summary>
    /// An impulse sets a body moving, and despawning its entity takes the body with it.
    /// </summary>
    [Fact]
    public void AnImpulseMovesABodyAndADespawnRemovesIt()
    {
        using var harness = new EngineHarness(frames: 40, fps: 120, fixedHz: 60);
        harness.App.AddPlugin(new PhysicsPlugin(new PhysicsSettings { Gravity = Vec3.Zero }));

        var ball = Entity.None;
        var frame = 0;
        var speed = 0f;
        var countAfter = -1;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var physics = ctx.Res<PhysicsWorld>();

            ball = ctx.Ecs.Spawn();
            ctx.Ecs.Add(ball, Transform.Identity);
            physics.Add(ball, PhysicsShape.Sphere(0.5f), BodyKind.Dynamic, Transform.Identity, mass: 2f);

            // Two units of momentum on two units of mass is one unit a second.
            physics.ApplyImpulse(ball, new Vec3(2f, 0f, 0f));
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            var physics = ctx.Res<PhysicsWorld>();
            frame++;

            if (frame == 5)
            {
                speed = physics.Velocity(ball).Linear.X;
                ctx.Ecs.Despawn(ball);
            }
            else if (frame == 10)
            {
                countAfter = physics.Count;
            }
        });

        harness.Run();

        Assert.InRange(speed, 0.95f, 1.0f);
        Assert.Equal(0, countAfter);
    }

    /// <summary>A body is the entity's once, and asking for a second is refused.</summary>
    [Fact]
    public void AnEntityHasOneBody()
    {
        using var physics = new PhysicsWorld();
        var entity = new Entity(42);

        physics.Add(entity, PhysicsShape.Sphere(1f), BodyKind.Dynamic, Transform.Identity);

        Assert.Throws<InvalidOperationException>(() =>
            physics.Add(entity, PhysicsShape.Sphere(1f), BodyKind.Dynamic, Transform.Identity));

        Assert.True(physics.Remove(entity));
        Assert.False(physics.Remove(entity));
        Assert.Equal(0, physics.Count);
    }
}
