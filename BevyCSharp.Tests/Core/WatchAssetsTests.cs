using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// What decides <see cref="Config.WatchAssets"/>, the program where it sets it, the environment
/// where the program left it alone, a program's Debug build failing both, and nothing otherwise,
/// with the app saying as it starts when watching was turned on for it.
/// </summary>
/// <remarks>
/// In the engine collection, since the environment is the process's and every app made reads it.
/// </remarks>
[Collection("engine")]
public sealed class WatchAssetsTests : IDisposable
{
    private readonly string? _asked = Environment.GetEnvironmentVariable(Config.WatchAssetsVariable);

    public WatchAssetsTests() => Environment.SetEnvironmentVariable(Config.WatchAssetsVariable, null);

    public void Dispose() => Environment.SetEnvironmentVariable(Config.WatchAssetsVariable, _asked);

    [Fact]
    public void WatchingIsOffWhereNothingAsks()
    {
        var config = new Config();

        Assert.False(config.WatchAssets);
        Assert.Equal("nothing, so off", config.WatchAssetsSource);
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("0", false)]
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
            && line.Text.Contains($"Watching the assets for changes, turned on by the environment's {Config.WatchAssetsVariable}", StringComparison.Ordinal));
    }
}
