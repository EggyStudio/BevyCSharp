// Bevy's pan_camera_controller example, examples/camera/pan_camera_controller.rs at v0.20.0, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Cameras;

// Bevy's pan camera on a 2D camera with its default settings, moved with WASD, turned
// with Q and E, and zoomed with the wheel or the plus and minus keys, over Bevy's bird.
internal static class PanCameraController
{
    public static void Configure(Config config) => config.CameraControllers = CameraControllers.Pan;

    public static void Build(App app) => app.Startup(ctx =>
    {
        var camera = Render2d.SpawnCamera2d();
        Render.SetPanCamera(camera, new PanCameraSettings());

        var bird = ctx.Ecs.Spawn();
        ctx.Ecs.Add(bird, Transform.Identity);
        Render2d.SetSprite(ctx.Ecs, bird, AssetServer.Load(AssetKind.Image, "branding/bevy_bird_dark.png"));

        // The controller's own description of its controls, as Bevy's example shows it.
        Ui.SpawnText(Render.PanCameraControls(camera), new UiSettings { Absolute = true, Top = Length.Px(-16f), Left = Length.Px(12f) });
    }, "pan_camera_controller.Setup");
}
