using Bevy;
using Bevy.Physics;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers bodies on collision layers, which pairs collide and which pass through, and what contacts, sensors, characters and rays follow.</summary>
/// <remarks>
/// Each steps a <see cref="PhysicsWorld"/> of its own by hand at sixty steps a second, with 3DEngine's
/// own cases for layers (its <c>8520dbe1</c>), a body made from components synced before each step.
/// </remarks>
[Collection("engine")]
public sealed class LayerTests
{
    private const float StepSeconds = 1f / 60f;

    /// <summary>
    /// Bodies on layers that do not collide pass through each other and report nothing, a box falls
    /// through a floor its layer does not collide with, and a ray cast from a body sees only what that
    /// body's layer collides with.
    /// </summary>
    [Fact]
    public void BodiesOnLayersThatDoNotCollidePassThroughEachOtherAndReportNothing()
    {
        var (sank, fell, met, pastUnder, seenUnderOrOver, layer) = (false, 0f, false, true, false, 0);

        InAWorld(new PhysicsSettings(), (ecs, physics) =>
        {
            Body(ecs, physics, new Vec3(0f, -0.5f, 0f), PhysicsShape.Box(new Vec3(40f, 1f, 40f)), BodyKind.Static);

            // A ball dropped onto another on a layer it does not collide with, and a box on a layer
            // the floor's does not collide with.
            physics.SetLayersCollide(1, 2, false);
            physics.SetLayersCollide(3, 0, false);
            var under = Body(ecs, physics, new Vec3(0f, 0.5f, 0f), PhysicsShape.Sphere(0.5f), BodyKind.Dynamic, mass: 50f);
            var over = Body(ecs, physics, new Vec3(0f, 3f, 0f), PhysicsShape.Sphere(0.5f), BodyKind.Dynamic);
            physics.SetLayer(under, 1);
            physics.SetLayer(over, 2);
            layer = physics.LayerOf(over);
            var sinking = Body(ecs, physics, new Vec3(5f, 1f, 0f), PhysicsShape.Box(Vec3.One), BodyKind.Dynamic);
            physics.SetLayer(sinking, 3);

            var bus = new MessageBus();
            for (var i = 0; i < 240 && !sank; i++)
            {
                physics.Step(ecs, StepSeconds, bus);
                bus.Swap();
                foreach (var contact in bus.Read<ContactStarted>())
                    met |= (contact.A == over && contact.B == under) || (contact.A == under && contact.B == over);
                sank = ecs.GetOrDefault<Transform>(sinking).Translation.Y < -3f;
            }

            fell = ecs.GetOrDefault<Transform>(over).Translation.Y;

            // A ray sees every layer, and one cast from a body only what that body's layer collides with.
            var past = Body(ecs, physics, new Vec3(0f, 5f, 0f), PhysicsShape.Sphere(0.2f), BodyKind.Dynamic);
            physics.SetLayer(past, 2);
            pastUnder = physics.Raycast(new Vec3(0f, 5f, 0f), new Vec3(0f, -1f, 0f), 20f, past)?.Entity == under;
            var below = physics.Raycast(new Vec3(0f, 4f, 0f), new Vec3(0f, -1f, 0f), 20f)?.Entity;
            seenUnderOrOver = below == under || below == over;
        });

        Assert.Equal(2, layer);
        Assert.True(sank, "the box did not fall through a floor its layer does not collide with");
        Assert.True(fell < 0.7f, $"the ball came to rest at {fell}, on the other rather than through it to the floor");
        Assert.False(met, "two bodies whose layers do not collide reported a contact");
        Assert.False(pastUnder, "a ray from a body on layer 2 saw layer 1");
        Assert.True(seenUnderOrOver, "a ray from no body did not see a ball of either layer");
    }

    /// <summary>A sensor on a layer reports only the bodies whose layers collide with its own.</summary>
    [Fact]
    public void ASensorOnALayerReportsOnlyTheLayersItCollidesWith()
    {
        var seen = new HashSet<Entity>();
        var (crate, player) = (Entity.None, Entity.None);

        InAWorld(new PhysicsSettings { Gravity = Vec3.Zero }, (ecs, physics) =>
        {
            physics.SetLayersCollide(5, 0, false);
            var sensor = Body(ecs, physics, Vec3.Zero, PhysicsShape.Box(new Vec3(4f)), BodyKind.Static, sensor: true);
            physics.SetLayer(sensor, 5);

            // Each in a lane of its own through the sensor, so they do not meet each other.
            crate = Body(ecs, physics, new Vec3(-3f, 0f, 1.2f), PhysicsShape.Box(Vec3.One), BodyKind.Dynamic);
            player = Body(ecs, physics, new Vec3(3f, 0f, -1.2f), PhysicsShape.Box(Vec3.One), BodyKind.Dynamic);
            physics.SetLayer(player, 6);
            physics.SetVelocity(crate, new Vec3(4f, 0f, 0f));
            physics.SetVelocity(player, new Vec3(-4f, 0f, 0f));

            var bus = new MessageBus();
            for (var i = 0; i < 240 && ecs.GetOrDefault<Transform>(player).Translation.X >= -2f; i++)
            {
                physics.Step(ecs, StepSeconds, bus);
                bus.Swap();
                foreach (var contact in bus.Read<ContactStarted>())
                    if (contact.A == sensor || contact.B == sensor) seen.Add(contact.A == sensor ? contact.B : contact.A);
            }
        });

        Assert.Contains(player, seen);
        Assert.DoesNotContain(crate, seen);
    }

