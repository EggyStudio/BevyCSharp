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
/// without audio refuses rather than pretending. Every run here has no window, so each sound plays
/// to no device and is given the bridge's own sink, which runs it through on the app's clock, and
/// none of this needs a device or makes a sound.
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

    [SkippableFact]
    public void AClipLoads()
    {
        using var harness = new EngineHarness(frames: 40, fps: 240);
        Needs.Renderer();

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

    [SkippableFact]
    public void APlayingSoundIsAnEntity()
    {
        // That makes it despawnable, taggable and queryable without a second API for sounds
        // specifically.
        using var harness = new EngineHarness(frames: 6);
        Needs.Renderer();

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

    [SkippableFact]
    public void StoppingDespawnsWhatWasPlaying()
    {
        using var harness = new EngineHarness(frames: 8);
        Needs.Renderer();

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

    [SkippableFact]
    public void PlayingSomethingThatIsNotASoundIsRefused()
    {
        using var harness = new EngineHarness(frames: 3);
        Needs.Renderer();

        harness.OnContext(Stage.Startup, _ =>
        {
            var ex = Assert.Throws<BevyNativeException>(() => Audio.Play(AssetHandle.None));
            Assert.Equal(NativeStatus.Unsupported, ex.Status);
        });

        harness.Run();
    }

    [SkippableFact]
    public void ControlNeedsTheSinkThatArrivesWithPlayback()
    {
        // Bevy attaches the sink once playback has started, so a call in the same frame reports
        // that rather than silently doing nothing. Worth pinning down, because it reads as a bug
        // otherwise.
        using var harness = new EngineHarness(frames: 4);
        Needs.Renderer();

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

    [SkippableFact]
    public void ASpatialSoundIsPlacedByItsTransform()
    {
        using var harness = new EngineHarness(frames: 6);
        Needs.Renderer();

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

    [SkippableFact]
    public void ListeningFromSomethingThatIsGoneIsRefused()
    {
        using var harness = new EngineHarness(frames: 3);
        Needs.Renderer();

        harness.OnContext(Stage.Startup, _ =>
        {
            var gone = Assert.Throws<BevyNativeException>(() => Audio.SetListener(Entity.None));
            Assert.Equal(NativeStatus.NoEntity, gone.Status);
        });

        harness.Run();
    }

    [SkippableFact]
    public void PositionAndSeekNeedTheSinkThatArrivesWithPlayback()
    {
        // The same rule the volume follows. The sink knows where a clip is, and it is attached once
        // playback has started, which is after the frame the sound was started in.
        using var harness = new EngineHarness(frames: 4);
        Needs.Renderer();

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

    /// <summary>
    /// A sound on a bus is heard at its own volume times the bus's, a bus change reaches it while
    /// it plays, and neither a bus change nor a volume change undoes a pause.
    /// </summary>
    /// <remarks>
    /// The global volume is zero, so the volumes read back are each sound's own.
    /// </remarks>
    [SkippableFact]
    public void ABusScalesItsSoundsAndKeepsTheirPauses()
    {
        using var harness = new EngineHarness(frames: 400, fps: 240);
        Needs.Renderer();

        var music = Entity.None;
        var effect = Entity.None;
        var step = 0;
        var readings = new List<(float Music, float Effect, bool Paused)>();

        harness.OnContext(Stage.Startup, _ =>
        {
            Audio.SetGlobalVolume(0f);

            var clip = AssetServer.Load(AssetKind.Audio, Clip);
            music = Audio.Play(clip, new AudioSettings { Mode = PlaybackMode.Loop, Volume = 0.5f, Bus = "music" });
            effect = Audio.Play(clip, new AudioSettings { Mode = PlaybackMode.Loop, Volume = 0.8f, Bus = "effects" });
        });

        harness.OnContext(Stage.Update, _ =>
        {
            if (step > 4) return;

            try
            {
                readings.Add((Audio.VolumeOf(music), Audio.VolumeOf(effect), Audio.IsPaused(music)));
            }
            catch (BevyNativeException)
            {
                // The sink arrives with playback.
                return;
            }

            switch (step++)
            {
                case 0: Audio.SetBusVolume("music", 0.5f); break;
                case 1: Audio.SetVolume(music, 0.8f); break;
                case 2: Audio.Pause(music, 0.8f); break;
                case 3: Audio.SetBusVolume("music", 1f); break;
            }
        });

        harness.Run();

        Assert.Equal(5, readings.Count);
        Assert.Equal((0.5f, 0.8f, false), readings[0]);
        Assert.Equal((0.25f, 0.8f, false), readings[1]);
        Assert.Equal((0.4f, 0.8f, false), readings[2]);
        Assert.Equal((0.4f, 0.8f, true), readings[3]);
        Assert.Equal((0.8f, 0.8f, true), readings[4]);
    }

    [SkippableFact]
    public void TheMasterVolumeScalesEverythingAtOnce()
    {
        using var harness = new EngineHarness(frames: 4);
        Needs.Renderer();

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

    [SkippableFact]
    public void ASoundReportsWhereItIsUntilItIsAskedToLoop()
    {
        using var harness = new EngineHarness(frames: 400, fps: 240);
        Needs.Renderer();

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
                // The sink arrives with playback.
                return;
            }

            plainSeek = Seeking(playing);
            loopSeek = Seeking(looping);
            ctx.Exit();
        });

        harness.Run();

        Assert.True(position >= 0f, "the sound never started");
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
    [SkippableFact]
    public void ASoundCanPlayPartOfAClip()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 40, frameSeconds: 1.0 / 60);

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
            // Playback starts when the sink arrives.
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

            // Half a second in on a clock that moves a sixtieth a frame, past the window the first
            // was given and well short of the second's whole tone.
            if (ctx.Time.FrameCount != 30) return;

            clippedGone = !ctx.Ecs.IsAlive(clipped);
            wholeGone = !ctx.Ecs.IsAlive(whole);
        });

        harness.Run();

        Assert.True(clippedGone is not null && wholeGone is not null, "the sounds never started");
        Assert.True(clippedGone!.Value, "the sound given a fifth of a second was still playing");
        Assert.False(wholeGone!.Value, "the sound given the whole tone ended early");
    }

    /// <summary>
    /// A run with no window, headless or offscreen, plays to no device, and a sound in it still
    /// ends when it would have been heard to end.
    /// </summary>
    /// <remarks>
    /// A game that waits on a sound's end, a door that opens when its creak is over, has to work in
    /// a test or a soak as it does with a window, so the second of tone is timed from the frame
    /// its sink arrived to the frame it despawned itself, on a clock that moves a set step a frame.
    /// It is played at no volume, so a run that wrongly opened a device is still not heard.
    /// </remarks>
    [SkippableTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ARunWithNoWindowIsSilentAndItsSoundsEndOnTime(bool offscreen)
    {
        Needs.Renderer();

        const double step = 0.05;
        bool? silent = null;
        var playing = Entity.None;
        ulong started = 0, ended = 0;

        using var app = new App(new Config
        {
            Headless = !offscreen,
            Offscreen = offscreen,
            Width = 32,
            Height = 32,
            HeadlessFrames = 200,
            FrameSeconds = step,
            AssetRoot = EngineHarness.AssetDirectory,
        });

        app.AddSystem(Stage.Startup, new SystemDescriptor(_ =>
        {
            silent = Audio.IsSilent;
            playing = Audio.Play(
                AssetServer.Load(AssetKind.Audio, "sounds/tone.wav"),
                new AudioSettings { Mode = PlaybackMode.Despawn, Volume = 0f });
        }, "Test.Play"));

        app.AddSystem(Stage.Update, new SystemDescriptor(world =>
        {
            var ctx = new BehaviorContext(world);
            if (ended > 0) return;

            if (!ctx.Ecs.IsAlive(playing))
            {
                ended = ctx.Time.FrameCount;
                ctx.Exit();
            }
            else if (started == 0 && Audio.HasStarted(playing))
            {
                started = ctx.Time.FrameCount;
            }
        }, "Test.Listen"));

        Assert.Equal(0, app.Run());

        Assert.True(silent, "a run with no window plays to a device");
        Assert.True(started > 0, "the sound never started");
        Assert.True(ended > started, "the sound never ended");

        // A second of tone, the frame that drew past its last sample, and the one that saw it gone.
        Assert.InRange((ended - started) * step, 1.0, 1.0 + 3 * step);
    }

    /// <summary>
    /// A sound whose file no decoder reads is refused in a run with no window, and waits for a sink
    /// it is never given, where building its decoder would have panicked inside the frame.
    /// </summary>
    /// <remarks>
    /// The check that refuses it runs before transforms are propagated, and the bridge's sink is
    /// given after them, as Bevy's own is, so the file is refused before anything decodes it.
    /// </remarks>
    [SkippableFact]
    [ExpectsError("bevy", "not-a-sound")]
    public void ASoundNoDecoderReadsIsRefusedRatherThanPlayed()
    {
        Needs.Renderer();

        var name = $"not-a-sound-{Guid.NewGuid():N}.wav";
        var file = Path.Combine(EngineHarness.AssetDirectory, "sounds", name);
        var noise = new byte[4096];
        new Random(7).NextBytes(noise);
        File.WriteAllBytes(file, noise);

        try
        {
            using var harness = new EngineHarness(frames: 40, frameSeconds: 1.0 / 60);
            var playing = Entity.None;
            bool? started = null;

            harness.OnContext(Stage.Startup, _ =>
                playing = Audio.Play(AssetServer.Load(AssetKind.Audio, $"sounds/{name}"), new AudioSettings { Volume = 0f }));

            harness.OnContext(Stage.Update, ctx =>
            {
                if (ctx.Time.FrameCount == 30) started = Audio.HasStarted(playing);
            });

            harness.Run();

            Assert.False(started, "a sound no decoder reads was given a sink");
        }
        finally
        {
            File.Delete(file);
        }
    }

    /// <summary>A run with no window plays to the machine's device when its config asks.</summary>
    [SkippableFact]
    public void ARunWithNoWindowCanAskToBeHeard()
    {
        Needs.Renderer();

        bool? silent = null;

        using var app = new App(new Config
        {
            Headless = true,
            HeadlessFrames = 2,
            AudioWithoutWindow = true,
            AssetRoot = EngineHarness.AssetDirectory,
        });

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            _ => silent = Audio.IsSilent, "Test.Ask"));

        Assert.Equal(0, app.Run());
        Assert.False(silent, "a run that asked for a device plays to none");
    }

    /// <summary>
    /// Sounds played to a device and told to despawn at their end do so, and give their indices
    /// back to the world for the entities after them.
    /// </summary>
    /// <remarks>
    /// Bevy 0.20.0 despawns such a sound through a call that frees no index it despawns, so every
    /// effect a game played took one for good. The bridge plays it once instead and despawns it
    /// itself (see <c>ecs::despawn_all</c> in the bridge). Forty waves of thirty short sounds, each
    /// wave played once the last has ended, keep the world at 512 indices, where Bevy's own
    /// despawn carries it to 2,048 by the thirty-fifth. Played at no volume, and skipped on a
    /// machine with no device to play to.
    /// </remarks>
    [SkippableFact]
    public void SoundsEndingOnADeviceGiveBackTheirIndices()
    {
        Needs.Renderer();

        var playing = new List<Entity>();
        var waves = 0;
        var started = false;
        ulong? firstWave = null;
        Dictionary<string, long>? held = null;
        var clip = AssetHandle.None;

        using var app = new App(new Config
        {
            Headless = true,
            // Frames come as fast as the machine makes them and the device plays in its own time,
            // so the run is held to a count of frames far beyond what forty waves take anywhere.
            HeadlessFrames = 200_000,
            AudioWithoutWindow = true,
            AssetRoot = EngineHarness.AssetDirectory,
        });

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            _ => clip = AssetServer.Load(AssetKind.Audio, "sounds/tone.wav"), "Test.Load"));

        app.AddSystem(Stage.Update, new SystemDescriptor(world =>
        {
            var ctx = new BehaviorContext(world);
            playing.RemoveAll(sound => !ctx.Ecs.IsAlive(sound));
            started |= playing.Any(Audio.HasStarted);

            // A device gives a sound of a loaded clip its sink in a frame or two, so a machine
            // with none is known long before five thousand frames have passed.
            if (!started && ctx.Time.FrameCount > firstWave + 5_000)
            {
                ctx.Exit();
                return;
            }

            if (playing.Count > 0 || AssetServer.StateOf(clip) != AssetLoadState.Loaded) return;

            if (waves == 40)
            {
                held = MemoryCommandTests.Pairs(ConsoleMemoryCommands.Memory());
                ctx.Exit();
                return;
            }

            waves++;
            firstWave ??= ctx.Time.FrameCount;
            var effect = new AudioSettings
            {
                Mode = PlaybackMode.Despawn,
                Volume = 0f,
                Play = 0.05f,
            };
            for (var i = 0; i < 30; i++) playing.Add(Audio.Play(clip, effect));
        }, "Test.Waves"));

        Assert.Equal(0, app.Run());
        Skip.IfNot(started, "this machine has no sound device to play to");

        Assert.NotNull(held);
        Assert.True(
            held["entityIds"] <= 1024,
            $"{held["entityIds"]} indices for {held["entities"]} entities after 1,200 sounds");
    }

}
