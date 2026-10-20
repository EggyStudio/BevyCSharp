using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers recording a run as an animated GIF or PNG, the two writers read back by the engine's
/// reader, the palette a GIF's colors are cut to, the <c>record</c> command's words, and a
/// recording of a run that draws, its frames counted and their delays read from the file.
/// </summary>
/// <remarks>
/// The writers are read back by <c>AnimationReader</c>, which lays each frame over the ones before
/// as a browser does, so a frame written as the rectangle that changed is checked as the whole
/// picture it leaves. A PNG is lossless and comes back as written. A GIF holds 256 colors, so a
/// picture of fewer comes back exactly, each color the mean of the pixels that chose it, and a
/// gradient within the distance between the colors its part of the palette was cut to.
/// </remarks>
[Collection("engine")]
public sealed class RecordingTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-record-");

    public void Dispose() => _folder.Dispose();

    private static RgbaFrame Flat(int width, int height, byte r, byte g, byte b)
    {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = r;
            pixels[i + 1] = g;
            pixels[i + 2] = b;
            pixels[i + 3] = 255;
        }

        return new RgbaFrame(width, height, pixels);
    }

    private static RgbaFrame With(RgbaFrame frame, int left, int top, int width, int height, Func<int, int, (byte R, byte G, byte B)> color)
    {
        var pixels = (byte[])frame.Pixels.Clone();
        for (var y = top; y < top + height; y++)
        {
            for (var x = left; x < left + width; x++)
            {
                var (r, g, b) = color(x, y);
                var at = (y * frame.Width + x) * 4;
                pixels[at] = r;
                pixels[at + 1] = g;
                pixels[at + 2] = b;
                pixels[at + 3] = 255;
            }
        }

        return frame with { Pixels = pixels };
    }

    private static (byte R, byte G, byte B) Pixel(RgbaFrame frame, int x, int y)
    {
        var at = (y * frame.Width + x) * 4;
        return (frame.Pixels[at], frame.Pixels[at + 1], frame.Pixels[at + 2]);
    }

    /// <summary>Three frames, a flat one, one with a square laid over it and one with a gradient.</summary>
    private static RgbaFrame[] Three()
    {
        var first = Flat(40, 30, 200, 40, 30);
        var second = With(first, 5, 5, 10, 10, (_, _) => (20, 60, 220));
        var third = With(second, 20, 10, 16, 16, (x, y) => ((byte)((x - 20) * 16), (byte)(y * 9), 128));
        return [first, second, third];
    }

    [Fact]
    public void AnAnimatedPngReadsBackFrameByFrameAsItWasWritten()
    {
        var frames = Three();
        var file = new MemoryStream();
        PngWriter.Animated(file, frames, [100, 250, 1000]);

        var read = AnimationReader.Png(file.ToArray());

        Assert.Equal(3, read.Frames.Count);
        Assert.Equal(new[] { 100, 250, 1000 }, read.Delays);
        Assert.Equal(0, read.Loops);
        for (var i = 0; i < 3; i++) Assert.Equal(frames[i].Pixels, read.Frames[i].Pixels);
    }

    [Fact]
    public void AGifReadsBackWithItsFramesItsDelaysInHundredthsAndItsLoop()
    {
        var frames = Three();
        var file = new MemoryStream();
        GifWriter.Write(file, frames, [100, 250, 1004]);

        var read = AnimationReader.Gif(file.ToArray());

        Assert.Equal(3, read.Frames.Count);
        Assert.Equal(new[] { 100, 250, 1000 }, read.Delays);
        Assert.Equal(0, read.Loops);

        // The flat parts, a few colors among the gradient's, come back exactly, and the square laid
        // over the second frame is there in the third, which wrote only what changed.
        Assert.Equal((200, 40, 30), Pixel(read.Frames[0], 0, 0));
        Assert.Equal((20, 60, 220), Pixel(read.Frames[1], 9, 9));
        Assert.Equal((20, 60, 220), Pixel(read.Frames[2], 9, 9));
        Assert.Equal((200, 40, 30), Pixel(read.Frames[2], 39, 29));

        // The gradient within a few steps of what it was, by the pattern's mean over its tiles.
        double sum = 0;
        for (var y = 10; y < 26; y++)
        {
            for (var x = 20; x < 36; x++)
            {
                var (r, g, b) = Pixel(read.Frames[2], x, y);
                var (er, eg, eb) = Pixel(frames[2], x, y);
                sum += Math.Sqrt((r - er) * (r - er) + (g - eg) * (g - eg) + (b - eb) * (b - eb));
            }
        }

        Assert.InRange(sum / 256, 0, 12);
    }

    [Fact]
    public void TheMedianCutOfAKnownPictureHoldsEachOfItsColors()
    {
        // Sixteen colors in blocks, each in a bin of the cut's 32 steps a channel of its own, which
        // the cut keeps apart and gives back as the mean of the pixels in it.
        (byte R, byte G, byte B)[] colors =
        [
            (0, 0, 0), (255, 255, 255), (255, 0, 0), (0, 255, 0), (0, 0, 255), (255, 255, 0), (0, 255, 255), (255, 0, 255),
            (128, 128, 128), (64, 32, 16), (16, 32, 64), (250, 128, 114), (34, 139, 34), (70, 130, 180), (210, 105, 30), (218, 112, 214),
        ];

        var picture = Flat(64, 64, 0, 0, 0);
        for (var i = 0; i < colors.Length; i++)
        {
            var color = colors[i];
            picture = With(picture, i % 4 * 16, i / 4 * 16, 16, 16, (_, _) => color);
        }

        var cut = GifWriter.MedianCut([picture], 255);
        foreach (var (r, g, b) in colors)
        {
            var nearest = Enumerable.Range(0, cut.Count).Min(i =>
                Math.Abs(cut.Colors[i * 3] - r) + Math.Abs(cut.Colors[i * 3 + 1] - g) + Math.Abs(cut.Colors[i * 3 + 2] - b));
            Assert.Equal(0, nearest);
        }

        var file = new MemoryStream();
        GifWriter.Write(file, [picture], [100]);
        Assert.Equal(picture.Pixels, AnimationReader.Gif(file.ToArray()).Frames[0].Pixels);
    }

    [Fact]
    public void NoiseOfManyColorsSurvivesTheTableStartingAgain()
    {
        // 250 colors, one to a bin, so the palette is exactly them, scattered at random over a
        // picture whose codes fill the table of 4096 many times over.
        var random = new Random(7);
        var colors = Enumerable.Range(0, 250).Select(i => ((byte)((i % 8) * 32 + 3), (byte)((i / 8 % 8) * 32 + 5), (byte)((i / 64) * 32 + 7))).ToArray();
        var noise = With(Flat(200, 200, 0, 0, 0), 0, 0, 200, 200, (_, _) => colors[random.Next(colors.Length)]);

        var file = new MemoryStream();
        GifWriter.Write(file, [noise, noise], [50, 50]);
        var read = AnimationReader.Gif(file.ToArray());

        Assert.Equal(noise.Pixels, read.Frames[0].Pixels);
        Assert.Equal(noise.Pixels, read.Frames[1].Pixels);
    }

    [Fact]
    public void TheRecordCommandsWordsAreReadAsTheyAreWritten()
    {
        var counted = Recorder.Parse("a.gif", ["8"], out _)!;
        Assert.Equal((8, (Recorder.Measure?)null, Recorder.DefaultStepSeconds, false), (counted.Count!.Value, counted.Length, counted.Step.Value, counted.Step.InFrames));

        var timed = Recorder.Parse("a.png", ["5s", "every", "250ms", "scale", "0.5", "play", "100ms"], out _)!;
        Assert.Equal(new Recorder.Measure(5, false), timed.Length);
        Assert.Equal(new Recorder.Measure(0.25, false), timed.Step);
        Assert.Equal((0.5f, 100), (timed.Scale, timed.PlayMilliseconds!.Value));

        var framed = Recorder.Parse("a.gif", ["120f", "every", "15f"], out _)!;
        Assert.Equal((new Recorder.Measure(120, true), new Recorder.Measure(15, true)), (framed.Length!.Value, framed.Step));

        foreach (string[] wrong in (string[][])[[], ["5x"], ["2.5"], ["8", "every"], ["8", "scale", "2"], ["8", "every", "0ms"], ["8", "play", "15f"], ["8", "speed", "2"]])
        {
            Assert.Null(Recorder.Parse("a.gif", wrong, out var problem));
            Assert.False(string.IsNullOrEmpty(problem));
        }
    }

    [Fact]
    public void EachFrameIsShownForTheTimeToTheNextAndTheLastForTheStep()
    {
        var timed = new Recorder.Request("a.gif", new Recorder.Measure(0.25, false), null, 3, 1f, null);
        Assert.Equal(new[] { 250, 250, 250 }, Recorder.Delays([0, 0.25, 0.5], timed));

        // A step in frames, whose last is shown as long as the one before it.
        var framed = timed with { Step = new Recorder.Measure(15, true) };
        Assert.Equal(new[] { 250, 300, 300 }, Recorder.Delays([0, 0.25, 0.55], framed));

        Assert.Equal(new[] { 40, 40, 40 }, Recorder.Delays([0, 0.25, 0.5], timed with { PlayMilliseconds = 40 }));
    }

    [SkippableFact]
    public void ARecordingOfEightFramesAQuarterSecondApartIsAGifOfEightFramesWithThoseDelays()
    {
        Needs.Renderer();
        var path = _folder.File("eight.gif");
        string? answer = null;

        // Asserted after the run, since a system's exception is caught by the engine and said on
        // its log rather than failing the test.
        Record(world =>
        {
            if (world.Resource<Time>().FrameCount == 30) answer = ConsoleCommands.Run($"record {path} 8 every 250ms");
        });

        Assert.StartsWith("recording 8 pictures", answer);

        // Written on a worker once the last picture came back, which the run's last frames left
        // time for, and taken from the recording's task rather than waited for on the machine's clock.
        Assert.Equal(path, Recorder.Last!.GetAwaiter().GetResult());
        var read = AnimationReader.Gif(File.ReadAllBytes(path));
        Assert.Equal(8, read.Frames.Count);
        Assert.All(read.Delays, delay => Assert.Equal(250, delay));
        Assert.Equal((64, 48), (read.Frames[0].Width, read.Frames[0].Height));

        // The clear color's red climbs a step a frame, so each picture is redder than the last.
        var reds = read.Frames.Select(frame => Pixel(frame, 32, 24).R).ToArray();
        Assert.Equal(reds.Order(), reds);
        Assert.True(reds[^1] > reds[0]);
    }

    [SkippableFact]
    public void ARecordingStoppedByTheProgramIsAnAnimatedPngOfThePicturesItTook()
    {
        Needs.Renderer();
        var path = _folder.File("eight.png");
        Task<string>? written = null, stopped = null;

        Record(world =>
        {
            var frame = world.Resource<Time>().FrameCount;
            if (frame == 30) written = Capture.StartRecording(path, 250, scale: 0.5f);

            // Eight pictures, from the frame after it started, fifteen frames apart, and stopped
            // before the ninth.
            if (frame == 140) stopped = Capture.StopRecording();
        });

        Assert.NotNull(written);
        Assert.Same(written, stopped);
        Assert.Equal(path, written.GetAwaiter().GetResult());
        Assert.False(Capture.IsRecording);

        var read = AnimationReader.Png(File.ReadAllBytes(path));
        Assert.Equal(8, read.Frames.Count);
        Assert.All(read.Delays, delay => Assert.Equal(250, delay));
        Assert.Equal((32, 24), (read.Frames[0].Width, read.Frames[0].Height));
        Assert.Null(Capture.StopRecording());
    }

    /// <summary>
    /// Runs an offscreen app of 64 by 48 on a clock of a sixtieth of a second a frame, its clear
    /// color redder each frame, for long enough to take eight pictures a quarter of a second apart
    /// from its thirtieth frame and write them.
    /// </summary>
    private static void Record(Action<World> eachFrame)
    {
        var config = Config.OffscreenFor(64, 48, frames: 220);
        config.FrameSeconds = 1.0 / 60.0;

        using var app = new App(config);
        app.AddPlugin(new EnginePlugin());
        app.AddSystem(Stage.Startup, new SystemDescriptor(_ => Render.SpawnCamera3d(), "Test.Camera"));
        app.AddSystem(Stage.Update, new SystemDescriptor(world =>
        {
            var frame = world.Resource<Time>().FrameCount;
            Render.SetClearColor((Math.Min(1f, frame / 220f), 0.2f, 0.4f, 1f));
            eachFrame(world);
        }, "Test.Record"));

        Assert.Equal(0, app.Run());
    }
}
