// Bevy's many_cameras_lights example, examples/stress_tests/many_cameras_lights.rs at v0.19.1, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.StressTests;

// Sixteen cameras in a grid across the window, each turning around a cube on a disc lit by five
// point lights casting shadows, so every light's shadows are drawn for every camera.
internal static class ManyCamerasLights
{
    private const int CameraRows = 4, CameraCols = 4, NumLights = 5;

    private static readonly List<Entity> Cameras = [];

    // Bevy's window for its stress tests, 1920 by 1080 at a scale factor of one with no vertical
    // sync, drawn as fast as it can, and, as Bevy's leaves them out, no frame times logged.
    public static void Configure(Config config)
    {
        (config.Width, config.Height, config.ScaleFactor) = (1920, 1080, 1f);
        config.Vsync = false;
        config.HeadlessFps = 0;
    }

    public static void Build(App app)
    {
        Cameras.Clear();
        app.Startup(Setup, "many_cameras_lights.Setup");
        app.Update(RotateCameras, "many_cameras_lights.RotateCameras");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var white = Render.CreateMaterial((1f, 1f, 1f, 1f));

        // A circular base, laid flat.
        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Circle, 4f), white, new Transform(Vec3.Zero, Quat.FromRotationX(-MathF.PI / 2f), Vec3.One));

        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), white, Transform.At(0f, 0.5f, 0f));

        // The lights around it, their hues spread around the circle. Bevy's hue at full saturation
        // and value is the same color as at full saturation and half lightness.
        for (var i = 0; i < NumLights; i++)
        {
            var angle = i / (float)NumLights * MathF.PI * 2f;
            var (r, g, b, _) = Color.FromHsl(angle * 180f / MathF.PI, 1f, 0.5f);
            var light = Render.SpawnLight(new LightSettings
            {
                Kind = LightKind.Point,
                Color = (r, g, b),
                Intensity = 2_000_000f / NumLights,
                Shadows = true,
            });
            ecs.Add(light, Transform.At(MathF.Sin(angle) * 4f, 2f, MathF.Cos(angle) * 4f));
        }

        // The cameras, each drawing into its own part of the window, in pixels, which at the scale
        // factor of one the test holds the window at are the window's own.
        var (width, height) = Window.Size();
        var (cellWidth, cellHeight) = (width / CameraCols, height / CameraRows);
        var index = 0;
        for (var y = 0; y < CameraCols; y++)
        {
            for (var x = 0; x < CameraRows; x++)
            {
                var angle = index / (float)(CameraRows * CameraCols) * MathF.PI * 2f;
                Cameras.Add(ecs.SpawnCamera3d(
                    Transform.LookingAt(new Vec3(MathF.Sin(angle) * 4f, 2.5f, MathF.Cos(angle) * 4f), Vec3.Zero, Vec3.UnitY),
                    new CameraSettings { Order = index, Viewport = ((uint)x * cellWidth, (uint)y * cellHeight, cellWidth, cellHeight) }));
                index++;
            }
        }
    }

    // Each camera carried around the middle, turning as it goes so it keeps looking at it.
    private static void RotateCameras(BehaviorContext ctx)
    {
        var turn = Quat.FromRotationY(ctx.Time.Delta);
        foreach (var camera in Cameras)
        {
            var at = ctx.Ecs.GetOrDefault<Transform>(camera);
            at.Translation = turn * at.Translation;
            at.Rotation = turn * at.Rotation;
            ctx.Ecs.Set(camera, at);
        }
    }
}
