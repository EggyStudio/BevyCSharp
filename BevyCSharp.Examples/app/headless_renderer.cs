// Bevy's headless_renderer example, examples/app/headless_renderer.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Application;

// Renders a scene with no window into an image of 1920 by 1080, copies it back once forty frames
// have let it settle, saves it as a PNG beside the program, in test_images, and stops.
//
// Bevy writes the copy out of the render world itself, and here the camera draws offscreen and the
// picture is captured, which is the same copy made for a C# program.
internal static class HeadlessRenderer
{
    private const uint PreRollFrames = 40;

    private static Capture? _capture;
    private static uint _frame, _fileNumber;

    public static void Configure(Config config)
    {
        (config.Offscreen, config.Width, config.Height) = (true, 1920, 1080);
    }

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_capture, _frame, _fileNumber) = (null, 0, 0);
            Render.SetClearColor((0f, 0f, 0f, 1f));
            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Circle, 4f), Render.CreateMaterial((1f, 1f, 1f, 1f)), new Transform(Vec3.Zero, Quat.FromRotationX(-MathF.PI / 2f), Vec3.One));
            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), Render.CreateMaterial(Color.FromSrgb8(124, 144, 255)), Transform.At(0f, 0.5f, 0f));
            ecs.SpawnPointLight(new Vec3(4f, 8f, 4f), shadows: true);

            // Tonemapping off, as Bevy's camera has it, so the saved picture is the light as drawn.
            var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2.5f, 4.5f, 9f), Vec3.Zero, Vec3.UnitY));
            ecs.Wrap<TonemappingRef>(camera).Value = TonemappingRef.ValueVariant.None;
        }, "headless_renderer.Setup");

        app.Update(ctx =>
        {
            if (++_frame < PreRollFrames) return;
            _capture ??= Render.BeginCapture();
            if (!Render.TryReadCapture(_capture.Value, out var picture) || picture is null) return;

            var directory = Path.Combine(AppContext.BaseDirectory, "test_images");
            Console.WriteLine($"Saving image to: {directory}");
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, $"{_fileNumber++:000}.png"), picture.ToPng());
            ctx.Exit();
        }, "headless_renderer.Update");
    }
}
