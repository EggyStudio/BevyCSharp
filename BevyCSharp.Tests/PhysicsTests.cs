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

    /// <summary>
    /// A ball falling through a sensor reports entering and leaving it without being slowed by it,
    /// and then reports landing on the floor, which it rests on.
    /// </summary>
    [Fact]
    public void ASensorReportsWhatPassesThroughWithoutStoppingIt()
    {
        using var harness = new EngineHarness(frames: 480, fps: 240, fixedHz: 120);
        harness.App.AddPlugin(new PhysicsPlugin());

        var ball = Entity.None;
        var sensor = Entity.None;
        var floor = Entity.None;
        var events = new List<string>();

        harness.OnContext(Stage.Startup, ctx =>
        {
            var physics = ctx.Res<PhysicsWorld>();

            floor = ctx.Ecs.Spawn();
            ctx.Ecs.Add(floor, Transform.At(0f, -0.5f, 0f));
            physics.Add(floor, PhysicsShape.Box(new Vec3(10f, 1f, 10f)), BodyKind.Static, Transform.At(0f, -0.5f, 0f));

            // A slab of air a unit above the floor, which the ball has to pass through.
            sensor = ctx.Ecs.Spawn();
            ctx.Ecs.Add(sensor, Transform.At(0f, 2f, 0f));
            physics.Add(sensor, PhysicsShape.Box(new Vec3(4f, 0.5f, 4f)), BodyKind.Static, Transform.At(0f, 2f, 0f), sensor: true);

            ball = ctx.Ecs.Spawn();
            ctx.Ecs.Add(ball, Transform.At(0f, 4f, 0f));
            physics.Add(ball, PhysicsShape.Sphere(0.25f), BodyKind.Dynamic, Transform.At(0f, 4f, 0f));
        });

        string Name(Entity entity) => entity == sensor ? "sensor" : entity == floor ? "floor" : "ball";

        harness.OnContext(Stage.Update, ctx =>
        {
            foreach (var started in ctx.Read<ContactStarted>())
                events.Add($"start {Name(started.A)}+{Name(started.B)}".Replace("ball+", "").Replace("+ball", ""));

            foreach (var ended in ctx.Read<ContactEnded>())
                events.Add($"end {Name(ended.A)}+{Name(ended.B)}".Replace("ball+", "").Replace("+ball", ""));
        });

        harness.Run();

        Assert.Equal(["start sensor", "end sensor", "start floor"], events);
    }

    /// <summary>
    /// A ball joined to a still pivot swings down under gravity while staying the joint's length
    /// from it, and removing the ball takes the joint with it.
    /// </summary>
    [Fact]
    public void APendulumSwingsOnItsJointAndTheJointGoesWithTheBall()
    {
        using var harness = new EngineHarness(frames: 240, fps: 240, fixedHz: 120);
        harness.App.AddPlugin(new PhysicsPlugin());

        var pivot = Entity.None;
        var ball = Entity.None;
        var joint = default(JointHandle);
        var lowest = float.MaxValue;
        var longest = 0f;
        var shortest = float.MaxValue;
        var frame = 0;
        var stillJoined = true;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var physics = ctx.Res<PhysicsWorld>();

            pivot = ctx.Ecs.Spawn();
            ctx.Ecs.Add(pivot, Transform.At(0f, 5f, 0f));
            physics.Add(pivot, PhysicsShape.Sphere(0.1f), BodyKind.Kinematic, Transform.At(0f, 5f, 0f));

            // Two units out to the side, level with the pivot, so it starts from horizontal.
            ball = ctx.Ecs.Spawn();
            ctx.Ecs.Add(ball, Transform.At(2f, 5f, 0f));
            physics.Add(ball, PhysicsShape.Sphere(0.2f), BodyKind.Dynamic, Transform.At(2f, 5f, 0f));

            joint = physics.Connect(pivot, ball, Joint.Ball(Vec3.Zero, new Vec3(-2f, 0f, 0f)));
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            var physics = ctx.Res<PhysicsWorld>();
            frame++;

            if (frame < 200)
            {
                var at = ctx.Ecs.GetOrDefault<Transform>(ball).Translation;
                var length = (at - new Vec3(0f, 5f, 0f)).Length;

                lowest = Math.Min(lowest, at.Y);
                if (frame > 10)
                {
                    longest = Math.Max(longest, length);
                    shortest = Math.Min(shortest, length);
                }
            }
            else if (frame == 200)
            {
                physics.Remove(ball);
                stillJoined = physics.Disconnect(joint);
            }
        });

        harness.Run();

        // It fell most of the way to the bottom of its swing, two units below the pivot.
        Assert.True(lowest < 3.3f, $"the ball only fell to {lowest}");
        Assert.InRange(shortest, 1.95f, 2.05f);
        Assert.InRange(longest, 1.95f, 2.05f);
        Assert.False(stillJoined);
    }

    /// <summary>
    /// A floor made from a plane mesh read back from the engine holds up a ball dropped on it from
    /// above, which is the side Bevy draws the plane's face on.
    /// </summary>
    [Fact]
    public void AMeshReadFromTheEngineIsAFloor()
    {
        if (!App.HasRenderer) return;

        using var physics = new PhysicsWorld();
        using var app = new App(Config.OffscreenFor(64, 64, frames: 200));
        app.AddPlugin(new EnginePlugin());

        var plane = AssetHandle.None;
        var ball = Entity.None;
        var read = false;
        var triangles = 0;
        var final = float.NaN;

        app.AddSystem(Stage.Startup, new SystemDescriptor(_ => plane = Render.CreateMesh(MeshShape.Plane, 10f, 10f), "Test.Plane"));

        app.AddSystem(Stage.Update, new SystemDescriptor(
            world =>
            {
                var ecs = world.Resource<EcsWorld>();

                if (!read)
                {
                    if (!Render.TryReadMesh(plane, out var mesh)) return;
                    read = true;
                    triangles = mesh!.Indices!.Length / 3;

                    var floor = ecs.Spawn();
                    ecs.Add(floor, Transform.Identity);
                    physics.Add(floor, PhysicsShape.Mesh(mesh), BodyKind.Static, Transform.Identity);

                    ball = ecs.Spawn();
                    ecs.Add(ball, Transform.At(0.3f, 2f, -0.2f));
                    physics.Add(ball, PhysicsShape.Sphere(0.25f), BodyKind.Dynamic, Transform.At(0.3f, 2f, -0.2f));
                    return;
                }

                // Stepped here by hand, a sixtieth of a second a frame, since an offscreen run is
                // not paced and the fixed timestep would take few steps.
                physics.Step(ecs, 1f / 60f);
                final = ecs.GetOrDefault<Transform>(ball).Translation.Y;
            },
            "Test.Step"));

        app.Run();

        Assert.True(triangles >= 2, $"the plane read back as {triangles} triangles");
        Assert.InRange(final, 0.2f, 0.3f);
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

    /// <summary>
    /// A bouncy ball comes back up off the floor and a dull one stays down, and a box with no
    /// friction slides far further than one with plenty, each by a material of its own.
    /// </summary>
    [Fact]
    public void EachBodysMaterialDecidesHowItBouncesAndSlides()
    {
        using var harness = new EngineHarness(frames: 480, fps: 240, fixedHz: 120);
        harness.App.AddPlugin(new PhysicsPlugin());

        Entity bouncy = default, dull = default, icy = default, rough = default;
        var landed = new Dictionary<Entity, bool>();
        var rebound = new Dictionary<Entity, float>();
        float slid = 0f, stopped = 0f;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var physics = ctx.Res<PhysicsWorld>();

            var floor = ctx.Ecs.Spawn();
            ctx.Ecs.Add(floor, Transform.At(0f, -0.5f, 0f));
            physics.Add(floor, PhysicsShape.Box(new Vec3(60f, 1f, 60f)), BodyKind.Static, Transform.At(0f, -0.5f, 0f), material: new PhysicsMaterial(1f));

            Entity Ball(float x, float bounce)
            {
                var ball = ctx.Ecs.Spawn();
                ctx.Ecs.Add(ball, Transform.At(x, 3f, 0f));
                physics.Add(ball, PhysicsShape.Sphere(0.5f), BodyKind.Dynamic, Transform.At(x, 3f, 0f), material: new PhysicsMaterial(1f, bounce));
                return ball;
            }

            Entity Slider(float z, float friction)
            {
                var box = ctx.Ecs.Spawn();
                ctx.Ecs.Add(box, Transform.At(0f, 0.5f, z));
                physics.Add(box, PhysicsShape.Box(new Vec3(1f)), BodyKind.Dynamic, Transform.At(0f, 0.5f, z), material: new PhysicsMaterial(friction));
                physics.SetVelocity(box, new Vec3(4f, 0f, 0f));
                return box;
            }

            bouncy = Ball(-10f, 0.9f);
            dull = Ball(-14f, 0f);
            icy = Slider(10f, 0f);
            rough = Slider(14f, 1f);
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            foreach (var ball in new[] { bouncy, dull })
            {
                var y = ctx.Ecs.GetOrDefault<Transform>(ball).Translation.Y;

                // Highest point after first reaching the floor.
                if (y < 0.6f) landed[ball] = true;
                if (landed.GetValueOrDefault(ball)) rebound[ball] = Math.Max(rebound.GetValueOrDefault(ball), y);
            }

            slid = ctx.Ecs.GetOrDefault<Transform>(icy).Translation.X;
            stopped = ctx.Ecs.GetOrDefault<Transform>(rough).Translation.X;
        });

        harness.Run();

        // Dropped from two and a half units above where it rests, a bounce of 0.9 keeps 0.81 of the
        // height, back up to a little over two and a half, less what the air's damping takes.
        Assert.True(rebound[bouncy] > 2.1f, $"the bouncy ball came back up only to {rebound[bouncy]}");
        Assert.True(rebound[dull] < 0.8f, $"the dull ball bounced to {rebound[dull]}");

        Assert.True(slid > stopped + 3f, $"the icy box slid to {slid} and the rough one to {stopped}");
    }
}
