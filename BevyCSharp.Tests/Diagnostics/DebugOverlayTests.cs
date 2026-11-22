using System.Runtime.InteropServices;
using Bevy;
using Diagnostics;
using Bevy.Interop;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the debug overlay, F3 let go alone turning it over and a chord on F3 not, the key moved,
/// its lines saying the frame, the camera, the world, the system and the renderer of a scene whose
/// numbers are known, the strip of the last 240 frames' times and what is said beside it,
/// Shift+F3's profiler, a program's sections, and Bevy's FPS overlay hidden under it and given
/// back.
/// </summary>
/// <remarks>
/// The key is set to F3 before each test, since an example that holds F3 turns it off in the
/// process the suite shares with it, and the overlay, the profiler, the key and the sections the
/// tests add are put back after.
/// </remarks>
[Collection("engine")]
public sealed class DebugOverlayTests : IDisposable
{
    // The overlay's key put back to F3, since a test or an example in the process the suite shares
    // may have moved it.
    public DebugOverlayTests() => Debug.Keys.KeyOf(KeyAction.Overlay).Reset();

    public void Dispose()
    {
        Debug.Overlay.Shown = false;
        Debug.Overlay.Detailed = false;
        Debug.Keys.KeyOf(KeyAction.Overlay).Reset();
        Debug.Overlay.RemoveSection("World");
        Debug.Overlay.RemoveSection("Broken");
    }

    /// <summary>
    /// The scene's counts at the offsets the bridge writes them, which its own test holds to the
    /// same numbers.
    /// </summary>
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
            .Do("reading", _ => seen.Add(Debug.Overlay.Shown))
            .Do("holding F3 again", _ => SyntheticInput.Press(Key.F3))
            .Wait(2)
            .Do("pressing F4 with it", _ => SyntheticInput.Tap(Key.F4))
            .Wait(2)
            .Do("letting F3 go", _ => SyntheticInput.Lift(Key.F3))
            .Wait(2)
            .Do("reading again", _ => seen.Add(Debug.Overlay.Shown))
            .Do("moving the key and pressing F3", _ =>
            {
                Debug.Keys.KeyOf(KeyAction.Overlay).TrySet("F6");
                SyntheticInput.Tap(Key.F3);
            })
            .Wait(3)
            .Do("reading after", _ => seen.Add(Debug.Overlay.Shown))
            .Do("pressing the moved key", _ => SyntheticInput.Tap(Key.F6))
            .Wait(3)
            .Do("reading last", _ => seen.Add(Debug.Overlay.Shown))
            .Go();

