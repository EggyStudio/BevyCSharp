using System.Runtime.InteropServices;
using Bevy;
using Bevy.Interop;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the debug overlay, F3 let go alone turning it over and a chord on F3 not, the key moved,
/// its lines saying the frame, the camera, the world, the system and the renderer of a scene whose
/// numbers are known, the strip of the last 240 frames' times and what is said beside it, Shift+F3's
/// profiler, a program's sections, and Bevy's FPS overlay hidden under it and given back.
/// </summary>
/// <remarks>
/// The key is set to F3 before each test, since an example that holds F3 turns it off in the
/// process the suite shares with it, and the overlay, the profiler, the key and the sections the
/// tests add are put back after.
/// </remarks>
[Collection("engine")]
public sealed class DebugOverlayTests : IDisposable
{
    public DebugOverlayTests() => Debug.OverlayKey = Key.F3;

    public void Dispose()
    {
        Debug.OverlayShown = false;
        Debug.ProfilerShown = false;
        Debug.OverlayKey = Key.F3;
        Debug.RemoveOverlaySection("World");
        Debug.RemoveOverlaySection("Broken");
    }

    /// <summary>The scene's counts at the offsets the bridge writes them, which its own test holds to the same numbers.</summary>
    [Fact]
    public void TheSceneCountsLieWhereTheBridgeWritesThem()
    {
        Assert.Equal(8, Marshal.OffsetOf<NativeSceneCounts>(nameof(NativeSceneCounts.Triangles)).ToInt32());
        Assert.Equal(16, Marshal.OffsetOf<NativeSceneCounts>(nameof(NativeSceneCounts.Directional)).ToInt32());
        Assert.Equal(28, Marshal.OffsetOf<NativeSceneCounts>(nameof(NativeSceneCounts.Shadowed)).ToInt32());
        Assert.Equal(32, Marshal.OffsetOf<NativeSceneCounts>(nameof(NativeSceneCounts.Camera)).ToInt32());
        Assert.Equal(40, Marshal.OffsetOf<NativeSceneCounts>(nameof(NativeSceneCounts.Fov)).ToInt32());
        Assert.Equal(44, Marshal.OffsetOf<NativeSceneCounts>(nameof(NativeSceneCounts.X)).ToInt32());
        Assert.Equal(56, Marshal.OffsetOf<NativeSceneCounts>(nameof(NativeSceneCounts.ForwardX)).ToInt32());
        Assert.Equal(72, Marshal.SizeOf<NativeSceneCounts>());
    }

    [SkippableFact]
    public void F3LetGoAloneTurnsTheOverlayOverAndAChordOnItDoesNot()
    {
        Needs.Renderer();
        var seen = new List<bool>();

        var run = new PictureRun { Width = 64, Height = 48, Scene = ecs => PictureRun.Camera(ecs) };
        run.Wait(3)
            .Do("holding F3", _ => SyntheticInput.Press(Key.F3))
            .Wait(2)
            .Do("letting it go", _ => SyntheticInput.Lift(Key.F3))
            .Wait(2)
            .Do("reading", _ => seen.Add(Debug.OverlayShown))
            .Do("holding F3 again", _ => SyntheticInput.Press(Key.F3))
            .Wait(2)
            .Do("pressing F4 with it", _ => SyntheticInput.Tap(Key.F4))
            .Wait(2)
            .Do("letting F3 go", _ => SyntheticInput.Lift(Key.F3))
            .Wait(2)
            .Do("reading again", _ => seen.Add(Debug.OverlayShown))
            .Do("moving the key and pressing F3", _ =>
            {
                Debug.OverlayKey = Key.F6;
                SyntheticInput.Tap(Key.F3);
            })
            .Wait(3)
            .Do("reading after", _ => seen.Add(Debug.OverlayShown))
            .Do("pressing the moved key", _ => SyntheticInput.Tap(Key.F6))
            .Wait(3)
            .Do("reading last", _ => seen.Add(Debug.OverlayShown))
            .Go();

        // Shown by F3 alone, kept by the chord, kept by F3 once moved, and hidden by the moved key.
        Assert.Equal(new[] { true, true, true, false }, seen);
    }

