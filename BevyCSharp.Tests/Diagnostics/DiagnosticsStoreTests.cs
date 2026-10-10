using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers Bevy's diagnostics store as a game reaches it: a diagnostic of its own registered,
/// measured, read back and turned off, Bevy's own plugins measuring into it, and its log filtered.
/// </summary>
[Collection("engine")]
public sealed class DiagnosticsStoreTests
{
    [Fact]
    public void AGameDiagnosticIsMeasuredReadBackAndTurnedOff()
    {
        using var harness = new EngineHarness(frames: 8);
        var measured = 0;
        DiagnosticReading read = default, afterOff = default;
        IReadOnlyList<DiagnosticReading> all = [];
        var keptWhileOff = true;
        var keptUnregistered = true;

        harness.OnContext(Stage.Startup, _ =>
        {
            Diagnostics.Register("test/count", " things", history: 4);
            Assert.Throws<ArgumentException>(() => Diagnostics.Register("test//count"));
        });

        harness.OnContext(Stage.Update, _ =>
        {
            measured++;
            if (measured <= 6) Assert.True(Diagnostics.Measure("test/count", measured));
            if (measured != 6) return;

            Assert.True(Diagnostics.TryRead("test/count", out read));
            all = Diagnostics.All();

            Assert.True(Diagnostics.SetEnabled("test/count", false));
            keptWhileOff = Diagnostics.Measure("test/count", 100);
            Assert.True(Diagnostics.TryRead("test/count", out afterOff));
            keptUnregistered = Diagnostics.Measure("test/none", 1);
        });

        harness.Run();

        // The last six measured into a history of four, so the average is of three to six.
        Assert.Equal(6d, read.Value);
        Assert.Equal(4.5d, read.Average);
        Assert.Equal(4, read.History);
        Assert.True(read.Enabled);

        var listed = Assert.Single(all, reading => reading.Path == "test/count");
        Assert.Equal(" things", listed.Suffix);
        Assert.Equal(6d, listed.Value);

        // Off, a measurement is refused and the history kept, as one nobody registered is refused.
        Assert.False(keptWhileOff);
        Assert.Equal((6d, false), (afterOff.Value, afterOff.Enabled));
        Assert.False(keptUnregistered);
    }

    [Fact]
    public void BevysPluginsMeasureFramesAndEntities()
    {
        using var harness = new EngineHarness(frames: 8, configure: config => config.DiagnosticPlugins = DiagnosticPlugins.FrameTime | DiagnosticPlugins.EntityCount);

        DiagnosticReading frames = default, entities = default;
        var frame = 0;
        Exception? noLog = null;

        harness.OnContext(Stage.Startup, ctx => ctx.Ecs.Spawn());
        harness.OnContext(Stage.Update, _ =>
        {
            if (++frame != 6) return;

            Assert.True(Diagnostics.TryRead(Diagnostics.FrameCount, out frames));
            Assert.True(Diagnostics.TryRead(Diagnostics.EntityCount, out entities));
            noLog = Record.Exception(() => Diagnostics.SetLogFilter([Diagnostics.Fps]));
        });

        harness.Run();

        Assert.True(frames.Value >= 4, $"Bevy counted {frames.Value} frames by the sixth");
        Assert.True(entities.Value >= 1, $"Bevy counted {entities.Value} entities with one spawned");

        // An app that logs no diagnostics has no filter to set, which is said rather than ignored.
        Assert.IsType<InvalidOperationException>(noLog);
    }

    [Fact]
    public void TheLogTakesAFilterAndGoesBackToAll()
    {
        using var harness = new EngineHarness(frames: 4, configure: config => config.DiagnosticPlugins = DiagnosticPlugins.Log | DiagnosticPlugins.FrameTime);

        harness.OnContext(Stage.Update, _ =>
        {
            Diagnostics.SetLogFilter([Diagnostics.Fps, Diagnostics.FrameTime]);
            Diagnostics.SetLogFilter([]);
            Diagnostics.SetLogFilter(null);
        });

        harness.Run();
    }
}
