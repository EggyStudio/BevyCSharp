// Bevy's error_handling example, examples/ecs/error_handling.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Ecs;

// What becomes of an error in a system, a command and an observer. Bevy's systems return a Result,
// and an error goes to the app's error handler, which Bevy's example sets to warn rather than panic.
// A C# system throws instead, and the engine catches it at the system's edge, logs it and runs on,
// as Bevy's warn handler does, where Config.FailFastOnSystemException stops the app as Bevy's
// default panics. A system that means to deal with its own error catches it, as Bevy's pipes its
// result into a closure that logs it.
//
// The scene is a sphere covered in ten thousand red points sampled evenly over its surface, which
// Bevy samples with its UniformMeshSampler and this from the sphere's triangles read back, each
// picked by its area and a point picked evenly within it. Moving the pointer over the sphere slides
// it back and forth, through an observer that would fail on an entity with no transform.
internal static class ErrorHandling
{
    public static void Configure(Config config) => config.MeshPicking = true;

    public static void Build(App app)
    {
        app.Startup(Setup, "error_handling.Setup");
        app.Startup(FailingCommands, "error_handling.FailingCommands");
        app.Startup(FailingSystem, "error_handling.FailingSystem");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var random = new Random(1987836746);

        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 12f, 12f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.5f, 0.3f)), Transform.At(0f, -2.5f, 0f));
        ecs.SpawnPointLight(new Vec3(4f, 8f, 4f), shadows: true);
        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2f, 3f, 5f), Vec3.Zero, Vec3.UnitY));

        var sphereMesh = Render.CreateMesh(MeshShape.IcoSphere, 1f, 7f);
        var sphere = ecs.SpawnMesh(sphereMesh, Render.CreateMaterial((1f, 1f, 1f, 1f)), Transform.At(-1f, 1f, 0f));

        // Bevy's UniformMeshSampler over the sphere's triangles, as the code here can read them.
        if (!Render.TryReadMesh(sphereMesh, out var triangles) || triangles is null)
            throw new InvalidOperationException("The sphere's triangles could not be read back.");

        var pointMesh = Render.CreateMesh(MeshShape.IcoSphere, 0.01f, 3f);
        var pointMaterial = Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 0f, 0f, 1f), Emissive = (1f, 0f, 0f, 1f) });
        foreach (var point in SampleSurface(triangles, random).Take(10_000))
            ecs.SetParent(ecs.SpawnMesh(pointMesh, pointMaterial, Transform.At(point.X, point.Y, point.Z)), sphere);

        // Bevy's fallible_observer, sliding what the pointer moves over, which fails with nothing
        // to slide where the entity has no transform. Its step is kept between moves, as Bevy's Local.
        var step = 0f;
        ecs.Observe<Pointer<Move>>(on =>
        {
            if (!on.Ecs.Has<Transform>(on.Event.Entity)) throw new InvalidOperationException("No transform found.");

            ref var transform = ref on.Ecs.GetRef<Transform>(on.Event.Entity);
            step = transform.Translation.X > 3f ? -0.1f : transform.Translation.X < -3f || step == 0f ? 0.1f : step;
            transform.Translation = transform.Translation with { X = transform.Translation.X + step };
        });
    }

    // Points evenly over a mesh's surface, each triangle picked by its area and a point picked
    // evenly within it, as Bevy's UniformMeshSampler picks them.
    private static IEnumerable<Vec3> SampleSurface(MeshData mesh, Random random)
    {
        var positions = mesh.Positions;
        var indices = mesh.Indices ?? [.. Enumerable.Range(0, positions.Length).Select(i => (uint)i)];
        var triangles = indices.Length / 3;
        var areas = new float[triangles];
        var total = 0f;
        for (var t = 0; t < triangles; t++)
        {
            var (a, b, c) = (positions[indices[3 * t]], positions[indices[3 * t + 1]], positions[indices[3 * t + 2]]);
            total += Vec3.Cross(b - a, c - a).Length / 2f;
            areas[t] = total;
        }

        while (true)
        {
            var at = Array.BinarySearch(areas, random.NextSingle() * total);
            var t = at < 0 ? ~at : at;
            var (a, b, c) = (positions[indices[3 * t]], positions[indices[3 * t + 1]], positions[indices[3 * t + 2]]);

            // Two numbers folded into the triangle, which keeps the spread even over it.
            var (u, v) = (random.NextSingle(), random.NextSingle());
            if (u + v > 1f) (u, v) = (1f - u, 1f - v);
            yield return a + (b - a) * u + (c - a) * v;
        }
    }

    // Bevy's failing_commands, a command for an entity that is not there, which the bridge refuses
    // and the system's error is caught and logged by its own handler, as Bevy's queue_handled has
    // its own.
    private static void FailingCommands(BehaviorContext ctx)
    {
        try
        {
            ctx.Ecs.Add(new Entity(12_345_678), Transform.Identity);
        }
        catch (Exception error)
        {
            Log.Error($"{error.Message}, while adding a transform to an entity that does not exist");
        }
    }

    // Bevy's failing_system, whose result is piped into a closure that logs it as captured. A
    // resource the app never made is asked for, and the error is caught here rather than left to
    // the engine.
    private static void FailingSystem(BehaviorContext ctx)
    {
        try
        {
            _ = ctx.World.Resource<UninitializedResource>();
        }
        catch (InvalidOperationException error)
        {
            Log.Info($"captured error: {error.Message}");
        }
    }

    // A resource the app never inserts.
    private sealed class UninitializedResource;
}
