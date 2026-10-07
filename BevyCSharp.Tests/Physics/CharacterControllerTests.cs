using Bevy;
using Bevy.Physics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a dynamic body made a character by a <see cref="CharacterController"/>, a capsule 1.8 tall
/// and 0.4 in radius with its entity's origin at its feet, on a floor whose top is at zero.
/// </summary>
/// <remarks>
/// Each steps a world of its own sixty times a second from inside one system, so a test is the
/// same number of steps on every machine and takes no real time to run them.
/// </remarks>
[Collection("engine")]
public sealed class CharacterControllerTests
{
    private const float Step = 1f / 60f;
    private const float Radius = 0.4f;
    private const float Height = 1.8f;

    /// <summary>A static box of a size, centered at a point and turned about Z.</summary>
    private static Entity Block(EcsWorld ecs, Vec3 center, Vec3 size, float degreesAboutZ = 0f)
    {
        var block = ecs.Spawn();
        ecs.Add(block, new Transform(center, Quat.FromAxisAngle(Vec3.UnitZ, degreesAboutZ * MathF.PI / 180f), Vec3.One));
        ecs.Add(block, new RigidBody { Kind = BodyKind.Static });
        ecs.Add(block, new Collider { Shape = ColliderShape.Box, Size = size });
        return block;
    }

    /// <summary>A character standing with its feet at a point.</summary>
    private static Entity Character(EcsWorld ecs, Vec3 feet, CharacterController controller = default)
    {
        var character = ecs.Spawn();
        ecs.Add(character, Transform.At(feet.X, feet.Y, feet.Z));
        ecs.Add(character, new RigidBody { Kind = BodyKind.Dynamic, Mass = 80f });
        ecs.Add(character, new Collider { Shape = ColliderShape.Capsule, Size = new Vec3(Radius * 2f, Height, Radius * 2f), Offset = new Vec3(0f, Height / 2f, 0f) });
        ecs.Add(character, controller);
        return character;
    }

    /// <summary>
    /// Builds a level on a floor, steps it for a while, calling <paramref name="each"/> before every
    /// step, and hands back what <paramref name="read"/> finds at the end.
    /// </summary>
    private static T Simulate<T>(
        Func<EcsWorld, Entity[]> level,
        float seconds,
        Func<EcsWorld, PhysicsWorld, Entity[], T> read,
        Action<EcsWorld, Entity[], int>? each = null,
        Action<PhysicsWorld>? made = null)
    {
        using var harness = new EngineHarness(frames: 3);
        T? result = default;
        var ran = false;

        harness.OnContext(Stage.Update, ctx =>
        {
            if (ran) return;
            ran = true;

            using var physics = new PhysicsWorld();
            made?.Invoke(physics);
            Block(ctx.Ecs, new Vec3(0f, -0.5f, 0f), new Vec3(100f, 1f, 100f));
            var entities = level(ctx.Ecs);

            for (var step = 0; step < seconds / Step; step++)
            {
                each?.Invoke(ctx.Ecs, entities, step);
                physics.Sync(ctx.Ecs);
                physics.Step(ctx.Ecs, Step);
            }

            result = read(ctx.Ecs, physics, entities);
        });

        harness.Run();
        Assert.True(ran);
        return result!;
    }

    private static Vec3 Feet(EcsWorld ecs, Entity entity) => ecs.GetOrDefault<Transform>(entity).Translation;

    private static void Walk(EcsWorld ecs, Entity entity, Vec3 velocity)
    {
        var controller = ecs.GetOrDefault<CharacterController>(entity);
        controller.Move = velocity;
        ecs.Set(entity, controller);
    }

    private static void Stand(EcsWorld ecs, Entity entity, float height)
    {
        var controller = ecs.GetOrDefault<CharacterController>(entity);
        controller.Height = height;
        ecs.Set(entity, controller);
    }

    /// <summary>
    /// How tall a character stands, read by a ray cast straight down onto its head from
    /// <paramref name="above"/> over its feet, or NaN where the ray meets something else first.
    /// </summary>
    private static float Tall(EcsWorld ecs, PhysicsWorld physics, Entity entity, float above)
    {
        var feet = Feet(ecs, entity);
        return physics.Raycast(feet + new Vec3(0f, above, 0f), new Vec3(0f, -1f, 0f), above) is { } hit && hit.Entity == entity
            ? hit.Point.Y - feet.Y
            : float.NaN;
    }

