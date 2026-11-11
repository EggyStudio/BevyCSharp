using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// The bridge's entry points for audio, playback and its control, samples made on this side, the
/// listener and the global volume.
/// </summary>
internal static unsafe partial class NativeAudio
{
    // The bridge every entry point here is imported from, named once in Native.
    private const string Library = Native.Library;

    // Runs the loader's check before this class's first call, as Native's remarks say.
    static NativeAudio() => NativeLoader.Initialize(typeof(NativeAudio).Assembly);

    /// <summary>Plays a sound, returning the entity playing it or 0.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_audio_play(int clip, NativeAudioConfig* config);

    /// <summary>Makes a tone of one frequency for a length of time, returning its key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_create_pitch(float frequency, float seconds);

    /// <summary>Makes a stream of samples the game writes, returning its key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_create_samples(uint rate, uint channels);

    /// <summary>Writes samples after a stream's, returning how many it then holds.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_queue_samples(int key, float* samples, int count);

    /// <summary>How many samples a stream holds that have not been played.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_queued_samples(int key);

    /// <summary>Reads a playing sound's volume and whether it is paused.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_state(ulong entity, float* volume, int* paused);

    /// <summary>Sets a playing sound's volume and pause state.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_control(ulong entity, float volume, int paused);

    /// <summary>Stops a sound and despawns its entity.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_stop(ulong entity);

    /// <summary>Makes an entity the ear, with each ear placed exactly.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_listener_ears(ulong entity, float* left, float* right);

    /// <summary>Makes an entity the ear spatial sound is heard from.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_listener(ulong entity, float gap);

    /// <summary>Writes how far into its clip a sound has played, in seconds.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_position(ulong entity, float* seconds);

    /// <summary>Sets how fast a playing sound plays.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_speed(ulong entity, float speed);

    /// <summary>Mutes a playing sound or lets it be heard again.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_mute(ulong entity, int muted);

    /// <summary>Reads how fast a playing sound plays and whether it is muted.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_playback(ulong entity, float* speed, int* muted);

    /// <summary>Moves playback to a point in the clip.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_seek(ulong entity, float seconds);

    /// <summary>Scales every sound at once.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_global_volume(float volume);

    /// <summary>Writes whether this run plays its sounds to no device.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_audio_silent(int* silent);
}
