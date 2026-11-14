using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// What decides <see cref="Config.WatchAssets"/>, the program where it sets it, the environment
/// where the program left it alone, a program's Debug build failing both, and nothing otherwise,
/// with the app saying as it starts when watching was turned on for it, keeping what it decided for
/// its whole run, and a Debug program naming no root reading its project's assets folder.
/// </summary>
/// <remarks>
/// In the engine collection, since the environment is the process's and every app made reads it.
/// </remarks>
[Collection("engine")]
public sealed class WatchAssetsTests : IDisposable
{
    private readonly string? _asked = Environment.GetEnvironmentVariable(Config.WatchAssetsVariable);
    private readonly string _root = Streaming.AssetRoot;
    private readonly TestFolder _project = new("bcs-watch-project-");

    public WatchAssetsTests() => Environment.SetEnvironmentVariable(Config.WatchAssetsVariable, null);

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(Config.WatchAssetsVariable, _asked);
        BuildDefaults.ProjectAssets = null;
        BuildDefaults.ShaderCache = null;
        Streaming.AssetRoot = _root;
        _project.Dispose();
    }

    [Fact]
    public void WatchingIsOffWhereNothingAsks()
    {
        var config = new Config();

        Assert.False(config.WatchAssets);
        Assert.Equal("nothing, so off", config.WatchAssetsSource);
    }

    /// <summary>
    /// The environment decides where the program left it alone, its value read with the spaces a
    /// shell leaves around it and in any letter case.
    /// </summary>
    [Theory]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData(" 0 ", false)]
    [InlineData("True", true)]
    public void TheEnvironmentDecidesWhereTheProgramLeftItAlone(string value, bool watches)
    {
        Environment.SetEnvironmentVariable(Config.WatchAssetsVariable, value);
        var config = new Config();

        Assert.Equal(watches, config.WatchAssets);
        Assert.Equal($"the environment's {Config.WatchAssetsVariable}", config.WatchAssetsSource);
    }

    [Fact]
    public void TheProgramDecidesOverTheEnvironment()
    {
        Environment.SetEnvironmentVariable(Config.WatchAssetsVariable, "1");
        var config = new Config { WatchAssets = false };

        Assert.False(config.WatchAssets);
        Assert.Equal("the program", config.WatchAssetsSource);
    }

    /// <summary>
    /// A Debug build's call naming an assembly the process did not start from, as a game's assembly
    /// a test loads would make, turns nothing on.
    /// </summary>
    [Fact]
    public void ADebugBuildOfAnAssemblyTheProcessDidNotStartFromTurnsNothingOn()
    {
        BuildDefaults.ProgramBuiltInDebug(typeof(App).Assembly);

        Assert.False(BuildDefaults.WatchAssets);
        Assert.False(new Config().WatchAssets);
    }

    /// <summary>An app the environment turned watching on for says so as it starts, naming what turned it on.</summary>
    /// <remarks>
    /// Bevy's log reaches the console's ring where the run's log is started, which the suite's apps
    /// leave off, so the test starts it in a folder of its own, as a game's run does beside it.
    /// </remarks>
    [Fact]
    public void AnAppWatchingWhereItsProgramDidNotAskSaysWhy()
    {
        Environment.SetEnvironmentVariable(Config.WatchAssetsVariable, "1");
        using var folder = new TestFolder("bcs-watch-logs-");
        CrashLog.Start(folder.Path, GraphicsBackend.Vulkan);
        try
        {
            using var harness = new EngineHarness(frames: 3);
            harness.Run();
        }
        finally
        {
            CrashLog.Stop();
        }

        Assert.Contains(ConsoleLog.All(), line => line.Level == LogLevel.Info
            && line.Text.Contains($"for changes, turned on by the environment's {Config.WatchAssetsVariable}", StringComparison.Ordinal));
    }

    /// <summary>
    /// An app decides watching as it is made, so the environment turning it on later leaves the
    /// config, the data assets and live.status as the app decided, and a program setting it on the
    /// app's config is refused.
    /// </summary>
    [Fact]
    public void AnAppKeepsWhatItDecidedOfWatchingForItsWholeRun()
    {
        var (watching, data, status) = (true, true, "");
        Exception? refused = null;

        using (var harness = new EngineHarness(frames: 3))
        {
            harness.On(Stage.Update, world =>
            {
                Environment.SetEnvironmentVariable(Config.WatchAssetsVariable, "1");
                var config = world.Resource<Config>();
                watching = config.WatchAssets;
                data = DataAssets.Watching;
                using (ConsoleHost.Lend(world)) status = ConsoleCommands.Run("live.status") ?? "";
                refused = Record.Exception(() => config.WatchAssets = true);
            });
            harness.Run();
        }

        Assert.False(watching, "the environment set after the app was made turned watching on");
        Assert.False(data, "the data assets watched where the app decided not to");
        Assert.StartsWith("not watching the assets, since nothing asked", status);
        Assert.IsType<InvalidOperationException>(refused);
    }

    /// <summary>
    /// A program built in Debug whose config names no root reads and watches its project's assets
    /// folder rather than the copy beside its build, and says which folder it watches and, where it
    /// draws, which folder its shaders are cached in.
    /// </summary>
    /// <remarks>
    /// The environment turns watching on here where a Debug build's initializer turns it on for a
    /// game, since the test's process started from the test host.
    /// </remarks>
    [Fact]
    public void ADebugProgramNamingNoRootReadsItsProjectsAssetsFolder()
    {
        var assets = Directory.CreateDirectory(Path.Combine(_project.Path, "assets")).FullName;
        BuildDefaults.ProjectAssets = assets;
        var cache = BuildDefaults.ShaderCache = Path.Combine(_project.Path, "obj", ".slang-cache");
        Environment.SetEnvironmentVariable(Config.WatchAssetsVariable, "1");

        var root = "";
        using var logs = new TestFolder("bcs-watch-logs-");
        CrashLog.Start(logs.Path, GraphicsBackend.Vulkan);
        try
        {
            using var harness = new EngineHarness(frames: 3, configure: config => config.AssetRoot = null);
            harness.On(Stage.Update, _ => root = Streaming.AssetRoot);
            harness.Run();
        }
        finally
        {
            CrashLog.Stop();
        }

        Assert.Equal(assets, root);
        var said = Assert.Single(ConsoleLog.All(), line => line.Text.Contains($"Watching {assets} for changes", StringComparison.Ordinal));
        Assert.Equal(App.HasRenderer, said.Text.Contains($", its compiled shaders cached in {cache}.", StringComparison.Ordinal));
    }

    /// <summary>
    /// live.status says Bevy's assets are not read again where the app watches and Bevy built no
    /// watcher for its asset folder, as for a folder that is not there, on a bridge carrying the
    /// watcher and on one without it alike.
    /// </summary>
    [Fact]
    public void LiveStatusSaysBevysAssetsAreNotReadWhereNoWatcherWasBuilt()
    {
        var status = "";
        using (var harness = new EngineHarness(frames: 3, configure: config =>
               {
                   config.AssetRoot = Path.Combine(_project.Path, "not here");
                   config.WatchAssets = true;
               }))
        {
            harness.On(Stage.Update, world =>
            {
                using (ConsoleHost.Lend(world)) status = ConsoleCommands.Run("live.status") ?? "";
            });
            harness.Run();
        }

        Assert.StartsWith("watching the assets, as the program asks, Bevy's assets are not read again, since ", status);
    }

    /// <summary>
    /// A program not watching reads the copy beside its build, as one built in Release does, and so
    /// does one whose project's folder is not on this machine.
    /// </summary>
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void AProgramNotWatchingOrMovedOffItsProjectReadsTheCopy(bool watching, bool projectHere)
    {
        var assets = Path.Combine(_project.Path, "assets");
        if (projectHere) Directory.CreateDirectory(assets);
        BuildDefaults.ProjectAssets = assets;
        Environment.SetEnvironmentVariable(Config.WatchAssetsVariable, watching ? "1" : "0");

        var root = "";
        using (var harness = new EngineHarness(frames: 3, configure: config => config.AssetRoot = null))
        {
            harness.On(Stage.Update, _ => root = Streaming.AssetRoot);
            harness.Run();
        }

        Assert.Equal(Path.Combine(AppContext.BaseDirectory, "assets"), root);
    }

    /// <summary>
    /// A program reading its project's assets folder caches its compiled shaders under the project's
    /// build folder, and the assets folder holds only what was put there.
    /// </summary>
    [SkippableFact]
    public void AProgramReadingItsProjectsAssetsCachesItsShadersUnderTheBuildFolder()
    {
        Needs.Shaders();

        var assets = Directory.CreateDirectory(Path.Combine(_project.Path, "assets")).FullName;
        var cache = Path.Combine(_project.Path, "obj", ".slang-cache");
        File.WriteAllText(Path.Combine(assets, "main.slang"), """
            import bcs;

            [shader("fragment")]
            float4 fragment(MeshVertexOutput mesh) : SV_Target
            {
                return float4(0.0, 1.0, 0.0, 1.0);
            }
            """);
        BuildDefaults.ProjectAssets = assets;
        BuildDefaults.ShaderCache = cache;

        var program = ShaderProgram.None;
        new PictureRun
        {
            Configure = config =>
            {
                config.AssetRoot = null;
                config.WatchAssets = true;
            },
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);
                program = Shaders.CreateProgram("main.slang");
                PictureRun.Cube(ecs, Shaders.CreateMaterial(program));
            },
        }.Until("compiled", _ => program.State == ShaderProgramState.Ready).Go();

        Assert.NotEmpty(Directory.GetFiles(cache, "*.wgsl"));
        Assert.False(Directory.Exists(Path.Combine(assets, ".slang-cache")), "the assets folder holds the cache");
    }
}
