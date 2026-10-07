using Bevy;
using Bevy.Physics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a joint a scene file describes as an entity of its own, made where the entity stands and
/// along its up direction once its two bodies are, refused where it cannot be, and taken away with
/// its entity.
/// </summary>
/// <remarks>
/// Each writes its level to a scene file from one engine and loads it in another, so the two
/// entities a joint names reach it as the file numbered them, then steps a world of its own by
/// hand, as 3DEngine's test of the same does (its <c>e46058fc</c>).
/// </remarks>
[Collection("engine")]
public sealed class SceneJointTests : IDisposable
{
    private const float StepSeconds = 1f / 60f;

    private readonly TestFolder _folder = new("bcs-scenejoint-");

    public void Dispose() => _folder.Dispose();

    /// <summary>
    /// A hinge in a scene file holds its door up where the hinge's entity stands, lets it swing to
    /// its limit about the entity's up direction and no further, and goes with its entity.
    /// </summary>
    [Fact]
    public void AScenesHingeHangsItsDoorWhereItStandsAndKeepsItsLimits()
    {
        var path = Write(ecs =>
        {
            var post = Block(ecs, "Post", new Vec3(0f, 1f, 0f), new Vec3(0.1f, 2f, 0.1f), BodyKind.Kinematic);
            var door = Block(ecs, "Door", new Vec3(0.6f, 1f, 0f), new Vec3(1f, 2f, 0.1f), BodyKind.Dynamic);
            var hinge = ecs.Spawn();
            ecs.SetName(hinge, "Hinge");
            ecs.Add(hinge, Transform.At(0.05f, 1f, 0f));
            ecs.Add(hinge, new JointBetween { Kind = JointKind.Hinge, A = post, B = door, MinAngle = -45f, MaxAngle = 45f });
        });

        var (made, widest, lowest, gone, fallen) = (false, 0f, float.MaxValue, false, 0f);

        Load(path, new PhysicsSettings(), (ecs, physics, named) =>
        {
            var door = named["Door"];
            var handle = physics.JointOf(named["Hinge"]);
            made = handle is not null;

            // Swung the positive way about up, which takes its middle toward negative Z.
            physics.SetVelocity(door, new Vec3(0f, 0f, -2.2f), new Vec3(0f, 4f, 0f));
            for (var i = 0; i < 120; i++)
            {
                physics.Step(ecs, StepSeconds);
                var at = ecs.GetOrDefault<Transform>(door).Translation - new Vec3(0.05f, 1f, 0f);
                widest = MathF.Max(widest, MathF.Abs(MathF.Atan2(-at.Z, at.X) * 180f / MathF.PI));
                lowest = MathF.Min(lowest, at.Y);
            }

            ecs.Despawn(named["Hinge"]);
            physics.Sync(ecs);
            gone = physics.JointOf(named["Hinge"]) is null && handle is { } joint && !physics.Disconnect(joint);

            for (var i = 0; i < 30; i++) physics.Step(ecs, StepSeconds);
            fallen = ecs.GetOrDefault<Transform>(door).Translation.Y;
        });

        Assert.True(made, "the hinge was not made once both bodies were");
        Assert.InRange(widest, 30f, 55f);
        Assert.True(lowest > -0.05f, $"the door sagged to {lowest} on its hinge");
        Assert.True(gone, "the hinge stayed after its entity went");
        Assert.True(fallen < 0.5f, $"the door stayed up at {fallen} with no hinge");
    }

    /// <summary>
    /// A joint naming a static body is refused and not tried again, and is made once its component
    /// names a body that moves.
    /// </summary>
    [Fact]
    public void AJointThatCannotBeMadeIsRefusedUntilItChanges()
    {
        var path = Write(ecs =>
        {
            var floor = Block(ecs, "Floor", new Vec3(0f, -0.5f, 0f), new Vec3(10f, 1f, 10f), BodyKind.Static);
            var ball = Block(ecs, "Ball", new Vec3(0f, 2f, 0f), Vec3.One, BodyKind.Dynamic);
            Block(ecs, "Hook", new Vec3(0f, 3f, 0f), Vec3.One, BodyKind.Kinematic);
            var joint = ecs.Spawn();
            ecs.SetName(joint, "Joint");
            ecs.Add(joint, Transform.At(0f, 2.5f, 0f));
            ecs.Add(joint, new JointBetween { Kind = JointKind.Ball, A = floor, B = ball });
        });

        var (refused, still, madeAfter) = (true, true, false);
        var named = new Dictionary<string, Entity>();
        var frame = 0;

        // Changed in a frame of its own and synced in the next, as a game's system and the physics
        // plugin's are apart, since a change made after a sync in the same run of a system has the
        // sync's own tick and is not newer than it.
        using var physics = new PhysicsWorld(new PhysicsSettings());
        using var loading = new EngineHarness(frames: 5);
        loading.OnContext(Stage.Startup, ctx =>
        {
            named = Named(ctx.Ecs, path);
            physics.Sync(ctx.Ecs);
            refused = physics.JointOf(named["Joint"]) is null;
        });

        loading.OnContext(Stage.Update, ctx =>
        {
            var joint = named["Joint"];
            switch (++frame)
            {
                case 1:
                    physics.Step(ctx.Ecs, StepSeconds);
                    physics.Sync(ctx.Ecs);
                    still = physics.JointOf(joint) is null;
                    break;
                case 2:
                    ctx.Ecs.Set(joint, ctx.Ecs.GetOrDefault<JointBetween>(joint) with { A = named["Hook"] });
                    break;
                case 3:
                    physics.Sync(ctx.Ecs);
                    madeAfter = physics.JointOf(joint) is not null;
                    break;
            }
        });

        loading.Run();

        Assert.True(refused);
        Assert.True(still);
        Assert.True(madeAfter, "the joint was not made once it named a body that moves");
    }

