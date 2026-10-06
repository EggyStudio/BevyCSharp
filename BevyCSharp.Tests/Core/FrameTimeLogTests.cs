using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// An app that asks for its frame times logged is given Bevy's own diagnostics for them, a windowless
/// one among its minimal plugins too, and a scale factor where it has no window to scale.
/// </summary>
[Collection("engine")]
public sealed class FrameTimeLogTests
{
    [Fact]
    public void AWindowlessAppThatLogsItsFrameTimesRunsWithBevysDiagnostics()
    {
        // A windowless app has no diagnostics plugin among its minimal ones, and a frame time
        // plugin added without it would leave nothing to log, which the bridge adds first.
        using var app = new App(new Config { Headless = true, HeadlessFrames = 5, LogFrameTimes = true, ScaleFactor = 1f });
        app.AddPlugin(new EnginePlugin());
        var frames = 0;
        app.Update(_ => frames++, "Tests.Count");

        Assert.Equal(0, app.Run());
        Assert.Equal(5, frames);
    }
}
