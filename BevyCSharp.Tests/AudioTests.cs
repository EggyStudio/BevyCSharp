using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers playing sound.
/// </summary>
/// <remarks>
/// Whether anything is audible needs a sound card and an ear. What is checked here is that a clip
/// loads, that playing one produces an entity that behaves like any other, and that a build
/// without audio refuses rather than pretending.
/// </remarks>
[Collection("engine")]
public sealed class AudioTests
{
    private const string Clip = "sounds/beep.wav";

    /// <summary>Decibels and multipliers convert both ways, and zero has no decibels.</summary>
    /// <remarks>
    /// Arithmetic rather than engine behavior, and worth pinning because the exponent is easy to
    /// write with the wrong divisor. Twenty is for amplitude, where ten would be for power.
    /// </remarks>
    [Fact]
    public void VolumeAndDecibelsConvertBothWays()
    {
        Assert.Equal(1f, Audio.VolumeFromDecibels(0f), 4);
        Assert.Equal(0.5f, Audio.VolumeFromDecibels(-6.0206f), 3);
        Assert.Equal(2f, Audio.VolumeFromDecibels(6.0206f), 3);

        Assert.Equal(0f, Audio.DecibelsFromVolume(1f), 4);
        Assert.Equal(-6.0206f, Audio.DecibelsFromVolume(0.5f), 3);

        // Round trips, which is the property a settings slider depends on.
        Assert.Equal(0.35f, Audio.VolumeFromDecibels(Audio.DecibelsFromVolume(0.35f)), 4);

        // And silence is infinitely quiet rather than a large negative number pretending so.
        Assert.Equal(float.NegativeInfinity, Audio.DecibelsFromVolume(0f));
    }

    [Fact]
    public void AClipLoads()
    {
        using var harness = new EngineHarness(frames: 40, fps: 240);
        if (!App.HasRenderer) return;

        var state = AssetLoadState.Loading;

        harness.OnContext(Stage.Startup, _ => _clip = AssetServer.Load(AssetKind.Audio, Clip));

        harness.OnContext(Stage.Update, ctx =>
        {
            state = _clip.State;
            if (state != AssetLoadState.Loading) ctx.Exit();
        });

        harness.Run();

        Assert.Equal(AssetLoadState.Loaded, state);
    }

    [Fact]
    public void APlayingSoundIsAnEntity()
    {
        // That makes it despawnable, taggable and queryable without a second API for sounds
        // specifically.
        using var harness = new EngineHarness(frames: 6);
        if (!App.HasRenderer) return;

        var playing = Entity.None;
        var alive = false;

        harness.OnContext(Stage.Startup, ctx =>
        {
            playing = Audio.Play(
                AssetServer.Load(AssetKind.Audio, Clip),
                new AudioSettings { Mode = PlaybackMode.Loop, Volume = 0f });

            ctx.Ecs.Add(playing, new Sounding());
        });

        harness.OnContext(Stage.Last, ctx =>
            alive = ctx.Ecs.IsAlive(playing) && ctx.Ecs.Has<Sounding>(playing));

        harness.Run();

        Assert.NotEqual(Entity.None, playing);
        Assert.True(alive);
    }

    [Fact]
    public void StoppingDespawnsWhatWasPlaying()
    {
        using var harness = new EngineHarness(frames: 8);
        if (!App.HasRenderer) return;

        var playing = Entity.None;
        var goneAfterStop = false;

        harness.OnContext(Stage.Startup, _ => playing = Audio.Play(
            AssetServer.Load(AssetKind.Audio, Clip),
            new AudioSettings { Mode = PlaybackMode.Loop, Volume = 0f }));

        harness.OnContext(Stage.Update, ctx =>
        {
            if (!ctx.Ecs.IsAlive(playing)) return;

            Audio.Stop(playing);
            goneAfterStop = !ctx.Ecs.IsAlive(playing);
        });

        harness.Run();

        Assert.True(goneAfterStop, "the entity survived being stopped");
    }

    [Fact]
    public void PlayingSomethingThatIsNotASoundIsRefused()
    {
        using var harness = new EngineHarness(frames: 3);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
        {
            var ex = Assert.Throws<BevyNativeException>(() => Audio.Play(AssetHandle.None));
            Assert.Equal(NativeStatus.Unsupported, ex.Status);
        });

