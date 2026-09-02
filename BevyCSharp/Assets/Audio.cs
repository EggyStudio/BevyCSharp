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

    /// <summary>
    /// Place the sound in the world rather than in both ears equally.
    /// </summary>
    /// <remarks>
    /// A spatial sound is heard from where its entity's <see cref="Transform"/> is, quieter with
    /// distance and further to one side as it moves across. It takes two things: this, and an
    /// entity to hear from, which <see cref="Audio.SetListener"/> nominates.
    /// </remarks>
    public bool Spatial { get; set; }

    /// <summary>
    /// Scale applied to the distance between the sound and the listener.
    /// </summary>
    /// <remarks>
    /// A world measured in metres needs nothing here. One measured in pixels does: a sound a
    /// hundred units away would otherwise be inaudible, and a scale of <c>0.01</c> makes that a
    /// metre. Zero leaves Bevy's own scale in place.
    /// </remarks>
    public float SpatialScale { get; set; }

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
            Spatial = settings.Spatial ? 1 : 0,
            SpatialScale = settings.SpatialScale,
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

    /// <summary>
    /// Makes an entity the ear spatial sound is heard from.
    /// </summary>
    /// <remarks>
    /// Usually the camera, so that what is heard follows what is seen. One entity at a time:
    /// with several, Bevy hears from whichever it finds first.
    /// </remarks>
    /// <param name="entity">The entity to listen from. It is given a transform if it has none.</param>
    /// <param name="earGap">
    /// Distance between the two ears in world units, which is how pronounced the stereo is. Zero
    /// leaves Bevy's own.
    /// </param>
    /// <exception cref="BevyNativeException">The entity is gone, or this build has no audio.</exception>
    /// <example>
    /// <code>
    /// var camera = Render.SpawnCamera3d();
    /// Audio.SetListener(camera);
    ///
    /// var engine = Audio.Play(hum, new AudioSettings { Mode = PlaybackMode.Loop, Spatial = true });
    /// ctx.Ecs.Add(engine, Transform.At(4f, 0f, -2f));
    /// </code>
    /// </example>
    public static void SetListener(Entity entity, float earGap = 0f) =>
        Native.Check(
            Native.bcs_audio_listener(entity.Bits, earGap), $"listening from {entity}");

    /// <summary>
    /// How far into its clip a sound has played, in seconds.
    /// </summary>
    /// <remarks>
    /// Reaches the sink Bevy attaches once playback has started, so a sound asked in the frame it
    /// was started in reports that it carries none yet.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The entity is gone, is not playing yet, or this build has no audio.
    /// </exception>
    public static float PositionOf(Entity playing)
    {
        float seconds;
        Native.Check(
            Native.bcs_audio_position(playing.Bits, &seconds), $"reading the position of {playing}");

        return seconds;
    }

    /// <summary>
    /// Moves playback to a point in the clip, in seconds from its start.
    /// </summary>
    /// <remarks>
    /// <para>
    /// With <see cref="PositionOf"/> this is what survives a pause across a scene change: read
    /// the position, stop the sound, and seek the new one back to it.
    /// </para>
    /// <para>
    /// A sound playing on <see cref="PlaybackMode.Loop"/> refuses to be sought. Looping keeps the
    /// decoded samples around so the clip can start again, and what holds them has no way to move
    /// within them. Music that has to resume where it left off is played once and restarted, not
    /// looped.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The entity is gone, is not playing yet, the sound is looping, or this build has no audio.
    /// </exception>
    public static void Seek(Entity playing, float seconds) =>
        Native.Check(
            Native.bcs_audio_seek(playing.Bits, seconds), $"seeking {playing} to {seconds}s");

    /// <summary>
    /// Scales every sound at once, which is what a settings screen changes.
    /// </summary>
    /// <remarks>
    /// Multiplied with each sound's own volume rather than replacing it, so the mix a game set up
    /// survives the master slider being moved.
    /// </remarks>
    /// <param name="volume">1 leaves everything as mixed, 0 is silence.</param>
    /// <exception cref="BevyNativeException">
    /// The volume is negative, or this build has no audio.
    /// </exception>
    public static void SetGlobalVolume(float volume) =>
        Native.Check(Native.bcs_audio_global_volume(volume), "setting the global volume");
}