    [Fact]
    public void ACharacterStandsOnTheFloorAndReportsGround()
    {
        var (feet, controller, isCharacter) = Simulate(
            ecs => [Character(ecs, new Vec3(0f, 1f, 0f))],
            1.5f,
            (ecs, physics, e) => (Feet(ecs, e[0]), ecs.GetOrDefault<CharacterController>(e[0]), physics.IsCharacter(e[0])));

        Assert.True(isCharacter);
        Assert.True(controller.Grounded);
        Assert.InRange(feet.Y, -0.05f, 0.05f);
        Assert.InRange(controller.GroundNormal.Y, 0.999f, 1.001f);
    }

    [Fact]
    public void AWallStopsACharacterWalkingIntoItAndOneMetAtAnAngleIsSlidAlong()
    {
        // A wall whose face is at x 2.75, the length of the level.
        var (straight, slanted) = Simulate(
            ecs =>
            {
                Block(ecs, new Vec3(3f, 1f, 0f), new Vec3(0.5f, 2f, 60f));
                return [Character(ecs, Vec3.Zero), Character(ecs, new Vec3(0f, 0f, -20f))];
            },
            3f,
            (ecs, _, e) => (Feet(ecs, e[0]), Feet(ecs, e[1])),
            (ecs, e, step) =>
            {
                if (step != 0) return;
                Walk(ecs, e[0], new Vec3(4f, 0f, 0f));
                Walk(ecs, e[1], new Vec3(1f, 0f, 1f).Normalized * 4f);
            });

        Assert.InRange(straight.X, 2.2f, 2.75f - Radius + 0.05f);
        Assert.InRange(straight.Z, -0.05f, 0.05f);

        // Held back by the wall and carried along it, where friction would have held it to it.
        Assert.True(slanted.X < 2.75f - Radius + 0.05f, $"through the wall at {slanted.X}");
        Assert.True(slanted.Z > -14f, $"only at z {slanted.Z}");
    }

    [Fact]
    public void ACharacterRidesALowEdgeClimbsAStepAndIsStoppedByALedge()
    {
        // In three lanes, from x 2: an edge 0.15 high, a step 0.35 high, which is under its radius,
        // and a ledge 0.6 high, which is over it.
        var (edge, step, ledge) = Simulate(
            ecs =>
            {
                Block(ecs, new Vec3(4f, 0.075f, 0f), new Vec3(4f, 0.15f, 4f));
                Block(ecs, new Vec3(4f, 0.175f, 10f), new Vec3(4f, 0.35f, 4f));
                Block(ecs, new Vec3(4f, 0.3f, 20f), new Vec3(4f, 0.6f, 4f));
                return [Character(ecs, Vec3.Zero), Character(ecs, new Vec3(0f, 0f, 10f)), Character(ecs, new Vec3(0f, 0f, 20f))];
            },
            2f,
            (ecs, _, e) => (Feet(ecs, e[0]), Feet(ecs, e[1]), Feet(ecs, e[2])),
            (ecs, e, n) =>
            {
                if (n != 0) return;
                foreach (var character in e) Walk(ecs, character, new Vec3(3f, 0f, 0f));
            });

        Assert.InRange(edge.Y, 0.1f, 0.2f);
        Assert.True(edge.X > 3f, $"stopped at {edge.X}");
        Assert.InRange(step.Y, 0.3f, 0.4f);
        Assert.True(step.X > 3f, $"stopped at {step.X}");
        Assert.InRange(ledge.Y, -0.05f, 0.05f);
        Assert.True(ledge.X < 2f - Radius + 0.05f, $"went on to {ledge.X}");
    }

    [Fact]
    public void ACharacterHoldsAGentleSlopeAndSlidesOffASteepOne()
    {
        // Two ramps rising along X, of 25 degrees, under its steepest of 45, and 60, over it, each
        // with its top at about 0.3 and 0.5 where the character is dropped onto it.
        var (gentle, gentleGrounded, steep) = Simulate(
            ecs =>
            {
                Block(ecs, new Vec3(4f, 0f, 0f), new Vec3(10f, 0.5f, 4f), 25f);
                Block(ecs, new Vec3(4f, 0f, 20f), new Vec3(10f, 0.5f, 4f), 60f);
                return [Character(ecs, new Vec3(4f, 2f, 0f)), Character(ecs, new Vec3(4f, 3f, 20f))];
            },
            3f,
            (ecs, _, e) => (Feet(ecs, e[0]), ecs.GetOrDefault<CharacterController>(e[0]).Grounded, Feet(ecs, e[1])));

        Assert.True(gentleGrounded);
        Assert.True(MathF.Abs(gentle.X - 4f) < 0.1f, $"crept to x {gentle.X} standing still on 25 degrees");
        Assert.InRange(gentle.Y, 0.2f, 0.4f);

        // Down to the floor at the ramp's foot.
        Assert.True(steep.X < 3.8f && steep.Y < 0.2f, $"held at {steep} on 60 degrees");
    }

