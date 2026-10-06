using Bevy;
using Bevy.Physics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers what a kinematic body carries, when its entity is moved once a frame or once a step and
/// the simulation steps at its own rate.
/// </summary>
/// <remarks>
/// Most run their frames by hand inside one system, with a <see cref="PhysicsWorld"/> of their own,
/// the way Bevy and <see cref="PhysicsPlugin"/> run them. A frame's time is added to what the fixed
/// clock has not yet stepped through, as many steps as that holds run, the frame's update moves the
/// platform, and the frame's end observes where it went. So any frame rate is a loop away, frames
/// of uneven length too, and the numbers are the same on every machine. The last runs the plugin
/// itself, on a clock set to a length a frame.
/// </remarks>
[Collection("engine")]
public sealed class KinematicBodyTests
{
    /// <summary>Bevy's fixed step, sixty-four a second.</summary>
    private const double StepSeconds = 1.0 / 64.0;

    /// <summary>
    /// A crate on a platform moved at 2 units a second keeps that pace, measured over two seconds
    /// of frames of each length and of two lengths taking turns.
    /// </summary>
    /// <remarks>
    /// Before the body followed its entity's speed, the crate rode at 1.795 at 144 frames a second,
    /// 1.714 at 75, 2.129 at 60, 2.287 at 50 and 0.021 at 30, and at 0.078 and 0.835 over the two
    /// uneven runs, as REVIEW.md's model of one dimension put it.
    /// </remarks>
    [Theory]
    [InlineData(144.0, 144.0)]
    [InlineData(75.0, 75.0)]
    [InlineData(60.0, 60.0)]
    [InlineData(50.0, 50.0)]
    [InlineData(30.0, 30.0)]
    [InlineData(100.0, 25.0)]
    [InlineData(144.0, 35.0)]
    public void ACrateOnAPlatformKeepsItsPaceAtEveryFrameRate(double fps, double otherFps)
    {
        var pace = 0.0;

        InAWorld((ecs, physics) =>
        {
            var platform = Platform(ecs, physics);
            var crate = Crate(ecs, physics, Vec3.Zero);

            var frames = new Frames(ecs, physics, fps, otherFps);
            frames.Run(0.5);

            frames.Update = seconds => Move(ecs, platform, new Vec3(2f * (float)seconds, 0f, 0f));
            frames.Run(1.0);
            var from = ecs.GetOrDefault<Transform>(crate).Translation.X;
            var spent = frames.Run(2.0);
            pace = (ecs.GetOrDefault<Transform>(crate).Translation.X - from) / spent;
        });

        Assert.True(Math.Abs(pace - 2.0) < 0.02, $"the crate rode at {pace:0.000} on a platform at 2, at {fps} and {otherFps} frames a second");
    }

    /// <summary>
    /// A platform moved a step's distance each fixed step moves at its speed in every step, a
    /// frame holding several steps or none, and carries its crate at that pace.
    /// </summary>
    [Theory]
    [InlineData(144.0)]
    [InlineData(30.0)]
    public void APlatformMovedInTheStepsIsFollowedExactlyEachStep(double fps)
    {
        var speeds = new List<float>();
        var pace = 0.0;

        InAWorld((ecs, physics) =>
        {
            var platform = Platform(ecs, physics);
            var crate = Crate(ecs, physics, Vec3.Zero);

            var frames = new Frames(ecs, physics, fps, fps);
            frames.Run(0.5);

            frames.FixedUpdate = () => Move(ecs, platform, new Vec3(2f * (float)StepSeconds, 0f, 0f));
            frames.AfterStep = () => speeds.Add(physics.Velocity(platform).Linear.X);
            frames.Run(1.0);
            var from = ecs.GetOrDefault<Transform>(crate).Translation.X;
            var spent = frames.Run(2.0);
            pace = (ecs.GetOrDefault<Transform>(crate).Translation.X - from) / spent;
        });

        Assert.NotEmpty(speeds);
        Assert.All(speeds, speed => Assert.InRange(speed, 1.999f, 2.001f));
        Assert.True(Math.Abs(pace - 2.0) < 0.02, $"the crate rode at {pace:0.000} on a platform at 2, at {fps} frames a second");
    }

