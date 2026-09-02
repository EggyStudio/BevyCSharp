using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>How a sound should be played.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeAudioConfig
{
    /// <summary>0 once, 1 loop, 2 once then despawn.</summary>
    public int Mode;

    /// <summary>Loudness, 1 being as recorded.</summary>
    public float Volume;

    /// <summary>Playback rate, 1 being as recorded.</summary>
    public float Speed;

    /// <summary>Non-zero to start paused.</summary>
    public int Paused;
}
