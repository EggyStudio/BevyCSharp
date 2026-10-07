using Bevy;
using Bevy.Physics;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers a ball joint kept within a cone, a distance joint whose range changes after it is made, and a slider driven along its travel.</summary>
/// <remarks>
/// Each steps a <see cref="PhysicsWorld"/> of its own by hand at Bevy's sixty-four steps a second,
/// with 3DEngine's own cases for them (its <c>c5227118</c> and <c>979c97be</c>).
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

    /// <summary>
    /// A lift's car on a slider is driven up to the end of its travel and held there, keeps its line
    /// and its turn when pushed sideways and twisted, and is driven back down to the other end.
    /// </summary>
    [Fact]
    public void ASliderIsDrivenAlongItsAxisToItsTravelAndKeepsItsLineAndItsTurn()
    {
        var (rose, held, off, turned, fell, lowest) = (false, 0f, 0f, 0f, false, 0f);

        InAWorld(new PhysicsSettings(), (ecs, physics) =>
        {
            // A lift's car on a frame that does not move, sliding up its axis and nothing else.
            var frame = Body(ecs, physics, Vec3.Zero, PhysicsShape.Box(new Vec3(2f, 0.2f, 2f)), BodyKind.Kinematic);
            var car = Body(ecs, physics, new Vec3(0f, 1f, 0f), PhysicsShape.Box(Vec3.One), BodyKind.Dynamic, mass: 10f);
            var lift = physics.Connect(frame, car, Joint.Slider(Vec3.UnitY).WithTravel(0f, 3f).WithDrive(2f, 2000f));
            physics.ApplyImpulse(car, new Vec3(30f, 0f, 10f), new Vec3(0.5f, 0.5f, 0.5f));

            for (var i = 0; i < 300 && !rose; i++)
            {
                physics.Step(ecs, StepSeconds);
                rose = physics.SliderPosition(lift) > 2.95f;
            }

            Steps(ecs, physics, 32);
            held = physics.SliderPosition(lift) ?? 0f;
            var at = ecs.GetOrDefault<Transform>(car);
            off = new Vec2(at.Translation.X, at.Translation.Z).Length;
            turned = MathF.Abs(at.Rotation.W);

            physics.SetDrive(lift, -2f, 2000f);
            for (var i = 0; i < 300 && !fell; i++)
            {
                physics.Step(ecs, StepSeconds);
                fell = physics.SliderPosition(lift) < 0.05f;
            }

            Steps(ecs, physics, 32);
            lowest = ecs.GetOrDefault<Transform>(car).Translation.Y;
        });

        Assert.True(rose, "a positive speed did not drive it toward the axis's tip");
        Assert.InRange(held, 2.95f, 3.05f);
        Assert.True(off < 0.02f, $"pushed sideways it left its line by {off}");
        Assert.True(turned > 0.999f, $"twisted, it turned, its rotation's W at {turned}");
        Assert.True(fell, "a negative speed did not drive it back down");
        Assert.True(lowest > 0.9f, $"it went below the end of its travel, to {lowest}");
    }

    private static void Steps(EcsWorld ecs, PhysicsWorld physics, int count)
    {
        for (var i = 0; i < count; i++) physics.Step(ecs, StepSeconds);
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
