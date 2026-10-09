using Bevy.Interop;

namespace Bevy;

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
/// the profile that takes a system library, and a build without it reports that rather than
/// pretending to play.
/// </para>
/// <para>
/// A run with no window, headless or offscreen, plays every sound to no device unless
/// <see cref="Config.AudioWithoutWindow"/> asks for one, and its sounds still run their course on
/// the app's clock, so everything here answers as it would with a device. <see cref="IsSilent"/>
/// says which a run does.
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
    /// <summary>What the mixer knows of a sound: its bus, its own volume, and whether it is paused.</summary>
    private sealed record Mixed(string? Bus, float Volume, bool Paused);

    /// <summary>Every sound played since the app was made, by its entity's bits.</summary>
    /// <remarks>
    /// Kept so a bus can reach its sounds. A sound that ends on its own is not reported, so one is
    /// forgotten the first time reaching it finds its entity gone.
    /// </remarks>
    private static readonly Dictionary<ulong, Mixed> Sounds = [];

    /// <summary>Every bus somebody has set, by name.</summary>
    private static readonly Dictionary<string, float> Buses = new(StringComparer.Ordinal);

    /// <summary>Forgets every sound and bus, for an app starting from none.</summary>
    internal static void ResetMixer()
    {
        Sounds.Clear();
        Buses.Clear();
    }

    /// <summary>A bus's volume, which is one for a bus nobody has set and for no bus at all.</summary>
    public static float BusVolume(string? bus) =>
        bus is not null && Buses.TryGetValue(bus, out var volume) ? volume : 1f;

    /// <summary>
    /// Sets a bus's volume, which every sound on it is heard at times its own. Only valid inside a
    /// system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bevy has no mixer, so a bus is kept here: a name and a volume, and the sounds played on it
    /// through <see cref="AudioSettings.Bus"/>. Setting it reaches every sound on it now and every
    /// one played later, and a sound's own volume from <see cref="SetVolume"/> is kept apart, so a
    /// music slider and a fade on one track multiply rather than overwrite each other.
    /// <see cref="SetGlobalVolume"/> is over all of it.
    /// </para>
    /// <para>
    /// A sound started this frame has no sink yet to set, and is left at the volume it started at,
    /// which already took the bus into account.
    /// </para>
    /// </remarks>
    /// <param name="bus">Its name, such as <c>"music"</c>.</param>
    /// <param name="volume">1 leaves its sounds as mixed, 0 silences them.</param>
    public static void SetBusVolume(string bus, float volume)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentOutOfRangeException.ThrowIfNegative(volume);

        Buses[bus] = volume;

        foreach (var (bits, mixed) in Sounds.Where(sound => sound.Value.Bus == bus).ToList())
        {
            var answer = Native.bcs_audio_control(bits, mixed.Volume * volume, mixed.Paused ? 1 : 0);

            if (answer == NativeStatus.NoEntity) Sounds.Remove(bits);
        }
    }

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
            Volume = settings.Volume * BusVolume(settings.Bus),
            Speed = settings.Speed,
            Paused = settings.Paused ? 1 : 0,
            Spatial = settings.Spatial ? 1 : 0,
            SpatialScale = settings.SpatialScale,
            StartSeconds = settings.Start,
            PlaySeconds = settings.Play,
        };

        var bits = Native.bcs_audio_play(clip.Key, &native);
        if (bits == 0)
            throw new BevyNativeException(
                NativeStatus.Unsupported,
                $"Playing {clip} failed, because either it names no loaded sound or this native build "
                + "has no audio. Rebuild the bridge with build/build-native.sh --render.");

        Sounds[bits] = new Mixed(settings.Bus, settings.Volume, settings.Paused);
        return new Entity(bits);
    }

    /// <summary>Sets a playing sound's volume.</summary>
    /// <remarks>
    /// Reaches the sink Bevy attaches once playback has started, so this does nothing in the same
    /// frame the sound was started in. The sound is heard at this times its bus's volume.
    /// </remarks>
    public static void SetVolume(Entity playing, float volume) =>
        Control(playing, volume, paused: null, $"setting the volume of {playing}");

    /// <summary>Pauses a playing sound, keeping its place.</summary>
    public static void Pause(Entity playing, float volume = 1f) =>
        Control(playing, volume, paused: true, $"pausing {playing}");

    /// <summary>Resumes a paused sound.</summary>
    public static void Resume(Entity playing, float volume = 1f) =>
        Control(playing, volume, paused: false, $"resuming {playing}");

    /// <summary>
    /// Sets a sound's own volume and, where asked, whether it is paused, heard through its bus.
    /// </summary>
    /// <remarks>
    /// The bridge sets both at once, so a volume change keeps whatever the sound was last told
    /// about pausing rather than starting it again.
    /// </remarks>
    private static void Control(Entity playing, float volume, bool? paused, string doing)
    {
        var known = Sounds.GetValueOrDefault(playing.Bits) ?? new Mixed(null, volume, false);
        var now = known with { Volume = volume, Paused = paused ?? known.Paused };

        Native.Check(
            Native.bcs_audio_control(playing.Bits, volume * BusVolume(now.Bus), now.Paused ? 1 : 0),
            doing);

        Sounds[playing.Bits] = now;
    }

    /// <summary>A sound's volume, its own times its bus's, before the global volume.</summary>
    /// <remarks>
    /// Read once playback has started and Bevy has attached a sink, so a sound asked about in the
    /// frame it was started in reports that it carries none yet.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The entity is gone, is not playing yet, or this build has no audio.
    /// </exception>
    public static float VolumeOf(Entity playing)
    {
        float volume;
        Native.Check(Native.bcs_audio_state(playing.Bits, &volume, null), $"reading the volume of {playing}");
        return volume;
    }

    /// <summary>Whether Bevy has attached the sink that plays a sound, which the calls that reach the sink need.</summary>
    /// <remarks>
    /// A sound is given its sink the frame after it is started, once its clip has loaded. A run
    /// with a window on a machine with no device to play on, as a container often has not, gives
    /// it none, and a run with no window gives it one that plays to no device
    /// (<see cref="IsSilent"/>). A system that controls a sound asks this first, as Bevy's own skip
    /// a query for a sink that is not there.
    /// </remarks>
    /// <exception cref="BevyNativeException">The entity is gone, or this build has no audio.</exception>
    public static bool HasStarted(Entity playing)
    {
        var status = Native.bcs_audio_state(playing.Bits, null, null);
        if (status == NativeStatus.NotPresent) return false;
        Native.Check(status, $"asking whether {playing} has started");
        return true;
    }

    /// <summary>Whether a sound is paused.</summary>
    /// <exception cref="BevyNativeException">
    /// The entity is gone, is not playing yet, or this build has no audio.
    /// </exception>
    public static bool IsPaused(Entity playing)
    {
        int paused;
        Native.Check(Native.bcs_audio_state(playing.Bits, null, &paused), $"asking whether {playing} is paused");
        return paused != 0;
    }

    /// <summary>Stops a sound and despawns the entity playing it.</summary>
    public static void Stop(Entity playing)
    {
        Sounds.Remove(playing.Bits);
        Native.Check(Native.bcs_audio_stop(playing.Bits), $"stopping {playing}");
    }

    /// <summary>
    /// Makes an entity the ear spatial sound is heard from.
    /// </summary>
    /// <remarks>
    /// Usually the camera, so that what is heard follows what is seen. One entity at a time,
    /// because with several, Bevy hears from whichever it finds first.
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
    /// A volume multiplier from a number of decibels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every volume here is a multiplier, where one is unchanged and a half is half the amplitude.
    /// A mixer, a settings slider and anybody who has worked with sound think in decibels, because
    /// loudness is heard on a logarithmic scale and a linear slider spends most of its travel on
    /// differences nobody can hear.
    /// </para>
    /// <para>
    /// Zero decibels is unchanged, minus six is roughly half as loud, and minus eighty is near
    /// enough to silence for a fade to end on.
    /// </para>
    /// </remarks>
    /// <param name="decibels">How much louder or quieter than unchanged.</param>
    public static float VolumeFromDecibels(float decibels) => MathF.Pow(10f, decibels / 20f);

    /// <summary>
    /// The decibels a volume multiplier stands for.
    /// </summary>
    /// <remarks>
    /// The other way round, for showing a slider's position as a number somebody can read. A
    /// volume of zero has no answer, since silence is infinitely quiet, and this reports negative
    /// infinity rather than a large negative number pretending otherwise.
    /// </remarks>
    /// <param name="volume">The multiplier.</param>
    public static float DecibelsFromVolume(float volume) =>
        volume > 0f ? 20f * MathF.Log10(volume) : float.NegativeInfinity;

    /// <summary>
    /// Makes an entity the ear, with each ear placed exactly.
    /// </summary>
    /// <remarks>
    /// The long form of <see cref="SetListener(Entity, float)"/>, which puts the two ears a gap apart on the x
    /// axis. Placing them says where a head is facing as well as how wide it is, for a listener
    /// carried by a character rather than by a camera, or a first-person view with the ears behind
    /// the eyes.
    /// </remarks>
    /// <param name="entity">The entity to listen from. It is given a transform if it has none.</param>
    /// <param name="left">Where the left ear sits, relative to that entity.</param>
    /// <param name="right">Where the right ear sits.</param>
    /// <exception cref="BevyNativeException">The entity is gone, or this build has no audio.</exception>
    public static unsafe void SetListener(Entity entity, Vec3 left, Vec3 right)
    {
        var from = stackalloc float[3] { left.X, left.Y, left.Z };
        var to = stackalloc float[3] { right.X, right.Y, right.Z };

        Native.Check(
            Native.bcs_audio_listener_ears(entity.Bits, from, to),
            $"listening from {entity}");
    }

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
    /// With <see cref="PositionOf"/> this carries a pause across a scene change. Read the position,
    /// stop the sound, and seek the new one back to it.
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

    /// <summary>Sets how fast a playing sound plays, one as recorded, Bevy's <c>AudioSink::set_speed</c>.</summary>
    /// <remarks>
    /// The pitch goes with the speed, as a record played fast is higher, which Bevy's
    /// <c>audio_control</c> sweeps between a tenth and twice as fast. Reaches the sink Bevy attaches
    /// once playback has started, so a sound started this frame carries none yet.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The speed is zero or less, the entity is gone or not playing yet, or this build has no audio.
    /// </exception>
    public static void SetSpeed(Entity playing, float speed) =>
        Native.Check(Native.bcs_audio_speed(playing.Bits, speed), $"setting {playing}'s speed to {speed}");

    /// <summary>How fast a playing sound plays, one as recorded.</summary>
    /// <exception cref="BevyNativeException">The entity is gone, is not playing yet, or this build has no audio.</exception>
    public static float SpeedOf(Entity playing)
    {
        float speed;
        Native.Check(Native.bcs_audio_playback(playing.Bits, &speed, null), $"reading {playing}'s speed");
        return speed;
    }

    /// <summary>Mutes a playing sound or lets it be heard again, Bevy's <c>AudioSink::mute</c> and <c>unmute</c>.</summary>
    /// <remarks>
    /// Apart from the volume, which muting keeps, so the sound comes back as loud as it was, and a
    /// volume set while it is muted is the one heard once it is not.
    /// </remarks>
    /// <exception cref="BevyNativeException">The entity is gone, is not playing yet, or this build has no audio.</exception>
    public static void SetMuted(Entity playing, bool muted) =>
        Native.Check(Native.bcs_audio_mute(playing.Bits, muted ? 1 : 0), $"{(muted ? "muting" : "unmuting")} {playing}");

    /// <summary>Whether a playing sound is muted.</summary>
    /// <exception cref="BevyNativeException">The entity is gone, is not playing yet, or this build has no audio.</exception>
    public static bool IsMuted(Entity playing)
    {
        int muted;
        Native.Check(Native.bcs_audio_playback(playing.Bits, null, &muted), $"reading whether {playing} is muted");
        return muted != 0;
    }

    /// <summary>
    /// Scales every sound at once, as a settings screen does.
    /// </summary>
    /// <remarks>
    /// Multiplied with each sound's own volume rather than replacing it, so the mix a game set up
    /// survives the master slider being moved. It reaches the sounds already playing as well as
    /// the ones started later.
    /// </remarks>
    /// <param name="volume">1 leaves everything as mixed, 0 is silence.</param>
    /// <exception cref="BevyNativeException">
    /// The volume is negative, or this build has no audio.
    /// </exception>
    public static void SetGlobalVolume(float volume) =>
        Native.Check(Native.bcs_audio_global_volume(volume), "setting the global volume");

    /// <summary>
    /// Whether this run plays its sounds to no device, as a run with no window does unless
    /// <see cref="Config.AudioWithoutWindow"/> asks otherwise. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// A silent run still plays every sound through, on the app's clock, so everything else here
    /// answers as it would with a device, and a sound ends when it would have been heard to end.
    /// A build without audio has nothing to play to, and answers true.
    /// </remarks>
    /// <exception cref="BevyNativeException">Called outside a system.</exception>
    public static bool IsSilent
    {
        get
        {
            int silent;
            Native.Check(Native.bcs_audio_silent(&silent), "asking whether sound reaches a device");
            return silent != 0;
        }
    }
}
