using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the parameters a camera, a light and the window can be given.
/// </summary>
/// <remarks>
/// What a picture actually looks like needs a GPU and an eye, neither of which a test has. What
/// is checked here is the part that can go wrong silently: that the settings reach the engine and
/// are accepted, and that a build or a run without a window says so rather than pretending.
/// </remarks>
[Collection("engine")]
public sealed class RenderControlTests
{
    [Fact]
    public void ACameraAcceptsEveryProjection()
    {
        using var harness = new EngineHarness(frames: 2);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var perspective = Render.SpawnCamera3d(new CameraSettings
            {
                FieldOfView = 70f,
                Near = 0.05f,
                Far = 500f,
                Clear = ClearMode.Custom,
                ClearColor = (0.1f, 0.2f, 0.3f, 1f),
                Order = 1,
            });

            var orthographic = Render.SpawnCamera3d(new CameraSettings
            {
                Projection = CameraProjection.Orthographic,
                Height = 12f,
            });

            Assert.NotEqual(Entity.None, perspective);
            Assert.NotEqual(Entity.None, orthographic);
            Assert.NotEqual(perspective, orthographic);

            // Bevy's insert brings what a camera requires along with it, so each is drawable
            // rather than an entity carrying one lonely component.
            Assert.True(ctx.Ecs.HasById(perspective, NativeComponents.Transform));
            Assert.True(ctx.Ecs.HasById(orthographic, NativeComponents.Transform));
        });

        harness.Run();
    }

    [Fact]
    public void TheDefaultCameraStillTakesNoArguments()
    {
        using var harness = new EngineHarness(frames: 2);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ => Assert.NotEqual(Entity.None, Render.SpawnCamera3d()));
        harness.Run();
    }

    [Fact]
    public void EveryLightKindSpawns()
    {
        using var harness = new EngineHarness(frames: 2);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
        {
            var sun = Render.SpawnLight(new LightSettings
            {
                Kind = LightKind.Directional,
                Intensity = 12_000f,
                Color = (1f, 0.95f, 0.8f),
                Shadows = false,
            });

            var bulb = Render.SpawnLight(new LightSettings
            {
                Kind = LightKind.Point,
                Intensity = 800f,
                Range = 15f,
                Radius = 0.1f,
            });

            var torch = Render.SpawnLight(new LightSettings
            {
                Kind = LightKind.Spot,
                Intensity = 2_000f,
                InnerAngle = 0.2f,
                OuterAngle = 0.5f,
            });

            Assert.NotEqual(Entity.None, sun);
            Assert.NotEqual(Entity.None, bulb);
            Assert.NotEqual(Entity.None, torch);
            Assert.Equal(3, new HashSet<Entity> { sun, bulb, torch }.Count);
        });

        harness.Run();
    }

    [Fact]
    public void TheShortLightOverloadStillWorks()
    {
        using var harness = new EngineHarness(frames: 2);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
            Assert.NotEqual(Entity.None, Render.SpawnLight(LightKind.Directional, 10_000f)));

        harness.Run();
    }

    [Fact]
    public void WindowCallsReportThatAHeadlessRunHasNoWindow()
    {
        // The whole point of failing loudly: a behavior that locks the cursor should say why it
        // could not rather than leave a first-person camera mysteriously dead.
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Update, _ =>
        {
            var title = Assert.Throws<BevyNativeException>(() => Window.SetTitle("test"));
            var cursor = Assert.Throws<BevyNativeException>(() => Window.SetCursor(CursorGrab.Locked, false));
            var size = Assert.Throws<BevyNativeException>(() => Window.Size());

            // Unsupported on a headless build, absent on a render build running windowless.
            foreach (var status in new[] { title.Status, cursor.Status, size.Status })
                Assert.True(
                    status is NativeStatus.Unsupported or NativeStatus.NotPresent,
                    $"unexpected status {status}");
        });

        harness.Run();
    }

    [Fact]
    public void CursorAndModeRejectValuesTheyDoNotUnderstand()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Update, _ =>
        {
            // Straight at the bridge, because the managed enums cannot express these.
            Assert.Equal(NativeStatus.NullArgument, WorstCase(Native.bcs_window_set_mode(7)));
            Assert.Equal(NativeStatus.NullArgument, WorstCase(Native.bcs_window_set_cursor(9, 1)));
        });

        harness.Run();
        return;

        // A headless build refuses before it looks at the argument, which is equally correct.
        static int WorstCase(int status) =>
            status == NativeStatus.Unsupported ? NativeStatus.NullArgument : status;
    }
}
