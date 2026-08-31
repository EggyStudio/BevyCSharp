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
        // Which is what makes it despawnable, taggable and queryable without a second API for
        // sounds specifically.
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
        // that rather than silently doing nothing. Worth pinning down: it reads as a bug
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

    /// <summary>Tags a sound entity, to show it carries components like any other.</summary>
    private struct Sounding;

    private static AssetHandle _clip;
}
