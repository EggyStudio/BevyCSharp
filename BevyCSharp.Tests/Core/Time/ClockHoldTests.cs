using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a set clock held still, while an asset the app asked for is loading and until the hold
/// between commands lets it go, and the count of the frames it moved in.
/// </summary>
[Collection("engine")]
public sealed class ClockHoldTests
{
    /// <summary>
    /// Held for loads, a set clock stands still in each frame after one that found an asset
    /// loading, and moves a step in each frame after one that found none, so the game's time goes on
    /// when the load ends.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bevy moves a load on before the frame's systems, so a system finds the load as the engine
    /// finds it before the next frame's clock moves. The image is asked for in the third frame's
    /// systems, since one this small asked for at startup has loaded before the first frame's
    /// systems run, and an asset is loading from the moment it is asked for, so the frame after
    /// stands still however fast the load is.
    /// </para>
    /// <para>
    /// The load runs on Bevy's threads for loading and is taken in before each frame's systems,
    /// with no step of the clock in it, so a held clock holds nothing the load waits on. Its length
    /// is the machine's, so the frames are paced at sixty a second for as long as ten seconds, the
    /// run ending three frames after the load, where a set count of frames run as fast as they go
    /// can end before a slow runner's load does.
    /// </para>
    /// </remarks>
    [Fact]
    public void AClockHeldForLoadsStandsStillUntilTheyEnd()
    {
        using var harness = new EngineHarness(frames: 600, fps: 60, frameSeconds: 0.1);
        var image = AssetHandle.None;
        var seen = new List<(double Elapsed, ulong Clock, bool Loading)>();
        var after = 0;

        harness.OnContext(Stage.Startup, ctx => ctx.Time.Holds = ClockHolds.Loads);
        harness.OnContext(Stage.Update, ctx =>
        {
            if (ctx.Time.FrameCount == 3) image = AssetServer.Load(AssetKind.Image, "textures/checker.png");
            var loading = image.IsValid && AssetServer.StateWithDependenciesOf(image) == AssetLoadState.Loading;
            seen.Add((ctx.Time.ElapsedSeconds, ctx.Time.ClockFrames, loading));
            if (image.IsValid && !loading && ++after == 3) ctx.Exit();
        });

        harness.Run();

        Assert.True(seen.Count > 5 && seen[3].Loading, "the image was not loading in the frame it was asked for");
        Assert.False(seen[^1].Loading, "the image never loaded");
        for (var i = 1; i < seen.Count; i++)
        {
            var moved = seen[i].Elapsed - seen[i - 1].Elapsed;
            Assert.Equal(seen[i - 1].Loading ? 0.0 : 0.1, moved, 9);
            Assert.Equal((ulong)Math.Round(seen[i].Elapsed / 0.1), seen[i].Clock);
        }
    }

    /// <summary>
    /// Held for loads, a set clock stands still while the first clip played on a scene reads the
    /// file the scene came from again for the clips' names, so the clip starts at the moment of the
    /// clock it was first asked for, on any device.
    /// </summary>
    /// <remarks>
    /// Bevy frees a glTF file's own asset once the scene asked for by its label has loaded and
    /// nothing holds the file, so the scene stands ten frames before its clip is asked for, and the
    /// first ask finds the file gone and reads it again, as many_foxes did on every device. The
    /// ask's frame moves the clock, and each after it stands still until the clip plays.
    /// </remarks>
    [SkippableFact]
    public void AClockHeldForLoadsStandsStillWhileAScenesClipsAreRead()
    {
        Needs.Renderer();

        var model = AssetHandle.None;
        var scene = Entity.None;
        (double Elapsed, ulong Clock)? asked = null, played = null;
        var refused = 0;

        var run = new PictureRun
        {
            Configure = config => config.FrameSeconds = 0.1,
            Scene = ecs => scene = ecs.SpawnScene(model = AssetServer.LoadGltfScene("models/bend.gltf")),
        };

        run.Do("holding the clock for loads", world => world.Resource<Time>().Holds = ClockHolds.Loads)
            .Until("the scene arrives", _ => AssetServer.StateWithDependenciesOf(model) == AssetLoadState.Loaded)
            .Wait(10)
            .Until("the clip plays", world =>
            {
                var time = world.Resource<Time>();
                asked ??= (time.ElapsedSeconds, time.ClockFrames);
                if (!Animation.Play(scene, "Bend"))
                {
                    refused++;
                    return false;
                }

                played = (time.ElapsedSeconds, time.ClockFrames);
                return true;
            })
            .Go();

        Assert.True(refused > 0, "the clip played at its first ask, so the file was not read again and the test held nothing");
        Assert.Equal(asked!.Value.Elapsed, played!.Value.Elapsed, 9);
        Assert.Equal(asked.Value.Clock, played.Value.Clock);
    }

