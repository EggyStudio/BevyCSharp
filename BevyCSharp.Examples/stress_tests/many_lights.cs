// Bevy's many_lights example, examples/stress_tests/many_lights.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.StressTests;

// A hundred thousand small point lights spread evenly over the inside of a sphere, with the camera
// turning in the middle, to measure how lights are sorted into the clusters of the view. With
// orthographic as its argument the camera is orthographic.
internal static class ManyLights
{
    private const float LightRadius = 0.3f, LightIntensity = 1000f, Radius = 50f;
    private const int NLights = 100_000;

    // The point that keeps the spread even over the sphere, as Bevy's example sets it.
    private const double Epsilon = 0.36;

    private static Entity _camera;
    private static float _printing;

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
        _printing = 0f;
        app.Startup(Setup, "many_lights.Setup");
        app.Update(MoveCamera, "many_lights.MoveCamera");
        app.Update(PrintLightCount, "many_lights.PrintLightCount");
    }

    private static void Setup(BehaviorContext ctx)
    {
        StressTest.Warn();
        var ecs = ctx.Ecs;

        // The sphere they light, turned inside out so its inside faces the camera. Bevy subdivides
        // it nine times where the bridge's sphere is subdivided Bevy's default five.
        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Sphere, Radius), Render.CreateMaterial((1f, 1f, 1f, 1f)), new Transform(Vec3.Zero, Quat.Identity, new Vec3(-1f)));

        // A spiral over the sphere, which gives about as many lights in view whichever way the
        // camera looks, in doubles so the spread has no seams.
        var random = new Random();
        var goldenRatio = 0.5 * (1.0 + Math.Sqrt(5.0));
        for (var i = 0; i < NLights; i++)
        {
            var theta = Math.PI * 2.0 * (i / goldenRatio);
            var phi = Math.Acos(1.0 - 2.0 * (i + Epsilon) / (NLights - 1.0 + 2.0 * Epsilon));
            var at = new Vec3((float)(Math.Cos(theta) * Math.Sin(phi)), (float)(Math.Sin(theta) * Math.Sin(phi)), (float)Math.Cos(phi)) * Radius;

            var (r, g, b, _) = Color.FromHsl(random.NextSingle() * 360f, 1f, 0.5f);
            var light = Render.SpawnLight(new LightSettings
            {
                Kind = LightKind.Point,
                Range = LightRadius,
                Intensity = LightIntensity,
                Color = (r, g, b),
                Shadows = false,
            });
            ecs.Add(light, Transform.At(at.X, at.Y, at.Z));
        }

        // Orthographic as wide as twenty units, which Bevy fixes the width at and the bridge's
        // orthographic camera is given as the height that matches it.
        var orthographic = Environment.GetCommandLineArgs().Contains("orthographic");
        var (width, height) = Window.Size();
        _camera = ecs.SpawnCamera3d(Transform.Identity, orthographic
            ? new CameraSettings { Projection = CameraProjection.Orthographic, Height = 20f * height / width }
            : new CameraSettings());

        // One cube in deep pink, a mark to see the turning by.
        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), Render.CreateMaterial(Color.FromSrgb8(255, 20, 147)), new Transform(new Vec3(0f, Radius, 0f), Quat.Identity, new Vec3(5f)));
    }

    // Turned about its own Z and X, a little each frame.
    private static void MoveCamera(BehaviorContext ctx)
    {
        var delta = ctx.Time.Delta * 0.15f;
        var camera = ctx.Ecs.GetOrDefault<Transform>(_camera);
        camera.Rotation = Quat.FromRotationX(delta) * (Quat.FromRotationZ(delta) * camera.Rotation);
        ctx.Ecs.Set(_camera, camera);
    }

    // The lights once a second. Bevy also counts, in its render world, the lights a view saw and the
    // lights drawn, which no wrapper reaches.
    private static void PrintLightCount(BehaviorContext ctx)
    {
        _printing += ctx.Time.Delta;
        if (_printing < 1f) return;

        _printing -= 1f;
        Console.WriteLine($"Lights: {NLights}");
    }
}
