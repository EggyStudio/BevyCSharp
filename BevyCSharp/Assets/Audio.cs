using Bevy.Interop;

namespace Bevy;

/// <summary>What happens when a sound reaches its end.</summary>
public enum PlaybackMode
{
    /// <summary>Play through and stop, leaving the entity in place.</summary>
    Once = 0,

    /// <summary>Start again, for music and ambience.</summary>
    Loop = 1,

    /// <summary>
    /// Play through, then despawn the entity.
    /// </summary>
    /// <remarks>
    /// What a one-shot effect wants: nothing has to remember to clean it up, and a game firing
    /// hundreds of them does not accumulate entities.
    /// </remarks>
    Despawn = 2,
}

/// <summary>How a sound should be played.</summary>
public sealed class AudioSettings
{
    /// <summary>What happens when it reaches the end.</summary>
    public PlaybackMode Mode { get; set; } = PlaybackMode.Once;

    /// <summary>Loudness, where 1 is the sound as recorded and 0 is silence.</summary>
    public float Volume { get; set; } = 1f;

    /// <summary>
    /// Playback rate. 1 is as recorded.
    /// </summary>
    /// <remarks>
    /// Changes pitch with it, because it resamples rather than time-stretches. Slight variation
    /// on a repeated effect is what keeps it from sounding mechanical.
    /// </remarks>
    public float Speed { get; set; } = 1f;

    /// <summary>Start paused, to be released later with <see cref="Audio.Resume"/>.</summary>
    public bool Paused { get; set; }

    /// <summary>Plays once and cleans up after itself.</summary>
    public static AudioSettings Effect => new() { Mode = PlaybackMode.Despawn };

    /// <summary>Loops quietly, for music.</summary>
    public static AudioSettings Music => new() { Mode = PlaybackMode.Loop, Volume = 0.5f };
}

/// <summary>
/// Plays sound.
/// </summary>
/// <remarks>
/// <para>
/// A sound that is playing is an entity, so it can be despawned, parented, tagged with your own
/// components and found by a query like anything else. <see cref="Play(AssetHandle, AudioSettings)"/>
/// hands that entity back.
/// </para>
/// <para>
/// Needs a render build. Sound is compiled into the same profile as the renderer because it is
/// the profile that takes a system library, and a windowless run reports that rather than
/// pretending to play.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var clip = AssetServer.Load(AssetKind.Audio, "sounds/hit.ogg");
///
/// Audio.Play(clip, AudioSettings.Effect);
/// var music = Audio.Play(theme, AudioSettings.Music);
/// Audio.SetVolume(music, 0.2f);
/// </code>
/// </example>
public static unsafe class Audio
{
    /// <summary>Plays a sound and returns the entity playing it.</summary>
    /// <exception cref="BevyNativeException">The handle names no sound, or this build has no audio.</exception>
    public static Entity Play(AssetHandle clip) => Play(clip, new AudioSettings());

    /// <summary>Plays a sound as <paramref name="settings"/> describes.</summary>
    /// <remarks>
    /// The sound need not have finished loading; playback begins when it has.
    /// </remarks>
    /// <exception cref="BevyNativeException">The handle names no sound, or this build has no audio.</exception>
    public static Entity Play(AssetHandle clip, AudioSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeAudioConfig
        {
            Mode = (int)settings.Mode,
            Volume = settings.Volume,
            Speed = settings.Speed,
            Paused = settings.Paused ? 1 : 0,
        };

        var bits = Native.bcs_audio_play(clip.Key, &native);
        if (bits == 0)
            throw new BevyNativeException(
                NativeStatus.Unsupported,
                $"Playing {clip} failed: either it names no loaded sound, or this native build "
                + "has no audio. Rebuild the bridge with build/build-native.sh --render.");

        return new Entity(bits);
    }

    /// <summary>Sets a playing sound's volume.</summary>
    /// <remarks>
    /// Reaches the sink Bevy attaches once playback has started, so this does nothing in the same
    /// frame the sound was started in.
    /// </remarks>
    public static void SetVolume(Entity playing, float volume) =>
        Native.Check(
            Native.bcs_audio_control(playing.Bits, volume, 0),
            $"setting the volume of {playing}");

    /// <summary>Pauses a playing sound, keeping its place.</summary>
    public static void Pause(Entity playing, float volume = 1f) =>
        Native.Check(
            Native.bcs_audio_control(playing.Bits, volume, 1),
            $"pausing {playing}");

    /// <summary>Resumes a paused sound.</summary>
    public static void Resume(Entity playing, float volume = 1f) =>
        Native.Check(
            Native.bcs_audio_control(playing.Bits, volume, 0),
            $"resuming {playing}");

    /// <summary>Stops a sound and despawns the entity playing it.</summary>
    public static void Stop(Entity playing) =>
        Native.Check(Native.bcs_audio_stop(playing.Bits), $"stopping {playing}");
}
