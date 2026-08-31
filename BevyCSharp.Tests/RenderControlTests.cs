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
    public void TwoCamerasCanSplitTheWindow()
    {
        // Splitscreen: the same scene drawn twice, each into half the framebuffer. The second
        // camera must not clear, or it would wipe out the first one's half.
        using var harness = new EngineHarness(frames: 2);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
        {
            var left = Render.SpawnCamera3d(new CameraSettings
            {
                Viewport = (0, 0, 640, 720),
            });

            var right = Render.SpawnCamera3d(new CameraSettings
            {
                Viewport = (640, 0, 640, 720),
                Order = 1,
                Clear = ClearMode.Keep,
            });

            Assert.NotEqual(Entity.None, left);
            Assert.NotEqual(Entity.None, right);
            Assert.NotEqual(left, right);
        });

        harness.Run();
    }

    [Fact]
    public void RenderLayersSeparateWhatEachCameraSees()
    {
        const uint Minimap = 1u << 1;

        using var harness = new EngineHarness(frames: 3);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, ctx =>
        {
            // One camera for the world, one for the minimap, each on its own layer.
            var world = Render.SpawnCamera3d(new CameraSettings { Layers = 1u });
            var minimap = Render.SpawnCamera3d(new CameraSettings
            {
                Layers = Minimap,
                Order = 1,
                Clear = ClearMode.Keep,
                Viewport = (0, 0, 200, 200),
            });

            Assert.NotEqual(world, minimap);

            // A marker only the minimap draws, and a player both do.
            var marker = ctx.Ecs.Spawn();
            Render.SetMesh(ctx.Ecs, marker, Render.CreateMesh(MeshShape.Sphere, 0.2f));
            Render.SetLayers(ctx.Ecs, marker, Minimap);

            var player = ctx.Ecs.Spawn();
            Render.SetMesh(ctx.Ecs, player, Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f));
            Render.SetLayers(ctx.Ecs, player, 1u | Minimap);

            // Zero puts an entity back on the default layer rather than on none at all.
            Render.SetLayers(ctx.Ecs, player, 0u);
        });

        harness.Run();
    }

    [Fact]
    public void SettingLayersOnSomethingThatIsGoneIsRefused()
    {
        using var harness = new EngineHarness(frames: 2);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var ex = Assert.Throws<BevyNativeException>(
                () => Render.SetLayers(ctx.Ecs, Entity.None, 1u));

            Assert.Equal(NativeStatus.NoEntity, ex.Status);
        });

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
    public void ShadowsCanBeTuned()
    {
        using var harness = new EngineHarness(frames: 2);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
        {
            // Bias is per light, because one light's acne is another's floating shadow.
            var sun = Render.SpawnLight(new LightSettings
            {
                Kind = LightKind.Directional,
                ShadowDepthBias = 0.05f,
                ShadowNormalBias = 1.2f,
            });

            Assert.NotEqual(Entity.None, sun);

            // Size is global, and each kind can be set without knowing the other.
            Render.SetShadowMapSize(directional: 4096);
            Render.SetShadowMapSize(point: 2048);
            Render.SetShadowMapSize(directional: 2048, point: 1024);
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
    public void MonitorsAreCountedWithoutThrowing()
    {
        // A windowless run reports none rather than failing, so a settings screen can ask without
        // guarding first. With a window this is however many the platform sees.
        using var harness = new EngineHarness(frames: 2);
        var count = -1;

        harness.OnContext(Stage.Update, _ => count = Window.MonitorCount());
        harness.Run();

        Assert.True(count >= 0, "monitor count came back negative");

        if (count == 0) return;

        // Anything reported has to be describable, and past the end has to be refused.
        using var second = new EngineHarness(frames: 2);
        second.OnContext(Stage.Update, _ =>
        {
            var first = Window.Monitor(0);
            Assert.True(first.Width > 0 && first.Height > 0);
            Assert.Throws<BevyNativeException>(() => Window.Monitor(Window.MonitorCount()));

            // A name is optional, so the only thing guaranteed is that asking is safe and that
            // past the end is refused the same way the rest of the monitor surface refuses it.
            Assert.NotNull(Window.MonitorName(0));
            Assert.Throws<BevyNativeException>(() => Window.MonitorName(Window.MonitorCount()));
        });

        second.Run();
    }

    [Fact]
    public void MonitorNamesAreRefusedWithoutAWindow()
    {
        // Windowless has no monitors, so every index is past the end.
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Update, _ =>
        {
            if (Window.MonitorCount() > 0) return;

            Assert.Throws<BevyNativeException>(() => Window.MonitorName(0));
        });

        harness.Run();
    }

    [Fact]
    public void WindowStyleAndPositionReportTheirAbsence()
    {
        // Same contract as the rest of the window surface: a windowless run says so rather than
        // pretending, so a behavior that arranges the window fails visibly in a test.
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Update, _ =>
        {
            var position = Assert.Throws<BevyNativeException>(() => Window.SetPosition(100, 100));
            var style = Assert.Throws<BevyNativeException>(
                () => Window.SetStyle(decorations: false, alwaysOnTop: true));

            foreach (var status in new[] { position.Status, style.Status })
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
            Assert.Equal(NativeStatus.NullArgument, WorstCase(Native.bcs_window_set_mode(9)));
            Assert.Equal(NativeStatus.NullArgument, WorstCase(Native.bcs_window_set_cursor(9, 1)));
        });

        harness.Run();
        return;

        // A headless build refuses before it looks at the argument, which is equally correct.
        static int WorstCase(int status) =>
            status == NativeStatus.Unsupported ? NativeStatus.NullArgument : status;
    }
}
