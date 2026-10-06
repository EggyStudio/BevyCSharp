using Bevy;
using Bevy.Physics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a body that has come to rest and is then told to move, by a velocity, an impulse, a
/// motor, a joint let go or a platform setting off beneath it.
/// </summary>
/// <remarks>
/// Each steps a <see cref="PhysicsWorld"/> of its own by hand, at Bevy's sixty-four steps a second,
/// and lets the body rest for half a second, which is as long as Bepu waits before it counts a
/// body as ready to sleep, and for two seconds, by when it sleeps.
/// </remarks>
[Collection("engine")]
public sealed class SleepTests
{
    private const float StepSeconds = 1f / 64f;

    /// <summary>A crate at rest given a velocity moves off at it in the next step.</summary>
    [Theory]
    [InlineData(32)]
    [InlineData(128)]
    public void ACrateAtRestGivenAVelocityMoves(int rested)
    {
        var (moved, asleep) = (0f, true);

        InAWorld((ecs, physics) =>
        {
            Floor(ecs, physics);
            var crate = Crate(ecs, physics, Vec3.Zero);
            Steps(ecs, physics, rested);

            physics.SetVelocity(crate, new Vec3(3f, 0f, 0f));
            Steps(ecs, physics, 4);
            moved = ecs.GetOrDefault<Transform>(crate).Translation.X;
            asleep = physics.IsAsleep(crate);
        });

        Assert.False(asleep, "the crate went to sleep with a speed of 3");
        Assert.True(moved > 0.1f, $"the crate moved only to {moved} in four steps at 3 a second");
    }

    /// <summary>A crate at rest pushed by an impulse moves off in the next step.</summary>
    [Theory]
    [InlineData(32)]
    [InlineData(128)]
    public void ACrateAtRestPushedByAnImpulseMoves(int rested)
    {
        var moved = 0f;

        InAWorld((ecs, physics) =>
        {
            Floor(ecs, physics);
            var crate = Crate(ecs, physics, Vec3.Zero);
            Steps(ecs, physics, rested);

            physics.ApplyImpulse(crate, new Vec3(0f, 5f, 0f));
            Steps(ecs, physics, 4);
            moved = ecs.GetOrDefault<Transform>(crate).Translation.Y - 1f;
        });

        Assert.True(moved > 0.1f, $"the crate rose only {moved} in four steps after an impulse up");
    }

    /// <summary>A door hanging still on its hinge turns once its motor is set going.</summary>
    [Theory]
    [InlineData(32)]
    [InlineData(128)]
    public void ADoorAtRestTurnsWhenItsMotorIsSetGoing(int rested)
    {
        var turned = 0f;

        InAWorld((ecs, physics) =>
        {
            var post = ecs.Spawn();
            ecs.Add(post, Transform.At(0f, 2f, 0f));
            physics.Add(post, PhysicsShape.Sphere(0.1f), BodyKind.Kinematic, Transform.At(0f, 2f, 0f));

            var door = ecs.Spawn();
            ecs.Add(door, Transform.At(1f, 2f, 0f));
            physics.Add(door, PhysicsShape.Box(new Vec3(2f, 2f, 0.1f)), BodyKind.Dynamic, Transform.At(1f, 2f, 0f));
            var hinge = physics.Connect(post, door, Joint.Hinge(Vec3.Zero, Vec3.UnitY, new Vec3(-1f, 0f, 0f), Vec3.UnitY).WithMotor(0f, 100f));
            Steps(ecs, physics, rested);

            physics.SetMotor(hinge, 90f, 100f);
            Steps(ecs, physics, 32);
            var rotation = ecs.GetOrDefault<Transform>(door).Rotation;
            turned = 2f * MathF.Atan2(rotation.Y, rotation.W) * 180f / MathF.PI;
        });

        // Half a second at ninety degrees a second.
        Assert.InRange(turned, 35f, 55f);
    }

