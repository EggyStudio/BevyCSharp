// Bevy's many_materials example, examples/stress_tests/many_materials.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.StressTests;

// Draws a grid of cubes, each with a material of its own whose color is changed every frame, to
// measure how fast materials are written. -n or --grid-size sets how many cubes a side, 10 unless
// given.
internal static class ManyMaterials
{
    // Each cube's material, which Bevy finds through the cubes' material handles each frame.
    private static readonly List<AssetHandle> Materials = [];

    // Bevy's window for its stress tests, 1920 by 1080 at a scale factor of one with no vertical
    // sync, drawn as fast as it can, and its frame times logged once a second by Bevy's plugins.
    public static void Configure(Config config)
    {
        (config.Width, config.Height, config.ScaleFactor) = (1920, 1080, 1f);
        config.Vsync = false;
        config.HeadlessFps = 0;
        config.LogFrameTimes = true;
    }

    public static void Build(App app)
    {
        StressTest.Warn();
        Materials.Clear();

        app.Startup(ctx => Setup(ctx, GridSize()), "many_materials.Setup");
        app.Update(AnimateMaterials, "many_materials.AnimateMaterials");
    }

    // The size of the grid, from -n or --grid-size, as Bevy reads its arguments.
    private static int GridSize()
    {
        var arguments = Environment.GetCommandLineArgs();
        var index = Array.FindIndex(arguments, argument => argument is "-n" or "--grid-size");
        return index >= 0 && index + 1 < arguments.Length && int.TryParse(arguments[index + 1], out var size) ? size : 10;
    }

    private static void Setup(BehaviorContext ctx, int n)
    {
        var ecs = ctx.Ecs;
        var w = (float)n;
        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(w * 1.25f, w + 1f, w * 1.25f), new Vec3(0f, w * -1.1f + 1f, 0f), Vec3.UnitY));

        // Bevy's EulerRot::ZYX of nothing about Z, a radian about Y and an eighth of a turn down.
        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 3000f, Shadows = true });
        ecs.Add(light, new Transform(Vec3.Zero, Quat.FromRotationY(1f) * Quat.FromRotationX(-MathF.PI / 4f), Vec3.One));

        var mesh = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);
        for (var x = 0; x < n; x++)
        {
            for (var z = 0; z < n; z++)
            {
                var material = Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f) });
                Materials.Add(material);
                ecs.SpawnMesh(mesh, material, Transform.At(x, 0f, z));
            }
        }
    }

    // Each material's hue moved on with the clock, the hues spread so no two cubes match.
    private static void AnimateMaterials(BehaviorContext ctx)
    {
        var elapsed = ctx.Time.Elapsed;
        for (var i = 0; i < Materials.Count; i++)
        {
            var hue = (i * 2.345f + elapsed) * 100f % 360f;
            Render.WriteMaterial(Materials[i], new MaterialSettings { BaseColor = Color.FromHsl(hue, 1f, 0.5f) });
        }
    }
}
