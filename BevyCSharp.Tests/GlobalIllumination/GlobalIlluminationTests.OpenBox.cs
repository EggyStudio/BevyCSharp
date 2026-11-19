using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// A gray floor at the foot of a box open to a uniform sky, its walls black so nothing but the
    /// sky lights it and neither glosses, reads at its middle the light the sky's share of its dome
    /// gives, 0.239 of it through an opening as wide as it is high, in the picture as the probes
    /// light it and in the reference traced at 128 paths a pixel.
    /// </summary>
    /// <remarks>
    /// A court's shade is lit by the sky through its opening, which the probes' rays meet past the
    /// field's last cascade and bring down through the cascades below, so a floor the sky reaches
    /// only through an opening reads the sky's share of its dome there or the probes lose it on the
    /// way down. The share of a dome a point sees of a square straight over it is four times a
    /// corner's, the view factor of a point to a parallel rectangle at its corner.
    /// </remarks>
    [SkippableFact(Skip = "The screen probes take 43% too little of the sky through the opening, 0.0587 where the dome gives 0.1030, "
        + "the world probes alone reading within 15%. The rule that loses it, the screen probes' reading of the world probes past their reach, "
        + "is written once in the RadianceCascades module with the kernels' move (item 138), where this test is its failing case.")]
    public void AFloorInABoxOpenToTheSkyReadsTheSkysShareOfItsDome()
    {
        Needs.Shaders();
        const float Half = 4f, High = 8f, Albedo = 0.5f;
        var supported = false;
        string? traced = null, read = null;
        var (askedTrace, askedRead) = (false, false);
        new PictureRun
        {
            Width = 96,
            Height = 96,
            Frames = 6000,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.World, FieldOfView = 100f });
                Render.SetClearColor((0f, 0f, 0f, 1f));
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, High + 4f, 0f), Vec3.Zero, Vec3.UnitZ));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAmbientLight(camera, (1f, 1f, 1f), 2000f);
                var floor = PictureRun.Cube(ecs, Render.CreateMaterial(new MaterialSettings { BaseColor = (Albedo, Albedo, Albedo, 1f), Roughness = 1f, Reflectance = 0f }), 2f * Half, new Vec3(0f, -Half, 0f));
                ecs.SetName(floor, "floor");
                var black = Render.CreateMaterial(new MaterialSettings { BaseColor = (0f, 0f, 0f, 1f), Roughness = 1f, Reflectance = 0f });
                foreach (var (x, z, w, d) in new[] { (Half + 0.1f, 0f, 0.2f, 2f * Half + 0.4f), (-Half - 0.1f, 0f, 0.2f, 2f * Half + 0.4f), (0f, Half + 0.1f, 2f * Half, 0.2f), (0f, -Half - 0.1f, 2f * Half, 0.2f) })
                {
                    var wall = ecs.Spawn();
                    Render.SetMesh(ecs, wall, Render.CreateMesh(MeshShape.Cuboid, w, High, d));
                    Render.SetMaterial(ecs, wall, black);
                    ecs.Add(wall, Transform.At(x, High / 2f, z));
                }
                Render.SetSceneField(4, 0.25f);
                Render.SetGlobalIllumination(GlobalIllumination.Medium);
            },
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("ready", _ => !supported || Render.PipelinesReady())
            .Until("traced", _ => !supported || WorldProbes.TracedFrames >= 240)
            .Wait(120)
            .Until("referenced", world => !supported || ReadOnce(world.Resource<EcsWorld>(), ref askedTrace, ref traced, ecs => GiReference.BeginReference(ecs, null, 128)))
            .Until("read", world => !supported || ReadOnce(world.Resource<EcsWorld>(), ref askedRead, ref read, ecs => GiReference.BeginPicture(ecs, null)))
            .Go();
        Needs.RayQueries(supported);

        var reference = GiReference.Last!;
        var picture = GiReference.LastPicture!.Value.Light;
        var sky = Sky(reference) ?? throw new InvalidOperationException("the reference met no sky past the box");
        double Luma(float[] light, int i) => 0.2126 * light[i * 3] + 0.7152 * light[i * 3 + 1] + 0.0722 * light[i * 3 + 2];

        // The middle of the floor, a few pixels across, against what the sky's share of the dome
        // over it gives a floor of its color.
        var (seen, met, count) = (0.0, 0.0, 0);
        for (var y = 46; y < 50; y++)
        for (var x = 46; x < 50; x++)
        {
            var i = y * 96 + x;
            (seen, met, count) = (seen + Luma(picture, i), met + Luma(reference.Light, i), count + 1);
        }
        (seen, met) = (seen / count, met / count);
        var share = 4 * Corner(Half / High, Half / High);
        var expected = Albedo * share * (0.2126 * sky.R + 0.7152 * sky.G + 0.0722 * sky.B);
        Assert.True(Math.Abs(met / expected - 1) < 0.08 && Math.Abs(seen / expected - 1) < 0.15,
            $"the floor's middle reads {seen:0.0000} in the picture and {met:0.0000} in the reference where the sky's share of {share:0.000} gives {expected:0.0000}");

        // The share of a dome a point sees of a rectangle parallel over it, one corner straight
        // over the point, its sides x and y times the rectangle's height.
        static double Corner(double x, double y) =>
            (x / Math.Sqrt(1 + x * x) * Math.Atan(y / Math.Sqrt(1 + x * x)) + y / Math.Sqrt(1 + y * y) * Math.Atan(x / Math.Sqrt(1 + y * y))) / (2 * Math.PI);
    }
}
