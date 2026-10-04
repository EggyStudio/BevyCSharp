using Bevy;
using Bevy.Physics;

// The engine under load, for measuring what a frame holds. Two modes, each at a count given on the
// command line:
//
//   Stress movers <count>   entities a behavior moves every frame, with no renderer, so a frame is
//                           the managed systems, their calls into the bridge and Bevy's schedule
//   Stress drawn <count>    cubes drawn in a few materials under a few lights, one in ten of them a
//                           physics body, drawn offscreen with the render passes timed
//
// It runs until stopped, as fast as it can, and answers bcs, which measure.sh asks for
// frame.profile at each count.
if (args.Length != 2 || args[0] is not ("movers" or "drawn") || !int.TryParse(args[1], out var count) || count < 1)
{
    Console.Error.WriteLine("Stress movers|drawn <count>");
    return 2;
}

var drawn = args[0] == "drawn";

var config = drawn
    ? new Config { Offscreen = true, Width = 1280, Height = 720, GpuTimings = Environment.GetEnvironmentVariable("STRESS_GPU_TIMINGS") != "0" }
    : new Config { Headless = true };

// Unpaced, so a frame is as long as its work and no longer.
config.HeadlessFps = 0;
config.GameName = "Stress";
config.Serve = true;

Load.Drawn = drawn;
Load.Count = count;

return BevyApp.Run(app => app.AddPlugin(new PhysicsPlugin()), config);

/// <summary>The load the program puts the engine under, spawned at startup.</summary>
[Behavior]
public partial struct Load
{
    /// <summary>Whether the load is drawn cubes rather than movers.</summary>
    public static bool Drawn;

    /// <summary>How many.</summary>
    public static int Count;

    /// <summary>Whether the lights cast shadows, which STRESS_SHADOWS=0 turns off to see what they cost.</summary>
    public static bool Shadows = Environment.GetEnvironmentVariable("STRESS_SHADOWS") != "0";

    /// <summary>Spawns the load.</summary>
    [OnStartup]
    public static void Spawn(BehaviorContext ctx)
    {
        if (Drawn) Cubes(ctx);
        else Movers(ctx);

        Console.WriteLine($"[stress] {(Drawn ? "drawn" : "movers")} {Count}");
    }

    /// <summary>Movers in a box, each going its own way.</summary>
    private static void Movers(BehaviorContext ctx)
    {
        var random = new Random(1);
        for (var i = 0; i < Count; i++)
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, Transform.At(Next(random) * 50f, Next(random) * 50f, Next(random) * 50f));
            ctx.Ecs.Add(entity, new Mover { Velocity = new Vec3(Next(random), Next(random), Next(random)) * 5f });
        }
    }

    /// <summary>A square of cubes on a floor, seen from above, every tenth one falling onto it.</summary>
    private static void Cubes(BehaviorContext ctx)
    {
        var mesh = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);
        var materials = new[]
        {
            Render.CreateMaterial(0.8f, 0.3f, 0.3f),
            Render.CreateMaterial(0.3f, 0.8f, 0.3f),
            Render.CreateMaterial(0.3f, 0.3f, 0.8f),
            Render.CreateMaterial(0.8f, 0.8f, 0.3f),
        };

        var side = (int)MathF.Ceiling(MathF.Sqrt(Count));
        var across = side * 1.5f;

        var floor = ctx.Ecs.Spawn();
        ctx.Ecs.Add(floor, new Transform(new Vec3(0f, -0.5f, 0f), Quat.Identity, new Vec3(across + 4f, 1f, across + 4f)));
        ctx.Ecs.Add(floor, new RigidBody { Kind = BodyKind.Static });
        ctx.Ecs.Add(floor, new Collider { Shape = ColliderShape.Box, Size = Vec3.One });

        for (var i = 0; i < Count; i++)
        {
            var x = (i % side - side / 2f) * 1.5f;
            var z = (i / side - side / 2f) * 1.5f;
            var falls = i % 10 == 0;

            var cube = ctx.Ecs.Spawn();
            ctx.Ecs.Add(cube, Transform.At(x, falls ? 3f : 0.5f, z));
            Render.SetMesh(ctx.Ecs, cube, mesh);
            Render.SetMaterial(ctx.Ecs, cube, materials[i % materials.Length]);

            if (!falls) continue;
            ctx.Ecs.Add(cube, new RigidBody { Kind = BodyKind.Dynamic, Mass = 1f });
            ctx.Ecs.Add(cube, new Collider { Shape = ColliderShape.Box, Size = Vec3.One });
        }

        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 8000f, Shadows = Shadows });
        ctx.Ecs.Add(sun, Transform.LookingAt(new Vec3(1f, 2f, 1f), Vec3.Zero, Vec3.UnitY));

        foreach (var at in new[] { new Vec3(-across / 4f, 6f, 0f), new Vec3(across / 4f, 6f, 0f) })
        {
            var lamp = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 200_000f, Shadows = Shadows });
            ctx.Ecs.Add(lamp, Transform.At(at.X, at.Y, at.Z));
        }

        var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 60f });
        ctx.Ecs.Add(camera, Transform.LookingAt(new Vec3(0f, across * 0.9f + 10f, across * 0.4f), Vec3.Zero, Vec3.UnitY));
    }

    private static float Next(Random random) => (float)(random.NextDouble() * 2.0 - 1.0);
}

/// <summary>Something moving through a box, turning back at its walls.</summary>
[Behavior]
public partial struct Mover
{
    /// <summary>Where it is going, in units a second.</summary>
    public Vec3 Velocity;

    /// <summary>Moves every mover on through its transform, as a game moves what it moves.</summary>
    /// <remarks>
    /// A static method over a query rather than a method per mover, since a transform is another
    /// component, reached through the world, and the world is the main thread's alone, where a
    /// method per entity is spread across worker threads past a few thousand of them.
    /// </remarks>
    [OnUpdate]
    public static void Move(BehaviorContext ctx)
    {
        var delta = ctx.Time.Delta;

        foreach (var row in ctx.Ecs.Query<Mover>())
        {
            ref var mover = ref row.Component;
            ref var transform = ref ctx.Ecs.GetRef<Transform>(row.Entity);
            var at = transform.Translation + mover.Velocity * delta;

            if (MathF.Abs(at.X) > 50f) mover.Velocity = new Vec3(-mover.Velocity.X, mover.Velocity.Y, mover.Velocity.Z);
            if (MathF.Abs(at.Y) > 50f) mover.Velocity = new Vec3(mover.Velocity.X, -mover.Velocity.Y, mover.Velocity.Z);
            if (MathF.Abs(at.Z) > 50f) mover.Velocity = new Vec3(mover.Velocity.X, mover.Velocity.Y, -mover.Velocity.Z);

            transform.Translation = at;
        }
    }
}
