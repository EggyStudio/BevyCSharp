using Bevy;
using Bevy.Physics;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers how two touching bodies bounce, which is as the bouncier of the two does.</summary>
[Collection("engine")]
public sealed class BounceTests
{
    /// <summary>
    /// A ball of no bounce dropped on a bouncy pad comes back up, a ball of a little bounce comes
    /// back up as high as the pad's bounce says, and a ball of no bounce on a dull floor stays down.
    /// </summary>
    [Fact]
    public void ATouchingPairBouncesAsTheBouncierOfTheTwo()
    {
        using var harness = new EngineHarness(frames: 480, fps: 240, fixedHz: 120, frameSeconds: 1.0 / 240);
        harness.App.AddPlugin(new PhysicsPlugin());

        var balls = new List<Entity>();
        var landed = new Dictionary<Entity, bool>();
        var rebound = new Dictionary<Entity, float>();

        harness.OnContext(Stage.Startup, ctx =>
        {
            var physics = ctx.Res<PhysicsWorld>();

            void Floor(float x, float bounce)
            {
                var floor = ctx.Ecs.Spawn();
                ctx.Ecs.Add(floor, Transform.At(x, -0.5f, 0f));
                physics.Add(floor, PhysicsShape.Box(new Vec3(4f, 1f, 4f)), BodyKind.Static, Transform.At(x, -0.5f, 0f), material: new PhysicsMaterial(1f, bounce));
            }

            void Ball(float x, float bounce)
            {
                var ball = ctx.Ecs.Spawn();
                ctx.Ecs.Add(ball, Transform.At(x, 3f, 0f));
                physics.Add(ball, PhysicsShape.Sphere(0.5f), BodyKind.Dynamic, Transform.At(x, 3f, 0f), material: new PhysicsMaterial(1f, bounce));
                balls.Add(ball);
            }

            Floor(-10f, 0.9f);
            Floor(0f, 0.9f);
            Floor(10f, 0f);
            Ball(-10f, 0f);
            Ball(0f, 0.3f);
            Ball(10f, 0f);
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            foreach (var ball in balls)
            {
                var y = ctx.Ecs.GetOrDefault<Transform>(ball).Translation.Y;

                // Highest point after first reaching the floor.
                if (y < 0.6f) landed[ball] = true;
                if (landed.GetValueOrDefault(ball)) rebound[ball] = Math.Max(rebound.GetValueOrDefault(ball), y);
            }
        });

        harness.Run();

        // Dropped from two and a half units above where it rests, a bounce of 0.9 keeps 0.81 of the
        // height, back up to a little over two and a half, less what the air's damping takes.
        Assert.True(rebound[balls[0]] > 2.1f, $"the dull ball on the bouncy pad came back up only to {rebound[balls[0]]}");
        Assert.True(rebound[balls[1]] > 2.1f, $"the ball of a little bounce on the bouncy pad came back up only to {rebound[balls[1]]}");
        Assert.True(rebound[balls[2]] < 0.8f, $"the dull ball on the dull floor bounced to {rebound[balls[2]]}");
    }
}
