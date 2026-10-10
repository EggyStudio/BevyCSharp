// Bevy's many_meshlet_materials example, examples/stress_tests/many_meshlet_materials.rs at
// v0.20.0, by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.StressTests;

// A grid of meshlet bunnies in sunlight, to measure what preparing meshlet materials costs. All of
// them share one material unless --unique-materials gives each a color of its own, which Bevy's
// says once made it specialize a pipeline for every one each frame. -n or --grid-size sets how many
// bunnies a side, 50 unless given.
//
// Needs a bridge built with --meshlet and a GPU with 64-bit texture atomics. Elsewhere it opens and
// draws nothing, since a meshlet mesh is drawn by nothing else.
internal static class ManyMeshletMaterials
{
    // Bevy's window for its stress tests, 1920 by 1080 at a scale factor of one with no vertical
    // sync, drawn as fast as it can, its frame times logged, and room for the clusters it asks for.
    public static void Configure(Config config)
    {
        (config.Width, config.Height, config.ScaleFactor) = (1920, 1080, 1f);
        config.Vsync = false;
        config.HeadlessFps = 0;
        config.LogFrameTimes = true;
        config.MeshletClusters = 8192;
    }

    public static void Build(App app)
    {
        var arguments = Environment.GetCommandLineArgs();
        var n = GridSize(arguments);
        var unique = arguments.Contains("--unique-materials");

        Console.WriteLine("Meshlet Stress Test");
        Console.WriteLine($"Grid size: {n}x{n} ({n * n} instances)");
        Console.WriteLine($"Materials: {(unique ? "UNIQUE" : "SHARED")}");

        app.Startup(ctx => Setup(ctx, n, unique), "many_meshlet_materials.Setup");
    }

    // The size of the grid, from -n or --grid-size, as Bevy reads its arguments.
    private static int GridSize(string[] arguments)
    {
        var index = Array.FindIndex(arguments, argument => argument is "-n" or "--grid-size");
        return index >= 0 && index + 1 < arguments.Length && int.TryParse(arguments[index + 1], out var size) ? size : 50;
    }

    private static void Setup(BehaviorContext ctx, int n, bool unique)
    {
        StressTest.Warn();
        var ecs = ctx.Ecs;

        var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, n, n * 1.5f), Vec3.Zero, Vec3.UnitY));
        Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });

        // Bevy's EulerRot::ZYX of nothing about Z, a radian about Y and an eighth of a turn down.
        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 3000f, Shadows = true });
        ecs.Add(light, new Transform(Vec3.Zero, Quat.FromRotationY(1f) * Quat.FromRotationX(-MathF.PI / 4f), Vec3.One));

        if (!Render.MeshletsActive) return;

        // The bunny Bevy's example loads, cut into clusters ahead of time.
        var bunny = AssetServer.Load(AssetKind.MeshletMesh, "meshlet/bunny.meshlet_mesh");
        var shared = Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f) });
        const float spacing = 2f;

        for (var x = 0; x < n; x++)
        {
            for (var z = 0; z < n; z++)
            {
                var material = unique
                    ? Render.CreateMaterial(new MaterialSettings { BaseColor = Color.FromSrgb((float)x / n, 0.5f, (float)z / n) })
                    : shared;

                var entity = ecs.Spawn();
                ecs.Add(entity, Transform.At(x * spacing - n, 0f, z * spacing - n));
                Render.SetMeshletMesh(ecs, entity, bunny);
                Render.SetMaterial(ecs, entity, material);
            }
        }
    }
}