    [SkippableFact]
    public void TheOverlaySaysTheFrameTheCameraTheWorldTheSystemAndTheRenderer()
    {
        Needs.Renderer();
        Debug.Said? said = null;

        // A cube of twelve triangles seen from up and behind along -Z, a directional light with
        // shadow maps and a point light without.
        var run = new PictureRun
        {
            Width = 96,
            Height = 64,
            Scene = ecs =>
            {
                PictureRun.Camera(ecs, new Vec3(0f, 2f, 6f));
                PictureRun.Cube(ecs, Render.CreateMaterial(0.8f, 0.8f, 0.8f));
                Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = true });
                Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Shadows = false });
            },
        };
        run.Wait(10).Do("reading the lines", world => said = Debug.Lines(world)).Go();
        var (head, beside, left, right) = said!;

        Assert.StartsWith("BevyCSharp ", head[0]);
        Assert.Contains(" fps, ", head[1]);
        Assert.StartsWith("Now ", beside[0]);
        Assert.Contains(", 1% low ", beside[0]);
        Assert.Equal("GPU time not measured, Config.GpuTimings measures it", beside[1]);
        Assert.Contains("XYZ 0.000 / 2.000 / 6.000", left);
        Assert.Contains(left, line => line.StartsWith("Facing north (towards negative Z), heading 0.0, pitch -18.4", StringComparison.Ordinal));
        Assert.Contains("Field of view 45 degrees", left);
        Assert.Contains(left, line => line.StartsWith("Entities ", StringComparison.Ordinal));
        Assert.Contains(left, line => line.StartsWith("Meshes ", StringComparison.Ordinal));

        Assert.Contains(right, line => line.StartsWith(".NET ", StringComparison.Ordinal));
        Assert.Contains(right, line => line.StartsWith("CPU ", StringComparison.Ordinal));
        Assert.Contains(right, line => line.StartsWith("GPU ", StringComparison.Ordinal));
        Assert.Contains("Display offscreen, 96 x 64", right);
        Assert.Contains("Meshes in view 1, triangles 12", right);
        Assert.Contains("Lights 1 directional, 1 point, 0 spot, 1 with shadow maps", right);
    }

    [SkippableFact]
    public void TheStripHoldsTheLast240FramesTimesTheOldestFirst()
    {
        Needs.Renderer();
        var kept = new List<float>();
        float[] strip = [];
        var frames = 0;

        // Every frame's time as a system reads it, which is the time the overlay keeps of the
        // same frame, so the strip is the end of this list, taken before or after this frame's.
        var run = new PictureRun
        {
            Width = 64,
            Height = 48,
            Scene = ecs => PictureRun.Camera(ecs),
            EachFrame = world =>
            {
                var delta = world.Resource<Time>().Delta;
                if (delta > 0f) kept.Add(delta * 1000f);
            },
        };
        run.Wait(300)
            .Do("reading the strip", _ =>
            {
                strip = Debug.FrameTimes();
                frames = kept.Count;
            })
            .Go();

        Assert.True(frames > Debug.StripFrames, $"{frames} frames ran");
        Assert.Equal(Debug.StripFrames, strip.Length);
        Assert.True(
            kept.Take(frames).Skip(frames - Debug.StripFrames).SequenceEqual(strip)
                || kept.Take(frames - 1).Skip(frames - 1 - Debug.StripFrames).SequenceEqual(strip),
            "The strip is not the last 240 frames' times in order.");
    }

    /// <summary>
    /// The last time, the average and the one percent low, the slowest hundredth of the frames
    /// averaged and taken as a rate, three of 240 and one of fewer than a hundred.
    /// </summary>
    [Fact]
    public void TheStatsAreTheLastTheAverageAndTheSlowestHundredthsRate()
    {
        var steady = Enumerable.Repeat(16f, 237).Concat([40f, 50f, 60f]).ToArray();
        var (now, average, low) = Debug.Stats(steady);
        Assert.Equal(60f, now);
        Assert.Equal((237f * 16f + 150f) / 240f, average, 3);
        Assert.Equal(20f, low, 3);

        var (_, few, slowest) = Debug.Stats([10f, 30f, 20f]);
        Assert.Equal(20f, few, 3);
        Assert.Equal(1000f / 30f, slowest, 3);

        Assert.Equal((0f, 0f, 0f), Debug.Stats([]));
    }

    [SkippableFact]
    public void ASectionsLinesFollowTheEnginesAndOneThatThrowsSaysWhy()
    {
        Needs.Renderer();
        List<string> added = [], replaced = [], removed = [];

        var run = new PictureRun { Width = 64, Height = 48, Scene = ecs => PictureRun.Camera(ecs) };
        run.Wait(3)
            .Do("adding two sections", world =>
            {
                Debug.AddOverlaySection("World", () => ["seed 42", "chunk 3 -1"]);
                Debug.AddOverlaySection("Broken", () => throw new InvalidOperationException("no world yet"));
                added = Debug.Lines(world).Left;
            })
            .Do("replacing the first", world =>
            {
                Debug.AddOverlaySection("World", () => ["seed 7"]);
                replaced = Debug.Lines(world).Left;
            })
            .Do("removing both", world =>
            {
                Assert.True(Debug.RemoveOverlaySection("World"));
                Assert.True(Debug.RemoveOverlaySection("Broken"));
                Assert.False(Debug.RemoveOverlaySection("World"));
                removed = Debug.Lines(world).Left;
            })
            .Go();

        // Under the engine's lines in the order added, each after a gap and its name.
        var at = added.IndexOf("World");
        Assert.True(at > 0 && added[at - 1].Length == 0, string.Join(" | ", added));
        Assert.Equal(["World", "seed 42", "chunk 3 -1", "", "Broken", "Broken: InvalidOperationException, no world yet"], added.Skip(at));
        Assert.StartsWith("Meshes ", added[at - 2]);

        // Replaced where it stood, ahead of the one added after it.
        Assert.Equal(["World", "seed 7", "", "Broken", "Broken: InvalidOperationException, no world yet"], replaced.Skip(replaced.IndexOf("World")));

        Assert.DoesNotContain("World", removed);
        Assert.DoesNotContain("Broken", removed);
        Assert.StartsWith("Meshes ", removed[^1]);
    }

    [SkippableFact]
    public void ShiftF3ShowsTheProfilerWithTheOverlayAndF3AloneHidesBoth()
    {
        Needs.Renderer();
        var seen = new List<(bool Overlay, bool Profiler)>();
        Debug.Said? said = null;

        var run = new PictureRun
        {
            Width = 64,
            Height = 48,
            Configure = config => config.GpuTimings = true,
            Scene = ecs => PictureRun.Camera(ecs),
        };
        run.Wait(3)
            .Do("holding Shift", _ => SyntheticInput.Press(Key.ShiftLeft))
            .Wait(2)
            .Do("pressing F3 with it", _ => SyntheticInput.Tap(Key.F3))
            .Wait(2)
            .Do("letting Shift go", _ => SyntheticInput.Lift(Key.ShiftLeft))
            .Wait(30)
            .Do("reading", world =>
            {
                seen.Add((Debug.OverlayShown, Debug.ProfilerShown));
                said = Debug.Lines(world);
            })
            .Do("pressing F3 alone", _ => SyntheticInput.Tap(Key.F3))
            .Wait(3)
            .Do("reading again", _ => seen.Add((Debug.OverlayShown, Debug.ProfilerShown)))
            .Do("pressing F3 alone again", _ => SyntheticInput.Tap(Key.F3))
            .Wait(3)
            .Do("reading last", _ => seen.Add((Debug.OverlayShown, Debug.ProfilerShown)))
            .Go();

        // Both shown by Shift+F3, both hidden by F3, and the overlay alone shown by F3 again.
        Assert.Equal(new[] { (true, true), (false, false), (true, false) }, seen);

        // The passes the app asked to have timed, the slowest first, and the GPU's time beside the
        // strip, measured or said not to be where the adapter has no timestamp queries.
        var passes = said!.Right.IndexOf("Passes, the slowest first");
        Assert.True(passes > 0 && passes + 1 < said.Right.Count, string.Join(" | ", said.Right));
        Assert.Contains(" GPU ", said.Right[passes + 1]);
        Assert.StartsWith("GPU ", said.Beside[1]);
        Assert.NotEqual("GPU time not measured, Config.GpuTimings measures it", said.Beside[1]);
    }

    [SkippableFact]
    public void BevysFpsOverlayHidesUnderTheOverlayAndComesBackAsItWas()
    {
        Needs.Renderer();
        var seen = new List<(bool Text, bool Graph)>();

        static (bool, bool) Read(World world) =>
            world.Resource<EcsWorld>().Resource<FpsOverlayConfigRef>() is { } overlay
                ? (overlay.Enabled, overlay.FrameTimeGraphConfigEnabled)
                : throw new InvalidOperationException("The app has no FPS overlay.");

        var run = new PictureRun
        {
            Width = 64,
            Height = 48,
            Configure = config => config.DevTools = DevTools.FpsOverlay,
            Scene = ecs => PictureRun.Camera(ecs),
        };
        run.Wait(3)
            .Do("hiding its graph", world =>
            {
                var overlay = world.Resource<EcsWorld>().Resource<FpsOverlayConfigRef>()!.Value;
                overlay.FrameTimeGraphConfigEnabled = false;
                seen.Add(Read(world));
            })
            .Do("showing the overlay", _ => Debug.OverlayShown = true)
            .Wait(3)
            .Do("reading", world => seen.Add(Read(world)))
            .Do("hiding the overlay", _ => Debug.OverlayShown = false)
            .Wait(3)
            .Do("reading again", world => seen.Add(Read(world)))
            .Go();

        // The text shown and the graph not as the program set them, both hidden, then as they were.
        Assert.Equal(new[] { (true, false), (false, false), (true, false) }, seen);
    }

    /// <summary>
    /// The strip in the frame's pixels, its bars in the three colors of a frame's speed, none of
    /// which the picture holds before the overlay shows.
    /// </summary>
    [SkippableFact]
    public void TheStripDrawsTheFramesAsBarsUnderTheFirstLines()
    {
        Needs.Renderer();

        var run = new PictureRun { Width = 640, Height = 360, Scene = ecs => PictureRun.Camera(ecs) };
        run.Wait(10)
            .Capture("before")
            .Do("showing the overlay", _ => Debug.OverlayShown = true)
            .Wait(10)
            .Capture("after")
            .Go();

        static int Bars(CapturedImage picture) => PictureRun.Count(picture, (r, g, b) =>
            Near(r, g, b, 80, 220, 100) || Near(r, g, b, 240, 200, 60) || Near(r, g, b, 240, 80, 70));

        static bool Near(byte r, byte g, byte b, int wr, int wg, int wb) =>
            Math.Abs(r - wr) <= 6 && Math.Abs(g - wg) <= 6 && Math.Abs(b - wb) <= 6;

        Assert.Equal(0, Bars(run.Picture("before")));
        Assert.True(Bars(run.Picture("after")) >= Debug.StripFrames, $"{Bars(run.Picture("after"))} pixels of bars");
    }
}
