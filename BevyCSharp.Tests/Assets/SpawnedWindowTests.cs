using Bevy;
using Bevy.Interop;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a window a game spawns beside its first, a camera pointed at it, and what an offscreen
/// run draws in its place.
/// </summary>
/// <remarks>
/// A second window is Bevy's <c>Window</c> added through reflection, which an offscreen run opens
/// no more than its first, so these pin down the part the bridge adds. A camera can be aimed at
/// the window's entity, an offscreen run draws what the window would show into an image of its
/// size, and the picture is read back by the window, at whatever scale it has.
/// </remarks>
[Collection("engine")]
public sealed class SpawnedWindowTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-windows-");

    public void Dispose() => _folder.Dispose();

    /// <summary>
    /// What a camera aimed at a spawned window draws is read back at the window's size, which no
    /// other picture in the run has, so it can only have come from the window's image.
    /// </summary>
    [SkippableFact]
    public void ACameraAimedAtASpawnedWindowDrawsAtTheWindowsSize()
    {
        Needs.Renderer();

        var path = Captured(scale: null);
        Assert.Equal((96u, 64u), PngSize(path));
        Assert.False(AllBlack(path), "the window's picture came back black");
    }

    /// <summary>
    /// A window at a scale of two is captured as its camera drew it. Bevy tells pictures drawn into
    /// an image apart by the scale as well, and a capture asking at a scale of one was given a
    /// picture of its own that nothing drew, black, and wrote it over the window's.
    /// </summary>
    [SkippableFact]
    public void AWindowAtAnotherScaleIsCapturedAsItWasDrawn()
    {
        Needs.Renderer();

        var path = Captured(scale: 2f);
        Assert.Equal((96u, 64u), PngSize(path));
        Assert.False(AllBlack(path), "the window's picture came back black");
    }

    // An offscreen run with a window of 96 by 64 a camera clears to green, captured by the window
    // once every pipeline the run asked for has compiled. The window's picture is written into its
    // image by a pass whose pipeline Bevy compiles off the main thread, and until it has, the image
    // holds the zeros it was made with. A capture at the tenth frame, which a quiet machine reaches
    // with some thirty pipelines still compiling, read the window back black in one run of forty
    // with every core busy, and that run had drawn it by the twenty-fifth.
    private string Captured(float? scale)
    {
        var path = _folder.File($"window-{scale ?? 1f}.png");
        var window = Entity.None;
        var captured = -1L;

        using var app = new App(Config.OffscreenFor(320, 180, frames: 600));
        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            world =>
            {
                var ecs = world.Resource<EcsWorld>();
                window = ecs.Spawn();
                var settings = ecs.Insert<WindowRef>(window);
                (settings.ResolutionPhysicalWidth, settings.ResolutionPhysicalHeight) = (96u, 64u);
                settings.ResolutionScaleFactorOverride = scale;

                var camera = Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0.1f, 0.6f, 0.2f, 1f) });
                Render.SetCameraTarget(camera, window);
            },
            "Test.Window"));

        app.AddSystem(Stage.Update, new SystemDescriptor(
            world =>
            {
                var frame = (long)world.Resource<Time>().FrameCount;

                // From the tenth frame, since none is waiting before the first is asked for.
                if (captured < 0 && frame >= 10 && Render.PipelinesReady())
                {
                    Render.Screenshot(path, window);
                    captured = frame;
                }

                // The picture comes back off the GPU over the frames after it is asked for.
                if (captured >= 0 && frame == captured + 10) new BehaviorContext(world).Exit();
            },
            "Test.Capture"));

        Assert.Equal(0, app.Run());
        Assert.True(File.Exists(path), $"no capture appeared at {path}");
        return path;
    }

    /// <summary>An entity that carries no window is refused as a camera's target and as a picture to take.</summary>
    [SkippableFact]
    public void AnEntityThatIsNotAWindowIsRefused()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);

        harness.On(Stage.Update, world =>
        {
            var camera = Render.SpawnCamera3d();
            var plain = world.Resource<EcsWorld>().Spawn();

            Assert.Equal(NativeStatus.NoComponent, Assert.Throws<BevyNativeException>(() => Render.SetCameraTarget(camera, plain)).Status);
            Assert.Equal(NativeStatus.NoComponent, Assert.Throws<BevyNativeException>(() => Render.Screenshot(_folder.File("plain.png"), plain)).Status);
        });

        harness.Run();
    }

    /// <summary>
    /// A spawned window made to fill a monitor holds the monitor's entity in its mode and its
    /// position, and an entity that is no monitor, or no window, is refused.
    /// </summary>
    /// <remarks>
    /// A run with no display has no monitors, so the monitor here is Bevy's <c>Monitor</c> put on an
    /// entity of the test's own, which the bridge cannot tell from one the platform reported. The
    /// window is never opened, so nothing is filled but the settings.
    /// </remarks>
    [SkippableFact]
    public void AWindowFillsTheMonitorItsEntityNames()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        string? mode = null, position = null;
        int notMonitor = NativeStatus.Ok, notWindow = NativeStatus.Ok;

        harness.On(Stage.Update, world =>
        {
            var ecs = world.Resource<EcsWorld>();
            if (mode is not null) return;

            var monitor = ecs.Spawn();
            ecs.Insert<MonitorRef>(monitor).Name = "Test";
            var window = ecs.Spawn();
            ecs.Insert<WindowRef>(window);
            var plain = ecs.Spawn();

            notMonitor = Assert.Throws<BevyNativeException>(() => Window.SetFullscreenOn(window, plain)).Status;
            notWindow = Assert.Throws<BevyNativeException>(() => Window.SetFullscreenOn(plain, monitor)).Status;

            Window.SetFullscreenOn(window, monitor);
            mode = ecs.GetReflected(window, WindowRef.TypePath, "mode");
            position = ecs.GetReflected(window, WindowRef.TypePath, "position");
        });

        harness.Run();

        Assert.Equal(NativeStatus.NotPresent, notMonitor);
        Assert.Equal(NativeStatus.NoComponent, notWindow);
        Assert.Contains("Fullscreen", mode);
        Assert.Contains("Centered", position);
    }

    /// <summary>
    /// A pointer cut to a frame of an atlas takes that frame's hotspot, and a frame and a flip
    /// written through the window's wrapper keep it, the variant written over rather than made again.
    /// A pointer that is no image yet is refused an atlas.
    /// </summary>
    /// <remarks>
    /// The window is one the test marks as the first, since an offscreen run has none, and is never
    /// opened, so only the pointer's settings change. Bevy holds the hotspot as a pair a wrapper
    /// cannot type, so a variant made again would have put it back at the corner.
    /// </remarks>
    [SkippableFact]
    public void APointerFromAnAtlasKeepsItsHotspotAsItsFrameIsWritten()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        int notImage = NativeStatus.Ok;
        ulong? frame = null;
        var flipped = false;
        string? hotspot = null;

        harness.On(Stage.Update, world =>
        {
            var ecs = world.Resource<EcsWorld>();
            if (frame is not null) return;

            var window = ecs.Spawn();
            ecs.Insert<WindowRef>(window);
            ecs.Insert<PrimaryWindowRef>(window);
            var layout = Render2d.CreateAtlas(64, 64, 20, 10);

            notImage = Assert.Throws<BevyNativeException>(() => Window.SetCursorAtlas(layout, 3, 32, 16)).Status;
            Window.SetCursorImage(AssetServer.Load(AssetKind.Image, "textures/checker.png"));
            Window.SetCursorAtlas(layout, 3, 32, 16);

            var icon = ecs.Wrap<CursorIconRef>(window);
            var image = (CustomCursor.Image)((CursorIcon.Custom)icon.Value).Value;
            icon.Value = new CursorIcon.Custom(image with { TextureAtlas = new OptionTextureAtlas.Some(4), FlipX = true });

            var after = (CustomCursor.Image)((CursorIcon.Custom)icon.Value).Value;
            (frame, flipped) = ((after.TextureAtlas as OptionTextureAtlas.Some)?.Index, after.FlipX);
            hotspot = ecs.GetReflected(window, CursorIconRef.TypePath, ".0.0.hotspot");
        });

        harness.Run();

        Assert.Equal(NativeStatus.NotPresent, notImage);
        Assert.Equal(4ul, frame);
        Assert.True(flipped);
        Assert.Equal("[32,16]", hotspot);
    }

    // Whether every pixel of an eight-bit PNG is zero, read from its image data inflated, each row
    // after the byte that says how it was filtered. A filter writes each byte as its difference from
    // a guess made from the bytes before it, and from zeros alone every filter guesses zero, so the
    // differences are all zero exactly when the picture is all black.
    private static bool AllBlack(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var (width, _) = PngSize(path);
        var channels = bytes[25] switch { 2 => 3, 6 => 4, 0 => 1, 4 => 2, _ => throw new InvalidDataException($"color type {bytes[25]}") };

        using var data = new MemoryStream();
        for (var at = 8; at + 8 <= bytes.Length;)
        {
            var length = (int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(at, 4));
            if (System.Text.Encoding.ASCII.GetString(bytes, at + 4, 4) == "IDAT") data.Write(bytes, at + 8, length);
            at += 12 + length;
        }

        data.Position = 0;
        using var inflated = new MemoryStream();
        using (var zlib = new System.IO.Compression.ZLibStream(data, System.IO.Compression.CompressionMode.Decompress)) zlib.CopyTo(inflated);

        var row = 1 + (int)width * channels;
        var pixels = inflated.ToArray();
        for (var i = 0; i < pixels.Length; i++)
            if (i % row != 0 && pixels[i] != 0) return false;

        return true;
    }

    private static (uint Width, uint Height) PngSize(string path)
    {
        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length > 24, "the file is too short to be a PNG");

        return (
            System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4)),
            System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4)));
    }
}
