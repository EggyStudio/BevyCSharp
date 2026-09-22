using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers what a load that fails says about itself.
/// </summary>
/// <remarks>
/// A handle reports that a load failed and nothing else, so a misspelled path and a file that is
/// there and unreadable look the same. What the message carries is the pair that tells them apart,
/// and the test that matters is that the pair arrives at all: a failure nobody is told about is
/// the state this exists to end.
/// </remarks>
[Collection("engine")]
public sealed class AssetFailureTests
{
    [Fact]
    public void AMissingFileIsReportedWithItsPathAndReason()
    {
        var failures = new List<AssetLoadFailed>();

        using var harness = new EngineHarness(frames: 30, fps: 60);

        harness.OnContext(Stage.Startup, ctx =>
            AssetServer.Load(AssetKind.Image, "textures/not-here.png"));

        harness.OnContext(Stage.Update, ctx =>
        {
            foreach (var failure in ctx.Read<AssetLoadFailed>()) failures.Add(failure);
        });

        harness.Run();

        var failure = Assert.Single(failures);

        Assert.Contains("not-here.png", failure.Path);
        Assert.False(string.IsNullOrWhiteSpace(failure.Reason));
    }

    /// <summary>A load that works says nothing, which is what makes the message worth reading.</summary>
    [Fact]
    public void AFileThatLoadsIsNotReported()
    {
        var failures = 0;

        using var harness = new EngineHarness(frames: 30, fps: 60);

        harness.OnContext(Stage.Startup, ctx =>
            AssetServer.Load(AssetKind.Image, "textures/checker.png"));

        harness.OnContext(Stage.Update, ctx => failures += ctx.Read<AssetLoadFailed>().Length);

        harness.Run();

        Assert.Equal(0, failures);
    }
}
