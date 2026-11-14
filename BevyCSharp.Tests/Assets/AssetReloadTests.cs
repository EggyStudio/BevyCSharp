using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// An asset's file written while the app watches its folder is read again, drawn as the new version
/// under a standard material and a shader material and said with <see cref="AssetReloaded"/>, an
/// app whose config did not ask watches nothing, and an asset edited by code is no reload.
/// </summary>
/// <remarks>
/// Bevy's watcher is in bridges of the render profile and those over it, so each case needs a
/// renderer. The files are written in a folder of the test's own, which the app reads its assets
/// from, as a person saving a texture writes it in a game's folder.
/// </remarks>
[Collection("engine")]
public sealed class AssetReloadTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-asset-reload-");
    private readonly TestFolder _logs = new("bcs-asset-reload-logs-");
    private readonly string? _asked = Environment.GetEnvironmentVariable(Config.WatchAssetsVariable);

    public AssetReloadTests() => Environment.SetEnvironmentVariable(Config.WatchAssetsVariable, null);

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(Config.WatchAssetsVariable, _asked);
        _folder.Dispose();
        _logs.Dispose();
    }

    /// <summary>
    /// A red swatch written again blue while the app watches is drawn blue on the cube its standard
    /// material colors and on the one a shader material samples it on, and the reload is posted and
    /// said in the log.
    /// </summary>
    /// <remarks>
    /// A reloaded image is a new texture on the GPU under the same handle, so a material that kept
    /// the old texture's view in its bindings would go on drawing red. Bevy's log reaches the
    /// console's ring where the run's log is started, which the suite's apps leave off, so the case
    /// starts it in a folder of its own.
    /// </remarks>
    [SkippableFact]
    public void ATextureWrittenWhileTheAppWatchesIsDrawnAgainUnderBothMaterialsAndSaid()
    {
        Needs.Renderer();
        Needs.Shaders();
        Directory.CreateDirectory(_folder.File("shaders"));
        File.Copy(Path.Combine(EngineHarness.AssetDirectory, "shaders", "sampled.slang"), _folder.File("shaders/sampled.slang"));
        File.WriteAllBytes(_folder.File("swatch.png"), Swatch(255, 0, 0));

        var reloads = new List<AssetReloaded>();
        var run = new PictureRun
        {
            AssetRoot = _folder.Path,
            Width = 96,
            Height = 48,
            Frames = 1800,
            Configure = config => config.WatchAssets = true,
            Scene = ecs =>
            {
                var swatch = AssetServer.Load(AssetKind.Image, "swatch.png");
                PictureRun.Camera(ecs);
                PictureRun.Cube(ecs, Render.CreateMaterial(new MaterialSettings { BaseColorTexture = swatch, Unlit = true }), 2f, new Vec3(-1.5f, 0f, 0f));
                PictureRun.Cube(ecs, Shaders.CreateMaterial(Shaders.CreateProgram("shaders/sampled.slang")).SetTexture("picture", swatch), 2f, new Vec3(1.5f, 0f, 0f));
            },
            EachFrame = world => reloads.AddRange(world.Resource<MessageBus>().Read<AssetReloaded>()),
        };

        CrashLog.Start(_logs.Path, GraphicsBackend.Vulkan);
        try
        {
            run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
                .Until("ready", _ => Render.PipelinesReady())
                .Wait(10)
                .Capture("red")
                .Do("writing it blue", _ => File.WriteAllBytes(_folder.File("swatch.png"), Swatch(0, 0, 255)))
                .Until("read again", _ => reloads.Count > 0)
                .Wait(10)
                .Capture("blue")
                .Go();
        }
        finally
        {
            CrashLog.Stop();
        }

        Assert.Contains(new AssetReloaded("swatch.png", "Image"), reloads);
        Assert.Contains(ConsoleLog.All(), line => line.Text == "Read swatch.png again after its file changed, as an Image.");
        var (red, blue) = (run.Picture("red"), run.Picture("blue"));
        foreach (var (half, name) in new[] { (0u, "standard"), (1u, "shader") })
        {
            Assert.True(Count(red, half, (r, g, b) => r > 200 && b < 60) > 50, $"the {name} material did not draw the swatch red at first");
            Assert.True(Count(blue, half, (r, g, b) => b > 200 && r < 60) > 50, $"the {name} material did not draw the swatch blue once it was written again");
        }
    }

    /// <summary>
    /// A glTF written again while the app watches it has its scene spawned again under the same root,
    /// said ready again as a reload, and its scene and its mesh said read again.
    /// </summary>
    /// <remarks>
    /// Bevy spawns a scene again in place once its file is read again, which reads to a system as a
    /// first spawn unless the message says otherwise. It reads again the assets of the file still in
    /// use, the scene and the mesh it draws, and not the file's own root asset, which nothing holds.
    /// The same bytes are written, as an editor saving a file unchanged writes them, which the
    /// watcher takes as a change all the same.
    /// </remarks>
    [SkippableFact]
    public void AGltfWrittenAgainWhileTheAppWatchesIsSpawnedAgainAndSaidReloaded()
    {
        Needs.Renderer();
        var model = _folder.File("triangle.gltf");
        File.Copy(Path.Combine(EngineHarness.AssetDirectory, "models", "triangle.gltf"), model);

        var root = Entity.None;
        var ready = new List<WorldInstanceReady>();
        var reloads = new List<AssetReloaded>();
        var written = false;
        using var harness = new EngineHarness(frames: 600, fps: 120, configure: config =>
        {
            config.WatchAssets = true;
            config.AssetRoot = _folder.Path;
        });
        harness.OnContext(Stage.Startup, ctx => root = ctx.Ecs.SpawnScene(AssetServer.LoadGltfScene("triangle.gltf")));
        harness.OnContext(Stage.Update, ctx =>
        {
            ready.AddRange(ctx.Read<WorldInstanceReady>().ToArray().Where(message => message.Entity == root));
            reloads.AddRange(ctx.Read<AssetReloaded>().ToArray());
            if (!written && ready.Count == 1)
            {
                File.WriteAllBytes(model, File.ReadAllBytes(model));
                written = true;
            }

            if (ready.Count == 2) ctx.Exit();
        });
        harness.Run();

        Assert.Equal([false, true], ready.Select(message => message.Reloaded));
        Assert.Contains(new AssetReloaded("triangle.gltf#Scene0", "Scene"), reloads);
        Assert.Contains(new AssetReloaded("triangle.gltf#Mesh0/Primitive0", "Mesh"), reloads);
    }

    /// <summary>
    /// An app whose config left watching alone, made in the test assembly, which is built in Debug,
    /// has the bridge watch nothing and start no watcher thread, where one that asked starts one.
    /// </summary>
    /// <remarks>
    /// Bevy watches in every app of a bridge carrying its watcher unless told no, so the bridge says
    /// no outright where the config did not ask. The watcher's threads are named by the notify crate,
    /// and Linux lists a process's threads by name, so there they are counted before each app starts
    /// and while it runs, a thread an app before left behind counted in both. A bridge without the
    /// watcher, as the headless one, has the app that did not ask alone.
    /// </remarks>
    [Fact]
    public void AnAppWhoseConfigDidNotAskWatchesNothingAndStartsNoWatcher()
    {
        // The app that asked shows the threads are seen where they stand, on a bridge carrying
        // the watcher.
        foreach (var asked in App.HasRenderer ? new[] { false, true } : [false])
        {
            var (watching, before, during) = (-1, Watchers(), -1);
            using var harness = new EngineHarness(frames: 30, configure: asked ? config => config.WatchAssets = true : null);
            harness.OnContext(Stage.Update, ctx =>
            {
                if (ctx.Time.FrameCount < 10) return;
                watching = NativeAssets.bcs_assets_watching();
                during = Watchers();
                ctx.Exit();
            });
            harness.Run();

            Assert.Equal(asked ? 1 : 0, watching);
            if (OperatingSystem.IsLinux())
                Assert.True(asked ? during > before : during == before, $"{(asked ? "an app that asked" : "an app that did not ask")} had {during} watcher threads where {before} stood before it");
        }
    }

    /// <summary>An image whose pixels are written by code while the app watches is no reload and says nothing.</summary>
    /// <remarks>
    /// Bevy says an asset edited in place is modified as it says one read again is, and only the
    /// one read again from its file also ends a load, and that tells the two apart.
    /// </remarks>
    [SkippableFact]
    public void AnImageWrittenByCodeIsNoReload()
    {
        Needs.Renderer();
        File.WriteAllBytes(_folder.File("swatch.png"), Swatch(255, 0, 0));

        var reloads = new List<AssetReloaded>();
        var swatch = AssetHandle.None;
        new PictureRun
        {
            AssetRoot = _folder.Path,
            Frames = 600,
            Configure = config => config.WatchAssets = true,
            Scene = ecs =>
            {
                swatch = AssetServer.Load(AssetKind.Image, "swatch.png");
                PictureRun.Camera(ecs);
                PictureRun.Cube(ecs, Render.CreateMaterial(new MaterialSettings { BaseColorTexture = swatch, Unlit = true }));
            },
            EachFrame = world => reloads.AddRange(world.Resource<MessageBus>().Read<AssetReloaded>()),
        }
            .Until("loaded", _ => AssetServer.StateOf(swatch) == AssetLoadState.Loaded)
            .Do("writing its pixels", _ => Render.WriteImagePixels(swatch, Pixels(0, 255, 0)))
            .Wait(30)
            .Go();

        Assert.Empty(reloads);
    }

    /// <summary>The watcher threads standing in this process, by the names the notify crate gives them.</summary>
    private static int Watchers()
    {
        if (!OperatingSystem.IsLinux()) return 0;
        return Directory.EnumerateDirectories("/proc/self/task")
            .Count(task => File.Exists(Path.Combine(task, "comm"))
                           && File.ReadAllText(Path.Combine(task, "comm")).StartsWith("notify-rs", StringComparison.Ordinal));
    }

    /// <summary>The pixels of either half of a picture, left or right, that pass a test of their color.</summary>
    private static int Count(CapturedImage picture, uint half, Func<byte, byte, byte, bool> test)
    {
        var count = 0;
        for (var y = 0u; y < picture.Height; y++)
        for (var x = half * picture.Width / 2; x < (half + 1) * picture.Width / 2; x++)
        {
            var i = (int)(y * picture.Width + x) * 4;
            if (test(picture.Pixels[i], picture.Pixels[i + 1], picture.Pixels[i + 2])) count++;
        }

        return count;
    }

    private static byte[] Swatch(byte r, byte g, byte b) => PngWriter.Still(4, 4, Pixels(r, g, b));

    private static byte[] Pixels(byte r, byte g, byte b)
    {
        var pixels = new byte[4 * 4 * 4];
        for (var i = 0; i < pixels.Length; i += 4) (pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]) = (r, g, b, 255);
        return pixels;
    }
}
