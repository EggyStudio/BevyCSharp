using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers reading parts of files on worker threads within a budget a frame.</summary>
[Collection("engine")]
public sealed class StreamingTests
{
    /// <summary>A read of part of a file under the asset directory brings back exactly that part.</summary>
    [Fact]
    public void AReadBringsBackThePartAskedFor()
    {
        var file = WriteFile(256);

        try
        {
            byte[]? bytes = null;
            var read = default(StreamRead);

            using var engine = new EngineHarness(frames: 200, fps: 120);
            engine.On(Stage.Update, world =>
            {
                if (read.Ticket == 0) read = Streaming.Read(Path.GetFileName(file), offset: 10, length: 20);
                else if (bytes is null && Streaming.TryTake(read, out var arrived)) bytes = arrived;

                if (bytes is not null) App.RequestExit();
            });
            engine.Run();

            Assert.NotNull(bytes);
            Assert.Equal(Enumerable.Range(10, 20).Select(value => (byte)value), bytes);
        }
        finally
        {
            File.Delete(file);
        }
    }

    /// <summary>
    /// Two finished reads that together pass the budget are handed over on two frames, and the one
    /// with the higher priority first.
    /// </summary>
    [Fact]
    public void FinishedReadsAreHandedOverWithinTheBudget()
    {
        var file = WriteFile(200);
        var before = Streaming.BytesPerFrame;

        try
        {
            Streaming.BytesPerFrame = 100;

            var low = default(StreamRead);
            var high = default(StreamRead);
            ulong? lowFrame = null;
            ulong? highFrame = null;
            var started = false;

            using var engine = new EngineHarness(frames: 400, fps: 120);
            engine.On(Stage.Update, world =>
            {
                var frame = world.Resource<Time>().FrameCount;

                if (!started)
                {
                    started = true;
                    low = Streaming.Read(Path.GetFileName(file), 0, 80, priority: 0);
                    high = Streaming.Read(Path.GetFileName(file), 100, 80, priority: 5);
                    return;
                }

                // Nothing is taken until both have finished, so both compete for the same frame.
                if (Streaming.Pending > 0) return;

                if (highFrame is null && Streaming.TryTake(high, out _)) highFrame = frame;
                if (lowFrame is null && Streaming.TryTake(low, out _)) lowFrame = frame;

                if (lowFrame is not null && highFrame is not null) App.RequestExit();
            });
            engine.Run();

            Assert.NotNull(lowFrame);
            Assert.NotNull(highFrame);
            Assert.True(lowFrame > highFrame, $"the low one came on frame {lowFrame} and the high one on {highFrame}");
        }
        finally
        {
            Streaming.BytesPerFrame = before;
            File.Delete(file);
        }
    }

    /// <summary>A file that is not there is reported when the read is taken.</summary>
    [Fact]
    public void AMissingFileIsReportedWhenTaken()
    {
        Exception? failure = null;
        var read = default(StreamRead);

        using var engine = new EngineHarness(frames: 200, fps: 120);
        engine.On(Stage.Update, world =>
        {
            if (read.Ticket == 0)
            {
                read = Streaming.Read("no/such/file.bin");
                return;
            }

            try
            {
                Streaming.TryTake(read, out _);
            }
            catch (IOException error)
            {
                failure = error;
                App.RequestExit();
            }
        });
        engine.Run();

        Assert.IsType<IOException>(failure);
    }

    /// <summary>A file of <paramref name="length"/> bytes counting up, in the asset directory.</summary>
    private static string WriteFile(int length)
    {
        var path = Path.Combine(EngineHarness.AssetDirectory, $"stream-{Guid.NewGuid():N}.bin");
        File.WriteAllBytes(path, Enumerable.Range(0, length).Select(value => (byte)value).ToArray());
        return path;
    }
}
