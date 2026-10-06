using Bevy;
using Bevy.Physics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers how far a box slides on its friction, which is as far as the friction says.
/// </summary>
/// <remarks>
/// A box sliding at a speed <c>v</c> on a friction <c>μ</c> stops in <c>v² / (2 μ g)</c>. Each test
/// steps a <see cref="PhysicsWorld"/> of its own by hand, at Bevy's sixty-four steps a second, with
/// no damping, so the friction alone stops it.
/// </remarks>
[Collection("engine")]
public sealed class FrictionTests
{
    private const float StepSeconds = 1f / 64f;
    private const float Gravity = 9.81f;

    /// <summary>
    /// A box sent across a floor at 5 a second stops where its friction says, on a box for a floor
    /// and on a floor of triangles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Before the coefficient was scaled by a convex pair's contacts, a box on a box slid four times
    /// as far, 10.15 units at a friction of a half where 2.55 is right and 5.06 at a friction of 1
    /// where 1.27 is, and on the floor of triangles 2.19.
    /// </para>
    /// <para>
    /// Up to a friction of 1, since a cube slowed harder than gravity pulls it tips over its leading
    /// edge onto its next face rather than sliding, as a real one does, which at a friction of 4
    /// takes it 0.98.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(0.25f, false)]
    [InlineData(0.5f, false)]
    [InlineData(1f, false)]
    [InlineData(1f, true)]
    public void ABoxSlidesAsFarAsItsFrictionSays(float friction, bool mesh)
    {
        var slid = 0f;

        using var harness = new EngineHarness(frames: 1);
        harness.OnContext(Stage.Startup, ctx =>
        {
            var ecs = ctx.Ecs;
            using var physics = new PhysicsWorld(new PhysicsSettings { Friction = friction, LinearDamping = 0f, AngularDamping = 0f });

            var floor = ecs.Spawn();
            ecs.Add(floor, Transform.Identity);
            var shape = mesh
                ? PhysicsShape.Mesh(
                    [new Vec3(-40f, 0.5f, -40f), new Vec3(-40f, 0.5f, 40f), new Vec3(40f, 0.5f, 40f), new Vec3(40f, 0.5f, -40f)],
                    [0u, 1u, 2u, 0u, 2u, 3u])
                : PhysicsShape.Box(new Vec3(80f, 1f, 80f));
            physics.Add(floor, shape, BodyKind.Static, Transform.Identity);

            var box = ecs.Spawn();
            ecs.Add(box, Transform.At(0f, 1f, 0f));
            physics.Add(box, PhysicsShape.Box(new Vec3(1f)), BodyKind.Dynamic, Transform.At(0f, 1f, 0f));
            for (var i = 0; i < 8; i++) physics.Step(ecs, StepSeconds);

            var from = ecs.GetOrDefault<Transform>(box).Translation.X;
            physics.SetVelocity(box, new Vec3(5f, 0f, 0f));
            for (var i = 0; i < 256 && !physics.IsAsleep(box); i++) physics.Step(ecs, StepSeconds);
            slid = ecs.GetOrDefault<Transform>(box).Translation.X - from;
        });

        harness.Run();

        var right = 25f / (2f * friction * Gravity);
        Assert.True(Math.Abs(slid - right) < 0.1f * right + 0.03f, $"the box slid {slid} at a friction of {friction}, where {right} is right");
    }
}