        harness.Run();
    }

    [Fact]
    public void ControlNeedsTheSinkThatArrivesWithPlayback()
    {
        // Bevy attaches the sink once playback has started, so a call in the same frame reports
        // that rather than silently doing nothing. Worth pinning down, because it reads as a bug
        // otherwise.
        using var harness = new EngineHarness(frames: 4);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
        {
            var playing = Audio.Play(
                AssetServer.Load(AssetKind.Audio, Clip),
                new AudioSettings { Volume = 0f });

            var ex = Assert.Throws<BevyNativeException>(() => Audio.SetVolume(playing, 0.5f));
            Assert.Equal(NativeStatus.NotPresent, ex.Status);
        });

        harness.Run();
    }

    [Fact]
    public void ASpatialSoundIsPlacedByItsTransform()
    {
        using var harness = new EngineHarness(frames: 6);
        if (!App.HasRenderer) return;

        var engine = Entity.None;
        var placed = false;

        harness.OnContext(Stage.Startup, ctx =>
        {
            // The ear. Usually the camera, so what is heard follows what is seen.
            var listener = ctx.Ecs.Spawn();
            Audio.SetListener(listener, earGap: 3f);

            // And the long form, which places each ear rather than spacing them on one axis. The
            // second call replaces the first, as a listener that has turned its head would every
            // frame.
            Audio.SetListener(
                listener,
                new Vec3(-1.5f, 0f, 0.2f),
                new Vec3(1.5f, 0f, 0.2f));

            engine = Audio.Play(
                AssetServer.Load(AssetKind.Audio, Clip),
                new AudioSettings
                {
                    Mode = PlaybackMode.Loop,
                    Volume = 0f,
                    Spatial = true,
                    SpatialScale = 0.01f,
                });

            // A spatial sound is given a transform to be moved by, which places it.
            ctx.Ecs.Add(engine, Transform.At(4f, 0f, -2f));
        });

        harness.OnContext(Stage.Last, ctx =>
            placed = ctx.Ecs.IsAlive(engine)
                && ctx.Ecs.GetRef<Transform>(engine).Translation == new Vec3(4f, 0f, -2f));

        harness.Run();

        Assert.True(placed, "the spatial sound did not keep the place it was put");
    }

    [Fact]
    public void ListeningFromSomethingThatIsGoneIsRefused()
    {
        using var harness = new EngineHarness(frames: 3);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
        {
            var gone = Assert.Throws<BevyNativeException>(() => Audio.SetListener(Entity.None));
            Assert.Equal(NativeStatus.NoEntity, gone.Status);
        });

        harness.Run();
    }

    [Fact]
    public void PositionAndSeekNeedTheSinkThatArrivesWithPlayback()
    {
        // The same rule the volume follows. The sink knows where a clip is, and it is attached once
        // playback has started. A machine with no audio device never attaches one at all, which is
        // why the answer is checked for being refused rather than for a number.
        using var harness = new EngineHarness(frames: 4);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
        {
            var playing = Audio.Play(
                AssetServer.Load(AssetKind.Audio, Clip),
                new AudioSettings { Volume = 0f });

            var position = Assert.Throws<BevyNativeException>(() => Audio.PositionOf(playing));
            Assert.Equal(NativeStatus.NotPresent, position.Status);

            var seek = Assert.Throws<BevyNativeException>(() => Audio.Seek(playing, 0.5f));
            Assert.Equal(NativeStatus.NotPresent, seek.Status);

            var gone = Assert.Throws<BevyNativeException>(() => Audio.PositionOf(Entity.None));
            Assert.Equal(NativeStatus.NoEntity, gone.Status);
        });

        harness.Run();
    }

    [Fact]
    public void TheMasterVolumeScalesEverythingAtOnce()
    {
        using var harness = new EngineHarness(frames: 4);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
        {
            Audio.SetGlobalVolume(0f);
            Audio.SetGlobalVolume(0.4f);
            Audio.SetGlobalVolume(1f);

            // Negative loudness means nothing, so it is refused rather than clamped.
            var negative = Assert.Throws<BevyNativeException>(() => Audio.SetGlobalVolume(-1f));
            Assert.Equal(NativeStatus.NullArgument, negative.Status);
        });

        harness.Run();
    }

    [Fact]
    public void ASoundReportsWhereItIsUntilItIsAskedToLoop()
    {
        // Both halves need a sound device, because without one Bevy attaches no sink and there is
        // nothing to ask, which is why the assertions are guarded on having got an answer.
        using var harness = new EngineHarness(frames: 400, fps: 240);
        if (!App.HasRenderer) return;

        var playing = Entity.None;
        var looping = Entity.None;
        var position = -1f;
        var loopSeek = NativeStatus.Ok;
        var plainSeek = NativeStatus.Ok;

        harness.OnContext(Stage.Startup, _ =>
        {
            var clip = AssetServer.Load(AssetKind.Audio, Clip);

            playing = Audio.Play(clip, new AudioSettings { Volume = 0f });
            looping = Audio.Play(
                clip, new AudioSettings { Mode = PlaybackMode.Loop, Volume = 0f });
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            if (position >= 0f) return;

            try
            {
                position = Audio.PositionOf(playing);
            }
            catch (BevyNativeException)
            {
                // The sink arrives with playback, and only if there is a device to play on.
                return;
            }

            plainSeek = Seeking(playing);
            loopSeek = Seeking(looping);
            ctx.Exit();
        });

        harness.Run();

        if (position < 0f) return;

        Assert.True(position >= 0f);
        Assert.Equal(NativeStatus.Ok, plainSeek);

        // Looping keeps the decoded samples so the clip can start again, and what holds them
        // cannot move within them. Pinned down because it reads as a bug otherwise.
        Assert.Equal(NativeStatus.InvalidState, loopSeek);
    }

    /// <summary>Seeks a sound to its start and reports the status rather than throwing.</summary>
    private static int Seeking(Entity playing)
    {
        try
        {
            Audio.Seek(playing, 0f);
            return NativeStatus.Ok;
        }
        catch (BevyNativeException ex)
        {
            return ex.Status;
        }
    }

    /// <summary>Tags a sound entity, to show it carries components like any other.</summary>
    private struct Sounding;

    private static AssetHandle _clip;
    /// <summary>
    /// A sound can be told to play part of a clip rather than all of it.
    /// </summary>
    /// <remarks>
    /// What lets one file hold several effects, where the alternative is a file per effect and a
    /// decode each. Whether the right samples come out needs an ear; what is checked here is that
    /// the window is obeyed, by playing a second of tone twice and letting both clean up after
    /// themselves. The one given a fifth of a second is gone long before the one given all of it.
    /// </remarks>
    [Fact]
    public void ASoundCanPlayPartOfAClip()
    {
        if (!App.HasRenderer) return;

        using var harness = new EngineHarness(frames: 40, fps: 60);

        var clipped = Entity.None;
        var whole = Entity.None;
        var started = false;
        bool? clippedGone = null;
        bool? wholeGone = null;

        harness.OnContext(Stage.Startup, _ =>
        {
            var clip = AssetServer.Load(AssetKind.Audio, "sounds/tone.wav");

            clipped = Audio.Play(clip, new AudioSettings
            {
                Mode = PlaybackMode.Despawn,
                Volume = 0f,
                Play = 0.2f,
            });

            whole = Audio.Play(
                clip, new AudioSettings { Mode = PlaybackMode.Despawn, Volume = 0f });
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            // Playback starts when the sink arrives, and only where there is a device to play on.
            if (!started)
            {
                try
                {
                    _ = Audio.PositionOf(whole);
                    started = true;
                }
                catch (BevyNativeException)
                {
                    return;
                }
            }

            // A third of a second in: past the window the first was given and well short of the
            // second's whole tone.
            if (ctx.Time.FrameCount != 30) return;

            clippedGone = !ctx.Ecs.IsAlive(clipped);
            wholeGone = !ctx.Ecs.IsAlive(whole);
        });

        harness.Run();

        // No device on this machine, so there was nothing to play.
        if (clippedGone is not { } clippedEnded || wholeGone is not { } wholeEnded) return;

        Assert.True(clippedEnded, "the sound given a fifth of a second was still playing");
        Assert.False(wholeEnded, "the sound given the whole tone ended early");
    }

}
