using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers a playing sound's speed and mute, and asking whether it has its sink yet.</summary>
/// <remarks>
/// A sound has a sink only where the machine has a device to play on, which a test machine may not
/// have, so these hold what is true either way. A sound started this frame has no sink and says so, the
/// calls that reach the sink refuse until it has one, and a speed no sound can play at is refused
/// before any sink is looked for.
/// </remarks>
[Collection("engine")]
public sealed class AudioPlaybackTests
{
    private const string Clip = "sounds/beep.wav";

    /// <summary>A sound started this frame has no sink, its speed and mute wait for one, and a speed of zero is refused outright.</summary>
    [SkippableFact]
    public void ASoundStartedThisFrameWaitsForItsSinkAndAStillSpeedIsRefused()
    {
        Needs.Renderer();

        var ran = false;
        using var harness = new EngineHarness(frames: 3);
        harness.OnContext(Stage.Startup, _ =>
        {
            var playing = Audio.Play(AssetServer.Load(AssetKind.Audio, Clip), new AudioSettings { Volume = 0f });

            Assert.False(Audio.HasStarted(playing));
            Assert.Equal(NativeStatus.NotPresent, Assert.Throws<BevyNativeException>(() => Audio.SetSpeed(playing, 2f)).Status);
            Assert.Equal(NativeStatus.NotPresent, Assert.Throws<BevyNativeException>(() => Audio.SetMuted(playing, true)).Status);
            Assert.Equal(NativeStatus.NotPresent, Assert.Throws<BevyNativeException>(() => Audio.SpeedOf(playing)).Status);
            Assert.Equal(NativeStatus.InvalidState, Assert.Throws<BevyNativeException>(() => Audio.SetSpeed(playing, 0f)).Status);
            ran = true;
        });

        harness.Run();
        Assert.True(ran);
    }
}
