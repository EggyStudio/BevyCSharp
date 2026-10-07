using Bevy;
using Bevy.Physics;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers where and how hard two bodies meet, carried on the contact's message, and a fast body swept so it meets a thin wall.</summary>
/// <remarks>
/// Each steps a <see cref="PhysicsWorld"/> of its own by hand at Bevy's sixty-four steps a second,
/// gathering the contacts each step sends, as 3DEngine's own test of the speed a pair closed at does
/// (its <c>c5227118</c>).
/// </remarks>
[Collection("engine")]
public sealed class BodyContactTests
{
    private const float StepSeconds = 1f / 64f;

    /// <summary>
    /// A ball dropped onto the floor meets it at the speed its fall gave it, one placed on the floor
    /// meets it at rest, and two balls thrown at each other close at both their speeds.
    /// </summary>
    [Fact]
    public void AContactSaysHowFastTheBodiesClosedAsTheyMet()
    {
        var (dropped, placed, thrown) = (default(ContactStarted), default(ContactStarted), new List<ContactStarted>());
        var (ball, rest) = (Entity.None, Entity.None);

        InAWorld(new PhysicsSettings(), (ecs, physics) =>
        {
            Body(ecs, physics, new Vec3(0f, -0.5f, 0f), PhysicsShape.Box(new Vec3(10f, 1f, 10f)), BodyKind.Static);

            // Half a unit above the floor, it lands at the square root of twice gravity times that.
            ball = Body(ecs, physics, new Vec3(0f, 1f, 0f), PhysicsShape.Sphere(0.5f), BodyKind.Dynamic);
            rest = Body(ecs, physics, new Vec3(3f, 0.5f, 0f), PhysicsShape.Sphere(0.5f), BodyKind.Dynamic);

            var started = Steps(ecs, physics, 60);
            dropped = Assert.Single(started, c => c.A == ball || c.B == ball);
            placed = Assert.Single(started, c => c.A == rest || c.B == rest);
        });

        // Undamped, so each keeps the speed it was thrown at until they meet.
        InAWorld(new PhysicsSettings { Gravity = Vec3.Zero, LinearDamping = 0f, AngularDamping = 0f }, (ecs, physics) =>
        {
            var left = Body(ecs, physics, new Vec3(-2f, 0f, 0f), PhysicsShape.Sphere(0.5f), BodyKind.Dynamic);
            var right = Body(ecs, physics, new Vec3(2f, 0f, 0f), PhysicsShape.Sphere(0.5f), BodyKind.Dynamic);
            physics.SetVelocity(left, new Vec3(3f, 0f, 0f));
            physics.SetVelocity(right, new Vec3(-3f, 0f, 0f));
            thrown.AddRange(Steps(ecs, physics, 40));
        });

        Assert.InRange(dropped.Speed, MathF.Sqrt(2f * 9.81f * 0.5f) - 0.35f, MathF.Sqrt(2f * 9.81f * 0.5f) + 0.35f);
        Assert.True(placed.Speed < 0.3f, $"a ball placed on the floor met it at {placed.Speed}");

        // The normal points from B toward A, so up out of the floor where the ball is A.
        var up = dropped.A == ball ? dropped.Normal.Y : -dropped.Normal.Y;
        Assert.True(up > 0.99f, $"the normal {dropped.Normal} does not point out of the floor toward the ball");
        Assert.InRange(dropped.Point.Y, -0.05f, 0.05f);

        Assert.Equal(6f, Assert.Single(thrown).Speed, 1);
    }

    /// <summary>A ball at 40 units a second crosses a wall a fifth of a unit thick within a step, and one swept over each step meets it.</summary>
    [Fact]
    public void AFastBallCrossesAThinWallUnlessItIsSwept()
    {
        var (plainZ, sweptZ, sweeping) = (0f, 0f, false);

        InAWorld(new PhysicsSettings { Gravity = Vec3.Zero, LinearDamping = 0f, AngularDamping = 0f }, (ecs, physics) =>
        {
            Body(ecs, physics, new Vec3(0f, 0f, -5f), PhysicsShape.Box(new Vec3(20f, 20f, 0.2f)), BodyKind.Static);
            var plain = Body(ecs, physics, new Vec3(-2f, 0f, 0f), PhysicsShape.Sphere(0.05f), BodyKind.Dynamic, mass: 0.01f);
            var swept = Body(ecs, physics, new Vec3(2f, 0f, 0f), PhysicsShape.Sphere(0.05f), BodyKind.Dynamic, mass: 0.01f);
            physics.SetContinuous(swept, true);
            sweeping = physics.IsContinuous(swept);
            physics.SetVelocity(plain, new Vec3(0f, 0f, -40f));
            physics.SetVelocity(swept, new Vec3(0f, 0f, -40f));

            Steps(ecs, physics, 30);
            (plainZ, sweptZ) = (ecs.GetOrDefault<Transform>(plain).Translation.Z, ecs.GetOrDefault<Transform>(swept).Translation.Z);
        });

        Assert.True(sweeping);
        Assert.True(plainZ < -8f, $"the plain ball stopped at {plainZ}, where at 40 units a second it crosses the wall within a step");
        Assert.True(sweptZ > -5f, $"the swept ball reached {sweptZ}, through the wall");
    }

    private static List<ContactStarted> Steps(EcsWorld ecs, PhysicsWorld physics, int count)
    {
        var bus = new MessageBus();
        var started = new List<ContactStarted>();
        for (var i = 0; i < count; i++)
        {
            physics.Step(ecs, StepSeconds, bus);
            bus.Swap();
            started.AddRange(bus.Read<ContactStarted>().ToArray());
        }

        return started;
    }

    private static Entity Body(EcsWorld ecs, PhysicsWorld physics, Vec3 at, PhysicsShape shape, BodyKind kind, float mass = 1f)
    {
        var entity = ecs.Spawn();
        var transform = Transform.At(at.X, at.Y, at.Z);
        ecs.Add(entity, transform);
        physics.Add(entity, shape, kind, transform, mass);
        return entity;
    }

    private static void InAWorld(PhysicsSettings settings, Action<EcsWorld, PhysicsWorld> body)
    {
        using var harness = new EngineHarness(frames: 1);
        harness.OnContext(Stage.Startup, ctx =>
        {
            using var physics = new PhysicsWorld(settings);
            body(ctx.Ecs, physics);
        });

        harness.Run();
    }
}