    [Fact]
    public void AJumpLeavesTheGroundOnceAndOneAskedForInTheAirIsDropped()
    {
        var highest = 0f;
        var highestSecond = 0f;
        var airborne = false;
        var jumpLeft = -1f;

        Simulate(
            ecs => [Character(ecs, Vec3.Zero)],
            2.5f,
            (ecs, _, e) => 0,
            (ecs, e, step) =>
            {
                var controller = ecs.GetOrDefault<CharacterController>(e[0]);
                var feet = Feet(ecs, e[0]).Y;

                // Settled, then a jump at 5 a second, which rises 25 / 2g, about 1.27.
                if (step == 30)
                {
                    controller.Jump = 5f;
                    ecs.Set(e[0], controller);
                }

                if (step == 31)
                {
                    jumpLeft = controller.Jump;
                    airborne = !controller.Grounded;
                }

                // In the air, which does nothing, and is not kept for the landing.
                if (step == 45)
                {
                    controller.Jump = 5f;
                    ecs.Set(e[0], controller);
                }

                if (step < 100) highest = MathF.Max(highest, feet);
                else highestSecond = MathF.Max(highestSecond, feet);
            });

        Assert.Equal(0f, jumpLeft);
        Assert.True(airborne);
        Assert.InRange(highest, 1.1f, 1.4f);
        Assert.InRange(highestSecond, -0.05f, 0.05f);
    }

    [Fact]
    public void ACharacterKeepsTheRotationItsGameGaveItAndAControllerTakenAwayMakesAPlainBody()
    {
        var turned = Quat.FromAxisAngle(Vec3.UnitY, 1f);

        var (rotation, wasCharacter, isCharacter) = Simulate(
            ecs => [Character(ecs, Vec3.Zero)],
            1f,
            (ecs, physics, e) =>
            {
                var rotation = ecs.GetOrDefault<Transform>(e[0]).Rotation;
                var was = physics.IsCharacter(e[0]);
                ecs.Remove<CharacterController>(e[0]);
                physics.Sync(ecs);
                return (rotation, was, physics.IsCharacter(e[0]));
            },
            (ecs, e, step) =>
            {
                if (step != 10) return;
                ecs.GetRef<Transform>(e[0]).Rotation = turned;
                Walk(ecs, e[0], new Vec3(1f, 0f, 0f));
            });

        Assert.True(MathF.Abs(rotation.Y - turned.Y) < 1e-3f && MathF.Abs(rotation.W - turned.W) < 1e-3f, $"turned to {rotation}");
        Assert.True(wasCharacter);
        Assert.False(isCharacter);
    }

    [Fact]
    public void AFlyingCharacterRisesHoversAndIsStoppedByACeiling()
    {
        // A ceiling whose underside is at y 6, over the second character alone.
        var (risen, hovering, held, grounded) = Simulate(
            ecs =>
            {
                Block(ecs, new Vec3(10f, 6.5f, 0f), new Vec3(4f, 1f, 4f));
                return
                [
                    Character(ecs, Vec3.Zero, new CharacterController { Fly = true }),
                    Character(ecs, new Vec3(10f, 0f, 0f), new CharacterController { Fly = true }),
                ];
            },
            3f,
            (ecs, _, e) => (Feet(ecs, e[0]).Y, ecs.GetOrDefault<Transform>(e[0]).Translation, Feet(ecs, e[1]).Y, ecs.GetOrDefault<CharacterController>(e[0]).Grounded),
            (ecs, e, step) =>
            {
                // Up at two units a second for a second, then asked for nothing.
                var up = step < 60 ? new Vec3(0f, 2f, 0f) : Vec3.Zero;
                Walk(ecs, e[0], up);
                Walk(ecs, e[1], new Vec3(0f, 4f, 0f));
            });

        // Two units up and held there for two seconds against gravity, with no ground under it.
        Assert.InRange(risen, 1.8f, 2.2f);
        Assert.InRange(hovering.X, -0.05f, 0.05f);
        Assert.False(grounded, "a character two units up is not over ground it stands on");

        // Stopped under the ceiling, its head at the ceiling's underside.
        Assert.InRange(held, 6f - Height - 0.1f, 6f - Height + 0.02f);
    }

