using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a model's animation clips, played by name on the entity its scene was spawned under.
/// </summary>
/// <remarks>
/// These draw <c>assets/models/bend.gltf</c>, a strip two units tall on two joints, with a clip
/// that bends its upper half over and back and one that sways it from the bottom, written by
/// hand so it stays small enough to read. Its material is unlit white, so a picture of it is the
/// strip's shape on black and nothing else.
/// </remarks>
[Collection("engine")]
public sealed class AnimationTests
{
    private const string Model = "models/bend.gltf";

    /// <summary>The strip, spawned, with a camera looking at its middle over black.</summary>
    private static Entity Spawn(EcsWorld ecs)
    {
        var camera = Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0f, 0f, 0f, 1f) });
        ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 1f, 6f), new Vec3(0f, 1f, 0f), Vec3.UnitY));

        return ecs.SpawnScene(AssetServer.LoadGltfScene(Model));
    }

    /// <summary>How many pixels differ between two pictures of the same size.</summary>
    private static int Differing(CapturedImage a, CapturedImage b)
    {
        var differing = 0;

        for (uint y = 0; y < a.Height; y++)
        {
            for (uint x = 0; x < a.Width; x++)
            {
                var (ar, ag, ab, _) = a.At(x, y);
                var (br, bg, bb, _) = b.At(x, y);
                if (Math.Abs(ar - br) + Math.Abs(ag - bg) + Math.Abs(ab - bb) > 60) differing++;
            }
        }

        return differing;
    }

    /// <summary>
    /// The clips come back by name, and the strip held halfway through its bend is drawn in a
    /// different shape than at rest, which is skinning reaching the picture.
    /// </summary>
    [SkippableFact]
    public void AClipHeldHalfwayDrawsTheModelInAnotherPose()
    {
        Needs.Renderer();

        var scene = Entity.None;
        IReadOnlyList<string> clips = [];
        AnimationState? state = null;

        var run = new PictureRun { Scene = ecs => scene = Spawn(ecs) };

        run.Until("the clips arrive", _ => Animation.TryClips(scene, out clips))
            .Wait(ShaderMaterialTests.Settled)
            .Capture("rest")
            .Do("bending", _ =>
            {
                Assert.True(Animation.Play(scene, "Bend"));
                Animation.Pause(scene);
                Animation.Seek(scene, 0.5f);
            })
            .Wait(10)
            .Do("reading the state", _ => state = Animation.StateOf(scene))
            .Capture("bent")
            .Go();

        Assert.Equal(["Bend", "Sway"], clips);

        Assert.NotNull(state);
        Assert.Equal("Bend", state.Value.Clip);
        Assert.True(state.Value.Paused);
        Assert.InRange(state.Value.Seconds, 0.49f, 0.51f);

        var rest = run.Picture("rest");
        var bent = run.Picture("bent");

        // Something is drawn at all, a light grey once tonemapped, on black, so the comparison is
        // of the strip.
        Assert.True(PictureRun.Count(rest, (r, g, b) => r > 120 && g > 120 && b > 120) > 50, "the strip at rest was not drawn");
        Assert.True(Differing(rest, bent) > 50, "the strip bent halfway was drawn as it was at rest");
    }

    /// <summary>
    /// A clip played once says so when it ends, a clip that repeats does not, and an unknown clip
    /// is refused with the names there are.
    /// </summary>
    [SkippableFact]
    public void AClipPlayedOnceSaysWhenItEnds()
    {
        Needs.Renderer();

        var scene = Entity.None;
        var finished = new List<AnimationFinished>();
        ArgumentException? refused = null;

        var run = new PictureRun
        {
            Scene = ecs => scene = Spawn(ecs),
            EachFrame = world =>
            {
                foreach (var message in world.Resource<MessageBus>().Read<AnimationFinished>()) finished.Add(message);
            },
        };

        run.Until("the clips arrive", world => Animation.TryClips(scene, out _))
            .Do("swaying forever and bending once, fast", _ =>
            {
                refused = Assert.Throws<ArgumentException>(() => Animation.Play(scene, "Wave"));
                Animation.Play(scene, "Sway", new AnimationSettings { Repeat = true, Speed = 4f });
                Animation.Play(scene, "Bend", new AnimationSettings { Speed = 4f, Blend = 0.1f });
            })
            .Until("the bend ends", _ => finished.Count > 0)
            .Wait(30)
            .Go();

        Assert.Contains("Bend, Sway", refused!.Message);

        // Once, for the clip that plays once, and not for the one it replaced.
        var ended = Assert.Single(finished);
        Assert.Equal(scene, ended.Scene);
        Assert.Equal("Bend", ended.Clip);
    }

    /// <summary>
    /// A clip played a number of times finishes after that many plays and says so once, and one
    /// set to play for ever while it plays goes on past where it would have ended.
    /// </summary>
    [SkippableFact]
    public void AClipPlaysTheTimesItIsGivenOrForEverWhenSetSo()
    {
        Needs.Renderer();

        var scene = Entity.None;
        var finished = new List<AnimationFinished>();
        AnimationState? counted = null, forever = null;

        var run = new PictureRun
        {
            Scene = ecs => scene = Spawn(ecs),
            EachFrame = world =>
            {
                foreach (var message in world.Resource<MessageBus>().Read<AnimationFinished>()) finished.Add(message);
            },
        };

        run.Until("the clips arrive", world => Animation.TryClips(scene, out _))
            .Do("bending three times, fast", _ => Animation.Play(scene, "Bend", new AnimationSettings { Times = 3, Speed = 8f }))
            .Until("the bends end", _ => finished.Count > 0)
            .Do("reading it", _ => counted = Animation.StateOf(scene))
            .Do("bending again, then for ever", _ =>
            {
                Animation.Play(scene, "Bend", new AnimationSettings { Speed = 8f });
                Animation.SetRepeat(scene, 0);
            })
            .Wait(120)
            .Do("reading it again", _ => forever = Animation.StateOf(scene))
            .Go();

        Assert.Single(finished);
        Assert.Equal(3u, counted!.Value.Completions);
        Assert.False(forever!.Value.Finished);
        Assert.True(forever.Value.Completions >= 2, $"the clip set to play for ever played {forever.Value.Completions} times");
    }

    /// <summary>An entity no scene was spawned under has nothing to animate.</summary>
    [SkippableFact]
    public void AnEntityWithNoSceneIsRefused()
    {
        Needs.Renderer();

        BevyNativeException? refused = null;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var plain = ecs.Spawn();
                refused = Assert.Throws<BevyNativeException>(() => Animation.TryClips(plain, out _));
            },
        };

        run.Wait(2).Go();

        Assert.Equal(NativeStatus.NotPresent, refused!.Status);
    }

    /// <summary>
    /// A file animating two things apart has a player for each, and a clip moving both moves both,
    /// while a clip the file left unnamed is offered by the name Bevy gives it.
    /// </summary>
    [SkippableFact]
    public void EveryAnimatedRootPlaysAndAnUnnamedClipIsOffered()
    {
        Needs.Renderer();

        var scene = Entity.None;
        IReadOnlyList<string> clips = [];

        var run = new PictureRun
        {
            Width = 128,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0f, 0f, 0f, 1f) });
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 0f, 6f), Vec3.Zero, Vec3.UnitY));
                scene = ecs.SpawnScene(AssetServer.LoadGltfScene("models/pair.gltf"));
            },
        };

        run.Until("the clips arrive", _ => Animation.TryClips(scene, out clips))
            .Wait(ShaderMaterialTests.Settled)
            .Capture("rest")
            .Do("spinning both", _ =>
            {
                Animation.Play(scene, "Spin");
                Animation.Pause(scene);
                Animation.Seek(scene, 0.5f);
            })
            .Wait(10)
            .Capture("spun")
            .Go();

        Assert.Equal(["Spin", "Animation1"], clips);

        var rest = run.Picture("rest");
        var spun = run.Picture("spun");

        // Each half of the picture holds one strip, and each moved.
        int Moved(uint from, uint to)
        {
            var count = 0;
            for (var y = 0u; y < rest.Height; y++)
            {
                for (var x = from; x < to; x++)
                {
                    var (ar, _, _, _) = rest.At(x, y);
                    var (br, _, _, _) = spun.At(x, y);
                    if (Math.Abs(ar - br) > 60) count++;
                }
            }

            return count;
        }

        Assert.True(Moved(0, rest.Width / 2) > 30, "the left strip did not turn");
        Assert.True(Moved(rest.Width / 2, rest.Width) > 30, "the right strip did not turn");
    }
}