    /// <summary>A ball hanging still from a joint falls once the joint is taken away.</summary>
    [Theory]
    [InlineData(32)]
    [InlineData(128)]
    public void ABallHangingStillFallsWhenItsJointGoes(int rested)
    {
        var fell = 0f;

        InAWorld((ecs, physics) =>
        {
            var pivot = ecs.Spawn();
            ecs.Add(pivot, Transform.At(0f, 5f, 0f));
            physics.Add(pivot, PhysicsShape.Sphere(0.1f), BodyKind.Kinematic, Transform.At(0f, 5f, 0f));

            var ball = ecs.Spawn();
            ecs.Add(ball, Transform.At(0f, 3f, 0f));
            physics.Add(ball, PhysicsShape.Sphere(0.2f), BodyKind.Dynamic, Transform.At(0f, 3f, 0f));
            var rope = physics.Connect(pivot, ball, Joint.Ball(Vec3.Zero, new Vec3(0f, 2f, 0f)));
            Steps(ecs, physics, rested);

            var hung = ecs.GetOrDefault<Transform>(ball).Translation.Y;
            physics.Disconnect(rope);
            Steps(ecs, physics, 16);
            fell = hung - ecs.GetOrDefault<Transform>(ball).Translation.Y;
        });

        // A quarter of a second falling, about thirty centimeters.
        Assert.True(fell > 0.2f, $"the ball fell only {fell} in a quarter of a second");
    }

    /// <summary>A crate at rest on a platform is carried once the platform sets off.</summary>
    [Theory]
    [InlineData(32)]
    [InlineData(128)]
    public void ACrateAtRestIsCarriedWhenItsPlatformSetsOff(int rested)
    {
        var carried = 0f;

        InAWorld((ecs, physics) =>
        {
            var platform = ecs.Spawn();
            ecs.Add(platform, Transform.At(0f, 0.25f, 0f));
            physics.Add(platform, PhysicsShape.Box(new Vec3(40f, 0.5f, 40f)), BodyKind.Kinematic, Transform.At(0f, 0.25f, 0f));
            var crate = Crate(ecs, physics, Vec3.Zero);
            Steps(ecs, physics, rested);

            for (var i = 0; i < 64; i++)
            {
                var at = ecs.GetOrDefault<Transform>(platform);
                at.Translation.X += 2f * StepSeconds;
                ecs.Set(platform, at);
                physics.Step(ecs, StepSeconds);
            }

            carried = physics.Velocity(crate).Linear.X;
        });

        // Up to the platform's speed within the second, however rough the two are.
        Assert.True(Math.Abs(carried - 2f) < 0.05f, $"the crate went at {carried} on a platform at 2");
    }

    /// <summary>Runs a body in a system of a headless app, where the world can be reached.</summary>
    private static void InAWorld(Action<EcsWorld, PhysicsWorld> body)
    {
        using var harness = new EngineHarness(frames: 1);
        harness.OnContext(Stage.Startup, ctx =>
        {
            using var physics = new PhysicsWorld();
            body(ctx.Ecs, physics);
        });

        harness.Run();
    }

    private static void Steps(EcsWorld ecs, PhysicsWorld physics, int count)
    {
        for (var i = 0; i < count; i++) physics.Step(ecs, StepSeconds);
    }

    /// <summary>A static floor whose top is at half a unit up.</summary>
    private static void Floor(EcsWorld ecs, PhysicsWorld physics)
    {
        var floor = ecs.Spawn();
        ecs.Add(floor, Transform.At(0f, 0.25f, 0f));
        physics.Add(floor, PhysicsShape.Box(new Vec3(40f, 0.5f, 40f)), BodyKind.Static, Transform.At(0f, 0.25f, 0f));
    }

    /// <summary>A crate of a unit standing on the floor at <paramref name="at"/>.</summary>
    private static Entity Crate(EcsWorld ecs, PhysicsWorld physics, Vec3 at)
    {
        var crate = ecs.Spawn();
        var transform = Transform.At(at.X, 1f, at.Z);
        ecs.Add(crate, transform);
        physics.Add(crate, PhysicsShape.Box(new Vec3(1f)), BodyKind.Dynamic, transform);
        return crate;
    }
}
