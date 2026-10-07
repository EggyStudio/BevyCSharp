using Bevy;
using Bevy.Physics;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers a ball joint kept within a cone, and a distance joint whose range changes after it is made.</summary>
/// <remarks>
/// Each steps a <see cref="PhysicsWorld"/> of its own by hand at Bevy's sixty-four steps a second,
/// with 3DEngine's own cases for the two (its <c>c5227118</c>).
/// </remarks>
[Collection("engine")]
public sealed class JointTests
{
    private const float StepSeconds = 1f / 64f;

    /// <summary>A rod on a ball joint kept within a cone swings out to the cone's edge and no further, and twists no further than its limit.</summary>
    [Fact]
    public void ALimitedBallJointSwingsAndTwistsNoFurtherThanItsLimits()
    {
        var (widest, twist) = (0f, 0f);

        InAWorld(new PhysicsSettings { Gravity = Vec3.Zero }, (ecs, physics) =>
        {
            var anchor = Body(ecs, physics, new Vec3(0f, 5f, 0f), PhysicsShape.Box(new Vec3(0.2f)), BodyKind.Kinematic);
            // A rod hanging from the joint, its middle a unit down.
            var rod = Body(ecs, physics, new Vec3(0f, 4f, 0f), PhysicsShape.Box(new Vec3(0.2f, 1.8f, 0.2f)), BodyKind.Dynamic);
            physics.Connect(anchor, rod, Joint.Ball(Vec3.Zero, new Vec3(0f, 1f, 0f)).WithCone(new Vec3(0f, -1f, 0f), 30f, 20f));

            physics.ApplyImpulse(rod, new Vec3(20f, 0f, 0f), new Vec3(0f, -0.5f, 0f));
            var (linear, angular) = physics.Velocity(rod);
            physics.SetVelocity(rod, linear, angular + new Vec3(0f, 20f, 0f));

            for (var i = 0; i < 120; i++)
            {
                physics.Step(ecs, StepSeconds);
                var down = ecs.GetOrDefault<Transform>(rod).Rotation * new Vec3(0f, -1f, 0f);
                widest = MathF.Max(widest, MathF.Acos(Math.Clamp(-down.Y, -1f, 1f)) * 180f / MathF.PI);
            }

            var side = ecs.GetOrDefault<Transform>(rod).Rotation * new Vec3(1f, 0f, 0f);
            twist = MathF.Atan2(-side.Z, side.X) * 180f / MathF.PI;
        });

        Assert.InRange(widest, 25f, 33f);
        Assert.True(MathF.Abs(twist) < 23f, $"turned about its length, it twisted {twist} degrees past a limit of 20");
    }

    /// <summary>A distance joint reeled in a little each step holds its body at the new length, and only a distance joint takes a range.</summary>
    [Fact]
    public void ADistanceJointsRangeChangesWhereItHoldsABody()
    {
        var hung = 0f;
        var (otherKind, reversed) = (true, false);

        InAWorld(new PhysicsSettings(), (ecs, physics) =>
        {
            var hook = Body(ecs, physics, new Vec3(0f, 10f, 0f), PhysicsShape.Box(new Vec3(0.2f)), BodyKind.Kinematic);
            var weight = Body(ecs, physics, new Vec3(0f, 9f, 0f), PhysicsShape.Sphere(0.25f), BodyKind.Dynamic);
            var rope = physics.Connect(hook, weight, Joint.Distance(Vec3.Zero, Vec3.Zero, 0f, 3f));
            Steps(ecs, physics, 120);

            // Reeled in a little each step, as a winch does.
            for (var length = 3f; length > 1f; length -= 0.02f)
            {
                physics.SetDistance(rope, 0f, MathF.Max(1f, length));
                physics.Step(ecs, StepSeconds);
            }

            physics.SetDistance(rope, 0f, 1f);
            Steps(ecs, physics, 60);
            hung = ecs.GetOrDefault<Transform>(weight).Translation.Y;

            otherKind = physics.SetDistance(physics.Connect(hook, weight, Joint.Ball(Vec3.Zero, new Vec3(0f, 1f, 0f))), 0f, 1f);
            reversed = Assert.Throws<ArgumentException>(() => physics.SetDistance(rope, 2f, 1f)) is not null;
        });

        Assert.InRange(hung, 8.95f, 9.05f);
        Assert.False(otherKind, "a ball joint was given a distance joint's range");
        Assert.True(reversed);
    }

    private static void Steps(EcsWorld ecs, PhysicsWorld physics, int count)
    {
        for (var i = 0; i < count; i++) physics.Step(ecs, StepSeconds);
    }

    private static Entity Body(EcsWorld ecs, PhysicsWorld physics, Vec3 at, PhysicsShape shape, BodyKind kind)
    {
        var entity = ecs.Spawn();
        var transform = Transform.At(at.X, at.Y, at.Z);
        ecs.Add(entity, transform);
        physics.Add(entity, shape, kind, transform);
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
