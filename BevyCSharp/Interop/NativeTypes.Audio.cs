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

    /// <summary>Non-zero to place the sound at its entity's transform.</summary>
    public int Spatial;

    /// <summary>Scale applied to the distance to the listener; 0 for Bevy's own.</summary>
    public float SpatialScale;

    /// <summary>Where in the clip to start, in seconds.</summary>
    public float StartSeconds;

    /// <summary>How much of the clip to play from there, in seconds.</summary>
    public float PlaySeconds;
}