    /// <summary>
    /// A platform turned once a frame turns its body at the same rate through every step, and a
    /// crate standing off its middle is carried round with it.
    /// </summary>
    /// <remarks>
    /// A radian a second at three units out, which asks 3 a second squared of the friction toward
    /// the middle and 9.81 is there. When a box slid a quarter as rough as its friction, the crate
    /// slid outward as it was carried and went 1.64 radians round in the two seconds.
    /// </remarks>
    [Theory]
    [InlineData(144.0)]
    [InlineData(30.0)]
    public void ATurningPlatformCarriesACrateRound(double fps)
    {
        const float Rate = 1f;
        var spins = new List<float>();
        var (from, to, radius) = (0f, 0f, 0f);

        InAWorld((ecs, physics) =>
        {
            var platform = Platform(ecs, physics);
            var crate = Crate(ecs, physics, new Vec3(3f, 0f, 0f));

            var frames = new Frames(ecs, physics, fps, fps);
            frames.Run(0.5);

            var turned = 0f;
            frames.Update = seconds =>
            {
                turned += Rate * (float)seconds;
                var at = ecs.GetOrDefault<Transform>(platform);
                ecs.Set(platform, at with { Rotation = Quat.FromRotationY(turned) });
            };
            frames.Run(1.0);

            from = Angle(ecs, crate);
            frames.AfterStep = () => spins.Add(physics.Velocity(platform).Angular.Y);
            frames.Run(2.0);
            to = Angle(ecs, crate);

            var at = ecs.GetOrDefault<Transform>(crate).Translation;
            radius = MathF.Sqrt(at.X * at.X + at.Z * at.Z);
        });

        // Two radians, unwound once, since past a half turn the angle comes back from the other side.
        var swept = to - from;
        if (swept < 0f) swept += 2f * MathF.PI;
        Assert.All(spins, spin => Assert.InRange(spin, 0.99f, 1.01f));
        Assert.True(Math.Abs(swept - 2f) < 0.05f, $"the crate went {swept} radians round in two seconds of a radian");
        Assert.True(Math.Abs(radius - 3f) < 0.075f, $"the crate slid out to {radius} from 3");
    }

    /// <summary>
    /// A platform put far away, or near with a word that it was put, has its body put there at
    /// rest, and the crate that stood on it is not flung, whether the platform went from under it
    /// or is still beneath it.
    /// </summary>
    [Theory]
    [InlineData(200f, false)]
    [InlineData(5f, true)]
    public void APlatformPutSomewhereElseIsPutThereAndFlingsNothing(float distance, bool said)
    {
        var hit = (PhysicsHit?)null;
        var platformSpeed = float.MaxValue;
        var crateSpeed = float.MaxValue;
        var platformEntity = Entity.None;

        InAWorld((ecs, physics) =>
        {
            var platform = platformEntity = Platform(ecs, physics);
            var crate = Crate(ecs, physics, Vec3.Zero);

            var frames = new Frames(ecs, physics, 60.0, 60.0);
            frames.Run(0.5);

            Move(ecs, platform, new Vec3(0f, 0f, distance));
            if (said) physics.MarkPlaced(platform);
            frames.Run(0.1);

            hit = physics.Raycast(new Vec3(-10f, 5f, distance), new Vec3(0f, -1f, 0f), 10f);
            platformSpeed = physics.Velocity(platform).Linear.Length;
            // Along the ground only, since a crate the platform was put away from falls.
            var flung = physics.Velocity(crate).Linear;
            crateSpeed = MathF.Sqrt(flung.X * flung.X + flung.Z * flung.Z);
        });

        Assert.Equal(platformEntity, hit?.Entity);
        Assert.True(platformSpeed < 0.01f, $"the platform moves at {platformSpeed} where it was put");
        Assert.True(crateSpeed < 1f, $"the crate that stood on it was flung at {crateSpeed}");
    }