    /// <summary>A slider in a scene file runs along its entity's up direction, turned here to point along X, and its drive takes it to its travel's end.</summary>
    [Fact]
    public void AScenesSliderRunsAlongItsEntitysUpDirectionToTheEndOfItsTravel()
    {
        var path = Write(ecs =>
        {
            var frame = Block(ecs, "Frame", Vec3.Zero, new Vec3(0.2f, 0.2f, 0.2f), BodyKind.Kinematic);
            var block = Block(ecs, "Block", Vec3.Zero, new Vec3(0.5f, 0.5f, 0.5f), BodyKind.Dynamic);
            var slider = ecs.Spawn();
            ecs.SetName(slider, "Slider");
            ecs.Add(slider, new Transform(Vec3.Zero, Quat.FromAxisAngle(Vec3.UnitZ, -MathF.PI / 2f), Vec3.One));
            ecs.Add(slider, new JointBetween { Kind = JointKind.Slider, A = frame, B = block, MaxTravel = 0.5f, DriveSpeed = 1f, DriveForce = 100f });
        });

        var (position, at) = (float.NaN, Vec3.Zero);

        Load(path, new PhysicsSettings { Gravity = Vec3.Zero }, (ecs, physics, named) =>
        {
            for (var i = 0; i < 120; i++) physics.Step(ecs, StepSeconds);
            position = physics.JointOf(named["Slider"]) is { } joint ? physics.SliderPosition(joint) ?? float.NaN : float.NaN;
            at = ecs.GetOrDefault<Transform>(named["Block"]).Translation;
        });

        Assert.InRange(position, 0.45f, 0.55f);
        Assert.InRange(at.X, 0.45f, 0.55f);
        Assert.InRange(MathF.Abs(at.Y) + MathF.Abs(at.Z), 0f, 0.02f);
    }

    private static Entity Block(EcsWorld ecs, string name, Vec3 at, Vec3 size, BodyKind kind)
    {
        var block = ecs.Spawn();
        ecs.SetName(block, name);
        ecs.Add(block, Transform.At(at.X, at.Y, at.Z));
        ecs.Add(block, new RigidBody { Kind = kind, Mass = kind == BodyKind.Dynamic ? 1f : 0f });
        ecs.Add(block, new Collider { Shape = ColliderShape.Box, Size = size });
        return block;
    }

    // Writes the level a scene file holds, from an engine of its own.
    private string Write(Action<EcsWorld> level)
    {
        var path = _folder.File(Guid.NewGuid().ToString("n") + ".scene.json");
        using var making = new EngineHarness(frames: 1);
        making.OnContext(Stage.Startup, ctx =>
        {
            level(ctx.Ecs);
            SceneFile.Save(ctx.Ecs, path);
        });

        making.Run();
        return path;
    }

    // Loads a scene file, its entities by name.
    private static Dictionary<string, Entity> Named(EcsWorld ecs, string path) =>
        SceneFile.Load(ecs, path).Entities.ToDictionary(entity => ecs.NameOf(entity) ?? "", entity => entity);

    // Loads a scene file in another engine, syncs a world to it, and hands over its entities by name.
    private static void Load(string path, PhysicsSettings settings, Action<EcsWorld, PhysicsWorld, Dictionary<string, Entity>> body)
    {
        var ran = false;
        using var loading = new EngineHarness(frames: 1);
        loading.OnContext(Stage.Startup, ctx =>
        {
            var named = Named(ctx.Ecs, path);
            using var physics = new PhysicsWorld(settings);
            physics.Sync(ctx.Ecs);
            body(ctx.Ecs, physics, named);
            ran = true;
        });

        loading.Run();
        Assert.True(ran);
    }
}