    /// <summary>
    /// Held for loads, a set clock stands still while a program's Slang compiles and its compute
    /// pipeline is built, so the program reads ready one step of the clock after the moment it was
    /// made in, however many frames the two take.
    /// </summary>
    /// <remarks>
    /// The compile runs in a process of its own and the pipeline is built on the render side's
    /// thread, each taken in at the start of a later frame, so the frame that takes the pipeline in
    /// is the first to move the clock, and more than one frame runs before the program is ready.
    /// The game of life's compute steps start once its programs are ready, so on a running clock a
    /// slower device started them later and a picture taken at a frame caught another generation.
    /// </remarks>
    [SkippableFact]
    public void AClockHeldForLoadsStandsStillWhileAProgramCompiles()
    {
        Needs.Shaders();

        using var assets = PictureRun.Temporary();
        assets.Write("count.slang", """
            import bcs_compute;

            RWStructuredBuffer<uint> numbers;

            [shader("compute")]
            [numthreads(64, 1, 1)]
            void main(uint3 id : SV_DispatchThreadID)
            {
                numbers[id.x] = id.x;
            }
            """);

        var program = ShaderProgram.None;
        (ulong Frame, double Elapsed, ulong Clock)? made = null, ready = null;
        static (ulong, double, ulong) Now(World world)
        {
            var time = world.Resource<Time>();
            return (time.FrameCount, time.ElapsedSeconds, time.ClockFrames);
        }

        var run = new PictureRun
        {
            AssetRoot = assets.Root,
            Configure = config => config.FrameSeconds = 0.1,
            Scene = ecs => PictureRun.Camera(ecs),
        };

        run.Do("holding the clock for loads", world => world.Resource<Time>().Holds = ClockHolds.Loads)
            .Do("making the program", world =>
            {
                program = Shaders.CreateProgram(new ShaderProgramSettings { Compute = "count.slang" });
                made = Now(world);
            })
            .Until("the program is ready", world =>
            {
                if (program.State != ShaderProgramState.Ready) return false;
                ready = Now(world);
                return true;
            })
            .Go();

        var (was, now) = (made!.Value, ready!.Value);
        var seen = $"made in frame {was.Frame} at clock {was.Clock} and {was.Elapsed:F3} s, ready in frame {now.Frame} at clock {now.Clock} and {now.Elapsed:F3} s";
        Assert.True(now.Frame > was.Frame + 1, $"the program was ready in the frame after the one that made it, so its compile held nothing: {seen}");
        Assert.True(now.Clock == was.Clock + 1 && Math.Abs(now.Elapsed - was.Elapsed - 0.1) < 1e-9, $"the clock moved other than one step while the program compiled: {seen}");
    }

    /// <summary>
    /// Held still, as the server holds it between commands, a set clock passes no time to the game
    /// and runs no fixed step until it is let go, and the frames it stood in are left out of its
    /// count.
    /// </summary>
    [Fact]
    public void AClockHeldStillPassesNoTimeUntilLetGo()
    {
        using var harness = new EngineHarness(frames: 12, frameSeconds: 0.1, fixedHz: 20);
        var seen = new List<(ulong Frame, double Elapsed, ulong Clock, int Steps)>();
        var steps = 0;

        harness.OnContext(Stage.Startup, ctx => ctx.Time.Holds = ClockHolds.Commands);
        harness.On(Stage.FixedUpdate, _ => steps++);
        harness.OnContext(Stage.Update, ctx =>
        {
            var frame = ctx.Time.FrameCount;
            seen.Add((frame, ctx.Time.ElapsedSeconds, ctx.Time.ClockFrames, steps));
            ctx.Time.HoldStill(frame is >= 3 and < 7);
        });

        harness.Run();

        // Held in the frames after the third to the sixth, so the fourth to the seventh stand still.
        Assert.True(seen.Count >= 10, $"only {seen.Count} frames ran");
        var still = 0ul;
        for (var i = 1; i < seen.Count; i++)
        {
            var held = seen[i].Frame is >= 4 and <= 7;
            if (held) still++;

            Assert.Equal(held ? 0.0 : 0.1, seen[i].Elapsed - seen[i - 1].Elapsed, 9);
            Assert.Equal(seen[i].Frame - still, seen[i].Clock);
            if (held) Assert.Equal(seen[i - 1].Steps, seen[i].Steps);
        }

        Assert.Equal(4ul, still);
    }

    /// <summary>
    /// The machine's clock is not held, since it reads the time since the last frame it counted
    /// and would hand the frame after a hold the whole of it, so every frame after the first moves
    /// and is counted.
    /// </summary>
    [Fact]
    public void TheMachinesClockIsNotHeld()
    {
        using var harness = new EngineHarness(frames: 6);
        var seen = new List<(ulong Frame, double Delta, ulong Clock)>();

        harness.OnContext(Stage.Startup, ctx => ctx.Time.Holds = ClockHolds.Loads | ClockHolds.Commands);
        harness.OnContext(Stage.Update, ctx =>
        {
            ctx.Time.HoldStill(true);
            seen.Add((ctx.Time.FrameCount, ctx.Time.RawDeltaSeconds, ctx.Time.ClockFrames));
        });

        harness.Run();

        Assert.True(seen.Count >= 4, $"only {seen.Count} frames ran");
        Assert.All(seen.Skip(1), frame =>
        {
            Assert.True(frame.Delta > 0, $"frame {frame.Frame} passed no time on the machine's clock");
            Assert.Equal(frame.Frame, frame.Clock);
        });
    }
}