    /// <summary>
    /// Through <see cref="PhysicsPlugin"/>, a platform a system moves once a frame carries its
    /// crate at its pace, at a frame rate whose frames each hold one step or none and at one whose
    /// frames hold two or three.
    /// </summary>
    [Theory]
    [InlineData(144.0)]
    [InlineData(25.0)]
    public void ThePluginCarriesACrateAtItsPlatformsPace(double fps)
    {
        var frame = 0;
        var (platform, crate) = (Entity.None, Entity.None);
        var (from, to) = (0f, 0f);
        var (settled, measured) = ((int)(1.5 * fps), (int)(3.5 * fps));

        using var harness = new EngineHarness(frames: (uint)measured + 1, frameSeconds: 1.0 / fps);
        harness.App.AddPlugin(new PhysicsPlugin());
        harness.OnContext(Stage.Startup, ctx =>
        {
            var physics = ctx.Res<PhysicsWorld>();
            platform = Platform(ctx.Ecs, physics);
            crate = Crate(ctx.Ecs, physics, Vec3.Zero);
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            frame++;
            if (frame > fps / 2) Move(ctx.Ecs, platform, new Vec3(2f * ctx.Time.Delta, 0f, 0f));
            if (frame == settled) from = ctx.Ecs.GetOrDefault<Transform>(crate).Translation.X;
            if (frame == measured) to = ctx.Ecs.GetOrDefault<Transform>(crate).Translation.X;
        });

        harness.Run();

        var pace = (to - from) / ((measured - settled) / fps);
        Assert.True(Math.Abs(pace - 2.0) < 0.02, $"the crate rode at {pace:0.000} on a platform at 2, at {fps} frames a second");
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

    /// <summary>A kinematic platform forty units long, its top at half a unit up.</summary>
    private static Entity Platform(EcsWorld ecs, PhysicsWorld physics)
    {
        var platform = ecs.Spawn();
        ecs.Add(platform, Transform.At(0f, 0.25f, 0f));
        physics.Add(platform, PhysicsShape.Box(new Vec3(40f, 0.5f, 40f)), BodyKind.Kinematic, Transform.At(0f, 0.25f, 0f));
        return platform;
    }

    /// <summary>A crate of a unit standing on the platform at <paramref name="at"/>.</summary>
    private static Entity Crate(EcsWorld ecs, PhysicsWorld physics, Vec3 at)
    {
        var crate = ecs.Spawn();
        var transform = Transform.At(at.X, 1f, at.Z);
        ecs.Add(crate, transform);
        physics.Add(crate, PhysicsShape.Box(new Vec3(1f)), BodyKind.Dynamic, transform);
        return crate;
    }

    private static void Move(EcsWorld ecs, Entity entity, Vec3 by)
    {
        var at = ecs.GetOrDefault<Transform>(entity);
        at.Translation += by;
        ecs.Set(entity, at);
    }

    /// <summary>Where an entity is round the vertical axis through the origin, as the platform turns, in radians.</summary>
    private static float Angle(EcsWorld ecs, Entity entity)
    {
        var at = ecs.GetOrDefault<Transform>(entity).Translation;
        return MathF.Atan2(-at.Z, at.X);
    }

    /// <summary>Frames run by hand as Bevy and the plugin run them.</summary>
    private sealed class Frames(EcsWorld ecs, PhysicsWorld physics, double fps, double otherFps)
    {
        private double _overstep;
        private int _count;

        /// <summary>The frame's update, given the frame's seconds, after the frame's steps.</summary>
        public Action<double>? Update { get; set; }

        /// <summary>What runs in each fixed step before the simulation steps.</summary>
        public Action? FixedUpdate { get; set; }

        /// <summary>What runs in each fixed step after the simulation has stepped.</summary>
        public Action? AfterStep { get; set; }

        /// <summary>Runs frames until <paramref name="seconds"/> have passed, and says how many did.</summary>
        public double Run(double seconds)
        {
            var spent = 0.0;
            while (spent < seconds)
            {
                var frame = 1.0 / (_count++ % 2 == 0 ? fps : otherFps);
                spent += frame;

                _overstep += frame;
                while (_overstep >= StepSeconds)
                {
                    FixedUpdate?.Invoke();
                    physics.Step(ecs, (float)StepSeconds);
                    AfterStep?.Invoke();
                    _overstep -= StepSeconds;
                }

                Update?.Invoke(frame);
                physics.Observe(ecs, frame, _overstep);
            }

            return spent;
        }
    }
}