        // Shown by F3 alone, kept by the chord, kept by F3 once moved, and hidden by the moved key.
        Assert.Equal(new[] { true, true, true, false }, seen);
    }

    /// <summary>
    /// A key held as an app ends, which that app never saw let go, makes no chord of F3 in the
    /// next, since each app takes keys of its own.
    /// </summary>
    [SkippableFact]
    public void AKeyHeldAsTheLastAppEndedMakesNoChordInTheNext()
    {
        Needs.Renderer();
        new PictureRun { Width = 64, Height = 48, Scene = ecs => PictureRun.Camera(ecs) }
            .Wait(3)
            .Do("holding W to the end", _ => SyntheticInput.Press(Key.W))
            .Wait(2)
            .Go();

        var shown = false;
        new PictureRun { Width = 64, Height = 48, Scene = ecs => PictureRun.Camera(ecs) }
            .Wait(3)
            .Do("pressing F3", _ => SyntheticInput.Tap(Key.F3))
            .Wait(3)
            .Do("reading", _ => shown = Debug.Overlay.Shown)
            .Go();

        Assert.True(shown);
    }

    [SkippableFact]
    public void TheOverlaySaysTheFrameTheCameraTheWorldTheSystemAndTheRenderer()
    {
        Needs.Renderer();
        (List<string> Head, List<string> Beside, List<string> Left, List<string> Right)? said = null;

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
        run.Wait(10).Do("reading the lines", _ => said = Said()).Go();
        var (head, beside, left, right) = said!.Value;

        Assert.Contains(" fps, ", head[0]);
        Assert.EndsWith(" fps 1% low", head[0]);
        Assert.StartsWith("BevyCSharp ", head[1]);
        Assert.Contains(", frame ", head[1]);
        Assert.Equal("GPU time not measured, Config.GpuTimings measures it", beside[0]);
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
        Assert.Contains("Scene field off", right);
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
                var real = world.Resource<Time>().RawDeltaSeconds;
                if (real > 0.0) kept.Add((float)(real * 1000.0));
            },
        };
        run.Wait(300)
            .Do("reading the strip", _ =>
            {
                strip = [.. Debug.Overlay.FrameTimes];
                frames = kept.Count;
            })
            .Go();

        Assert.True(frames > OverlayModel.StripFrames, $"{frames} frames ran");
        Assert.Equal(OverlayModel.StripFrames, strip.Length);
        Assert.True(
            kept.Take(frames).Skip(frames - OverlayModel.StripFrames).SequenceEqual(strip)
                || kept.Take(frames - 1).Skip(frames - 1 - OverlayModel.StripFrames).SequenceEqual(strip),
            "The strip is not the last 240 frames' times in order.");
    }

    /// <summary>
    /// The overlay times frames on the machine's clock, so a frame of a game slowed to a quarter is
    /// said at its own length, and the strip goes on while the game is paused.
    /// </summary>
    [SkippableFact]
    public void TheOverlayTimesFramesOnTheMachinesClockWhateverTheGamesDoes()
    {
        Needs.Renderer();
        string slowed = "", paused = "";
        float[] strip = [];

        var run = new PictureRun
        {
            Width = 64,
            Height = 48,
            Configure = config => config.FrameSeconds = 0.02,
            Scene = ecs => PictureRun.Camera(ecs),
        };
        run.Wait(10)
            .Do("slowing the game to a quarter", world => world.Resource<Time>().SetSpeed(0.25f))
            .Wait(30)
            .Do("reading the frame's line", _ => slowed = Said().Head[0])
            .Do("pausing the game", world => world.Resource<Time>().Pause())
            .Wait(60)
            .Do("reading the frame's line and the strip", world =>
            {
                paused = Said().Head[0];
                strip = [.. Debug.Overlay.FrameTimes];
            })
            .Go();

        // The last frame's time, the line's second figure, the frames kept before the clock was
        // fixed counted in the rate and the average.
        Assert.Contains(" fps, 20.0 ms, ", slowed);
        Assert.Contains(" fps, 20.0 ms, ", paused);
        Assert.True(strip.Length >= 60, $"the strip holds {strip.Length} frames");
        Assert.All(strip[^60..], ms => Assert.Equal(20f, ms, 0.01f));
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
                Debug.Overlay.AddSection("World", OverlayPlace.Left, () => ["seed 42", "chunk 3 -1"]);
                Debug.Overlay.AddSection("Broken", OverlayPlace.Left, () => throw new InvalidOperationException("no world yet"));
                added = Said().Left;
            })
            .Do("replacing the first", world =>
            {
                Debug.Overlay.AddSection("World", OverlayPlace.Left, () => ["seed 7"]);
                replaced = Said().Left;
            })
            .Do("removing both", world =>
            {
                Assert.True(Debug.Overlay.RemoveSection("World"));
                Assert.True(Debug.Overlay.RemoveSection("Broken"));
                Assert.False(Debug.Overlay.RemoveSection("World"));
                removed = Said().Left;
            })
            .Go();

        // Under the engine's lines in the order added, the one that threw naming itself and why.
        var at = added.IndexOf("seed 42");
        Assert.True(at > 0, string.Join(" | ", added));
        Assert.Equal(["seed 42", "chunk 3 -1", "Broken failed. no world yet"], added.Skip(at));
        Assert.StartsWith("Meshes ", added[at - 1]);

        // Replaced where it stood, ahead of the one added after it.
        Assert.Equal(["seed 7", "Broken failed. no world yet"], replaced.Skip(replaced.IndexOf("seed 7")));

        Assert.DoesNotContain("seed 7", removed);
        Assert.DoesNotContain("Broken failed. no world yet", removed);
        Assert.StartsWith("Meshes ", removed[^1]);
    }

    [SkippableFact]
    public void ShiftF3ShowsTheProfilerWithTheOverlayAndF3AloneHidesBoth()
    {
        Needs.Renderer();
        var seen = new List<(bool Overlay, bool Profiler)>();
        (List<string> Head, List<string> Beside, List<string> Left, List<string> Right)? said = null;

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
                seen.Add((Debug.Overlay.Shown, Debug.Overlay.Detailed));
                said = Said();
            })
            .Do("pressing F3 alone", _ => SyntheticInput.Tap(Key.F3))
            .Wait(3)
            .Do("reading again", _ => seen.Add((Debug.Overlay.Shown, Debug.Overlay.Detailed)))
            .Do("pressing F3 alone again", _ => SyntheticInput.Tap(Key.F3))
            .Wait(3)
            .Do("reading last", _ => seen.Add((Debug.Overlay.Shown, Debug.Overlay.Detailed)))
            .Go();

        // Both shown by Shift+F3, both hidden by F3, and the overlay alone shown by F3 again.
        Assert.Equal(new[] { (true, true), (false, false), (true, false) }, seen);

        // The passes the app asked to have timed, the slowest first, and the GPU's time beside the
        // strip, measured or said not to be where the adapter has no timestamp queries.
        var lines = said!.Value;
        var passes = lines.Right.IndexOf("Passes, the slowest first");
        Assert.True(passes > 0 && passes + 1 < lines.Right.Count, string.Join(" | ", lines.Right));
        Assert.Contains(" GPU ", lines.Right[passes + 1]);
        Assert.StartsWith("GPU ", lines.Beside[0]);
        Assert.NotEqual("GPU time not measured, Config.GpuTimings measures it", lines.Beside[0]);
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
            .Do("showing the overlay", _ => Debug.Overlay.Shown = true)
            .Wait(3)
            .Do("reading", world => seen.Add(Read(world)))
            .Do("hiding the overlay", _ => Debug.Overlay.Shown = false)
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
            .Do("showing the overlay", _ => Debug.Overlay.Shown = true)
            .Wait(10)
            .Capture("after")
            .Go();

        static int Bars(CapturedImage picture) => PictureRun.Count(picture, (r, g, b) =>
            Near(r, g, b, 80, 220, 100) || Near(r, g, b, 240, 200, 60) || Near(r, g, b, 240, 80, 70));

        static bool Near(byte r, byte g, byte b, int wr, int wg, int wb) =>
            Math.Abs(r - wr) <= 6 && Math.Abs(g - wg) <= 6 && Math.Abs(b - wb) <= 6;

        Assert.Equal(0, Bars(run.Picture("before")));
        Assert.True(Bars(run.Picture("after")) >= OverlayModel.StripFrames, $"{Bars(run.Picture("after"))} pixels of bars");
    }

    /// <summary>The overlay's lines asked of its sections at once, a list for each place.</summary>
    private static (List<string> Head, List<string> Beside, List<string> Left, List<string> Right) Said()
    {
        Debug.Overlay.Refresh();
        return ([.. Debug.Overlay.Lines(OverlayPlace.Head)], [.. Debug.Overlay.Lines(OverlayPlace.Beside)],
            [.. Debug.Overlay.Lines(OverlayPlace.Left)], [.. Debug.Overlay.Lines(OverlayPlace.Right)]);
    }
}