    [Fact]
    public void ACharacterPlacedElsewhereStandsThereAtRest()
    {
        PhysicsWorld? world = null;
        var (feet, speed) = Simulate(
            ecs => [Character(ecs, Vec3.Zero)],
            2f,
            (ecs, physics, e) => (Feet(ecs, e[0]), physics.Velocity(e[0]).Linear.Length),
            (ecs, e, step) =>
            {
                // Walking east for half a second, then put twenty units away, high above the floor,
                // and asked for nothing more.
                if (step == 0) Walk(ecs, e[0], new Vec3(4f, 0f, 0f));
                if (step == 30)
                {
                    Walk(ecs, e[0], Vec3.Zero);
                    world!.Place(ecs, e[0], new Vec3(-20f, 3f, 5f));
                    Assert.Equal(new Vec3(-20f, 3f, 5f), Feet(ecs, e[0]));
                }
            },
            physics => world = physics);

        // Fallen from where it was put to the floor under it, not walked or carried back east.
        Assert.InRange(feet.X, -20.05f, -19.95f);
        Assert.InRange(feet.Z, 4.95f, 5.05f);
        Assert.InRange(feet.Y, -0.05f, 0.05f);
        Assert.InRange(speed, 0f, 0.1f);
    }

    [Fact]
    public void ACharacterCrouchesAndStandsWithItsFeetWhereTheyWere()
    {
        // Settled standing, then asked to crouch to a unit, and the other way round.
        var (crouched, feet) = Simulate(
            ecs => [Character(ecs, Vec3.Zero)],
            1f,
            (ecs, physics, e) => (Tall(ecs, physics, e[0], 5f), Feet(ecs, e[0])),
            (ecs, e, step) =>
            {
                if (step == 20) Stand(ecs, e[0], 1f);
            });

        var stood = Simulate(
            ecs => [Character(ecs, Vec3.Zero, new CharacterController { Height = 1f })],
            1f,
            (ecs, physics, e) => Tall(ecs, physics, e[0], 5f),
            (ecs, e, step) =>
            {
                if (step == 20) Stand(ecs, e[0], Height);
            });

        Assert.InRange(crouched, 0.95f, 1.05f);
        Assert.InRange(feet.Y, -0.05f, 0.05f);
        Assert.InRange(MathF.Abs(feet.X) + MathF.Abs(feet.Z), 0f, 0.05f);
        Assert.InRange(stood, Height - 0.05f, Height + 0.05f);
    }

    [Fact]
    public void ACharacterCrouchedUnderALedgeStandsOnceItWalksOutFromUnderIt()
    {
        // A ledge whose underside is 1.5 up, two wide over where it crouches, which it fits under
        // crouched and not standing.
        static Entity[] Level(EcsWorld ecs)
        {
            Block(ecs, new Vec3(0f, 1.75f, 0f), new Vec3(2f, 0.5f, 2f));
            return [Character(ecs, Vec3.Zero, new CharacterController { Height = 1f })];
        }

        var under = Simulate(
            Level,
            1f,
            (ecs, physics, e) => Tall(ecs, physics, e[0], 1.45f),
            (ecs, e, step) =>
            {
                if (step == 10) Stand(ecs, e[0], Height);
            });

        var (outside, feet) = Simulate(
            Level,
            2f,
            (ecs, physics, e) => (Tall(ecs, physics, e[0], 5f), Feet(ecs, e[0])),
            (ecs, e, step) =>
            {
                if (step != 10) return;
                Stand(ecs, e[0], Height);
                Walk(ecs, e[0], new Vec3(3f, 0f, 0f));
            });

        Assert.InRange(under, 0.95f, 1.05f);
        Assert.True(feet.X > 2f, $"stopped at {feet.X}");
        Assert.InRange(outside, Height - 0.05f, Height + 0.05f);
    }
}