    /// <summary>A character falls through a platform its layer does not collide with and stands on the floor below.</summary>
    [Fact]
    public void ACharacterFallsThroughAPlatformItsLayerDoesNotCollideWith()
    {
        var (feet, grounded) = (0f, false);

        InAWorld(new PhysicsSettings(), (ecs, physics) =>
        {
            Block(ecs, new Vec3(0f, -0.5f, 0f), new Vec3(40f, 1f, 40f), layer: 0);
            Block(ecs, new Vec3(0f, 2.5f, 0f), new Vec3(4f, 1f, 4f), layer: 8);
            physics.SetLayersCollide(8, 9, false);

            var character = ecs.Spawn();
            ecs.Add(character, Transform.At(0f, 3.1f, 0f));
            ecs.Add(character, new RigidBody { Kind = BodyKind.Dynamic, Mass = 80f, Layer = 9 });
            ecs.Add(character, new Collider { Shape = ColliderShape.Capsule, Size = new Vec3(0.8f, 1.8f, 0.8f), Offset = new Vec3(0f, 0.9f, 0f) });
            ecs.Add(character, new CharacterController());

            for (var i = 0; i < 180; i++)
            {
                physics.Sync(ecs);
                physics.Step(ecs, StepSeconds);
            }

            feet = ecs.GetOrDefault<Transform>(character).Translation.Y;
            grounded = ecs.GetOrDefault<CharacterController>(character).Grounded;
        });

        Assert.True(feet < 0.2f, $"the character stood at {feet}, on the platform it passes through rather than the floor");
        Assert.True(grounded, "the character on the floor was not grounded");
    }

    /// <summary>A crate asleep on a floor wakes and falls through once the floor's layer stops colliding with its own.</summary>
    [Fact]
    public void ACrateAsleepOnAFloorFallsOnceTheFloorsLayerStopsCollidingWithItsOwn()
    {
        var (asleep, fell) = (false, 0f);

        InAWorld(new PhysicsSettings(), (ecs, physics) =>
        {
            physics.SetLayersCollide(4, 0, false);
            var floor = Body(ecs, physics, new Vec3(0f, -0.5f, 0f), PhysicsShape.Box(new Vec3(40f, 1f, 40f)), BodyKind.Static);
            var crate = Body(ecs, physics, new Vec3(0f, 0.5f, 0f), PhysicsShape.Box(Vec3.One), BodyKind.Dynamic);
            for (var i = 0; i < 180; i++) physics.Step(ecs, StepSeconds);
            asleep = physics.IsAsleep(crate);

            // A static's change wakes what rests within its bounds, which a sleeping pair needs.
            physics.SetLayer(floor, 4);
            for (var i = 0; i < 60; i++) physics.Step(ecs, StepSeconds);
            fell = ecs.GetOrDefault<Transform>(crate).Translation.Y;
        });

        Assert.True(asleep, "the crate did not go to sleep on the floor");
        Assert.True(fell < -1f, $"the crate stayed at {fell} on a floor its layer no longer collides with");
    }

    private static void Block(EcsWorld ecs, Vec3 center, Vec3 size, int layer)
    {
        var block = ecs.Spawn();
        ecs.Add(block, Transform.At(center.X, center.Y, center.Z));
        ecs.Add(block, new RigidBody { Kind = BodyKind.Static, Layer = layer });
        ecs.Add(block, new Collider { Shape = ColliderShape.Box, Size = size });
    }

    private static Entity Body(EcsWorld ecs, PhysicsWorld physics, Vec3 at, PhysicsShape shape, BodyKind kind, float mass = 1f, bool sensor = false)
    {
        var entity = ecs.Spawn();
        var transform = Transform.At(at.X, at.Y, at.Z);
        ecs.Add(entity, transform);
        physics.Add(entity, shape, kind, transform, mass, sensor);
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
